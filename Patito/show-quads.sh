#!/usr/bin/env bash
# =============================================================================
#  show-quads.sh — Genera y muestra los cuadruplos de los programas de ejemplo.
#  Autor: Victor Misael Escalante Alvarado, A01741176
#
#  Uso:
#    ./show-quads.sh               # todos los ejemplos validos
#    ./show-quads.sh examples/03_condicion.patito
#    ./show-quads.sh examples/03_condicion.patito examples/04_ciclo.patito
#
#  Requiere:
#    dotnet SDK instalado y el proyecto compilado (o se compila en el momento).
# =============================================================================

set -euo pipefail

# ── Colores (siempre activos para que el log preserve el formato) ─────────────
BOLD="\033[1m"
CYAN="\033[1;36m"
GREEN="\033[1;32m"
YELLOW="\033[1;33m"
RED="\033[1;31m"
DIM="\033[2m"
RESET="\033[0m"

# ── Rutas ────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPILER_PROJECT="$SCRIPT_DIR/src/Patito.Compiler/Patito.Compiler.csproj"

# Logs: guarda la salida del script en el directorio de logs con formato YYYYMMDD-HHMMSS-show-quads.log
LOG_DIR="$SCRIPT_DIR/logs"
mkdir -p "$LOG_DIR"
TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
LOGFILE="$LOG_DIR/${TIMESTAMP}-show-quads.log"

# Redirige toda la salida (stdout y stderr) al logfile, pero también la muestra en pantalla
exec > >(tee -a "$LOGFILE") 2>&1

# Compilar el proyecto si aun no esta compilado (build silencioso)
echo -e "${DIM}[build] Compilando el proyecto...${RESET}"
dotnet build "$COMPILER_PROJECT" -c Release -v quiet 2>/dev/null \
  && echo -e "${DIM}[build] OK${RESET}" \
  || { echo -e "${RED}[ERROR] Fallo la compilacion. Ejecuta 'dotnet build' para ver detalles.${RESET}"; exit 1; }

echo ""

# ── Archivos a procesar ───────────────────────────────────────────────────────
if [ $# -gt 0 ]; then
  FILES=("$@")
else
  # Por default: todos los ejemplos validos (excluye los invalido_*)
  FILES=()
  while IFS= read -r _f; do
    FILES+=("$_f")
  done < <(find "$SCRIPT_DIR/examples" -name "*.patito" ! -name "invalido_*" | sort)
fi

# ── Contadores globales ───────────────────────────────────────────────────────
TOTAL_FILES=0
TOTAL_QUADS=0
FAILED=0

# ── Funcion: procesar un archivo ──────────────────────────────────────────────
process_file() {
  local filepath="$1"
  local filename
  filename="$(basename "$filepath")"

  TOTAL_FILES=$((TOTAL_FILES + 1))

  # Encabezado del programa
  local sep
  sep="$(printf '═%.0s' {1..62})"
  echo -e "${CYAN}${BOLD}"
  echo "  $sep"
  printf "  %-62s\n" "  PROGRAMA: $filename"
  echo "  $sep"
  echo -e "${RESET}"

  # Codigo fuente (con numeros de linea, omitir lineas en blanco/comentarios)
  echo -e "${YELLOW}${BOLD}  ── Código fuente ──────────────────────────────────────${RESET}"
  echo ""
  local ln=0
  while IFS= read -r line; do
    ln=$((ln + 1))
    # Mostrar todas las lineas no vacias
    if [[ -n "${line// }" ]]; then
      printf "  ${DIM}%3d${RESET}  %s\n" "$ln" "$line"
    fi
  done < "$filepath"
  echo ""

  # Ejecutar el compilador con --quads
  echo -e "${YELLOW}${BOLD}  ── Fila de cuádruplos ─────────────────────────────────${RESET}"
  echo ""

  local output
  local exit_code=0
  output=$(dotnet run \
    --project "$COMPILER_PROJECT" \
    -c Release \
    --no-build \
    -- "$filepath" --quads 2>&1) || exit_code=$?

  if [ $exit_code -ne 0 ]; then
    echo -e "${RED}  *** Error de compilacion ***${RESET}"
    echo "$output" | grep -E "^\[(LEX|PARSE|SEM|FAIL)\]" | while IFS= read -r err; do
      echo -e "  ${RED}$err${RESET}"
    done
    echo ""
    FAILED=$((FAILED + 1))
    return
  fi

  # Extraer y mostrar solo la seccion de cuadruplos
  local in_quads=false
  local quad_count=0
  while IFS= read -r line; do
    if [[ "$line" == *"=== Fila de Cuadruplos ==="* ]]; then
      in_quads=true
      continue
    fi
    if $in_quads; then
      if [[ -z "${line// }" ]]; then
        # Linea en blanco despues de la tabla → fin de cuadruplos
        break
      fi
      if [[ "$line" == *"---"* ]] || [[ "$line" == "   #"* ]]; then
        # Linea de separador o encabezado de tabla
        echo -e "  ${DIM}$line${RESET}"
      elif [[ "$line" == *"(sin cuadruplos)"* ]]; then
        echo -e "  ${DIM}(sin cuadruplos)${RESET}"
      else
        # Linea de cuadruplo — resaltar segun la operacion
        local color="$RESET"
        if [[ "$line" =~ (GotoF|Goto) ]]; then
          color="$YELLOW"
        elif [[ "$line" =~ Print ]]; then
          color="$GREEN"
        elif [[ "$line" =~ (ERA|EndFunc) ]]; then
          color="$BOLD"
        elif [[ "$line" =~ (Param|Gosub) ]]; then
          color="$CYAN"
        fi
        echo -e "  ${color}$line${RESET}"
        quad_count=$((quad_count + 1))
      fi
    fi
  done <<< "$output"

  echo ""

  # Estadisticas del programa
  local ok_line
  ok_line=$(echo "$output" | grep "^\[OK\]" || true)
  if [[ -n "$ok_line" ]]; then
    echo -e "  ${GREEN}${ok_line}${RESET}"
  fi
  echo ""

  TOTAL_QUADS=$((TOTAL_QUADS + quad_count))
}

# ── Loop principal ─────────────────────────────────────────────────────────────
for f in "${FILES[@]}"; do
  process_file "$f"
done

# ── Resumen final ─────────────────────────────────────────────────────────────
sep="$(printf '─%.0s' {1..62})"
echo -e "${BOLD}"
echo "  $sep"
printf "  %-30s %s\n" "Programas procesados:" "$TOTAL_FILES"
printf "  %-30s %s\n" "Total de cuadruplos:" "$TOTAL_QUADS"
if [ $FAILED -gt 0 ]; then
  printf "  %-30s %s\n" "Programas con errores:" "$FAILED"
fi
echo "  $sep"
echo -e "${RESET}"

[ $FAILED -eq 0 ] && exit 0 || exit 1
