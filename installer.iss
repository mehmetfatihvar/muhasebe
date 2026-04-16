; Inno Setup Script — Windows Installer (.exe kurulum dosyası)
; Kurulum için: https://jrsoftware.org/isinfo.php adresinden Inno Setup indirin
; Bu dosyayı Inno Setup ile açıp "Build > Compile" yapın

[Setup]
AppName=Muhasebe Sistemi
AppVersion=1.0.0
AppPublisher=MFV
AppPublisherURL=https://github.com/mehmetfatihvar/muhasebe
AppSupportURL=https://github.com/mehmetfatihvar/muhasebe
AppUpdatesURL=https://github.com/mehmetfatihvar/muhasebe
DefaultDirName={autopf}\MuhasebeSistemi
DefaultGroupName=Muhasebe Sistemi
AllowNoIcons=yes
; Installer çıktısı
OutputDir=installer_output
OutputBaseFilename=MuhasebeSistemi_Kurulum_v1.0
SetupIconFile=assets\logo.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; Windows 7 ve üzeri
MinVersion=6.1
; 64-bit
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon";    Description: "Masaüstüne kısayol oluştur";    GroupDescription: "Ek görevler:"; Flags: unchecked
Name: "startupicon";   Description: "Başlangıçta otomatik başlat";    GroupDescription: "Ek görevler:"; Flags: unchecked

[Files]
; PyInstaller'ın ürettiği exe
Source: "dist\MuhasebeSistemi.exe"; DestDir: "{app}"; Flags: ignoreversion

; Varsa ek assets
Source: "assets\*"; DestDir: "{app}\assets"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Başlat menüsü
Name: "{group}\Muhasebe Sistemi";         Filename: "{app}\MuhasebeSistemi.exe"
Name: "{group}\Kaldır";                   Filename: "{uninstallexe}"

; Masaüstü (seçildiyse)
Name: "{autodesktop}\Muhasebe Sistemi";   Filename: "{app}\MuhasebeSistemi.exe"; Tasks: desktopicon

[Run]
; Kurulum bittikten sonra çalıştır
Filename: "{app}\MuhasebeSistemi.exe"; Description: "Muhasebe Sistemini şimdi başlat"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Kaldırırken veritabanını SİLME — kullanıcının verileri korunsun
; Sadece log vs. geçici dosyaları sil
Type: filesandordirs; Name: "{app}\__pycache__"
