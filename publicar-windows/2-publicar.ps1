# Compila Saborator en modo Release (producción) dentro de la carpeta "publicado".
# Uso (PowerShell, desde esta carpeta):  .\2-publicar.ps1

$ErrorActionPreference = "Stop"
$raiz      = Split-Path -Parent $PSScriptRoot
$proyecto  = Join-Path $raiz "ControlClientesRestaurante\ControlClientesRestaurante.csproj"
$destino   = Join-Path $raiz "publicado"

if (Get-Process -Name "ControlClientesRestaurante" -ErrorAction SilentlyContinue) {
    Write-Host "Saborator está en ejecución. Deténlo antes de publicar (cierra su ventana o el vigilante)." -ForegroundColor Red
    exit 1
}

dotnet publish $proyecto -c Release -o $destino
if ($LASTEXITCODE -ne 0) {
    Write-Host "La compilación FALLÓ." -ForegroundColor Red
    exit 1
}

Write-Host "Listo: $destino" -ForegroundColor Green
