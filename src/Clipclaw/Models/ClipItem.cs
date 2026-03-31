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
    /// Optional user-assigned short label. When set, it shows as the primary
    /// row heading in the panel instead of the truncated text preview.
    /// Maximum 60 characters; null means no label has been assigned.
    /// </summary>
    public string? ShortName { get; set; }

    /// <summary>
    /// Returns the first 80 characters of <see cref="Text"/> for display in the panel.
    /// Appends "…" when the text is truncated.
    /// </summary>
    public string DisplayText =>
        Text.Length <= 80 ? Text : string.Concat(Text.AsSpan(0, 80), "…");

    /// <summary>
    /// The primary label shown in the panel row.
    /// Returns <see cref="ShortName"/> when set; otherwise falls back to
    /// the truncated <see cref="DisplayText"/>.
    /// </summary>
    public string DisplayLabel =>
        string.IsNullOrWhiteSpace(ShortName) ? DisplayText : ShortName!;

    /// <summary>True when a short name has been assigned to this item.</summary>
    public bool HasShortName => !string.IsNullOrWhiteSpace(ShortName);

    /// <summary>True when this item qualifies for the "Frequently Used" section.</summary>
    public bool IsFrequent => PasteCount >= 5;
}
