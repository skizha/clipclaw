using System.Windows;
using Clipclaw.Models;

namespace Clipclaw.Services;

public interface IClipboardService
{
    void StartMonitoring(Window messageHost);
    void StopMonitoring();

    IReadOnlyList<ClipItem> GetHistory();

    /// <summary>
    /// Writes the item at <paramref name="index"/> (0-based, most-recent first)
    /// to the Windows clipboard and increments its paste count.
    /// Does nothing if the index is out of range.
    /// </summary>
    void SetActiveClipboard(int index);

    /// <summary>
    /// Writes the item assigned to <paramref name="slot"/> (1–5) to the clipboard.
    /// If no item has that slot explicitly assigned, falls back to the Nth item in
    /// history (0-based: slot 1 → index 0). Does nothing if nothing matches.
    /// </summary>
    void SetActiveClipboardBySlot(int slot);

    /// <summary>
    /// Reloads the in-memory history snapshot from the database.
    /// Must be called after any manual add/edit so hotkey lookups see the latest data.
    /// </summary>
    Task RefreshHistoryAsync();
}
