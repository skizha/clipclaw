using System.IO;
using Clipclaw.Infrastructure;
using Clipclaw.Models;
using Microsoft.Data.Sqlite;

namespace Clipclaw.Services;

/// <summary>
/// Stores all clip items, settings, and shortcut bindings in a single SQLite
/// database file at %LOCALAPPDATA%\Clipclaw\clipclaw.db.
/// All queries use parameterised commands — no string interpolation near SQL.
/// </summary>
internal sealed class SqlitePersistenceService : IPersistenceService
{
    private readonly string _connectionString;

    public SqlitePersistenceService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Clipclaw");

        Directory.CreateDirectory(folder);

        var dbPath = Path.Combine(folder, "clipclaw.db");
        _connectionString = $"Data Source={dbPath}";
    }

    // ── Schema setup ─────────────────────────────────────────────────────────

    public async Task InitialiseAsync()
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await CreateTablesAsync(conn);
        await MigrateAsync(conn);
        await SeedDefaultsAsync(conn);
    }

    // Microsoft.Data.Sqlite executes only the first statement per command,
    // so each DDL statement gets its own ExecuteNonQueryAsync call.
    private static async Task CreateTablesAsync(SqliteConnection conn)
    {
        await ExecuteNonQueryAsync(conn, """
            CREATE TABLE IF NOT EXISTS ClipItems (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Text         TEXT    NOT NULL UNIQUE,
                CopiedAt     TEXT    NOT NULL,
                LastPastedAt TEXT,
                PasteCount   INTEGER NOT NULL DEFAULT 0,
                IsPinned     INTEGER NOT NULL DEFAULT 0,
                DisplayOrder INTEGER NOT NULL DEFAULT 0,
                ShortName    TEXT    NULL
            )
            """);

        await ExecuteNonQueryAsync(conn,
            "CREATE INDEX IF NOT EXISTS idx_clipitems_copiedat   ON ClipItems (CopiedAt DESC)");

        await ExecuteNonQueryAsync(conn,
            "CREATE INDEX IF NOT EXISTS idx_clipitems_pastecount ON ClipItems (PasteCount DESC)");

        await ExecuteNonQueryAsync(conn, """
            CREATE TABLE IF NOT EXISTS AppSettings (
                Id              INTEGER PRIMARY KEY DEFAULT 1,
                MaxHistorySize  INTEGER NOT NULL DEFAULT 50,
                LaunchOnStartup INTEGER NOT NULL DEFAULT 1,
                PersistHistory  INTEGER NOT NULL DEFAULT 1,
                PanelShortcut   TEXT    NOT NULL DEFAULT 'Ctrl+Shift+C',
                Theme           TEXT    NOT NULL DEFAULT 'Dark'
            )
            """);

        await ExecuteNonQueryAsync(conn, """
            CREATE TABLE IF NOT EXISTS ShortcutBindings (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                ActionName  TEXT    NOT NULL UNIQUE,
                Modifiers   TEXT    NOT NULL,
                Key         TEXT    NOT NULL,
                IsEnabled   INTEGER NOT NULL DEFAULT 1
            )
            """);
    }

    private static async Task MigrateAsync(SqliteConnection conn)
    {
        // Additive migrations: ALTER TABLE ADD COLUMN is idempotent via try/catch.
        // Existing databases gain the new columns; fresh databases already have them
        // because CreateTablesAsync includes them above.
        await TryAlterAsync(conn,
            "ALTER TABLE ClipItems ADD COLUMN ShortName TEXT NULL");
        await TryAlterAsync(conn,
            "ALTER TABLE AppSettings ADD COLUMN Theme TEXT NOT NULL DEFAULT 'Dark'");
    }

    private static async Task TryAlterAsync(SqliteConnection conn, string alterSql)
    {
        // SQLite throws when a column already exists; that means the migration
        // already ran on a previous startup — swallow silently.
        try
        {
            await ExecuteNonQueryAsync(conn, alterSql);
        }
        catch (SqliteException) { /* column already exists — migration already applied */ }
    }

    private static async Task SeedDefaultsAsync(SqliteConnection conn)
    {
        // Insert default settings row if absent
        await ExecuteNonQueryAsync(conn,
            "INSERT OR IGNORE INTO AppSettings (Id) VALUES (1);");

        // Insert default shortcut bindings if absent
        foreach (var (action, binding) in HotkeyConstants.DefaultBindings)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "INSERT OR IGNORE INTO ShortcutBindings (ActionName, Modifiers, Key) " +
                "VALUES (@action, @mod, @key);";
            cmd.Parameters.AddWithValue("@action", action);
            cmd.Parameters.AddWithValue("@mod",    binding.Modifiers);
            cmd.Parameters.AddWithValue("@key",    binding.Key);
            await cmd.ExecuteNonQueryAsync();
        }

        // Migration: replace old Win+Shift defaults that conflict with Windows taskbar shortcuts.
        // Only updates rows that still carry the original shipped values — user customisations are preserved.
        foreach (var (action, newBinding) in HotkeyConstants.DefaultBindings)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "UPDATE ShortcutBindings SET Modifiers = @newMod " +
                "WHERE ActionName = @action AND Modifiers = 'Win+Shift' AND Key = @key;";
            cmd.Parameters.AddWithValue("@action", action);
            cmd.Parameters.AddWithValue("@newMod", newBinding.Modifiers);
            cmd.Parameters.AddWithValue("@key",    newBinding.Key);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    // ── Clip items ────────────────────────────────────────────────────────────

    public async Task<List<ClipItem>> GetAllClipItemsAsync()
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT Id, Text, CopiedAt, LastPastedAt, PasteCount, IsPinned, DisplayOrder, ShortName " +
            "FROM ClipItems ORDER BY IsPinned DESC, CopiedAt DESC;";

        var items = new List<ClipItem>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(ReadClipItem(reader));

        return items;
    }

    public async Task UpsertClipItemAsync(ClipItem item)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ClipItems (Text, CopiedAt, LastPastedAt, PasteCount, IsPinned, DisplayOrder, ShortName)
            VALUES (@text, @copiedAt, @lastPastedAt, @pasteCount, @isPinned, @displayOrder, @shortName)
            ON CONFLICT(Text) DO UPDATE SET
                CopiedAt     = excluded.CopiedAt,
                LastPastedAt = excluded.LastPastedAt,
                PasteCount   = excluded.PasteCount,
                IsPinned     = excluded.IsPinned,
                DisplayOrder = excluded.DisplayOrder,
                ShortName    = COALESCE(excluded.ShortName, ShortName);
            """;
        // COALESCE preserves an existing ShortName when a new capture has ShortName = null
        // (e.g., ClipboardService re-capturing the same text externally).
        AddClipItemParams(cmd, item);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteClipItemAsync(int id)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM ClipItems WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ClearNonPinnedAsync()
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await ExecuteNonQueryAsync(conn, "DELETE FROM ClipItems WHERE IsPinned = 0;");
    }

    public async Task IncrementPasteCountAsync(int id)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "UPDATE ClipItems SET PasteCount = PasteCount + 1, LastPastedAt = @now WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task TrimToMaxSizeAsync(int maxSize)
    {
        // Delete oldest non-pinned items that exceed the limit
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            DELETE FROM ClipItems
            WHERE IsPinned = 0
              AND Id NOT IN (
                  SELECT Id FROM ClipItems WHERE IsPinned = 0
                  ORDER BY CopiedAt DESC LIMIT @max
              );
            """;
        cmd.Parameters.AddWithValue("@max", maxSize);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    public async Task<AppSettings> GetSettingsAsync()
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT Id, MaxHistorySize, LaunchOnStartup, PersistHistory, PanelShortcut, Theme " +
            "FROM AppSettings WHERE Id = 1;";

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return ReadAppSettings(reader);

        return new AppSettings();
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO AppSettings (Id, MaxHistorySize, LaunchOnStartup, PersistHistory, PanelShortcut, Theme)
            VALUES (1, @maxSize, @startup, @persist, @shortcut, @theme)
            ON CONFLICT(Id) DO UPDATE SET
                MaxHistorySize  = excluded.MaxHistorySize,
                LaunchOnStartup = excluded.LaunchOnStartup,
                PersistHistory  = excluded.PersistHistory,
                PanelShortcut   = excluded.PanelShortcut,
                Theme           = excluded.Theme;
            """;
        cmd.Parameters.AddWithValue("@maxSize",  settings.MaxHistorySize);
        cmd.Parameters.AddWithValue("@startup",  settings.LaunchOnStartup ? 1 : 0);
        cmd.Parameters.AddWithValue("@persist",  settings.PersistHistory  ? 1 : 0);
        cmd.Parameters.AddWithValue("@shortcut", settings.PanelShortcut);
        cmd.Parameters.AddWithValue("@theme",    settings.Theme.ToString());
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Shortcut bindings ─────────────────────────────────────────────────────

    public async Task<List<ShortcutBinding>> GetShortcutBindingsAsync()
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT Id, ActionName, Modifiers, Key, IsEnabled FROM ShortcutBindings;";

        var bindings = new List<ShortcutBinding>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            bindings.Add(ReadShortcutBinding(reader));

        return bindings;
    }

    public async Task SaveShortcutBindingAsync(ShortcutBinding binding)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ShortcutBindings (ActionName, Modifiers, Key, IsEnabled)
            VALUES (@action, @mod, @key, @enabled)
            ON CONFLICT(ActionName) DO UPDATE SET
                Modifiers = excluded.Modifiers,
                Key       = excluded.Key,
                IsEnabled = excluded.IsEnabled;
            """;
        cmd.Parameters.AddWithValue("@action",  binding.ActionName);
        cmd.Parameters.AddWithValue("@mod",     binding.Modifiers);
        cmd.Parameters.AddWithValue("@key",     binding.Key);
        cmd.Parameters.AddWithValue("@enabled", binding.IsEnabled ? 1 : 0);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task ExecuteNonQueryAsync(SqliteConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    private static void AddClipItemParams(SqliteCommand cmd, ClipItem item)
    {
        cmd.Parameters.AddWithValue("@text",         item.Text);
        cmd.Parameters.AddWithValue("@copiedAt",     item.CopiedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@lastPastedAt", item.LastPastedAt?.ToString("O") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@pasteCount",   item.PasteCount);
        cmd.Parameters.AddWithValue("@isPinned",     item.IsPinned     ? 1 : 0);
        cmd.Parameters.AddWithValue("@displayOrder", item.DisplayOrder);
        cmd.Parameters.AddWithValue("@shortName",    (object?)item.ShortName ?? DBNull.Value);
    }

    private static ClipItem ReadClipItem(SqliteDataReader r) => new()
    {
        Id           = r.GetInt32(0),
        Text         = r.GetString(1),
        CopiedAt     = DateTime.Parse(r.GetString(2)),
        LastPastedAt = r.IsDBNull(3) ? null : DateTime.Parse(r.GetString(3)),
        PasteCount   = r.GetInt32(4),
        IsPinned     = r.GetInt32(5) == 1,
        DisplayOrder = r.GetInt32(6),
        ShortName    = r.IsDBNull(7) ? null : r.GetString(7),
    };

    private static AppSettings ReadAppSettings(SqliteDataReader r) => new()
    {
        Id              = r.GetInt32(0),
        MaxHistorySize  = r.GetInt32(1),
        LaunchOnStartup = r.GetInt32(2) == 1,
        PersistHistory  = r.GetInt32(3) == 1,
        PanelShortcut   = r.GetString(4),
        Theme           = Enum.TryParse<ClipTheme>(r.GetString(5), out var t) ? t : ClipTheme.Dark,
    };

    private static ShortcutBinding ReadShortcutBinding(SqliteDataReader r) => new()
    {
        Id         = r.GetInt32(0),
        ActionName = r.GetString(1),
        Modifiers  = r.GetString(2),
        Key        = r.GetString(3),
        IsEnabled  = r.GetInt32(4) == 1,
    };
}
