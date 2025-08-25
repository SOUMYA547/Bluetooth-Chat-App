using System.Diagnostics;
using System.Text.Json;
using System.Collections.Generic;

namespace BluetoothChatApp.Services {
  public class PythonScannerService {
    private readonly string _exePath;
    public PythonScannerService(string exePath) { _exePath = exePath; }
    public record Device(string name, string address, int? rssi);
    public List<Device> Scan(int timeoutSec = 5) {
      var psi = new ProcessStartInfo {
        FileName = _exePath,
        Arguments = $"--timeout {timeoutSec}",
        CreateNoWindow = true,
        UseShellExecute = false,
        RedirectStandardOutput = true
      };
      using var p = Process.Start(psi)!;
      var json = p.StandardOutput.ReadToEnd();
      p.WaitForExit();
      try {
        var doc = JsonDocument.Parse(json);
        var list = new List<Device>();
        foreach (var el in doc.RootElement.GetProperty("devices").EnumerateArray()) {
          list.Add(new Device(
            el.GetProperty("name").GetString() ?? "",
            el.GetProperty("address").GetString() ?? "",
            el.TryGetProperty("rssi", out var r) && r.ValueKind==JsonValueKind.Number ? r.GetInt32() : null
          ));
        }
        return list;
      } catch { return new List<Device>(); }
    }
  }
}
