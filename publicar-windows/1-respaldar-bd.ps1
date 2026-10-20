# Respaldo completo de la base de datos SQL Server de Saborator.
# Ejecútalo ANTES de arrancar una versión nueva: al iniciar, la app puede añadir columnas a la base.
# No borra ni modifica datos: solo crea un archivo .bak.
#
# Uso (PowerShell):  .\1-respaldar-bd.ps1
# Requiere sqlcmd (viene con SQL Server Express / SQL Server Management Studio).

param(
    [string]$Servidor = "localhost\SQLEXPRESS",
    [string]$BaseDatos = "ControlClientesDB",
    # SQL Server escribe el archivo con su propia cuenta de servicio: usa una carpeta a la que tenga acceso.
    [string]$Carpeta = "C:\SaboratorRespaldos"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    Write-Host "No se encontró sqlcmd. Instala 'sqlcmd' o haz el respaldo desde SQL Server Management Studio:" -ForegroundColor Red
    Write-Host "  clic derecho en $BaseDatos > Tareas > Copia de seguridad..." -ForegroundColor Yellow
    exit 1
}

New-Item -ItemType Directory -Force -Path $Carpeta | Out-Null
$archivo = Join-Path $Carpeta ("{0}_{1}.bak" -f $BaseDatos, (Get-Date -Format "yyyyMMdd_HHmmss"))

Write-Host "Respaldando $BaseDatos en $archivo ..."
sqlcmd -S $Servidor -E -b -Q "BACKUP DATABASE [$BaseDatos] TO DISK = N'$archivo' WITH INIT, CHECKSUM"
if ($LASTEXITCODE -ne 0) {
    Write-Host "El respaldo FALLÓ. No arranques la versión nueva hasta resolverlo." -ForegroundColor Red
    exit 1
}

sqlcmd -S $Servidor -E -b -Q "RESTORE VERIFYONLY FROM DISK = N'$archivo'"
if ($LASTEXITCODE -ne 0) {
    Write-Host "El archivo de respaldo no pasó la verificación." -ForegroundColor Red
    exit 1
}

Write-Host "Respaldo creado y verificado: $archivo" -ForegroundColor Green
