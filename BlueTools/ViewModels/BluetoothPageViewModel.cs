using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BlueTools.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using System.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Advertisement;

namespace BlueTools.ViewModels;

/// <summary>
/// ViewModel для страницы Bluetooth устройств
/// </summary>
public partial class BluetoothPageViewModel : ViewModelBase
{
    private readonly ObservableCollection<BluetoothDevice> _newDevices = new();
    private readonly ObservableCollection<BluetoothDevice> _pairedDevices = new();
    private BluetoothLEWatcher? _watcher;

    public ObservableCollection<BluetoothDevice> NewDevices => _newDevices;
    public ObservableCollection<BluetoothDevice> PairedDevices => _pairedDevices;

    [ObservableProperty]
    private BluetoothDevice? _selectedNewDevice;

    [ObservableProperty]
    private BluetoothDevice? _selectedPairedDevice;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusMessage = "Готов к сканированию";

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

        // Загружаем сопряженные устройства при старте
        LoadPairedDevices();
    }

    private void LoadPairedDevices()
    {
        _pairedDevices.Clear();
        
        try
        {
            // Получаем все Bluetooth адаптеры
            var adapters = BluetoothAdapter.GetAdapters();
            
            if (adapters.Count == 0)
            {
                StatusMessage = "Bluetooth адаптер не найден";
                return;
            }

            foreach (var adapter in adapters)
            {
                // Получаем все сопряженные устройства для этого адаптера
                var paired = adapter.GetPairedDevices();
                
                foreach (var device in paired)
                {
                    // Добавляем устройство, если его еще нет в списке
                    if (_pairedDevices.All(d => d.Address != device.BluetoothAddress.ToString()))
                    {
                        _pairedDevices.Add(new BluetoothDevice
                        {
                            Name = string.IsNullOrEmpty(device.Name) ? "Неизвестное устройство" : device.Name,
                            Address = device.BluetoothAddress.ToString(),
                            IsPaired = true,
                            IsConnected = device.ConnectionStatus == BluetoothConnectionStatus.Connected,
                            Rssi = 0 // Для сопряженных устройств RSSI не доступен без подключения
                        });
                    }
                }
            }

            StatusMessage = $"Найдено {_pairedDevices.Count} сопряженных устройств.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки сопряженных устройств: {ex.Message}";
            ErrorMessage = ex.Message;
        }
    }

    private async Task ScanAsync()
    {
        if (IsScanning)
            return;

        IsScanning = true;
        StatusMessage = "Сканирование...";
        _newDevices.Clear();

        try
        {
            // Создаем watcher для BLE устройств
            _watcher = BluetoothLEAdvertisementWatcher.Create();

            _watcher.Received += async (sender, args) =>
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        var address = args.BluetoothAddress.ToString();
                        
                        // Проверяем, не является ли устройство уже сопряженным
                        var isPaired = _pairedDevices.Any(d => d.Address == address);
                        
                        // Добавляем только если НЕ сопряжено и еще не в списке новых
                        if (!isPaired && _newDevices.All(d => d.Address != address))
                        {
                            var name = args.Advertisement.LocalName;
                            if (string.IsNullOrEmpty(name))
                                name = $"Устройство {address.Substring(address.Length - 5)}";

                            _newDevices.Add(new BluetoothDevice
                            {
                                Name = name,
                                Address = address,
                                IsPaired = false,
                                IsConnected = false,
                                Rssi = args.RawSignalStrengthInDBm
                            });
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки обработки отдельных устройств
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);
            };

            _watcher.Stopped += (sender, args) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    IsScanning = false;
                    StatusMessage = $"Сканирование завершено. Найдено {_newDevices.Count} новых устройств.";
                });
            };

            _watcher.Start();
            
            // Сканируем 10 секунд
            await Task.Delay(10000);
            _watcher.Stop();
        }
        catch (Exception ex)
        {
            IsScanning = false;
            StatusMessage = $"Ошибка сканирования: {ex.Message}";
            ErrorMessage = ex.Message;
        }
    }

    private async void ConnectDevice()
    {
        var device = SelectedNewDevice ?? SelectedPairedDevice;
        if (device == null)
        {
            System.Windows.MessageBox.Show("Выберите устройство для подключения.", "Внимание", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            StatusMessage = $"Подключение к {device.Name}...";
            
            // Парсим MAC адрес
            if (!ulong.TryParse(device.Address.Replace(":", ""), 
                System.Globalization.NumberStyles.HexNumber, null, out ulong bluetoothAddress))
            {
                StatusMessage = "Неверный формат адреса устройства";
                return;
            }

            // Попытка подключения через BluetoothLEDevice
            var bluetoothDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);

            if (bluetoothDevice != null)
            {
                device.IsConnected = bluetoothDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;
                device.IsPaired = true;
                
                // Обновляем списки
                if (SelectedNewDevice != null)
                {
                    _newDevices.Remove(device);
                    if (_pairedDevices.All(d => d.Address != device.Address))
                    {
                        _pairedDevices.Add(device);
                    }
                }
                
                StatusMessage = device.IsConnected 
                    ? $"Успешно подключено к {device.Name}" 
                    : $"Не удалось подключиться к {device.Name}";
                    
                bluetoothDevice.Dispose();
            }
            else
            {
                StatusMessage = $"Не удалось подключиться к {device.Name}. Устройство может быть недоступно.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка подключения: {ex.Message}";
            ErrorMessage = ex.Message;
        }
    }

    private async void ForgetDevice()
    {
        var device = SelectedPairedDevice;
        if (device == null)
        {
            System.Windows.MessageBox.Show("Выберите устройство из списка сопряженных, чтобы забыть его.", 
                "Внимание", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            StatusMessage = $"Удаление {device.Name}...";
            
            // Находим устройство в системе для удаления сопряжения
            var selector = $"System.DeviceInterface.Bluetooth.DeviceAddress:=\"{device.Address}\"";
            var devices = await DeviceInformation.FindAllAsync(selector);
            
            if (devices.Count > 0)
            {
                var devInfo = devices[0];
                if (devInfo.Pairing.CanUnpair)
                {
                    var result = await devInfo.Pairing.UnpairAsync();
                    if (result.Status == DeviceUnpairingResultStatus.Unpaired)
                    {
                        _pairedDevices.Remove(device);
                        StatusMessage = $"Устройство {device.Name} удалено.";
                    }
                    else
                    {
                        StatusMessage = $"Не удалось удалить сопряжение: {result.Status}";
                    }
                }
                else
                {
                    StatusMessage = "Устройство не поддерживает отмену сопряжения программно.";
                }
            }
            else
            {
                // Если не нашли через запрос, просто удаляем из списка UI
                _pairedDevices.Remove(device);
                StatusMessage = $"Устройство {device.Name} удалено из списка.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка при удалении: {ex.Message}";
            ErrorMessage = ex.Message;
        }
    }

    private void ShowDetails()
    {
        var device = SelectedNewDevice ?? SelectedPairedDevice;
        if (device == null)
        {
            System.Windows.MessageBox.Show("Выберите устройство для просмотра сведений.", 
                "Внимание", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        string message = $"Имя: {device.Name}\n" +
                         $"Адрес: {device.Address}\n" +
                         $"Сигнал: {device.Rssi} dBm\n" +
                         $"Сопряжено: {(device.IsPaired ? "Да" : "Нет")}\n" +
                         $"Подключено: {(device.IsConnected ? "Да" : "Нет")}";
        
        System.Windows.MessageBox.Show(message, $"Сведения: {device.Name}", 
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
