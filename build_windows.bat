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
if exist "assets\logo.svg" (
    echo       SVG logo bulundu, ICO donusturuluyor...
    pip install cairosvg --quiet --no-warn-script-location
    python logo_convert.py assets\logo.svg --cikti assets\logo.ico
    if errorlevel 1 ( echo [HATA] SVG donusturulemedi. & pause & exit /b 1 )
    echo       Logo hazir.
) else if exist "assets\logo.png" (
    echo       PNG logo bulundu, ICO donusturuluyor...
    python logo_convert.py assets\logo.png --cikti assets\logo.ico
    if errorlevel 1 ( echo [HATA] PNG donusturulemedi. & pause & exit /b 1 )
    echo       Logo hazir.
) else if exist "assets\logo.jpg" (
    echo       JPG logo bulundu, ICO donusturuluyor...
    python logo_convert.py assets\logo.jpg --cikti assets\logo.ico
    if errorlevel 1 ( echo [HATA] JPG donusturulemedi. & pause & exit /b 1 )
    echo       Logo hazir.
) else if exist "assets\logo.ico" (
    echo       Hazir ICO kullaniliyor.
) else (
    echo       [UYARI] assets\ klasorunde logo bulunamadi.
    echo       Logo olmadan devam ediliyor...
    :: spec dosyasindan icon ve assets satırını kaldır
    powershell -Command "(Get-Content muhasebe.spec) -replace \"icon='assets/logo.ico',\", \"icon=None,\" | Set-Content muhasebe.spec"
    powershell -Command "(Get-Content muhasebe.spec) -replace \".*assets/logo.ico.*\n\", '' | Set-Content muhasebe.spec"
)

echo [3/4] .exe olusturuluyor, lutfen bekleyin...
python -m PyInstaller muhasebe.spec --noconfirm --clean
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
