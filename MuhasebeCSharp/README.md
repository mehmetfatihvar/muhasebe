# Muhasebe Sistemi — C# / WPF (.NET 8)

Ticari kullanım için yeniden yazılan profesyonel versiyon.

## Gereksinimler

- Windows 10/11 (64-bit)
- Visual Studio 2022 Community (ücretsiz)
- .NET 8 SDK

## Visual Studio ile Açma

1. `MuhasebeCSharp/MuhasebeSistemi.csproj` dosyasını VS2022 ile açın
2. NuGet paketleri otomatik yüklenir
3. F5 ile çalıştırın

## Build (tek .exe üretmek için)

```
MuhasebeCSharp\build_csharp.bat
```

Çıktı: `publish_output\MuhasebeSistemi.exe`

## Proje Yapısı

```
MuhasebeCSharp/
├── MuhasebeSistemi.csproj
├── App.xaml / App.xaml.cs
├── Models/
│   └── Models.cs              ← Hareket, Kalem, Ozet modelleri
├── Data/
│   ├── Database.cs            ← SQLite tüm işlemler
│   ├── SqliteExtensions.cs    ← Yardımcı extension metodlar
│   └── JsonExtensions.cs      ← JSON yedek yükleme yardımcıları
├── Views/
│   ├── Theme.xaml             ← Karanlık tema (tüm stiller)
│   ├── MainWindow.xaml        ← Ana pencere layout
│   ├── MainWindow.xaml.cs     ← Ana pencere logic
│   └── HareketDuzenleWindow.cs ← Hareket düzenleme dialog
└── Assets/
    └── logo.ico               ← (logo.png'i buraya koyun)
```

## Mevcut Python Yedekten Veri Aktarma

Python sürümünde aldığınız `.json` yedekleri bu sürüme aktarılabilir:
Üst çubukta **"📂 Yedek Yükle"** → yedek dosyasını seçin.

## NuGet Paketleri

- `Microsoft.Data.Sqlite 8.0.0` — SQLite bağlantısı
- `CommunityToolkit.Mvvm 8.3.2` — MVVM yardımcıları
