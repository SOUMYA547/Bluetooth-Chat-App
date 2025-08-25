using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BluetoothChatApp.Services;

namespace BluetoothChatApp.ViewModels {
  public partial class SettingsViewModel : ObservableObject {
    private readonly DatabaseService _db;
    [ObservableProperty] private string accountName = "Me";
    [ObservableProperty] private string avatarPath = "Assets/default-avatar.png";
    [ObservableProperty] private bool darkMode = false;

    public SettingsViewModel(DatabaseService db) { _db = db; var u = db.LoadUser(); AccountName=u.name; AvatarPath=u.avatar; }
    [RelayCommand] public void Save() { _db.SaveUser(AccountName, AvatarPath); BluetoothChatApp.App.DarkMode = DarkMode; }
  }
}
