using Clipclaw.Models;

namespace Clipclaw.Services;

public interface IUsageTrackingService
{
    /// <summary>Raised after a paste count is incremented so the panel can refresh.</summary>
    event EventHandler UsageUpdated;

    Task RecordPasteAsync(ClipItem item);
}
