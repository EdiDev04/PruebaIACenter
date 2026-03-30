#!/usr/bin/env bash
# ──────────────────────────────────────────────
# start-local.sh — Levanta los 3 servicios del Cotizador
#   1. cotizador-core-mock  (Node/Express  → :3001)
#   2. cotizador-backend    (.NET 8        → :5001)
#   3. cotizador-webapp     (Vite/React    → :5173)
# ──────────────────────────────────────────────
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
PIDS=()

cleanup() {
  echo ""
  echo "⏹  Deteniendo servicios..."
  for pid in "${PIDS[@]}"; do
    kill "$pid" 2>/dev/null || true
  done
  wait 2>/dev/null
  echo "✅ Todos los servicios detenidos."
}
trap cleanup EXIT INT TERM

# ── Colores ──
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m'

# ── 1. Core Mock (puerto 3001) ──
echo -e "${CYAN}[1/3]${NC} Instalando dependencias de cotizador-core-mock..."
cd "$ROOT_DIR/cotizador-core-mock"
npm install --silent
echo -e "${GREEN}[1/3]${NC} Iniciando cotizador-core-mock en :3001"
npm run dev &
PIDS+=($!)

# ── 2. Backend .NET (puerto 5001) ──
echo -e "${CYAN}[2/3]${NC} Restaurando paquetes del backend..."
cd "$ROOT_DIR/cotizador-backend"
dotnet restore --verbosity quiet
echo -e "${GREEN}[2/3]${NC} Iniciando cotizador-backend en :5001"
dotnet run --project src/Cotizador.API --launch-profile http &
PIDS+=($!)

# ── 3. Frontend Vite (puerto 5173) ──
echo -e "${CYAN}[3/3]${NC} Instalando dependencias de cotizador-webapp..."
cd "$ROOT_DIR/cotizador-webapp"
npm install --silent
echo -e "${GREEN}[3/3]${NC} Iniciando cotizador-webapp en :5173"
npm run dev &
PIDS+=($!)

echo ""
echo "════════════════════════════════════════════════"
echo "  Core Mock  → http://localhost:3001"
echo "  Backend    → http://localhost:5001"
echo "  Frontend   → http://localhost:5173"
echo "════════════════════════════════════════════════"
echo "  Presiona Ctrl+C para detener todos los servicios"
echo ""

wait
