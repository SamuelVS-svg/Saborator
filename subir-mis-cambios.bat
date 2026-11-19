@echo off
rem Doble clic: hace el commit de tus cambios locales y los sube a GitHub (rama cambios-locales).
cd /d "%~dp0publicar-windows"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\6-subir-mis-cambios.ps1"
pause
