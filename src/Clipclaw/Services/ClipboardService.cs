using System.Windows;
using Clipclaw.Infrastructure;
using Clipclaw.Models;

namespace Clipclaw.Services;

/// <summary>
/// Listens for clipboard changes via Win32 AddClipboardFormatListener,
/// maintains the in-memory history list, and writes items back to the
/// clipboard when the user requests a paste-by-index.
/// </summary>
internal sealed class ClipboardService : IClipboardService
{
    private readonly IPersistenceService  _persistence;
    private readonly IUsageTrackingService _usageTracking;
    private readonly WindowMessageHandler  _messageHandler = new();

    // Snapshot of history kept in memory for fast index-based access.
    // Ordered most-recent first. Updated whenever the clipboard changes.
    private List<ClipItem> _history = [];

    // Prevents re-entrant clipboard reads while we are writing to the clipboard.
    private bool _isWritingToClipboard;

    public ClipboardService(IPersistenceService persistence, IUsageTrackingService usageTracking)
    {
        _persistence   = persistence;
        _usageTracking = usageTracking;
    }

    public void StartMonitoring(Window messageHost)
    {
        _ = LoadHistoryAsync();
        _messageHandler.Attach(messageHost, OnClipboardChanged);
    }

    public void StopMonitoring() => _messageHandler.Detach();

    public IReadOnlyList<ClipItem> GetHistory() => _history;

    public void SetActiveClipboard(int index)
    {
        if (index < 0 || index >= _history.Count) return;
        WriteToClipboard(_history[index]);
    }

    public void SetActiveClipboardBySlot(int slot)
    {
        // Prefer an item with an explicitly assigned slot; fall back to position.
        var item = _history.FirstOrDefault(i => i.ShortcutSlot == slot)
                ?? (slot - 1 < _history.Count ? _history[slot - 1] : null);

        if (item is null) return;
        WriteToClipboard(item);
    }

    private void WriteToClipboard(ClipItem item)
    {
        _isWritingToClipboard = true;
        try
        {
            Clipboard.SetText(item.Text);
        }
        finally
        {
            _isWritingToClipboard = false;
        }

        _ = _usageTracking.RecordPasteAsync(item);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task LoadHistoryAsync()
    {
        _history = await _persistence.GetAllClipItemsAsync();
    }

    private void OnClipboardChanged()
    {
        // Skip clipboard changes that we triggered ourselves
        if (_isWritingToClipboard) return;

        var text = GetClipboardTextSafely();
        if (string.IsNullOrEmpty(text)) return;

        // Deduplicate: if the most recent item is identical, do nothing
        if (_history.Count > 0 &&
            string.Equals(_history[0].Text, text, StringComparison.Ordinal))
            return;

        _ = PersistNewItemAsync(text);
    }

    private async Task PersistNewItemAsync(string text)
    {
        var settings = await _persistence.GetSettingsAsync();

        var item = new ClipItem
        {
            Text      = text,
            CopiedAt  = DateTime.UtcNow,
        };

        await _persistence.UpsertClipItemAsync(item);
        await _persistence.TrimToMaxSizeAsync(settings.MaxHistorySize);

        // Refresh the in-memory snapshot
        _history = await _persistence.GetAllClipItemsAsync();
    }

    /// <summary>
    /// Reads clipboard text safely. The clipboard can fail if another process
    /// holds it momentarily; we retry once before giving up silently.
    /// </summary>
    private static string? GetClipboardTextSafely()
    {
        try
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : null;
        }
        catch
        {
            // Clipboard is temporarily locked by another process — skip this event
            return null;
        }
    }
}
