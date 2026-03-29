using System.Collections.ObjectModel;
using Clipclaw.Infrastructure;
using Clipclaw.Models;
using Clipclaw.Services;

namespace Clipclaw.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IPersistenceService _persistence;
    private readonly IHotkeyService      _hotkeys;

    private AppSettings _settings = new();
    public AppSettings Settings
    {
        get => _settings;
        private set => SetProperty(ref _settings, value);
    }

    public ObservableCollection<ShortcutBinding> Bindings { get; } = [];

    private string _conflictMessage = string.Empty;
    public string ConflictMessage
    {
        get => _conflictMessage;
        set => SetProperty(ref _conflictMessage, value);
    }

    public RelayCommand SaveSettingsCommand  { get; }
    public RelayCommand<ShortcutBinding> RecordShortcutCommand { get; }

    internal SettingsViewModel(IPersistenceService persistence, IHotkeyService hotkeys)
    {
        _persistence = persistence;
        _hotkeys     = hotkeys;

        SaveSettingsCommand    = new RelayCommand(() => _ = SaveSettingsAsync());
        RecordShortcutCommand  = new RelayCommand<ShortcutBinding>(_ => { /* handled in view */ });

        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        Settings = await _persistence.GetSettingsAsync();

        var bindings = await _persistence.GetShortcutBindingsAsync();
        Bindings.Clear();
        foreach (var b in bindings)
            Bindings.Add(b);
    }

    private async Task SaveSettingsAsync()
    {
        // Clamp MaxHistorySize to valid range before saving
        Settings.MaxHistorySize = Math.Clamp(Settings.MaxHistorySize, 10, 500);
        await _persistence.SaveSettingsAsync(Settings);
        await _persistence.TrimToMaxSizeAsync(Settings.MaxHistorySize);
    }

    /// <summary>
    /// Validates a new shortcut binding and saves it if there are no conflicts.
    /// Returns null on success, or a human-readable conflict message on failure.
    /// </summary>
    public async Task<string?> TryApplyShortcutAsync(ShortcutBinding binding,
        string newModifiers, string newKey)
    {
        // Reject reserved Windows system shortcuts
        if (HotkeyConstants.IsReserved(newModifiers, newKey))
            return $"\"{newModifiers}+{newKey}\" is reserved by Windows and cannot be used.";

        // Reject duplicates among existing bindings
        var duplicate = Bindings.FirstOrDefault(b =>
            b.ActionName != binding.ActionName &&
            b.Modifiers  == newModifiers &&
            b.Key        == newKey &&
            b.IsEnabled);

        if (duplicate is not null)
            return $"\"{newModifiers}+{newKey}\" is already assigned to \"{duplicate.ActionName}\".";

        binding.Modifiers = newModifiers;
        binding.Key       = newKey;

        await _persistence.SaveShortcutBindingAsync(binding);
        _hotkeys.RegisterFromDatabaseAsync();

        ConflictMessage = string.Empty;
        return null;
    }
}
