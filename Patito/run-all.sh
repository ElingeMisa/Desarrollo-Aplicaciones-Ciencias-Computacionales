#!/usr/bin/env bash
# =============================================================================
#  run-all.sh — Orquestador de toda la suite de verificación del compilador
#  Patito. Ejecuta en orden:
#
#    Fase 1 · Build    — compila Patito.Compiler + Patito.Tests
#    Fase 2 · Tests    — suite de pruebas unitarias (xUnit)
#    Fase 3 · Quads    — demo visual de cuadruplos (QuadruplesDemoTests)
#    Fase 4 · Examples — compila todos los archivos .patito de ejemplo
#
#  Autor: Victor Misael Escalante Alvarado, A01741176
#
#  Uso:
#    ./run-all.sh           # ejecuta las 4 fases
#    ./run-all.sh --no-demo # omite la fase de demo de cuadruplos (más rápido)
# Salida :
#   este script se ejecuta y guarda la salida del run completo en el archivo run-all.log en la carpeta de logs
# =============================================================================

set -euo pipefail

# ── Colores (siempre activos para que el log preserve el formato) ─────────────
BOLD="\033[1m"; CYAN="\033[1;36m"; GREEN="\033[1;32m"; RED="\033[1;31m"
YELLOW="\033[1;33m"; DIM="\033[2m"; RESET="\033[0m"

# ── Rutas ────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPILER_PROJECT="$SCRIPT_DIR/src/Patito.Compiler/Patito.Compiler.csproj"
TESTS_PROJECT="$SCRIPT_DIR/tests/Patito.Tests/Patito.Tests.csproj"

SKIP_DEMO=false
[ "${1:-}" = "--no-demo" ] && SKIP_DEMO=true


# Logs: guarda la salida del script en el directorio de logs con formato YYYYMMDD-HHMMSS-run-all.log
LOG_DIR="$SCRIPT_DIR/logs"
mkdir -p "$LOG_DIR"
TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
LOGFILE="$LOG_DIR/${TIMESTAMP}-run-all.log"

# Redirige toda la salida (stdout y stderr) al logfile, pero también la muestra en pantalla
exec > >(tee -a "$LOGFILE") 2>&1

# ── Helpers ───────────────────────────────────────────────────────────────────
PHASE_ERRORS=()

phase_header() {
  local num="$1" label="$2"
  echo ""
  echo -e "${CYAN}${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${RESET}"
  echo -e "${CYAN}${BOLD}  Fase $num · $label${RESET}"
  echo -e "${CYAN}${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${RESET}"
  echo ""
}

phase_ok()   { echo -e "\n  ${GREEN}${BOLD}✔ Fase $1 completada correctamente${RESET}\n"; }
phase_fail() { echo -e "\n  ${RED}${BOLD}✖ Fase $1 falló${RESET}\n"; PHASE_ERRORS+=("$1"); }

# =============================================================================
#  FASE 1 — BUILD
# =============================================================================
phase_header "1" "Build — Patito.Compiler + Patito.Tests"

BUILD_OK=true

echo -e "${DIM}  Compilando Patito.Compiler (Release)...${RESET}"
dotnet build "$COMPILER_PROJECT" -c Release -v quiet 2>&1 \
  && echo -e "${GREEN}  ✔ Patito.Compiler${RESET}" \
  || { echo -e "${RED}  ✖ Patito.Compiler${RESET}"; BUILD_OK=false; }

echo -e "${DIM}  Compilando Patito.Tests (Debug)...${RESET}"
dotnet build "$TESTS_PROJECT" -c Debug -v quiet 2>&1 \
  && echo -e "${GREEN}  ✔ Patito.Tests${RESET}" \
  || { echo -e "${RED}  ✖ Patito.Tests${RESET}"; BUILD_OK=false; }

if $BUILD_OK; then
  phase_ok "1"
else
  phase_fail "1 (Build)"
  echo -e "${RED}  El build falló. Corriendo 'dotnet build' manualmente para ver detalles.${RESET}"
  exit 1
fi

# =============================================================================
#  FASE 2 — SUITE DE PRUEBAS UNITARIAS
# =============================================================================
phase_header "2" "Tests unitarios — Scanner · Parser · Semántica · CodeGen"

TESTS_EXIT=0
dotnet test "$TESTS_PROJECT" \
  --no-build \
  --filter "FullyQualifiedName!~QuadruplesDemoTests" \
  --verbosity normal 2>&1 || TESTS_EXIT=$?

if [ "$TESTS_EXIT" -eq 0 ]; then
  phase_ok "2"
else
  phase_fail "2 (Tests unitarios)"
fi

# =============================================================================
#  FASE 3 — DEMO DE CUADRUPLOS
# =============================================================================
if $SKIP_DEMO; then
  echo ""
  echo -e "${YELLOW}  [Fase 3 omitida — flag --no-demo]${RESET}"
else
  phase_header "3" "Demo de cuadruplos — QuadruplesDemoTests"

  DEMO_EXIT=0
  dotnet test "$TESTS_PROJECT" \
    --no-build \
    --filter "FullyQualifiedName~QuadruplesDemoTests" \
    --verbosity normal 2>&1 || DEMO_EXIT=$?

  if [ "$DEMO_EXIT" -eq 0 ]; then
    phase_ok "3"
  else
    phase_fail "3 (Demo de cuadruplos)"
  fi
fi

# =============================================================================
#  FASE 4 — COMPILACIÓN DE EJEMPLOS .patito
# =============================================================================
phase_header "4" "Compilación de ejemplos .patito"

EXAMPLES_EXIT=0
bash "$SCRIPT_DIR/compile-examples.sh" --all || EXAMPLES_EXIT=$?

if [ "$EXAMPLES_EXIT" -eq 0 ]; then
  phase_ok "4"
else
  phase_fail "4 (Ejemplos)"
fi

# =============================================================================
#  RESUMEN FINAL
# =============================================================================
echo ""
echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${RESET}"
echo -e "${BOLD}  RESUMEN FINAL${RESET}"
echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${RESET}"
echo ""

TOTAL_PHASES=4
$SKIP_DEMO && TOTAL_PHASES=3

if [ ${#PHASE_ERRORS[@]} -eq 0 ]; then
  echo -e "  ${GREEN}${BOLD}✔ Todas las fases completadas correctamente${RESET}"
else
  echo -e "  ${RED}${BOLD}✖ ${#PHASE_ERRORS[@]} fase(s) con errores:${RESET}"
  for e in "${PHASE_ERRORS[@]}"; do
    echo -e "    ${RED}· Fase $e${RESET}"
  done
fi
echo ""

[ ${#PHASE_ERRORS[@]} -eq 0 ] && exit 0 || exit 1
