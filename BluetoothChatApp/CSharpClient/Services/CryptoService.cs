using System.Text;

namespace BluetoothChatApp.Services {
  public class CryptoService {
    private readonly int _offset;
    public CryptoService(int offset = 1) { _offset = offset; }
    public string Encrypt(string text) {
      var sb = new StringBuilder(text.Length);
      foreach (var ch in text) sb.Append((char)(ch + _offset));
      return sb.ToString();
    }
    public string Decrypt(string text) {
      var sb = new StringBuilder(text.Length);
      foreach (var ch in text) sb.Append((char)(ch - _offset));
      return sb.ToString();
    }
  }
}
