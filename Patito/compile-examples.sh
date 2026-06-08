#!/usr/bin/env bash
# =============================================================================
#  compile-examples.sh — Compila todos los programas de ejemplo con el
#  compilador de Patito y verifica los resultados esperados.
#  Autor: Victor Misael Escalante Alvarado, A01741176
#
#  Convencion:
#    archivos validos   → deben compilar sin error   (resultado esperado: OK)
#    archivos invalido_ → deben producir al menos un error (resultado: FAIL)
#
#  Uso:
#    ./compile-examples.sh           # solo ejemplos validos
#    ./compile-examples.sh --all     # validos + invalidos
# =============================================================================

set -euo pipefail

#  Colores (siempre activos para que el log preserve el formato) 
BOLD="\033[1m"; CYAN="\033[1;36m"; GREEN="\033[1;32m"; RED="\033[1;31m"
YELLOW="\033[1;33m"; DIM="\033[2m"; RESET="\033[0m"

#  Rutas 
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPILER_PROJECT="$SCRIPT_DIR/src/Patito.Compiler/Patito.Compiler.csproj"
EXAMPLES_DIR="$SCRIPT_DIR/examples"

#  Logs 
LOG_DIR="$SCRIPT_DIR/logs"
mkdir -p "$LOG_DIR"
LOGFILE="$LOG_DIR/$(date +%Y%m%d-%H%M%S)-compile-examples.log"
exec > >(tee -a "$LOGFILE") 2>&1

MODE="${1:---valid}"

#  Build silencioso 
echo -e "${DIM}[build] Compilando el proyecto...${RESET}"
dotnet build "$COMPILER_PROJECT" -c Release -v quiet 2>/dev/null \
  && echo -e "${DIM}[build] OK${RESET}\n" \
  || { echo -e "${RED}[ERROR] Fallo la compilacion.${RESET}"; exit 1; }

#  Encabezado de tabla 
echo -e "${BOLD}  PATITO — COMPILACIÓN DE EJEMPLOS${RESET}\n"
printf "  ${DIM}%-3s  %-42s  %-8s  %-8s  %s${RESET}\n" \
  "#" "Archivo" "Esperado" "Obtenido" "Resultado"
printf "  %s\n" "$(printf '%.0s' {1..75})"

#  Recopilar archivos 
FILES=()
if [ "$MODE" = "--all" ]; then
  while IFS= read -r f; do FILES+=("$f"); done \
    < <(find "$EXAMPLES_DIR" -name "*.patito" | sort)
else
  while IFS= read -r f; do FILES+=("$f"); done \
    < <(find "$EXAMPLES_DIR" -name "*.patito" ! -name "invalido_*" | sort)
fi

#  Procesar cada archivo 
IDX=0
PASS=0
FAIL=0
UNEXPECTED=0

for f in "${FILES[@]}"; do
  IDX=$((IDX+1))
  base="$(basename "$f")"

  # Resultado esperado según nombre del archivo
  if [[ "$base" == invalido_* ]]; then
    EXPECTED="FAIL"
  else
    EXPECTED="OK"
  fi

  # Compilar
  EXIT_CODE=0
  OUTPUT=$(dotnet run \
    --project "$COMPILER_PROJECT" \
    -c Release \
    --no-build \
    -- "$f" 2>&1) || EXIT_CODE=$?

  # Determinar resultado real
  if echo "$OUTPUT" | grep -q "^\[OK\]"; then
    ACTUAL="OK"
  else
    ACTUAL="FAIL"
  fi

  # ¿Coincide con lo esperado?
  if [ "$ACTUAL" = "$EXPECTED" ]; then
    PASS=$((PASS+1))
    printf "  ${GREEN}%-3s  %-42s  %-8s  %-8s  ✔ PASS${RESET}\n" \
      "$IDX" "$base" "$EXPECTED" "$ACTUAL"
  else
    FAIL=$((FAIL+1))
    UNEXPECTED=$((UNEXPECTED+1))
    printf "  ${RED}%-3s  %-42s  %-8s  %-8s  ✖ FAIL${RESET}\n" \
      "$IDX" "$base" "$EXPECTED" "$ACTUAL"
    # Mostrar errores del compilador
    echo "$OUTPUT" | grep -E "^\[(LEX|PARSE|SEM|FAIL)\]" | while IFS= read -r err; do
      echo -e "       ${RED}$err${RESET}"
    done
  fi
done

#  Resumen 
echo ""
sep="$(printf '%.0s' {1..48})"
echo -e "  ${BOLD}$sep${RESET}"
printf "  ${BOLD}%-30s %s${RESET}\n" "Archivos procesados:"  "$IDX"
printf "  ${GREEN}${BOLD}%-30s %s${RESET}\n" "Resultados correctos:"  "$PASS"
if [ "$FAIL" -gt 0 ]; then
  printf "  ${RED}${BOLD}%-30s %s${RESET}\n" "Resultados incorrectos:" "$FAIL"
fi
echo -e "  ${BOLD}$sep${RESET}\n"

[ "$UNEXPECTED" -eq 0 ] && exit 0 || exit 1
