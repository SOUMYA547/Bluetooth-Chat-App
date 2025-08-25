using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using BluetoothChatApp.Services;
using BluetoothChatApp.ViewModels;
using Microsoft.Web.WebView2.Core;
using Windows.Devices.Enumeration;

namespace BluetoothChatApp.Views {
  public partial class MainWindow : Window {
    PythonScannerService _scanner;
    DatabaseService _db;
    CryptoService _crypto;
    TransportService _transport;
    RfcommTransportService _rfcomm = new();
    string _meName = "Me";

    public MainWindow() {
      InitializeComponent();
      var cfg = JsonDocument.Parse(File.ReadAllText("appsettings.json")).RootElement;
      var scannerPath = cfg.GetProperty("PythonScannerPath").GetString()!;
      var dbPath = cfg.GetProperty("DbPath").GetString()!;
      var port = cfg.GetProperty("TcpPort").GetInt32();
      var off = cfg.GetProperty("DefaultOffset").GetInt32();

      _scanner = new PythonScannerService(scannerPath);
      _db = new DatabaseService(dbPath);
      var u = _db.LoadUser();
      _meName = u.name;
      _crypto = new CryptoService(off);
      _transport = new TransportService(port);
      _transport.StartServer();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) {
      await _rfcomm.StartServerAsync(); // advertise RFCOMM
      await Web.EnsureCoreWebView2Async();
      // Map local React build (if present) else blank
      string uiPath = System.IO.Path.GetFullPath("UiWeb");
      if (Directory.Exists(uiPath)) {
        Web.CoreWebView2.SetVirtualHostNameToFolderMapping("app.local", uiPath, CoreWebView2HostResourceAccessKind.Allow);
        Web.Source = new Uri("https://app.local/index.html");
      }
      Web.CoreWebView2.WebMessageReceived += (s, a) => {
        var msg = a.TryGetWebMessageAsString();
        try {
          var dto = System.Text.Json.JsonSerializer.Deserialize<dynamic>(msg);
          if ((string)dto["type"] == "send") {
            Dispatcher.Invoke(async () => { await _rfcomm.SendAsync((string)dto["text"]); });
          }
        } catch {}
      };
    }

    private void Scan_Click(object sender, RoutedEventArgs e) {
      var list = _scanner.Scan(5);
      DevicesList.ItemsSource = list; // BLE devices
      DevicesList.DisplayMemberPath = "name";
    }

    private async void Scan_Rfcomm_Click(object sender, RoutedEventArgs e) {
      var peers = await RfcommTransportService.FindPeersAsync(Guid.Parse("8D7C3C4E-3BF8-4F70-9E9A-4E9F5B3E1A11"));
      DevicesList.ItemsSource = peers; // RFCOMM services
      DevicesList.DisplayMemberPath = "Name";
    }

    private void OpenChat_Click(object sender, RoutedEventArgs e) {
      if (DevicesList.SelectedItem is null) { MessageBox.Show("Select a device"); return; }
      if (DevicesList.SelectedItem is DeviceInformation di) {
        OpenRfcommChat(di);
      } else {
        dynamic dev = DevicesList.SelectedItem;
        OpenTcpChat((string)dev.name);
      }
    }

    private async void OpenRfcommChat(DeviceInformation di) {
      bool ok = await _rfcomm.ConnectToPeerAsync(di.Id);
      if (!ok) { MessageBox.Show("Connect failed"); return; }
      var vm = new ChatViewModel(_meName, di.Name, _crypto, _db, new TransportAdapter(_rfcomm));
      var w = new ChatWindow { DataContext = vm };
      _rfcomm.MessageReceived += (txt) => Dispatcher.Invoke(() => {
        var mi = typeof(ChatViewModel).GetMethod("Send", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance);
        // Post to React UI too
        try { Web.CoreWebView2.PostWebMessageAsString(System.Text.Json.JsonSerializer.Serialize(new { type="incoming", payload=txt })); } catch {}
      });
      w.Show();
    }

    private void OpenTcpChat(string peerName) {
      var vm = new ChatViewModel(_meName, peerName, _crypto, _db, _transport);
      var w = new ChatWindow { DataContext = vm };
      w.Show();
    }

    private void Settings_Click(object sender, RoutedEventArgs e) {
      var vm = new SettingsViewModel(_db);
      var w = new SettingsWindow { DataContext = vm };
      w.ShowDialog();
    }
  }
}
