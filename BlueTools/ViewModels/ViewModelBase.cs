using CommunityToolkit.Mvvm.ComponentModel;

namespace BlueTools.ViewModels;

/// <summary>
/// Базовый класс для всех ViewModel
/// </summary>
public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public virtual void OnNavigatedTo()
    {
        // Вызывается при навигации на страницу
    }

    public virtual void OnNavigatedFrom()
    {
        // Вызывается при уходе со страницы
    }
}
