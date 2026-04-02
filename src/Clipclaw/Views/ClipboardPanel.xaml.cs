using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Clipclaw.Models;
using Clipclaw.ViewModels;

namespace Clipclaw.Views;

public partial class ClipboardPanel : Window
{
    // Visible rows used for Page Up/Down jump size calculation
    private const int PageJumpSize = 5;

    /// <summary>
    /// When non-zero, the panel lost activation to a modal dialog or message box we opened;
    /// do not treat that as "click outside" (which would hide the panel).
    /// </summary>
    private int _suppressDeactivateHideDepth;

    private readonly PanelViewModel _viewModel;

    public event EventHandler? PasteRequested;

    public ClipboardPanel(PanelViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.PasteRequested += (_, _) =>
        {
            PasteRequested?.Invoke(this, EventArgs.Empty);
            Hide();
        };

        Deactivated += OnDeactivated;
        PreviewKeyDown += OnPreviewKeyDown;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Closed += (_, _) => _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PanelViewModel.SelectedItem))
            SyncListBoxSelections();
    }

    /// <summary>
    /// Each section uses its own ListBox. OneWay SelectedItem binding does not clear the
    /// previous list's selection when the VM points at an item in another section — sync explicitly.
    /// </summary>
    private void SyncListBoxSelections()
    {
        var sel = _viewModel.SelectedItem;
        SyncOneList(PinnedList,   sel);
        SyncOneList(FrequentList, sel);
        SyncOneList(RecentList,  sel);
    }

    private static void SyncOneList(ListBox list, ClipItem? sel)
    {
        var next = sel is not null && list.Items.Contains(sel) ? sel : null;
        if (!Equals(list.SelectedItem, next))
            list.SelectedItem = next;
    }

    // ── Open / close ──────────────────────────────────────────────────────────

    public async void OpenAndLoadAsync()
    {
        await _viewModel.LoadItemsAsync();
        _viewModel.SearchText = string.Empty;

        PositionAtCenter();
        Show();
        Activate();

        // Focus the list so keyboard navigation is immediately available
        RecentList.Focus();
        BringSelectedItemIntoView();
    }

    private void PositionAtCenter()
    {
        var workArea = SystemParameters.WorkArea;
        // Width is fixed; use MaxHeight for vertical centering (panel grows from top).
        // Sitting slightly above true center feels more natural for a quick-pick UI.
        Left = workArea.Left + (workArea.Width  - Width)   / 2;
        Top  = workArea.Top  + (workArea.Height - MaxHeight) / 2 - 30;
    }

    // ── Keyboard contract (from contracts/keyboard-contract.md) ───────────────

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        // Modifier-qualified shortcuts checked before the plain-key switch
        if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
        {
            TogglePinSelected();
            return;
        }
        if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
        {
            HandleAdd();
            return;
        }
        if ((e.Key == Key.Apps) ||
            (e.Key == Key.F10 && Keyboard.Modifiers == ModifierKeys.Shift))
        {
            ShowContextMenuForSelected();
            return;
        }

        switch (e.Key)
        {
            case Key.Down:     Navigate(_viewModel.SelectNext);                              break;
            case Key.Up:       Navigate(_viewModel.SelectPrevious);                          break;
            case Key.PageDown: Navigate(() => _viewModel.SelectByPageOffset(+PageJumpSize)); break;
            case Key.PageUp:   Navigate(() => _viewModel.SelectByPageOffset(-PageJumpSize)); break;
            case Key.Home:     Navigate(_viewModel.SelectFirst);                             break;
            case Key.End:      Navigate(_viewModel.SelectLast);                              break;
            case Key.Return:   _viewModel.PasteSelectedCommand.Execute(null);                break;
            case Key.Escape:   HandleEscape();                                               break;
            case Key.Delete:   HandleDelete();                                               break;
            case Key.F2:       HandleEdit();                                                 break;
            default:           HandleTypableKey(e);                                          break;
        }
    }

    private void Navigate(Action move)
    {
        move();
        BringSelectedItemIntoView();
    }

    private void HandleEscape()
    {
        if (!string.IsNullOrEmpty(_viewModel.SearchText))
            _viewModel.SearchText = string.Empty; // first Escape clears search
        else
            Hide();                               // second Escape closes panel
    }

    private void HandleDelete()
    {
        if (_viewModel.SelectedItem is not { } item) return;
        ConfirmAndDelete(item);
    }

    private void ConfirmAndDelete(ClipItem item)
    {
        _suppressDeactivateHideDepth++;
        MessageBoxResult result;
        try
        {
            result = MessageBox.Show(
                this,
                "Delete this item?",
                "Clipclaw",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
        }
        finally
        {
            _suppressDeactivateHideDepth--;
        }

        if (result == MessageBoxResult.Yes)
            _viewModel.DeleteCommand.Execute(item);
    }

    private void HandleEdit()
    {
        if (_viewModel.SelectedItem is not { } item) return;

        // Build occupied-slot map excluding the item being edited so its own
        // current slot doesn't appear as "taken by someone else".
        var occupied = OccupiedSlots(excludeItem: item);
        var dialog = new EditClipDialog(item, this, occupied);
        _suppressDeactivateHideDepth++;
        try
        {
            dialog.ShowDialog();
        }
        finally
        {
            _suppressDeactivateHideDepth--;
        }

        if (dialog.Result is { } updated)
            _ = _viewModel.EditItemAsync(item, updated);
    }

    private void HandleAdd()
    {
        var occupied = OccupiedSlots(excludeItem: null);
        var blank    = new ClipItem { CopiedAt = DateTime.UtcNow };
        var dialog   = new EditClipDialog(blank, this, occupied);
        _suppressDeactivateHideDepth++;
        try
        {
            dialog.ShowDialog();
        }
        finally
        {
            _suppressDeactivateHideDepth--;
        }

        if (dialog.Result is { } added)
            _ = _viewModel.AddItemAsync(added);
    }

    private IReadOnlyDictionary<int, string> OccupiedSlots(ClipItem? excludeItem)
        => _viewModel.GetFlatVisibleList()
            .Where(i => i.ShortcutSlot.HasValue && !ReferenceEquals(i, excludeItem))
            .ToDictionary(i => i.ShortcutSlot!.Value, i => i.DisplayLabel);

    private void HandleTypableKey(KeyEventArgs e)
    {
        // Any printable character redirects focus to the search box;
        // let the key propagate so the character appears in the text.
        if (IsTypableCharacter(e.Key) && !SearchBox.IsFocused)
        {
            e.Handled = false;
            SearchBox.Focus();
            SearchBox.CaretIndex = SearchBox.Text.Length;
        }
        else
        {
            e.Handled = false;
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e) => HandleAdd();

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Down arrow in search box → move to list
        if (e.Key == Key.Down)
        {
            _viewModel.SelectNext();
            BringSelectedItemIntoView();
            e.Handled = true;
        }
    }

    // ── List click handling ───────────────────────────────────────────────────

    private void AnyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // When a list row is clicked, sync to the ViewModel. SyncListBoxSelections()
        // clears the other two lists when SelectedItem changes (WPF does not reliably
        // clear stale selection when the VM points at an item in another section).
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is ClipItem clicked)
            _viewModel.SelectedItem = clicked;
    }

    // ── Context menu ──────────────────────────────────────────────────────────

    private void AnyList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Select whichever row was right-clicked before showing the menu
        var listBox = (ListBox)sender;
        var hit = listBox.InputHitTest(e.GetPosition(listBox)) as DependencyObject;
        while (hit is not null and not ListBoxItem)
            hit = VisualTreeHelper.GetParent(hit);

        if (hit is ListBoxItem { DataContext: ClipItem clicked })
            _viewModel.SelectedItem = clicked;

        ShowContextMenuForSelected();
        e.Handled = true;
    }

    private void AnyList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        HandleEdit();
        e.Handled = true;
    }

    private void ShowContextMenuForSelected()
    {
        if (_viewModel.SelectedItem is not { } item) return;

        var menu = new ContextMenu();

        var pinItem = new MenuItem
        {
            Header  = item.IsPinned ? "Unpin" : "Pin",
            Command = item.IsPinned
                ? _viewModel.UnpinCommand
                : _viewModel.PinCommand,
            CommandParameter = item,
        };

        var editItem = new MenuItem { Header = "Edit" };
        editItem.Click += (_, _) => HandleEdit();

        var deleteItem = new MenuItem { Header = "Delete" };
        deleteItem.Click += (_, _) => ConfirmAndDelete(item);

        menu.Items.Add(pinItem);
        menu.Items.Add(editItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(deleteItem);
        menu.IsOpen = true;
    }

    private void TogglePinSelected()
    {
        if (_viewModel.SelectedItem is not { } item) return;

        if (item.IsPinned)
            _viewModel.UnpinCommand.Execute(item);
        else
            _viewModel.PinCommand.Execute(item);
    }

    // ── Scroll helper ─────────────────────────────────────────────────────────

    private void BringSelectedItemIntoView()
    {
        var selected = _viewModel.SelectedItem;
        if (selected is null) return;

        // Try each ListBox until we find the one that contains the selected item
        foreach (var list in new[] { PinnedList, FrequentList, RecentList })
        {
            var container = list.ItemContainerGenerator
                               .ContainerFromItem(selected) as ListBoxItem;
            container?.BringIntoView();
            if (container is not null) break;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void OnDeactivated(object? sender, EventArgs e)
    {
        // Opening a modal Edit/MessageBox moves activation away; don't treat that as leaving the panel.
        if (_suppressDeactivateHideDepth > 0)
            return;
        // Owned modal children (e.g. EditClipDialog) — extra guard while the dialog is visible.
        if (OwnedWindows.Count > 0)
            return;

        // Auto-close when the user clicks outside the panel
        Hide();
    }

    private static bool IsTypableCharacter(Key key)
        => key is >= Key.A and <= Key.Z
        || key is >= Key.D0 and <= Key.D9
        || key is >= Key.NumPad0 and <= Key.NumPad9
        || key is Key.Space or Key.OemPeriod or Key.OemMinus or Key.OemPlus;
}
