# Build & Run

## Prerequisites
- Windows 10/11
- Visual Studio 2022 with .NET desktop workload
- Python 3.10+
- Node.js 18+ (for React UI)
- WebView2 Runtime (Evergreen)

## 1) Python Scanner
```
cd PythonScanner
python -m venv .venv
. .venv/Scripts/activate
pip install -r requirements.txt
# Test
python BluetoothScanner.py --timeout 5
# Freeze to exe
pip install pyinstaller
pyinstaller --onefile BluetoothScanner.py
# Copy dist/BluetoothScanner.exe to CSharpClient/ and update appsettings.json if needed
```

## 2) React UI
```
cd ReactFrontend
npm i
npm run build
# Copy ReactFrontend/dist/* to CSharpClient/UiWeb/
```

## 3) C# WPF App
- Open `CSharpClient/BluetoothChatApp.csproj` in Visual Studio
- Restore NuGet packages
- Build & Run

## 4) RFCOMM (Classic Bluetooth) Notes
- Pair devices in Windows
- MSIX packaging is recommended to grant capabilities.
- Use the RFCOMM Scan button, select peer, Open Chat.

## 5) Publish EXE
- Visual Studio → Publish → Folder (self-contained).

## 6) MSIX Installer
- Create a Windows Application Packaging Project and reference the WPF app.
- Add Bluetooth capabilities in the manifest as shown in `Installer/AppxManifest.xml`.
