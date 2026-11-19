# Sube tus cambios locales a GitHub haciendo el commit y el push por ti.
# Usa una rama propia (por defecto "cambios-locales") para no tocar main ni la rama de Claude.
#
# Qué hace en cada vuelta:
#   1. Cambia a tu rama (la crea si no existe; tus cambios sin guardar se llevan con ella).
#   2. Prepara solo las carpetas del proyecto (nunca claves, bases de datos, bin/obj ni respaldos).
#   3. Si encuentra algo que parece una clave o un archivo prohibido, NO sube nada y te lo dice.
#   4. Si hay cambios, hace el commit con fecha y hora y lo sube a GitHub.
#
# Uso (PowerShell, desde la carpeta publicar-windows):
#   .\6-subir-mis-cambios.ps1                 # sube una vez
#   .\6-subir-mis-cambios.ps1 -Cada 300       # revisa y sube cada 5 minutos, hasta Ctrl + C
#   .\6-subir-mis-cambios.ps1 -SoloVerificar  # solo muestra qué subiría, sin cambiar nada
#   .\6-subir-mis-cambios.ps1 -Rama mi-home -Mensaje "Ajuste de la portada"
# Lo que hace queda en subir-cambios.log

param(
    [string]$Rama = "cambios-locales",
    [int]$Cada = 0,
    [string]$Mensaje = "",
    [switch]$SoloVerificar
)

$ErrorActionPreference = "Continue"
$raiz     = Split-Path -Parent $PSScriptRoot
$registro = Join-Path $PSScriptRoot "subir-cambios.log"
$script:ok = $true

# Solo se preparan estas rutas del proyecto
$RUTAS = @("ControlClientesRestaurante", "ControlClientesRestaurante.Tests", "publicar-windows", "frontend",
           "android-tracking", "Dockerfile", "railway.json", "actualizar-y-correr.bat", ".gitignore")

# Si alguno de estos patrones aparece entre los archivos a subir, se cancela todo
$PROHIBIDOS = @(
    "appsettings\.Local\.json$", "(^|/)\.env(\.|$)", "\.pfx$", "\.pem$", "\.key$", "credenciales-iniciales",
    "\.db$", "\.db-(wal|shm)$", "\.bak$", "(^|/)(bin|obj|node_modules|publicado)/", "\.log$", "respaldo"
)

function Escribir($texto) {
    $linea = "{0}  {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $texto
    Add-Content -Path $registro -Value $linea
    Write-Host $linea
}

function G {
    $salida = & git -C $raiz @args 2>&1
    $script:ok = ($LASTEXITCODE -eq 0)
    return $salida
}

function Texto($x) { return (($x | Out-String).Trim()) }

function Subir {
    $actual = Texto (G branch --show-current)

    if (-not $SoloVerificar -and $actual -ne $Rama) {
        G rev-parse --verify --quiet "refs/heads/$Rama" | Out-Null
        if ($script:ok) { $r = G checkout $Rama } else { $r = G checkout -b $Rama }
        if (-not $script:ok) { Escribir ("No pude pasar a la rama " + $Rama + ": " + (Texto $r)); return }
        Escribir "Estás ahora en la rama $Rama."
    }

    foreach ($ruta in $RUTAS) {
        # Los .log (incluido el de este script) nunca se preparan
        if (Test-Path (Join-Path $raiz $ruta)) { G add -- $ruta ":(exclude)*.log" | Out-Null }
    }

    $archivos = @((Texto (G diff --cached --name-only)) -split "`n" | Where-Object { $_ -ne "" })
    if ($archivos.Count -eq 0) { if (-not $SoloVerificar) { Escribir "Sin cambios por subir." } else { Write-Host "Sin cambios por subir." }; return }

    $malos = @($archivos | Where-Object { $a = $_; $PROHIBIDOS | Where-Object { $a -match $_ } })
    if ($malos.Count -gt 0) {
        G reset -q | Out-Null
        Escribir ("NO subí nada: hay archivos que no deben ir a GitHub:`n  " + ($malos -join "`n  ") + "`nSácalos de la carpeta o agrégalos a .gitignore y vuelve a intentarlo.")
        return
    }

    if ($SoloVerificar) {
        Write-Host ("Subiría " + $archivos.Count + " archivo(s) a la rama " + $Rama + ":`n  " + ($archivos -join "`n  "))
        G reset -q | Out-Null
        return
    }

    $msg = $Mensaje
    if (-not $msg) { $msg = "Cambios locales " + (Get-Date -Format "yyyy-MM-dd HH:mm") }
    $r = G commit -m $msg
    if (-not $script:ok) { Escribir ("El commit falló: " + (Texto $r)); return }
    Escribir ("Commit hecho con " + $archivos.Count + " archivo(s).")

    $r = G push -u origin $Rama
    if ($script:ok) { Escribir "Subido a GitHub, rama $Rama." }
    else { Escribir ("No pude subir a GitHub (¿sin internet o sin sesión iniciada?): " + (Texto $r)) }
}

if ($Cada -gt 0 -and -not $SoloVerificar) {
    Escribir "Vigilando cambios cada $Cada s en la rama $Rama. Ctrl + C para detener."
    while ($true) {
        try { Subir } catch { Escribir ("Error inesperado: " + $_.Exception.Message) }
        Start-Sleep -Seconds $Cada
    }
} else {
    Subir
}
