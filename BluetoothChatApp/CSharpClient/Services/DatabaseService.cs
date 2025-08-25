using System.Data.SQLite;
using System.IO;
using BluetoothChatApp.Models;

namespace BluetoothChatApp.Services {
  public class DatabaseService {
    private readonly SQLiteConnection _conn;
    public DatabaseService(string dbPath) {
      bool first = !File.Exists(dbPath);
      _conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
      _conn.Open();
      if (first) Init();
    }
    private void Init() {
      using var cmd1 = new SQLiteCommand("CREATE TABLE IF NOT EXISTS users (id INTEGER PRIMARY KEY, name TEXT, avatar TEXT);", _conn);
      cmd1.ExecuteNonQuery();
      using var cmd2 = new SQLiteCommand("CREATE TABLE IF NOT EXISTS messages (id INTEGER PRIMARY KEY, sender TEXT, receiver TEXT, ciphertext TEXT);", _conn);
      cmd2.ExecuteNonQuery();
    }
    public void SaveUser(string name, string avatar) {
      using var del = new SQLiteCommand("DELETE FROM users;", _conn);
      del.ExecuteNonQuery();
      using var ins = new SQLiteCommand("INSERT INTO users(name,avatar) VALUES (@n,@a);", _conn);
      ins.Parameters.AddWithValue("@n", name);
      ins.Parameters.AddWithValue("@a", avatar);
      ins.ExecuteNonQuery();
    }
    public (string name, string avatar) LoadUser() {
      using var cmd = new SQLiteCommand("SELECT name,avatar FROM users LIMIT 1;", _conn);
      using var r = cmd.ExecuteReader();
      return r.Read() ? (r.GetString(0), r.GetString(1)) : ("Me", "Assets/default-avatar.png");
    }
    public void SaveMessage(Message m) {
      using var cmd = new SQLiteCommand("INSERT INTO messages(sender,receiver,ciphertext) VALUES (@s,@r,@c);", _conn);
      cmd.Parameters.AddWithValue("@s", m.Sender);
      cmd.Parameters.AddWithValue("@r", m.Receiver);
      cmd.Parameters.AddWithValue("@c", m.Ciphertext);
      cmd.ExecuteNonQuery();
    }
  }
}
