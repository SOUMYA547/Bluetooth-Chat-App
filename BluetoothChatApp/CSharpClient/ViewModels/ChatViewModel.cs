using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BluetoothChatApp.Services;
using BluetoothChatApp.Models;
using System.Threading.Tasks;

namespace BluetoothChatApp.ViewModels {
  public partial class ChatViewModel : ObservableObject {
    private readonly CryptoService _crypto;
    private readonly DatabaseService _db;
    private dynamic _transport;

    [ObservableProperty] private string outgoing = string.Empty;
    [ObservableProperty] private string chatLog = string.Empty;
    public string Me { get; }
    public string Peer { get; }

    public ChatViewModel(string me, string peer, CryptoService crypto, DatabaseService db, dynamic transport) {
      Me = me; Peer = peer; _crypto = crypto; _db = db; _transport = transport;
      _transport.MessageReceived += (System.Action<string>)OnIncoming;
    }

    private void OnIncoming(string plaintext) {
      ChatLog += $"{Peer}: {plaintext}\n";
    }

    [RelayCommand]
    public async Task Send() {
      if (string.IsNullOrWhiteSpace(Outgoing)) return;
      var cipher = _crypto.Encrypt(Outgoing);
      _db.SaveMessage(new Message{ Sender=Me, Receiver=Peer, Ciphertext=cipher, Plaintext=Outgoing });
      await _transport.SendAsync(Outgoing);
      ChatLog += $"{Me}: {Outgoing}\n";
      Outgoing = string.Empty;
    }
  }
}
