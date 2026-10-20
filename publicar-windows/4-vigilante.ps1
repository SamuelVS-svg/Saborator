# Vigilante: mantiene Saborator en marcha. Cada 30 segundos consulta http://127.0.0.1:5106/salud;
# si la app no responde 2 veces seguidas (o no está corriendo), la reinicia.
# Escribe lo que hace en publicar-windows\vigilante.log
#
# Uso manual (PowerShell):  .\4-vigilante.ps1
# Para que arranque solo al iniciar sesión en Windows, mira LEEME.md (tú registras la tarea).

$raiz       = Split-Path -Parent $PSScriptRoot
$iniciar    = Join-Path $PSScriptRoot "3-iniciar-saborator.ps1"
$registro   = Join-Path $PSScriptRoot "vigilante.log"
$urlSalud   = "http://127.0.0.1:5106/salud"
$fallos     = 0
$proceso    = $null

function Escribir($texto) {
    $linea = "{0}  {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $texto
    Add-Content -Path $registro -Value $linea
    Write-Host $linea
}

function Arrancar {
    Get-Process -Name "ControlClientesRestaurante" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    $script:proceso = Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$iniciar`"" -WindowStyle Minimized -PassThru
    Escribir "Saborator iniciado (PID del lanzador $($script:proceso.Id)). Esperando a que responda..."
    Start-Sleep -Seconds 25
}

Escribir "Vigilante iniciado."
if (-not (Get-Process -Name "ControlClientesRestaurante" -ErrorAction SilentlyContinue)) { Arrancar }

while ($true) {
    try {
        $r = Invoke-WebRequest -Uri $urlSalud -UseBasicParsing -TimeoutSec 10
        if ($r.StatusCode -eq 200) {
            if ($fallos -gt 0) { Escribir "La app volvió a responder." }
            $fallos = 0
        } else {
            $fallos++
            Escribir "Respuesta inesperada ($($r.StatusCode)). Fallo $fallos."
        }
    } catch {
        $fallos++
        Escribir "Sin respuesta de $urlSalud. Fallo $fallos."
    }

    if ($fallos -ge 2) {
        Escribir "Reiniciando Saborator..."
        Arrancar
        $fallos = 0
    }

    Start-Sleep -Seconds 30
}
