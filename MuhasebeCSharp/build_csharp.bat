@echo off
chcp 65001 >nul
echo.
echo ================================================
echo   Muhasebe Sistemi C# - Build Script
echo ================================================
echo.

:: .NET SDK kontrolu
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [HATA] .NET SDK bulunamadi.
    echo https://dotnet.microsoft.com/download/dotnet/8.0 adresinden indirin.
    pause & exit /b 1
)

cd MuhasebeCSharp

echo [1/2] Derleniyor...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ..\publish_output
if errorlevel 1 (
    echo [HATA] Derleme basarisiz.
    pause & exit /b 1
)

echo [2/2] Tamamlandi!
echo.
echo ================================================
echo   publish_output\MuhasebeSistemi.exe hazir!
echo ================================================
echo.
pause
