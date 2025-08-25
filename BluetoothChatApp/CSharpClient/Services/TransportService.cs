using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System;

namespace BluetoothChatApp.Services {
  public class TransportService {
    private readonly int _port;
    private TcpListener? _listener;
    public event Action<string>? MessageReceived;
    public TransportService(int port) { _port = port; }
    public void StartServer() {
      _listener = new TcpListener(IPAddress.Loopback, _port);
      _listener.Start();
      _ = AcceptLoop();
    }
    private async Task AcceptLoop() {
      while (true) {
        var client = await _listener!.AcceptTcpClientAsync();
        _ = Handle(client);
      }
    }
    private async Task Handle(TcpClient c) {
      using var stream = c.GetStream();
      using var reader = new StreamReader(stream, Encoding.UTF8);
      var msg = await reader.ReadToEndAsync();
      MessageReceived?.Invoke(msg);
    }
    public async Task SendAsync(string message) {
      using var c = new TcpClient();
      await c.ConnectAsync(IPAddress.Loopback, _port);
      using var stream = c.GetStream();
      var data = Encoding.UTF8.GetBytes(message);
      await stream.WriteAsync(data, 0, data.Length);
    }
  }
}
