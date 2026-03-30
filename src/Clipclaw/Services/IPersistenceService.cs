using Clipclaw.Models;

namespace Clipclaw.Services;

public interface IPersistenceService
{
    Task InitialiseAsync();

    // ── Clip items ───────────────────────────────────────────────────────────
    Task<List<ClipItem>> GetAllClipItemsAsync();
    Task UpsertClipItemAsync(ClipItem item);
    Task DeleteClipItemAsync(int id);
    Task ClearNonPinnedAsync();
    Task IncrementPasteCountAsync(int id);
    Task TrimToMaxSizeAsync(int maxSize);

    // ── Settings ─────────────────────────────────────────────────────────────
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);

    // ── Shortcut bindings ────────────────────────────────────────────────────
    Task<List<ShortcutBinding>> GetShortcutBindingsAsync();
    Task SaveShortcutBindingAsync(ShortcutBinding binding);
}
