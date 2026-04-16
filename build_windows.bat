@echo off
chcp 65001 >nul
echo.
echo ================================================
echo    Muhasebe Sistemi - Windows Build Script
echo ================================================
echo.

python --version >nul 2>&1
if errorlevel 1 (
    echo [HATA] Python bulunamadi. https://python.org adresinden yukleyin.
    pause & exit /b 1
)

echo [1/4] Kutuphaneler yukleniyor...
pip install PyQt5 pyinstaller pillow --quiet --no-warn-script-location
if errorlevel 1 ( echo [HATA] Kutuphane yuklenemedi. & pause & exit /b 1 )
echo       Tamam.

echo [2/4] Logo kontrol ediliyor...
if exist "assets\logo.png" (
    python logo_convert.py assets\logo.png
    echo       Tum formatlar olusturuldu.
) else if exist "assets\logo.ico" (
    echo       Mevcut .ico kullaniliyor.
) else (
    echo       Logo bulunamadi - varsayilan simge kullanilacak.
    powershell -Command "(Get-Content muhasebe.spec) -replace \"icon='assets/logo.ico',\", '' | Set-Content muhasebe.spec"
)

echo [3/4] .exe olusturuluyor, lutfen bekleyin...

python -m PyInstaller muhasebe.spec --noconfirm --clean >nul 2>&1
if not errorlevel 1 goto basarili

for /f "delims=" %%i in ('python -c "import sys,os; print(os.path.join(os.path.dirname(sys.executable), 'Scripts', 'pyinstaller.exe'))"') do set PYINST=%%i
if exist "%PYINST%" (
    "%PYINST%" muhasebe.spec --noconfirm --clean
    if not errorlevel 1 goto basarili
)

echo [HATA] PyInstaller bulunamadi!
echo Lutfen: pip install pyinstaller --force-reinstall
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
