using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BlueTools.ViewModels;

/// <summary>
/// ViewModel для навигации между страницами
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly Dictionary<string, object> _pages = new();
    
    [ObservableProperty]
    private ViewModelBase? _currentPage;

    [ObservableProperty]
    private string? _currentTitle;

    public IRelayCommand<string> NavigateCommand { get; }

    public MainViewModel()
    {
        NavigateCommand = new RelayCommand<string>(Navigate);
        
        // Регистрируем страницы
        RegisterPage("Bluetooth", "Bluetooth Устройства", typeof(BluetoothPageViewModel));
        
        // Открываем страницу Bluetooth по умолчанию
        Navigate("Bluetooth");
    }

    /// <summary>
    /// Зарегистрировать страницу для навигации
    /// </summary>
    public void RegisterPage(string key, string title, Type viewModelType)
    {
        _pages[key] = new PageRegistration
        {
            Title = title,
            ViewModelType = viewModelType
        };
    }

    private void Navigate(string? pageKey)
    {
        if (string.IsNullOrEmpty(pageKey) || !_pages.ContainsKey(pageKey))
            return;

        var registration = (PageRegistration)_pages[pageKey];
        
        // Создаем экземпляр ViewModel
        var viewModel = (ViewModelBase)Activator.CreateInstance(registration.ViewModelType)!;
        
        CurrentPage = viewModel;
        CurrentTitle = registration.Title;
        
        viewModel.OnNavigatedTo();
    }

    private class PageRegistration
    {
        public string Title { get; set; } = string.Empty;
        public Type ViewModelType { get; set; } = typeof(ViewModelBase);
    }
}
