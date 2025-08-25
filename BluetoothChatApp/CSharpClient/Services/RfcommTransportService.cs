using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace BluetoothChatApp.Services {
  public class RfcommTransportService {
    public event Action<string>? MessageReceived;
    private readonly Guid _serviceId = Guid.Parse("8D7C3C4E-3BF8-4F70-9E9A-4E9F5B3E1A11");
    private RfcommServiceProvider? _provider;
    private StreamSocketListener? _listener;
    private StreamSocket? _clientSocket;

    public async Task StartServerAsync() {
      _provider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.FromUuid(_serviceId));
      _listener = new StreamSocketListener();
      _listener.ConnectionReceived += async (s, e) => {
        var reader = new DataReader(e.Socket.InputStream) { InputStreamOptions = InputStreamOptions.Partial, UnicodeEncoding = UnicodeEncoding.Utf8 };
        while (true) {
          uint loaded = await reader.LoadAsync(4);
          if (loaded == 0) break;
          uint len = reader.ReadUInt32();
          await reader.LoadAsync(len);
          string txt = reader.ReadString(len);
          MessageReceived?.Invoke(txt);
        }
      };
      await _listener.BindServiceNameAsync("1");
      _provider.StartAdvertising(_listener, true);
    }

    public async Task StopServerAsync() {
      _provider?.StopAdvertising();
      _listener?.Dispose();
      _provider = null; _listener = null;
    }

    public async Task<bool> ConnectToPeerAsync(string deviceId) {
      var dev = await RfcommDeviceService.FromIdAsync(deviceId);
      if (dev == null) return false;
      _clientSocket?.Dispose();
      _clientSocket = new StreamSocket();
      await _clientSocket.ConnectAsync(dev.ConnectionHostName, dev.ConnectionServiceName);
      return true;
    }

    public async Task SendAsync(string message) {
      if (_clientSocket == null) throw new InvalidOperationException("Not connected");
      var writer = new DataWriter(_clientSocket.OutputStream) { UnicodeEncoding = UnicodeEncoding.Utf8 };
      var bytes = Encoding.UTF8.GetBytes(message);
      writer.WriteUInt32((uint)bytes.Length);
      writer.WriteString(message);
      await writer.StoreAsync();
      await writer.FlushAsync();
    }

    public static async Task<DeviceInformationCollection> FindPeersAsync(Guid serviceId) {
      string selector = RfcommDeviceService.GetDeviceSelector(RfcommServiceId.FromUuid(serviceId));
      return await DeviceInformation.FindAllAsync(selector);
    }
  }
}
