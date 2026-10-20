# Publica Saborator en internet desde este equipo con Tailscale Funnel, por fases.
# Se DETIENE en cuanto una fase falla y te dice qué hacer.
#
# Uso (PowerShell, dentro de la carpeta publicar-windows):
#   powershell -ExecutionPolicy Bypass -File .\0-publicar-todo.ps1

$ErrorActionPreference = "Stop"
$puerto = 5106

function Fase($n, $texto) { Write-Host "`n=== Fase ${n}: $texto ===" -ForegroundColor Cyan }
function Ok($texto)       { Write-Host "  OK  $texto" -ForegroundColor Green }
function Falla($texto)    { Write-Host "  FALLÓ  $texto" -ForegroundColor Red; Write-Host "Me detengo aquí. Copia este mensaje y envíaselo a Claude." -ForegroundColor Yellow; exit 1 }

# ── Fase 2a: respaldo ─────────────────────────────────────────────────────────
Fase "2a" "Respaldo de la base de datos"
& (Join-Path $PSScriptRoot "1-respaldar-bd.ps1")
if ($LASTEXITCODE -ne 0) { Falla "El respaldo no se pudo crear." }
Ok "Respaldo creado."

# ── Fase 2b: compilar ─────────────────────────────────────────────────────────
Fase "2b" "Compilar en modo producción"
& (Join-Path $PSScriptRoot "2-publicar.ps1")
if ($LASTEXITCODE -ne 0) { Falla "La compilación falló." }
Ok "Compilado."

# ── Fase 2c: arrancar con el vigilante ────────────────────────────────────────
Fase "2c" "Arrancar Saborator (con vigilante)"
$vigilante = Join-Path $PSScriptRoot "4-vigilante.ps1"
Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$vigilante`"" -WindowStyle Minimized | Out-Null
$salud = $null
for ($i = 0; $i -lt 24 -and -not $salud; $i++) {
    Start-Sleep -Seconds 5
    try { $salud = Invoke-RestMethod "http://127.0.0.1:$puerto/salud" -TimeoutSec 5 } catch { }
}
if (-not $salud -or -not $salud.ok) {
    Falla "La app no respondió en http://127.0.0.1:$puerto/salud en 2 minutos. Revisa la ventana minimizada de Saborator y publicar-windows\vigilante.log (¿SQL Server Express en marcha?)."
}
Ok "Saborator responde en http://127.0.0.1:$puerto (solo este equipo)."

# ── Fase 3: Tailscale ─────────────────────────────────────────────────────────
Fase "3" "Tailscale"
if (-not (Get-Command tailscale -ErrorAction SilentlyContinue)) {
    Falla "Tailscale no está instalado. Instálalo desde https://tailscale.com/download/windows, inicia sesión y vuelve a ejecutar este script."
}
$estado = tailscale status 2>&1 | Out-String
if ($LASTEXITCODE -ne 0 -or $estado -match "Logged out|NeedsLogin|stopped") {
    Falla "Tailscale no tiene sesión iniciada. Ábrelo desde la bandeja del sistema, inicia sesión y vuelve a ejecutar este script.`n$estado"
}
Ok "Tailscale conectado."

# ── Fase 4: publicar ──────────────────────────────────────────────────────────
Fase "4" "Publicar con Funnel"
$salida = tailscale funnel --bg $puerto 2>&1 | Out-String
Write-Host $salida
if ($LASTEXITCODE -ne 0) {
    Falla "Funnel no se pudo activar. Si arriba aparece un enlace, ábrelo, pulsa el botón para habilitar HTTPS/Funnel y vuelve a ejecutar este script. Si pide HTTPS: panel de Tailscale > DNS > HTTPS Certificates > Enable HTTPS."
}
$estadoFunnel = tailscale funnel status 2>&1 | Out-String
Write-Host $estadoFunnel
$url = ([regex]::Match($estadoFunnel, "https://[^\s]+\.ts\.net")).Value
if (-not $url) { Falla "No se encontró la URL pública en 'tailscale funnel status'." }
Ok "Publicado en $url"

# ── Fase 5: probar la URL pública ─────────────────────────────────────────────
Fase "5" "Probar la URL pública"
$publica = $null
for ($i = 0; $i -lt 12 -and -not $publica; $i++) {
    try { $publica = Invoke-RestMethod "$url/salud" -TimeoutSec 10 } catch { Start-Sleep -Seconds 10 }
}
if (-not $publica) { Falla "La URL $url no respondió (puede tardar 1-2 minutos la primera vez: espera y vuelve a ejecutar)." }
if (-not $publica.https) { Falla "La app no detecta HTTPS detrás de Tailscale. Envía este resultado a Claude: $($publica | ConvertTo-Json -Compress)" }
Ok "HTTPS detectado."
if ($publica.host -match "127\.0\.0\.1|localhost") {
    Write-Host "  AVISO  La app ve el host '$($publica.host)': los pagos con Wompi usarían el modo de pruebas. Avísale a Claude." -ForegroundColor Yellow
} else {
    Ok "Host público correcto: $($publica.host)"
}

$cabeceras = (Invoke-WebRequest "$url/Login" -UseBasicParsing -TimeoutSec 15).Headers["Set-Cookie"] -join " | "
if ($cabeceras -match "(?i)secure") { Ok "Las cookies llevan la marca Secure." } else { Falla "Las cookies NO llevan Secure: $cabeceras" }

Write-Host "`n==============================================================" -ForegroundColor Green
Write-Host " URL PÚBLICA: $url" -ForegroundColor Green
Write-Host "==============================================================" -ForegroundColor Green
Write-Host "Falta que TÚ compruebes:"
Write-Host "  1. Iniciar sesión en $url y recorrer Dashboard, Domicilios, Cocina y Caja."
Write-Host "  2. Abrir la URL desde el celular con datos móviles (Wi-Fi apagado)."
Write-Host "  3. Cambiar la contraseña de 'admin' y desactivar las cuentas de ejemplo en Personal."
Write-Host "  4. Registrar la tarea programada (comando en LEEME.md, Fase 6)."
