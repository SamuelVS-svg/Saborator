# Arranca Saborator en modo producción, escuchando SOLO en 127.0.0.1 (no accesible desde la red local).
# El acceso público llega únicamente a través de Tailscale Funnel.
# Uso (PowerShell):  .\3-iniciar-saborator.ps1

$ErrorActionPreference = "Stop"
$raiz       = Split-Path -Parent $PSScriptRoot
$publicado  = Join-Path $raiz "publicado"
$ejecutable = Join-Path $publicado "ControlClientesRestaurante.exe"

if (-not (Test-Path $ejecutable)) {
    Write-Host "No existe $ejecutable. Ejecuta primero .\2-publicar.ps1" -ForegroundColor Red
    exit 1
}

$env:ASPNETCORE_ENVIRONMENT = "Production"   # sin modo depuración
$env:SABORATOR_PUERTO       = "5106"         # puerto local (solo 127.0.0.1)
$env:SABORATOR_PROXY_HTTPS  = "true"         # detrás de Tailscale Funnel todo el tráfico público es HTTPS

# Secretos: NO los escribas aquí. Defínelos como variables de entorno de tu usuario de Windows
# (Panel de control > Sistema > Variables de entorno), por ejemplo:
#   WOMPI_PRIVATE_KEY, WOMPI_INTEGRITY_SECRET, WOMPI_EVENT_SECRET,
#   TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN, TWILIO_PHONE_NUMBER

Set-Location $publicado
& $ejecutable
