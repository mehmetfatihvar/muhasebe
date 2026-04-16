@echo off
setlocal

:: Turkce karakter sorunu olmadan calistir
chcp 65001 > nul 2>&1

echo.
echo ================================================
echo    Muhasebe Sistemi - Windows Build Script
echo ================================================
echo.

:: Python kontrolu
python --version > nul 2>&1
if %errorlevel% neq 0 (
    echo [HATA] Python bulunamadi.
    echo Python indirmek icin: https://python.org
    echo Kurulumda "Add Python to PATH" secenegini isaretleyin.
    pause
    exit /b 1
)

echo [1/4] Kutuphaneler yukleniyor...
python -m pip install PyQt5 pyinstaller pillow --quiet --no-warn-script-location
if %errorlevel% neq 0 (
    echo [HATA] Kutuphane yuklenemedi.
    pause
    exit /b 1
)
echo       Tamam.

echo [2/4] Logo kontrol ediliyor...
if exist "assets\logo.svg" (
    echo       SVG logo bulundu...
    python -m pip install cairosvg --quiet --no-warn-script-location
    python logo_convert.py assets\logo.svg --cikti assets\logo.ico
    if %errorlevel% neq 0 ( echo [HATA] Logo donusturulemedi. & pause & exit /b 1 )
    echo       Logo hazir.
) else if exist "assets\logo.png" (
    echo       PNG logo bulundu...
    python logo_convert.py assets\logo.png --cikti assets\logo.ico
    if %errorlevel% neq 0 ( echo [HATA] Logo donusturulemedi. & pause & exit /b 1 )
    echo       Logo hazir.
) else if exist "assets\logo.jpg" (
    echo       JPG logo bulundu...
    python logo_convert.py assets\logo.jpg --cikti assets\logo.ico
    if %errorlevel% neq 0 ( echo [HATA] Logo donusturulemedi. & pause & exit /b 1 )
    echo       Logo hazir.
) else if exist "assets\logo.ico" (
    echo       Hazir ICO kullaniliyor.
) else (
    echo       Logo bulunamadi, varsayilan simge kullanilacak.
    powershell -Command "(Get-Content muhasebe.spec) -replace \"icon='assets/logo.ico',\", \"icon=None,\" | Set-Content muhasebe.spec"
)

echo [3/4] .exe olusturuluyor, lutfen bekleyin...
python -m PyInstaller muhasebe.spec --noconfirm --clean
if %errorlevel% equ 0 goto basarili

echo [HATA] Build basarisiz oldu.
pause
exit /b 1

:basarili
if not exist "dist\MuhasebeSistemi.exe" (
    echo [HATA] .exe olusturulamadi.
    pause
    exit /b 1
)

echo       dist\MuhasebeSistemi.exe olusturuldu.
echo.
echo [4/4] Tamamlandi!
echo.
echo ================================================
echo   dist\MuhasebeSistemi.exe hazir!
echo.
echo   Inno Setup ile installer.iss dosyasini derleyerek
echo   profesyonel kurulum paketi olusturabilirsiniz.
echo ================================================
echo.
pause
endlocal
