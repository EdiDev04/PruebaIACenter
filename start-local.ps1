# ──────────────────────────────────────────────
# start-local.ps1 — Levanta los 3 servicios del Cotizador
#   1. cotizador-core-mock  (Node/Express  → :3001)
#   2. cotizador-backend    (.NET 8        → :5001)
#   3. cotizador-webapp     (Vite/React    → :5173)
#
# Uso: .\start-local.ps1
# Requiere: Node.js 20+, .NET SDK 8, npm 9+
# ──────────────────────────────────────────────
$ErrorActionPreference = "Stop"

$ROOT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Definition
$jobs = @()

function Stop-AllJobs {
    Write-Host ""
    Write-Host "Deteniendo servicios..." -ForegroundColor Yellow
    foreach ($job in $jobs) {
        Stop-Job -Job $job -ErrorAction SilentlyContinue
        Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
    }
    Write-Host "Todos los servicios detenidos." -ForegroundColor Green
}

# ── 1. Core Mock (puerto 3001) ──
Write-Host "[1/3] Instalando dependencias de cotizador-core-mock..." -ForegroundColor Cyan
Set-Location "$ROOT_DIR\cotizador-core-mock"
npm install --silent
Write-Host "[1/3] Iniciando cotizador-core-mock en :3001" -ForegroundColor Green
$jobs += Start-Job -ScriptBlock {
    Set-Location $using:ROOT_DIR\cotizador-core-mock
    npm run dev
}

# ── 2. Backend .NET (puerto 5001) ──
Write-Host "[2/3] Restaurando paquetes del backend..." -ForegroundColor Cyan
Set-Location "$ROOT_DIR\cotizador-backend"
dotnet restore --verbosity quiet
Write-Host "[2/3] Iniciando cotizador-backend en :5001" -ForegroundColor Green
$jobs += Start-Job -ScriptBlock {
    Set-Location $using:ROOT_DIR\cotizador-backend
    dotnet run --project src\Cotizador.API --launch-profile http
}

# ── 3. Frontend Vite (puerto 5173) ──
Write-Host "[3/3] Instalando dependencias de cotizador-webapp..." -ForegroundColor Cyan
Set-Location "$ROOT_DIR\cotizador-webapp"
npm install --silent
Write-Host "[3/3] Iniciando cotizador-webapp en :5173" -ForegroundColor Green
$jobs += Start-Job -ScriptBlock {
    Set-Location $using:ROOT_DIR\cotizador-webapp
    npm run dev
}

Set-Location $ROOT_DIR

Write-Host ""
Write-Host "================================================" -ForegroundColor White
Write-Host "  Core Mock  -> http://localhost:3001" -ForegroundColor White
Write-Host "  Backend    -> http://localhost:5001" -ForegroundColor White
Write-Host "  Frontend   -> http://localhost:5173" -ForegroundColor White
Write-Host "================================================" -ForegroundColor White
Write-Host "  Presiona Ctrl+C para detener todos los servicios" -ForegroundColor Yellow
Write-Host ""

try {
    # Mantener vivo el script y redirigir output de los jobs
    while ($true) {
        foreach ($job in $jobs) {
            $output = Receive-Job -Job $job -ErrorAction SilentlyContinue
            if ($output) { Write-Host $output }
        }

        # Verificar si algún job falló
        $failed = $jobs | Where-Object { $_.State -eq 'Failed' }
        if ($failed) {
            Write-Host "ERROR: Uno o mas servicios fallaron." -ForegroundColor Red
            foreach ($f in $failed) {
                Receive-Job -Job $f | Write-Host
            }
            break
        }

        Start-Sleep -Milliseconds 500
    }
}
finally {
    Stop-AllJobs
}
