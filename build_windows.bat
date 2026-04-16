@echo off
chcp 65001 >nul
echo.
echo ================================================
echo    Muhasebe Sistemi - Windows Build Script
echo ================================================
echo.

:: Python kontrolu
python --version >nul 2>&1
if errorlevel 1 (
    echo [HATA] Python bulunamadi. https://python.org adresinden yukleyin.
    pause & exit /b 1
)

:: Kutuphaneleri yukle
echo [1/4] Kutuphaneler yukleniyor...
pip install PyQt5 pyinstaller pillow --quiet --no-warn-script-location
if errorlevel 1 ( echo [HATA] Kutuphane yuklenemedi. & pause & exit /b 1 )
echo       Tamam.

:: Logo donustur
echo [2/4] Logo kontrol ediliyor...
if exist "assets\logo.png" (
    python logo_convert.py assets\logo.png
    echo       Logo .ico formatina donusturuldu.
) else if exist "assets\logo.ico" (
    echo       Mevcut .ico kullaniliyor.
) else (
    echo       Logo bulunamadi - varsayilan simge kullanilacak.
    powershell -Command "(Get-Content muhasebe.spec) -replace \"icon='assets/logo.ico',\", '' | Set-Content muhasebe.spec"
)

:: PyInstaller yolunu bul ve calistir
echo [3/4] .exe olusturuluyor, lutfen bekleyin...

:: Yontem 1: python -m ile
python -m PyInstaller muhasebe.spec --noconfirm --clean >nul 2>&1
if not errorlevel 1 goto basarili

:: Yontem 2: Scripts klasorunden direkt
for /f "delims=" %%i in ('python -c "import sys,os; print(os.path.join(os.path.dirname(sys.executable), 'Scripts', 'pyinstaller.exe'))"') do set PYINST=%%i
if exist "%PYINST%" (
    "%PYINST%" muhasebe.spec --noconfirm --clean
    if not errorlevel 1 goto basarili
)

:: Yontem 3: AppData Scripts klasoru
for /f "delims=" %%i in ('python -c "import site; print(site.getusersitepackages())"') do set SITE=%%i
set PYINST2=%SITE%\..\..\Scripts\pyinstaller.exe
if exist "%PYINST2%" (
    "%PYINST2%" muhasebe.spec --noconfirm --clean
    if not errorlevel 1 goto basarili
)

echo [HATA] PyInstaller bulunamadi!
echo Lutfen su komutu calistirin ve tekrar deneyin:
echo    pip install pyinstaller --force-reinstall
pause & exit /b 1

:basarili
if not exist "dist\MuhasebeSistemi.exe" (
    echo [HATA] .exe olusturulamadi.
    pause & exit /b 1
)

echo       dist\MuhasebeSistemi.exe olusturuldu.
echo.
echo [4/4] Tamamlandi!
echo.
echo ================================================
echo   dist\MuhasebeSistemi.exe hazir!
echo.
echo   Sonraki adim (opsiyonel):
echo   Inno Setup ile installer.iss dosyasini derle
echo   MuhasebeSistemi_Kurulum_v1.0.exe olusur.
echo ================================================
echo.
pause
