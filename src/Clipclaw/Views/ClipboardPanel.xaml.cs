using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clipclaw.Models;
using Clipclaw.ViewModels;

namespace Clipclaw.Views;

public partial class ClipboardPanel : Window
{
    // Visible rows used for Page Up/Down jump size calculation
    private const int PageJumpSize = 5;

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
    }

    // ── Open / close ──────────────────────────────────────────────────────────

    public async void OpenAndLoadAsync()
    {
        await _viewModel.LoadItemsAsync();
        _viewModel.SearchText = string.Empty;

        PositionNearTray();
        Show();
        Activate();

        // Focus the list so keyboard navigation is immediately available
        RecentList.Focus();
        BringSelectedItemIntoView();
    }

    private void PositionNearTray()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right  - Width  - 16;
        Top  = workArea.Bottom - ActualHeight - 16;
    }

    // ── Keyboard contract (from contracts/keyboard-contract.md) ───────────────

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                _viewModel.SelectNext();
                BringSelectedItemIntoView();
                e.Handled = true;
                break;

            case Key.Up:
                _viewModel.SelectPrevious();
                BringSelectedItemIntoView();
                e.Handled = true;
                break;

            case Key.PageDown:
                _viewModel.SelectByPageOffset(+PageJumpSize);
                BringSelectedItemIntoView();
                e.Handled = true;
                break;

            case Key.PageUp:
                _viewModel.SelectByPageOffset(-PageJumpSize);
                BringSelectedItemIntoView();
                e.Handled = true;
                break;

            case Key.Home:
                _viewModel.SelectFirst();
                BringSelectedItemIntoView();
                e.Handled = true;
                break;

            case Key.End:
                _viewModel.SelectLast();
                BringSelectedItemIntoView();
                e.Handled = true;
                break;

            case Key.Return:
                _viewModel.PasteSelectedCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Escape:
                if (!string.IsNullOrEmpty(_viewModel.SearchText))
                    _viewModel.SearchText = string.Empty; // First Escape clears search
                else
                    Hide();                               // Second Escape closes panel
                e.Handled = true;
                break;

            case Key.Delete:
                if (_viewModel.SelectedItem is not null)
                    _viewModel.DeleteCommand.Execute(_viewModel.SelectedItem);
                e.Handled = true;
                break;

            default:
                // Any printable character redirects focus to the search box
                if (IsTypableCharacter(e.Key) && !SearchBox.IsFocused)
                {
                    SearchBox.Focus();
                    SearchBox.CaretIndex = SearchBox.Text.Length;
                    // Let the key propagate so the character appears in the search box
                }
                break;
        }
    }

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
        // When a list row is clicked, ensure the ViewModel SelectedItem is updated
        // and the other two lists are cleared so only one item is highlighted.
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is ClipItem clicked)
        {
            _viewModel.SelectedItem = clicked;

            if (sender != PinnedList)   PinnedList.SelectedItem   = null;
            if (sender != FrequentList) FrequentList.SelectedItem = null;
            if (sender != RecentList)   RecentList.SelectedItem   = null;
        }
    }

    // ── Context menu (Application key / Shift+F10) ────────────────────────────

    private void OnPreviewKeyDownForContextMenu(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Apps || (e.Key == Key.F10 && Keyboard.Modifiers == ModifierKeys.Shift))
        {
            ShowContextMenuForSelected();
            e.Handled = true;
        }
        else if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
        {
            TogglePinSelected();
            e.Handled = true;
        }
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

        var deleteItem = new MenuItem
        {
            Header           = "Delete",
            Command          = _viewModel.DeleteCommand,
            CommandParameter = item,
        };

        menu.Items.Add(pinItem);
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
        // Auto-close when the user clicks outside the panel
        Hide();
    }

    private static bool IsTypableCharacter(Key key)
        => key is >= Key.A and <= Key.Z
        || key is >= Key.D0 and <= Key.D9
        || key is >= Key.NumPad0 and <= Key.NumPad9
        || key is Key.Space or Key.OemPeriod or Key.OemMinus or Key.OemPlus;
}
