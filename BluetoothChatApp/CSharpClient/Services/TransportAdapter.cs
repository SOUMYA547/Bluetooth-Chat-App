using System;
using System.Threading.Tasks;

namespace BluetoothChatApp.Services {
  public class TransportAdapter {
    private readonly RfcommTransportService _rf;
    public event Action<string>? MessageReceived {
      add { _rf.MessageReceived += value; }
      remove { _rf.MessageReceived -= value; }
    }
    public TransportAdapter(RfcommTransportService rf){ _rf = rf; }
    public Task SendAsync(string m) => _rf.SendAsync(m);
  }
}
