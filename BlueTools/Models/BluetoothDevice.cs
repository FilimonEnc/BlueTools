namespace BlueTools.Models;

/// <summary>
/// Модель Bluetooth устройства
/// </summary>
public class BluetoothDevice
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Rssi { get; set; }
    public bool IsConnected { get; set; }
    public System.DateTime LastSeen { get; set; } = System.DateTime.Now;
}
