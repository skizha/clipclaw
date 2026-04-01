using System.Collections.ObjectModel;
using Clipclaw.Infrastructure;
using Clipclaw.Models;
using Clipclaw.Services;

namespace Clipclaw.ViewModels;

public sealed class PanelViewModel : ViewModelBase
{
    private readonly IPersistenceService   _persistence;
    private readonly IClipboardService     _clipboard;
    private readonly IUsageTrackingService _usageTracking;

    // Backing lists — full, unfiltered
    private List<ClipItem> _allPinned   = [];
    private List<ClipItem> _allFrequent = [];
    private List<ClipItem> _allRecent   = [];

    public ObservableCollection<ClipItem> PinnedItems   { get; } = [];
    public ObservableCollection<ClipItem> FrequentItems { get; } = [];
    public ObservableCollection<ClipItem> RecentItems   { get; } = [];

    private ClipItem? _selectedItem;
    public ClipItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value)) return;
            ApplyFilter();
        }
    }

    public RelayCommand PasteSelectedCommand    { get; }
    public RelayCommand CloseCommand            { get; }
    public RelayCommand<ClipItem> PinCommand    { get; }
    public RelayCommand<ClipItem> UnpinCommand  { get; }
    public RelayCommand<ClipItem> DeleteCommand { get; }

    public event EventHandler? PasteRequested;

    public PanelViewModel(
        IPersistenceService   persistence,
        IClipboardService     clipboard,
        IUsageTrackingService usageTracking)
    {
        _persistence   = persistence;
        _clipboard     = clipboard;
        _usageTracking = usageTracking;

        PasteSelectedCommand = new RelayCommand(PasteSelected,
            () => SelectedItem is not null);
        CloseCommand  = new RelayCommand(() => PasteRequested?.Invoke(this, EventArgs.Empty));
        PinCommand    = new RelayCommand<ClipItem>(item => { if (item is not null) _ = TogglePinAsync(item, pin: true); });
        UnpinCommand  = new RelayCommand<ClipItem>(item => { if (item is not null) _ = TogglePinAsync(item, pin: false); });
        DeleteCommand = new RelayCommand<ClipItem>(item => { if (item is not null) _ = DeleteItemAsync(item); });

        _usageTracking.UsageUpdated += (_, _) => _ = LoadItemsAsync();
    }

    // ── Loading ───────────────────────────────────────────────────────────────

    public async Task LoadItemsAsync()
    {
        var allItems = await _persistence.GetAllClipItemsAsync();

        _allPinned   = allItems.Where(i => i.IsPinned).ToList();
        _allFrequent = allItems.Where(i => !i.IsPinned && i.IsFrequent)
                               .OrderByDescending(i => i.PasteCount).ToList();
        _allRecent   = allItems.Where(i => !i.IsPinned && !i.IsFrequent)
                               .OrderByDescending(i => i.CopiedAt).ToList();

        ApplyFilter();

        // Default selection: first item in the visible list
        SelectedItem = PinnedItems.FirstOrDefault()
                    ?? FrequentItems.FirstOrDefault()
                    ?? RecentItems.FirstOrDefault();
    }

    // ── Filter ────────────────────────────────────────────────────────────────

    private void ApplyFilter()
    {
        var query = _searchText.Trim();

        RefreshCollection(PinnedItems,   Filter(_allPinned,   query));
        RefreshCollection(FrequentItems, Filter(_allFrequent, query));
        RefreshCollection(RecentItems,   Filter(_allRecent,   query));
    }

    private static IEnumerable<ClipItem> Filter(IEnumerable<ClipItem> source, string query)
        => string.IsNullOrEmpty(query)
            ? source
            : source.Where(i =>
                i.Text.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (i.ShortName != null &&
                 i.ShortName.Contains(query, StringComparison.OrdinalIgnoreCase)));

    private static void RefreshCollection(ObservableCollection<ClipItem> collection,
        IEnumerable<ClipItem> newItems)
    {
        collection.Clear();
        foreach (var item in newItems)
            collection.Add(item);
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    private void PasteSelected()
    {
        if (SelectedItem is null) return;

        var history = _clipboard.GetHistory().ToList();
        var index   = history.IndexOf(SelectedItem);

        // Fall back to text-based lookup if index-list is stale
        if (index < 0)
            index = history.ToList()
                           .FindIndex(i => i.Text == SelectedItem.Text);

        if (index >= 0)
            _clipboard.SetActiveClipboard(index);

        PasteRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task TogglePinAsync(ClipItem item, bool pin)
    {
        item.IsPinned = pin;
        await _persistence.UpsertClipItemAsync(item);
        await LoadItemsAsync();
    }

    private async Task DeleteItemAsync(ClipItem item)
    {
        // Capture the deleted item's position so we can restore focus to the
        // nearest remaining item rather than always jumping back to the top.
        var flat         = GetFlatVisibleList();
        var deletedIndex = flat.IndexOf(item);

        await _persistence.DeleteClipItemAsync(item.Id);
        await LoadItemsAsync();

        if (deletedIndex < 0) return;

        var newFlat = GetFlatVisibleList();
        if (newFlat.Count > 0)
            SelectedItem = newFlat[Math.Min(deletedIndex, newFlat.Count - 1)];
    }

    /// <summary>
    /// Saves an edited clip item: deletes the original (by Id) then upserts the
    /// updated version. This cleanly handles text changes without leaving stale rows.
    /// </summary>
    public async Task EditItemAsync(ClipItem original, ClipItem updated)
    {
        if (updated.ShortcutSlot.HasValue)
            await _persistence.ClearShortcutSlotAsync(updated.ShortcutSlot.Value, original.Id);

        // Delete by Id first so a text change does not create a duplicate row.
        await _persistence.DeleteClipItemAsync(original.Id);
        await _persistence.UpsertClipItemAsync(updated);
        await LoadItemsAsync();

        // Restore selection to the edited item (matched by text after reload)
        var newFlat = GetFlatVisibleList();
        var match = newFlat.FirstOrDefault(i => i.Text == updated.Text);
        if (match is not null) SelectedItem = match;
    }

    /// <summary>
    /// Persists a brand-new clip item entered manually by the user.
    /// </summary>
    public async Task AddItemAsync(ClipItem item)
    {
        if (item.ShortcutSlot.HasValue)
            await _persistence.ClearShortcutSlotAsync(item.ShortcutSlot.Value, excludeId: 0);

        await _persistence.UpsertClipItemAsync(item);
        await LoadItemsAsync();

        var newFlat = GetFlatVisibleList();
        var match = newFlat.FirstOrDefault(i => i.Text == item.Text);
        if (match is not null) SelectedItem = match;
    }

    // ── Keyboard navigation helpers ───────────────────────────────────────────

    /// <summary>
    /// Returns a flat ordered list of all visible items across the three sections,
    /// used by the view's keyboard navigation code.
    /// </summary>
    public List<ClipItem> GetFlatVisibleList()
        => [.. PinnedItems, .. FrequentItems, .. RecentItems];

    public void SelectNext()
    {
        var flat = GetFlatVisibleList();
        if (flat.Count == 0) return;
        var currentIndex = SelectedItem is null ? -1 : flat.IndexOf(SelectedItem);
        SelectedItem = flat[Math.Min(currentIndex + 1, flat.Count - 1)];
    }

    public void SelectPrevious()
    {
        var flat = GetFlatVisibleList();
        if (flat.Count == 0) return;
        var currentIndex = SelectedItem is null ? flat.Count : flat.IndexOf(SelectedItem);
        SelectedItem = flat[Math.Max(currentIndex - 1, 0)];
    }

    public void SelectFirst()
    {
        var flat = GetFlatVisibleList();
        if (flat.Count > 0) SelectedItem = flat[0];
    }

    public void SelectLast()
    {
        var flat = GetFlatVisibleList();
        if (flat.Count > 0) SelectedItem = flat[^1];
    }

    public void SelectByPageOffset(int offset)
    {
        var flat = GetFlatVisibleList();
        if (flat.Count == 0) return;
        var currentIndex = SelectedItem is null ? 0 : flat.IndexOf(SelectedItem);
        var newIndex = Math.Clamp(currentIndex + offset, 0, flat.Count - 1);
        SelectedItem = flat[newIndex];
    }
}

/// <summary>Typed relay command for convenience.</summary>
public sealed class RelayCommand<T> : RelayCommand
{
    public RelayCommand(Action<T?> execute) : base(p => execute((T?)p)) { }
}
