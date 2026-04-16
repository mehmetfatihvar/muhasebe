@echo off
chcp 65001 >nul
echo.
echo ╔══════════════════════════════════════════════╗
echo ║   Muhasebe Sistemi — Windows Build Script   ║
echo ╚══════════════════════════════════════════════╝
echo.

:: ── Gereklilik kontrolü ──────────────────────────────────
python --version >nul 2>&1
if errorlevel 1 (
    echo [HATA] Python bulunamadı. https://python.org adresinden yükleyin.
    pause & exit /b 1
)

:: ── Bağımlılıkları yükle ─────────────────────────────────
echo [1/4] Gerekli kütüphaneler yükleniyor...
pip install PyQt5 pyinstaller pillow --quiet
if errorlevel 1 ( echo [HATA] Kütüphane yüklenemedi. & pause & exit /b 1 )
echo       Tamam.

:: ── Logo dönüştür (varsa) ────────────────────────────────
echo [2/4] Logo kontrol ediliyor...
if exist "assets\logo.png" (
    python logo_convert.py assets\logo.png
    echo       Logo .ico'ya dönüştürüldü.
) else if exist "assets\logo.ico" (
    echo       Mevcut .ico kullanılıyor.
) else (
    echo       Logo bulunamadı — varsayılan simge kullanılacak.
    :: Spec dosyasından icon satırını kaldır
    powershell -Command "(Get-Content muhasebe.spec) -replace \"icon='assets/logo.ico',\", \"\" | Set-Content muhasebe.spec"
)

:: ── PyInstaller ile .exe üret ────────────────────────────
echo [3/4] .exe oluşturuluyor (bu birkaç dakika sürebilir)...
pyinstaller muhasebe.spec --noconfirm --clean
if errorlevel 1 ( echo [HATA] Build başarısız. & pause & exit /b 1 )
echo       dist\MuhasebeSistemi.exe oluşturuldu.

:: ── Sonuç ────────────────────────────────────────────────
echo [4/4] Tamamlandı!
echo.
echo ┌─────────────────────────────────────────────────┐
echo │  ✅ dist\MuhasebeSistemi.exe hazır!             │
echo │                                                  │
echo │  Sonraki adım (opsiyonel):                       │
echo │  Inno Setup ile installer.iss dosyasını derle   │
echo │  → MuhasebeSistemi_Kurulum_v1.0.exe oluşur      │
echo └─────────────────────────────────────────────────┘
echo.
pause
