using System.Windows;
using BlueTools.ViewModels;

namespace BlueTools;

/// <summary>
/// Главное окно приложения с навигацией по страницам
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
