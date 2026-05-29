#!/usr/bin/env bash
# =============================================================================
#  build.sh — Compila el proyecto del compilador y/o el proyecto de pruebas.
#  Autor: Victor Misael Escalante Alvarado, A01741176
#
#  Uso:
#    ./build.sh              # compila ambos proyectos
#    ./build.sh --compiler   # solo el compilador
#    ./build.sh --tests      # solo las pruebas
# Salida -> Guarda la salida del scrip en el direcctorio de logs, con el formato YYYYMMDD-HHMMSS-build.log
# =============================================================================

set -euo pipefail

# ── Colores (siempre activos para que el log preserve el formato) ─────────────
BOLD="\033[1m"; CYAN="\033[1;36m"; GREEN="\033[1;32m"
RED="\033[1;31m"; DIM="\033[2m"; RESET="\033[0m"

# ── Rutas ────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPILER_PROJECT="$SCRIPT_DIR/src/Patito.Compiler/Patito.Compiler.csproj"
TESTS_PROJECT="$SCRIPT_DIR/tests/Patito.Tests/Patito.Tests.csproj"

# Logs: guarda la salida del script en el directorio de logs con formato YYYYMMDD-HHMMSS-build.log
LOG_DIR="$SCRIPT_DIR/logs"
mkdir -p "$LOG_DIR"
TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
LOGFILE="$LOG_DIR/${TIMESTAMP}-build.log"

# Redirige toda la salida (stdout y stderr) al logfile, pero también la muestra en pantalla
exec > >(tee -a "$LOGFILE") 2>&1

MODE="${1:---all}"

# ── Helper ───────────────────────────────────────────────────────────────────
build_project() {
  local proj="$1"
  local label="$2"
  local config="$3"

  echo -e "${CYAN}${BOLD}── Compilando $label ($config)...${RESET}"
  if dotnet build "$proj" -c "$config" -v quiet 2>&1; then
    echo -e "${GREEN}  ✔ $label OK${RESET}\n"
    return 0
  else
    echo -e "${RED}  ✖ Error al compilar $label — ejecuta 'dotnet build $proj' para detalles${RESET}\n"
    return 1
  fi
}

# ── Build ────────────────────────────────────────────────────────────────────
echo -e "\n${BOLD}  PATITO — BUILD${RESET}\n"

ERRORS=0

case "$MODE" in
  --compiler)
    build_project "$COMPILER_PROJECT" "Patito.Compiler" "Release" || ERRORS=$((ERRORS+1))
    ;;
  --tests)
    build_project "$TESTS_PROJECT"    "Patito.Tests"    "Debug"   || ERRORS=$((ERRORS+1))
    ;;
  *)  # --all o cualquier otra cosa
    build_project "$COMPILER_PROJECT" "Patito.Compiler" "Release" || ERRORS=$((ERRORS+1))
    build_project "$TESTS_PROJECT"    "Patito.Tests"    "Debug"   || ERRORS=$((ERRORS+1))
    ;;
esac

# ── Resumen ───────────────────────────────────────────────────────────────────
sep="$(printf '─%.0s' {1..48})"
echo -e "${BOLD}  $sep${RESET}"
if [ "$ERRORS" -eq 0 ]; then
  echo -e "  ${GREEN}${BOLD}✔ Build completado sin errores${RESET}"
else
  echo -e "  ${RED}${BOLD}✖ Build completado con $ERRORS error(es)${RESET}"
fi
echo -e "${BOLD}  $sep${RESET}\n"

[ "$ERRORS" -eq 0 ] && exit 0 || exit 1
