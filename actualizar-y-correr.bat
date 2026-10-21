@echo off
rem Doble clic: vigila GitHub y mantiene Saborator actualizado y corriendo.
cd /d "%~dp0publicar-windows"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\5-auto-actualizar.ps1"
pause
