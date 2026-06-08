#!/usr/bin/env bash
# =============================================================================
#  run-example.sh — Compila Y EJECUTA (en la VM) uno o todos los ejemplos
#  de Patito, mostrando la salida de sus 'escribe'/'imprime'.
#  Autor: Victor Misael Escalante Alvarado, A01741176
#
#  Uso:
#    ./run-example.sh <numero>            # ej. 18  -> 18_Fibonacci_recursivo.patito
#    ./run-example.sh <nombre o patron>   # ej. fibonacci, retorno, 11_retorno_tipo
#    ./run-example.sh --all               # corre todos los ejemplos validos
#    ./run-example.sh --list              # lista los ejemplos disponibles
#
#  Internamente invoca al compilador con la bandera --run (-r), que hace que
#  la maquina virtual ejecute los cuadruplos generados e imprima en consola
#  todo lo que el programa Patito escriba con 'escribe'.
# =============================================================================

set -euo pipefail

# ── Colores (siempre activos para que el log preserve el formato) ─────────────
BOLD="\033[1m"; CYAN="\033[1;36m"; GREEN="\033[1;32m"; RED="\033[1;31m"
YELLOW="\033[1;33m"; DIM="\033[2m"; RESET="\033[0m"

# ── Rutas ────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPILER_PROJECT="$SCRIPT_DIR/src/Patito.Compiler/Patito.Compiler.csproj"
EXAMPLES_DIR="$SCRIPT_DIR/examples"

# ── Logs ─────────────────────────────────────────────────────────────────────
LOG_DIR="$SCRIPT_DIR/logs"
mkdir -p "$LOG_DIR"
LOGFILE="$LOG_DIR/$(date +%Y%m%d-%H%M%S)-run-example.log"
exec > >(tee -a "$LOGFILE") 2>&1

# ── Validar argumentos ────────────────────────────────────────────────────────
if [ $# -lt 1 ]; then
  echo "Uso:"
  echo "  $0 <numero>            # ej. 18  -> 18_Fibonacci_recursivo.patito"
  echo "  $0 <nombre o patron>   # ej. fibonacci, retorno, 11_retorno_tipo"
  echo "  $0 --all               # corre todos los ejemplos validos"
  echo "  $0 --list              # lista los ejemplos disponibles"
  exit 1
fi

QUERY="$1"

# Solo ejemplos "validos" (los invalido_* no producen cuadruplos ejecutables).
# Nota: usamos un bucle 'while read' (en vez de 'mapfile'/'readarray') porque
# el bash 3.2 que trae macOS por defecto no soporta esos builtins de bash 4+.
ALL_EXAMPLES=()
while IFS= read -r f; do ALL_EXAMPLES+=("$f"); done \
  < <(find "$EXAMPLES_DIR" -maxdepth 1 -name "*.patito" ! -name "invalido_*" | sort)

# ── --list: solo mostrar los ejemplos disponibles ─────────────────────────────
if [ "$QUERY" = "--list" ]; then
  echo -e "${BOLD}  Ejemplos disponibles:${RESET}\n"
  for f in "${ALL_EXAMPLES[@]}"; do
    echo "    $(basename "$f")"
  done
  echo ""
  exit 0
fi

# ── Build silencioso (una sola vez) ───────────────────────────────────────────
echo -e "${DIM}[build] Compilando el proyecto...${RESET}"
dotnet build "$COMPILER_PROJECT" -c Release -v quiet 2>/dev/null \
  && echo -e "${DIM}[build] OK${RESET}\n" \
  || { echo -e "${RED}[ERROR] Fallo la compilacion. Ejecuta './build.sh' para detalles.${RESET}"; exit 1; }

# ── Funcion: compila + ejecuta UN archivo y muestra su salida ─────────────────
run_one() {
  local filepath="$1"
  local filename
  filename="$(basename "$filepath")"
  local sep
  sep="$(printf '═%.0s' {1..62})"

  echo -e "${CYAN}${BOLD}"
  echo "  $sep"
  printf "  %-60s\n" "  EJECUTANDO: $filename"
  echo "  $sep"
  echo -e "${RESET}"

  # En modo --run, Program.cs NO imprime la linea "[OK] ..."; stdout es
  # exactamente lo que la VM escribe via 'escribe'/'imprime'. Si la
  # compilacion falla, escribe "[FAIL] ..." a stderr y sale con codigo 1;
  # si la VM truena en tiempo de ejecucion, escribe "[VM ERROR] ..." a
  # stderr y sale con codigo 3. Exito = codigo 0.
  local exit_code=0
  local output
  output=$(dotnet run \
    --project "$COMPILER_PROJECT" \
    -c Release \
    --no-build \
    -- "$filepath" --run 2>&1) || exit_code=$?

  if [ "$exit_code" -ne 0 ]; then
    echo -e "  ${RED}✖ No se pudo compilar/ejecutar (codigo $exit_code)${RESET}\n"
    echo "$output" | grep -E "^\[(LEX|PARSE|SEM|FAIL|VM ERROR)\]" | while IFS= read -r err; do
      echo -e "  ${RED}$err${RESET}"
    done
    echo ""
    return 1
  fi

  echo -e "${YELLOW}${BOLD}  ── Salida del programa (VM) ───────────────────────────${RESET}\n"
  if [ -z "$(echo "$output" | tr -d '[:space:]')" ]; then
    echo -e "  ${DIM}(el programa no produjo salida con 'escribe'/'imprime')${RESET}"
  else
    echo "$output" | while IFS= read -r line; do
      echo "  $line"
    done
  fi
  echo ""
  echo -e "  ${GREEN}${BOLD}✔ Ejecucion completa${RESET}\n"
  return 0
}

# ── --all: correr todos los ejemplos validos ──────────────────────────────────
if [ "$QUERY" = "--all" ]; then
  TOTAL=0
  OK=0
  FAIL=0
  for f in "${ALL_EXAMPLES[@]}"; do
    TOTAL=$((TOTAL+1))
    if run_one "$f"; then OK=$((OK+1)); else FAIL=$((FAIL+1)); fi
  done

  sep="$(printf '─%.0s' {1..48})"
  echo -e "  ${BOLD}$sep${RESET}"
  printf "  ${BOLD}%-30s %s${RESET}\n" "Ejemplos ejecutados:" "$TOTAL"
  printf "  ${GREEN}${BOLD}%-30s %s${RESET}\n" "Exitosos:" "$OK"
  if [ "$FAIL" -gt 0 ]; then
    printf "  ${RED}${BOLD}%-30s %s${RESET}\n" "Con errores:" "$FAIL"
  fi
  echo -e "  ${BOLD}$sep${RESET}\n"

  [ "$FAIL" -eq 0 ] && exit 0 || exit 1
fi

# ── Buscar un ejemplo especifico por numero o nombre/patron ───────────────────
MATCHES=()
# 1) Coincidencia por numero de prefijo (ej. "18" -> "18_Fibonacci_recursivo.patito")
if [[ "$QUERY" =~ ^[0-9]+$ ]]; then
  while IFS= read -r f; do MATCHES+=("$f"); done \
    < <(printf '%s\n' "${ALL_EXAMPLES[@]}" | grep -E "/0*${QUERY}_" || true)
fi
# 2) Si no hubo match por numero, buscar por nombre/patron (insensible a mayusculas)
if [ ${#MATCHES[@]} -eq 0 ]; then
  while IFS= read -r f; do MATCHES+=("$f"); done \
    < <(printf '%s\n' "${ALL_EXAMPLES[@]}" | grep -i "$QUERY" || true)
fi

if [ ${#MATCHES[@]} -eq 0 ]; then
  echo -e "${RED}No se encontro ningun ejemplo que coincida con '${QUERY}'.${RESET}"
  echo -e "${DIM}Usa './run-example.sh --list' para ver los disponibles.${RESET}"
  exit 1
fi

if [ ${#MATCHES[@]} -gt 1 ]; then
  echo -e "${YELLOW}Varios ejemplos coinciden con '${QUERY}':${RESET}"
  for f in "${MATCHES[@]}"; do
    echo "    $(basename "$f")"
  done
  echo -e "${DIM}Se ejecutara el primero. Usa un patron mas especifico para elegir otro.${RESET}\n"
fi

run_one "${MATCHES[0]}"
