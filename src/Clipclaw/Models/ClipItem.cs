namespace Clipclaw.Models;

/// <summary>
/// One captured clipboard entry plus its usage metadata.
/// This is a plain data class — no business logic lives here.
/// </summary>
public sealed class ClipItem
{
    public int       Id           { get; set; }
    public string    Text         { get; set; } = string.Empty;
    public DateTime  CopiedAt     { get; set; }
    public DateTime? LastPastedAt { get; set; }
    public int       PasteCount   { get; set; }
    public bool      IsPinned     { get; set; }
    public int       DisplayOrder { get; set; }

    /// <summary>
    /// Returns the first 80 characters of <see cref="Text"/> for display in the panel.
    /// Appends "…" when the text is truncated.
    /// </summary>
    public string DisplayText =>
        Text.Length <= 80 ? Text : string.Concat(Text.AsSpan(0, 80), "…");

    /// <summary>True when this item qualifies for the "Frequently Used" section.</summary>
    public bool IsFrequent => PasteCount >= 5;
}
