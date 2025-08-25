namespace BluetoothChatApp.Models {
  public class Message {
    public int Id { get; set; }
    public string Sender { get; set; } = "";
    public string Receiver { get; set; } = "";
    public string Ciphertext { get; set; } = "";
    public string Plaintext { get; set; } = "";
  }
}
