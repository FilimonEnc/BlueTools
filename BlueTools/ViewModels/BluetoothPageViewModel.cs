using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BlueTools.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;

namespace BlueTools.ViewModels;

/// <summary>
/// ViewModel для страницы Bluetooth устройств
/// </summary>
public partial class BluetoothPageViewModel : ViewModelBase
{
    private readonly ObservableCollection<BluetoothDevice> _newDevices = new();
    private readonly ObservableCollection<BluetoothDevice> _pairedDevices = new();

    public ObservableCollection<BluetoothDevice> NewDevices => _newDevices;
    public ObservableCollection<BluetoothDevice> PairedDevices => _pairedDevices;

    [ObservableProperty]
    private BluetoothDevice? _selectedNewDevice;

    [ObservableProperty]
    private BluetoothDevice? _selectedPairedDevice;

    [ObservableProperty]
    private bool _isScanning;

    public IRelayCommand ScanCommand { get; }
    public IRelayCommand ConnectCommand { get; }
    public IRelayCommand ForgetCommand { get; }
    public IRelayCommand DetailsCommand { get; }

    public BluetoothPageViewModel()
    {
        ScanCommand = new AsyncRelayCommand(ScanAsync);
        ConnectCommand = new RelayCommand(ConnectDevice);
        ForgetCommand = new RelayCommand(ForgetDevice);
        DetailsCommand = new RelayCommand(ShowDetails);

        // Добавим тестовые данные для демонстрации
        LoadSampleData();
    }

    private void LoadSampleData()
    {
        _newDevices.Add(new BluetoothDevice { Name = "iPhone 13", Address = "AA:BB:CC:DD:EE:01", Rssi = -45 });
        _newDevices.Add(new BluetoothDevice { Name = "AirPods Pro", Address = "AA:BB:CC:DD:EE:02", Rssi = -60 });
        
        _pairedDevices.Add(new BluetoothDevice { Name = "Microsoft Mouse", Address = "AA:BB:CC:DD:EE:03", Rssi = -70, IsConnected = true });
        _pairedDevices.Add(new BluetoothDevice { Name = "Keyboard K380", Address = "AA:BB:CC:DD:EE:04", Rssi = -75, IsConnected = false });
    }

    private async Task ScanAsync()
    {
        if (IsScanning)
            return;

        IsScanning = true;
        _newDevices.Clear();

        try
        {
            // Здесь будет реальная логика сканирования Bluetooth
            await Task.Delay(2000); // Имитация сканирования

            // Добавляем найденные устройства (для демонстрации)
            _newDevices.Add(new BluetoothDevice { Name = "Galaxy Watch", Address = "AA:BB:CC:DD:EE:05", Rssi = -55 });
            _newDevices.Add(new BluetoothDevice { Name = "JBL Speaker", Address = "AA:BB:CC:DD:EE:06", Rssi = -65 });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сканирования: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void ConnectDevice()
    {
        if (SelectedNewDevice == null && SelectedPairedDevice == null)
            return;

        var device = SelectedNewDevice ?? SelectedPairedDevice;
        if (device == null)
            return;

        // Логика подключения
        device.IsConnected = !device.IsConnected;
        
        if (device.IsConnected && !_pairedDevices.Contains(device))
        {
            _newDevices.Remove(device);
            _pairedDevices.Add(device);
        }
    }

    private void ForgetDevice()
    {
        var device = SelectedNewDevice ?? SelectedPairedDevice;
        if (device == null)
            return;

        // Логика удаления устройства
        _pairedDevices.Remove(device);
        _newDevices.Remove(device);
    }

    private void ShowDetails()
    {
        var device = SelectedNewDevice ?? SelectedPairedDevice;
        if (device == null)
            return;

        // Логика отображения информации об устройстве
        System.Windows.MessageBox.Show(
            $"Устройство: {device.Name}\n" +
            $"Адрес: {device.Address}\n" +
            $"Сигнал: {device.Rssi} dBm\n" +
            $"Подключено: {(device.IsConnected ? "Да" : "Нет")}",
            "Информация об устройстве");
    }
}
