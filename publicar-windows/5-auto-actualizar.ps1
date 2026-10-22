# Mantiene Saborator al día: cada minuto revisa GitHub y, si hay cambios nuevos en tu rama, los trae,
# comprueba que compilan, reinicia la app y la deja funcionando. Después solo refrescas el navegador.
#
# Qué hace en cada vuelta:
#   1. git fetch de tu rama.
#   2. Si hay commits nuevos Y no tienes cambios locales Y se pueden traer sin conflicto:
#        - respalda la base de datos (1-respaldar-bd.ps1; si falla, NO actualiza),
#        - detiene la app, trae los cambios (solo avance limpio, nunca rebase ni borrado de tu trabajo),
#        - compila; si NO compila, vuelve a la versión anterior y la deja corriendo,
#        - arranca la app de nuevo.
#   3. Si no hay nada nuevo, no toca nada (y si la app está caída, la levanta).
#
# Uso (PowerShell, desde la carpeta publicar-windows):
#   .\5-auto-actualizar.ps1                  # vigila la rama en la que estás
#   .\5-auto-actualizar.ps1 -SoloVerificar   # solo dice qué haría, sin cambiar nada
#   .\5-auto-actualizar.ps1 -SinRespaldo     # no respalda la base antes de actualizar
#   .\5-auto-actualizar.ps1 -Rama main -Intervalo 120
# Para detenerlo: Ctrl + C (la app sigue corriendo). Lo que hace queda en auto-actualizar.log

param(
    [string]$Rama = "",
    [int]$Intervalo = 60,
    [switch]$SinRespaldo,
    [switch]$SoloVerificar
)

$ErrorActionPreference = "Continue"
$raiz      = Split-Path -Parent $PSScriptRoot
$proyecto  = Join-Path $raiz "ControlClientesRestaurante"
$registro  = Join-Path $PSScriptRoot "auto-actualizar.log"
$respaldo  = Join-Path $PSScriptRoot "1-respaldar-bd.ps1"
$puerto    = 5106
$urlSalud  = "http://127.0.0.1:$puerto/salud"
$script:lanzador  = $null
$script:rechazado = ""     # commit remoto que no compiló: no se reintenta hasta que cambie
$script:ultimo    = ""
$script:gitOk     = $true

function Escribir($texto) {
    $linea = "{0}  {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $texto
    Add-Content -Path $registro -Value $linea
    Write-Host $linea
}

function G {
    $salida = & git -C $raiz @args 2>&1
    $script:gitOk = ($LASTEXITCODE -eq 0)
    return $salida
}

function Texto($x) { return (($x | Out-String).Trim()) }

function App-Responde {
    try { return ((Invoke-WebRequest -Uri $urlSalud -UseBasicParsing -TimeoutSec 5).StatusCode -eq 200) } catch { return $false }
}

function Detener-App {
    if ($script:lanzador -and -not $script:lanzador.HasExited) {
        Stop-Process -Id $script:lanzador.Id -Force -ErrorAction SilentlyContinue
    }
    Get-Process -Name "ControlClientesRestaurante" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    # Si algo más sigue escuchando en el puerto de Saborator (solo si es de .NET), también se cierra
    $conexiones = Get-NetTCPConnection -LocalPort $puerto -State Listen -ErrorAction SilentlyContinue
    foreach ($c in $conexiones) {
        $p = Get-Process -Id $c.OwningProcess -ErrorAction SilentlyContinue
        if ($p -and $p.ProcessName -match "ControlClientesRestaurante|dotnet") {
            Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        }
    }
    Start-Sleep -Seconds 2
}

function Construir {
    Push-Location $proyecto
    try {
        $salida = & dotnet build -nologo -v q -clp:ErrorsOnly 2>&1 | Out-String
        $ok = ($LASTEXITCODE -eq 0)
    } finally { Pop-Location }
    if (-not $ok) { Escribir ("La compilación falló:`n" + $salida.Trim()) }
    return $ok
}

function Iniciar-App {
    $carpeta = $proyecto.Replace("'", "''")
    $orden   = "Set-Location '$carpeta'; dotnet run --no-build"
    $script:lanzador = Start-Process powershell -ArgumentList "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", $orden -WindowStyle Minimized -PassThru
    for ($i = 0; $i -lt 45; $i++) {
        Start-Sleep -Seconds 2
        if (App-Responde) { return $true }
    }
    return $false
}

# Devuelve: sin-conexion | rama-desconocida | al-dia | rechazado | otra-rama | local-adelantado | divergido | cambios-locales | actualizar
function Decidir($rama) {
    G fetch origin $rama --quiet | Out-Null
    if (-not $script:gitOk) {
        # ¿falló por falta de conexión o porque esa rama no existe en GitHub?
        G ls-remote origin | Out-Null
        if ($script:gitOk) { return "rama-desconocida" }
        return "sin-conexion"
    }
    $local  = Texto (G rev-parse HEAD)
    $remoto = Texto (G rev-parse "origin/$rama")
    if (-not $script:gitOk) { return "rama-desconocida" }
    if ($local -eq $remoto) { return "al-dia" }
    if ($remoto -eq $script:rechazado) { return "rechazado" }
    $actual = Texto (G branch --show-current)
    if ($actual -ne $rama) { return "otra-rama" }
    G merge-base --is-ancestor $remoto $local | Out-Null
    if ($script:gitOk) { return "local-adelantado" }
    G merge-base --is-ancestor $local $remoto | Out-Null
    if (-not $script:gitOk) { return "divergido" }
    $sucio = Texto (G status --porcelain --untracked-files=no)
    if ($sucio) { return "cambios-locales" }
    return "actualizar"
}

$mensajes = @{
    "sin-conexion"     = "No pude consultar GitHub (¿sin internet?). Reintento más tarde."
    "rama-desconocida" = "Esa rama no existe en GitHub (usa -Rama para indicar cuál vigilar)."
    "al-dia"           = "Estás al día."
    "rechazado"        = "El último cambio de GitHub no compiló; espero a que se suba uno nuevo."
    "otra-rama"        = "Estás en otra rama distinta a la vigilada; no toco nada."
    "local-adelantado" = "Tienes commits locales que no están en GitHub; no toco nada."
    "divergido"        = "Tu copia y GitHub avanzaron por caminos distintos; hay que resolverlo a mano. No toco nada."
    "cambios-locales"  = "Tienes cambios sin guardar en archivos del proyecto; no actualizo para no pisarlos."
    "actualizar"       = "Hay cambios nuevos en GitHub."
}

function Actualizar($rama) {
    $viejo  = Texto (G rev-parse HEAD)
    $remoto = Texto (G rev-parse "origin/$rama")
    $resumen = Texto (G log --oneline -1 "origin/$rama")
    Escribir "Actualizando a: $resumen"

    if (-not $SinRespaldo -and (Test-Path $respaldo)) {
        & $respaldo
        if ($LASTEXITCODE -ne 0) {
            Escribir "El respaldo de la base falló: NO actualizo (usa -SinRespaldo si quieres omitirlo)."
            $script:rechazado = $remoto
            return
        }
    }

    Detener-App
    G merge --ff-only "origin/$rama" | Out-Null
    if (-not $script:gitOk) {
        Escribir "No pude traer los cambios (se mantiene la versión anterior)."
        Construir | Out-Null; Iniciar-App | Out-Null
        $script:rechazado = $remoto
        return
    }

    if (Construir) {
        if (Iniciar-App) { Escribir "Listo: Saborator actualizado y funcionando. Refresca el navegador (Ctrl + F5)." }
        else { Escribir "Actualizado, pero la app no respondió a tiempo. Revisa la ventana minimizada de Saborator." }
    } else {
        Escribir "Vuelvo a la versión anterior porque la nueva no compila."
        G reset --hard $viejo | Out-Null
        Construir | Out-Null
        Iniciar-App | Out-Null
        $script:rechazado = $remoto
    }
}

# ── Programa principal ────────────────────────────────────────────────────────
$ramaVigilada = $Rama
if (-not $ramaVigilada) { $ramaVigilada = Texto (G branch --show-current) }
if (-not $ramaVigilada) { Write-Host "No pude saber en qué rama estás. Indícala con -Rama." -ForegroundColor Red; exit 1 }

if ($SoloVerificar) {
    $d = Decidir $ramaVigilada
    Write-Host ("Rama: {0}`nDecisión: {1}`n{2}" -f $ramaVigilada, $d, $mensajes[$d])
    exit 0
}

Escribir "Vigilante de actualizaciones iniciado (rama $ramaVigilada, cada $Intervalo s)."
if (-not (App-Responde)) {
    Escribir "La app no está corriendo: la compilo y la arranco."
    if (Construir) { Iniciar-App | Out-Null } else { Escribir "No arranco la app porque no compila." }
}

while ($true) {
    try {
        $d = Decidir $ramaVigilada
        if ($d -eq "actualizar") {
            Actualizar $ramaVigilada
            $script:ultimo = ""
        } else {
            if ($d -ne $script:ultimo -and $d -ne "al-dia") { Escribir $mensajes[$d] }
            $script:ultimo = $d
            if ($d -eq "al-dia" -and -not (App-Responde)) {
                Escribir "La app no responde: la reinicio."
                Detener-App
                if (Construir) { Iniciar-App | Out-Null }
            }
        }
    } catch {
        Escribir ("Error inesperado: " + $_.Exception.Message)
    }
    Start-Sleep -Seconds $Intervalo
}
