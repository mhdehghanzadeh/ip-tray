@echo off
echo Building IPTray...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /optimize /out:IPTray.exe Program.cs
if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] IPTray.exe built successfully!
) else (
    echo [ERROR] Build failed.
)
pause
