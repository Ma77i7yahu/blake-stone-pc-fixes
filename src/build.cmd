@echo off
rem Builds BlakeStoneMods.exe (this folder) with the modded bstone.exe embedded.
rem Usage: build.cmd <path to the modded bstone.exe built by build_release.sh>
cd /d "%~dp0"
set EXE=%~1
if "%EXE%"=="" (echo usage: build.cmd path-to-bstone.exe & exit /b 1)
if not exist "%EXE%" (echo bstone.exe not found: %EXE% & exit /b 1)
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /optimize /out:BlakeStoneMods.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll "/resource:%EXE%,bstone.exe" Installer.cs || exit /b 1
echo built BlakeStoneMods.exe
