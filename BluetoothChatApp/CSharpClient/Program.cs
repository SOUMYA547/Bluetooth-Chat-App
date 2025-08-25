using System;
using System.Windows;

namespace BluetoothChatApp {
  public class Program {
    [STAThread]
    public static void Main(string[] args) {
      var app = new App();
      app.InitializeComponent();
      app.Run();
    }
  }
}
