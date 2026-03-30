using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Clipclaw.ViewModels;

/// <summary>
/// Base class for all view models. Provides a concise <see cref="SetProperty{T}"/>
/// helper so derived classes never write boilerplate change-notification code.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets <paramref name="field"/> to <paramref name="value"/> and raises
    /// <see cref="PropertyChanged"/> when the value actually changes.
    /// Returns true if the value changed.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
