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
}
