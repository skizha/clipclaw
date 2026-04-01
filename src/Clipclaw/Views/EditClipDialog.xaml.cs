using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clipclaw.Models;

namespace Clipclaw.Views;

public partial class EditClipDialog : Window
{
    private readonly ClipItem _original;

    /// <summary>
    /// The edited item on Save, or null when the user cancels.
    /// Callers should check this after <see cref="ShowDialog"/> returns.
    /// </summary>
    public ClipItem? Result { get; private set; }

    public EditClipDialog(ClipItem item, Window owner)
    {
        InitializeComponent();
        Owner     = owner;
        _original = item;

        // Adjust title based on whether this is a new or existing item
        Title = item.Id == 0 ? "Add Clip" : "Edit Clip";

        // Pre-fill fields
        ShortNameBox.Text = item.ShortName ?? string.Empty;
        TextBox.Text      = item.Text;

        // Pre-select the slot combo
        SelectSlot(item.ShortcutSlot);

        // Focus the text field for new items (user needs to type content first);
        // focus the short-name field for existing items (name is the quick tweak).
        Loaded += (_, _) =>
        {
            if (item.Id == 0)
            {
                TextBox.Focus();
            }
            else
            {
                ShortNameBox.Focus();
                ShortNameBox.SelectAll();
            }
        };

        PreviewKeyDown += OnDialogPreviewKeyDown;
    }

    // ── Keyboard handlers ──────────────────────────────────────────────────────

    private void OnDialogPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Cancel();
            e.Handled = true;
        }
    }

    private void ShortNameBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Enter in the short name field saves (not a multiline field)
        if (e.Key == Key.Return)
        {
            Save();
            e.Handled = true;
        }
    }

    private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Enter alone adds a newline; Ctrl+Enter saves.
        if (e.Key == Key.Return && Keyboard.Modifiers == ModifierKeys.Control)
        {
            Save();
            e.Handled = true;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e) => Save();

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Cancel();

    // ── Actions ───────────────────────────────────────────────────────────────

    private void Save()
    {
        var newText = TextBox.Text.Trim();
        if (string.IsNullOrEmpty(newText))
        {
            MessageBox.Show("Clip text cannot be empty.", "Clipclaw",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TextBox.Focus();
            return;
        }

        var newShortName = ShortNameBox.Text.Trim();
        var slot         = SelectedSlot();

        // Build the updated item, preserving Id and metadata from the original
        Result = new ClipItem
        {
            Id           = _original.Id,
            Text         = newText,
            CopiedAt     = _original.CopiedAt == default ? DateTime.UtcNow : _original.CopiedAt,
            LastPastedAt = _original.LastPastedAt,
            PasteCount   = _original.PasteCount,
            IsPinned     = _original.IsPinned,
            DisplayOrder = _original.DisplayOrder,
            ShortName    = string.IsNullOrWhiteSpace(newShortName) ? null : newShortName,
            ShortcutSlot = slot,
        };

        DialogResult = true;
        Close();
    }

    private void Cancel()
    {
        Result       = null;
        DialogResult = false;
        Close();
    }

    // ── Slot helpers ──────────────────────────────────────────────────────────

    private void SelectSlot(int? slot)
    {
        // ComboBoxItem index: 0=None, 1=slot1, 2=slot2, 3=slot3, 4=slot4, 5=slot5
        SlotComboBox.SelectedIndex = slot.HasValue ? slot.Value : 0;
    }

    private int? SelectedSlot()
    {
        if (SlotComboBox.SelectedItem is not ComboBoxItem { Tag: string tag }) return null;
        return int.TryParse(tag, out var slot) ? slot : null;
    }
}
