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
    /// Writes the item that has <paramref name="slot"/> (1–5) assigned in Edit to the clipboard.
    /// Does nothing if no item uses that slot.
    /// </summary>
    void SetActiveClipboardBySlot(int slot);

    /// <summary>
    /// Reloads the in-memory history snapshot from the database.
    /// Must be called after any manual add/edit so hotkey lookups see the latest data.
    /// </summary>
    Task RefreshHistoryAsync();
}
