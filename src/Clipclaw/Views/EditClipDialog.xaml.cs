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

    /// <param name="item">Item to edit (Id == 0 for new items).</param>
    /// <param name="owner">Parent window for centering.</param>
    /// <param name="occupiedSlots">
    /// Maps slot number (1–5) → the label of the item that currently holds it,
    /// excluding <paramref name="item"/> itself. Used to warn the user when they
    /// pick a slot that is already in use by another item.
    /// </param>
    public EditClipDialog(ClipItem item, Window owner,
        IReadOnlyDictionary<int, string> occupiedSlots)
    {
        InitializeComponent();
        Owner     = owner;
        _original = item;

        Title = item.Id == 0 ? "Add Clip" : "Edit Clip";

        ShortNameBox.Text = item.ShortName ?? string.Empty;
        TextBox.Text      = item.Text;

        BuildSlotItems(occupiedSlots);
        SelectSlot(item.ShortcutSlot);

        Loaded += (_, _) =>
        {
            if (item.Id == 0) TextBox.Focus();
            else { ShortNameBox.Focus(); ShortNameBox.SelectAll(); }
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

    private void BuildSlotItems(IReadOnlyDictionary<int, string> occupiedSlots)
    {
        // Rebuild combo items so occupied slots show who holds them.
        // Tag stays null for None and an int for slots 1–5.
        SlotComboBox.Items.Clear();
        SlotComboBox.Items.Add(new ComboBoxItem { Content = "None", Tag = null });

        for (var n = 1; n <= 5; n++)
        {
            string label = occupiedSlots.TryGetValue(n, out var holder)
                ? $"Ctrl+Shift+{n}  —  taken by \"{TruncateLabel(holder)}\""
                : $"Ctrl+Shift+{n}";

            SlotComboBox.Items.Add(new ComboBoxItem { Content = label, Tag = n });
        }
    }

    private void SelectSlot(int? slot)
    {
        // Index 0 = None, index N = slot N
        SlotComboBox.SelectedIndex = slot ?? 0;
    }

    private int? SelectedSlot()
    {
        if (SlotComboBox.SelectedItem is ComboBoxItem { Tag: int slot }) return slot;
        return null;
    }

    private static string TruncateLabel(string label)
        => label.Length <= 20 ? label : string.Concat(label.AsSpan(0, 20), "…");
}
