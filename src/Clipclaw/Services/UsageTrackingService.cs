using Clipclaw.Models;

namespace Clipclaw.Services;

/// <summary>
/// Persists paste-count increments and notifies listeners so the panel
/// can refresh its "Frequently Used" section without reopening.
/// </summary>
internal sealed class UsageTrackingService : IUsageTrackingService
{
    private readonly IPersistenceService _persistence;

    public event EventHandler? UsageUpdated;

    public UsageTrackingService(IPersistenceService persistence)
    {
        _persistence = persistence;
    }

    public async Task RecordPasteAsync(ClipItem item)
    {
        await _persistence.IncrementPasteCountAsync(item.Id);
        UsageUpdated?.Invoke(this, EventArgs.Empty);
    }
}
