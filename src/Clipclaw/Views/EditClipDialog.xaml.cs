using System.Windows;
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
        Owner    = owner;
        _original = item;

        // Pre-fill fields with existing values
        ShortNameBox.Text = item.ShortName ?? string.Empty;
        TextBox.Text      = item.Text;

        // Start focus in the short name field for quick labelling
        Loaded += (_, _) =>
        {
            ShortNameBox.Focus();
            ShortNameBox.SelectAll();
        };

        PreviewKeyDown += OnDialogPreviewKeyDown;
    }

    // ── Keyboard handlers ─────────────────────────────────────────────────────

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
        // Enter alone adds a newline in the text field (multiline).
        // Ctrl+Enter saves from the text field.
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

        // Build the updated item, preserving Id and metadata from the original
        Result = new ClipItem
        {
            Id           = _original.Id,
            Text         = newText,
            CopiedAt     = _original.CopiedAt,
            LastPastedAt = _original.LastPastedAt,
            PasteCount   = _original.PasteCount,
            IsPinned     = _original.IsPinned,
            DisplayOrder = _original.DisplayOrder,
            ShortName    = string.IsNullOrWhiteSpace(newShortName) ? null : newShortName,
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
}
