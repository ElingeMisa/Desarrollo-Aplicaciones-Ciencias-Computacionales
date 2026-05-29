#!/usr/bin/env bash
# =============================================================================
#  compile-example.sh — Compila un programa Patito y muestra el resultado.
#  Autor: Victor Misael Escalante Alvarado, A01741176
#
#  Uso:
#    ./compile-example.sh <archivo.patito>
#    ./compile-example.sh <archivo.patito> --quads   # también muestra cuadruplos
# =============================================================================

set -euo pipefail

# ── Validar argumentos ────────────────────────────────────────────────────────
if [ $# -lt 1 ]; then
  echo "Uso: $0 <archivo.patito> [--quads]"
  exit 1
fi

FILEPATH="$1"
EXTRA_FLAG="${2:-}"

if [ ! -f "$FILEPATH" ]; then
  echo "Error: no se encontró el archivo '$FILEPATH'"
  exit 1
fi

# ── Colores (siempre activos para que el log preserve el formato) ─────────────
BOLD="\033[1m"; CYAN="\033[1;36m"; GREEN="\033[1;32m"; RED="\033[1;31m"
YELLOW="\033[1;33m"; DIM="\033[2m"; RESET="\033[0m"

# ── Rutas ────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPILER_PROJECT="$SCRIPT_DIR/src/Patito.Compiler/Patito.Compiler.csproj"

# ── Logs ─────────────────────────────────────────────────────────────────────
LOG_DIR="$SCRIPT_DIR/logs"
mkdir -p "$LOG_DIR"
LOGFILE="$LOG_DIR/$(date +%Y%m%d-%H%M%S)-compile-example.log"
exec > >(tee -a "$LOGFILE") 2>&1

# ── Build silencioso ─────────────────────────────────────────────────────────
echo -e "${DIM}[build] Compilando el proyecto...${RESET}"
dotnet build "$COMPILER_PROJECT" -c Release -v quiet 2>/dev/null \
  && echo -e "${DIM}[build] OK${RESET}" \
  || { echo -e "${RED}[ERROR] Fallo la compilacion. Ejecuta './build.sh' para detalles.${RESET}"; exit 1; }

echo ""

# ── Encabezado ────────────────────────────────────────────────────────────────
FILENAME="$(basename "$FILEPATH")"
sep="$(printf '═%.0s' {1..60})"
echo -e "${CYAN}${BOLD}"
echo "  $sep"
printf "  %-60s\n" "  COMPILANDO: $FILENAME"
echo "  $sep"
echo -e "${RESET}"

# ── Código fuente ─────────────────────────────────────────────────────────────
echo -e "${YELLOW}${BOLD}  ── Código fuente ──────────────────────────────────────${RESET}"
echo ""
LN=0
while IFS= read -r line; do
  LN=$((LN+1))
  [[ -n "${line// }" ]] && printf "  ${DIM}%3d${RESET}  %s\n" "$LN" "$line"
done < "$FILEPATH"
echo ""

# ── Compilar ──────────────────────────────────────────────────────────────────
echo -e "${YELLOW}${BOLD}  ── Resultado ──────────────────────────────────────────${RESET}"
echo ""

EXIT_CODE=0
OUTPUT=$(dotnet run \
  --project "$COMPILER_PROJECT" \
  -c Release \
  --no-build \
  -- "$FILEPATH" $EXTRA_FLAG 2>&1) || EXIT_CODE=$?

if [ "$EXIT_CODE" -ne 0 ]; then
  echo -e "${RED}  ✖ Compilación fallida${RESET}"
  echo ""
  echo "$OUTPUT" | grep -E "^\[(LEX|PARSE|SEM|FAIL)\]" | while IFS= read -r err; do
    echo -e "  ${RED}$err${RESET}"
  done
  echo ""
  exit 1
else
  # Mostrar output con indentación
  echo "$OUTPUT" | while IFS= read -r line; do
    echo "  $line"
  done
  echo ""
  echo -e "  ${GREEN}${BOLD}✔ Compilación exitosa${RESET}"
  echo ""
fi
