@echo off
rem Doble clic: igual que actualizar-y-correr.bat pero para un PC SIN SQL Server (usa la base SQLite integrada).
cd /d "%~dp0publicar-windows"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\5-auto-actualizar.ps1" -ConSqlite
pause
