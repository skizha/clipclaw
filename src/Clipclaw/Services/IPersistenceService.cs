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
    /// <summary>
    /// Removes the shortcut slot assignment from every item that currently holds
    /// <paramref name="slot"/>, except the item identified by <paramref name="excludeId"/>.
    /// Call this before saving an item with a slot to keep slot assignments unique.
    /// </summary>
    Task ClearShortcutSlotAsync(int slot, int excludeId);
    Task IncrementPasteCountAsync(int id);
    Task TrimToMaxSizeAsync(int maxSize);

    // ── Settings ─────────────────────────────────────────────────────────────
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);

    // ── Shortcut bindings ────────────────────────────────────────────────────
    Task<List<ShortcutBinding>> GetShortcutBindingsAsync();
    Task SaveShortcutBindingAsync(ShortcutBinding binding);
}
