#!/bin/bash

# =========================================================
# PATITO TEST RUNNER
# =========================================================

Project_Ruta="tests/Patito.Tests/Patito.Tests.csproj"

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
BOLD='\033[1m'
NC='\033[0m'

clear

echo -e "${MAGENTA}${BOLD}"
echo "         PATITO TEST RUNNER          "
echo -e "${NC}"

# ---------------------------------------------------------
# Verificar proyecto
# ---------------------------------------------------------

if [[ ! -f "$Project_Ruta" ]]; then
  echo -e "${RED}✖ Error:${NC} No se encontró:"
  echo "  $Project_Ruta"
  exit 1
fi

echo -e "${CYAN}  Ejecutando pruebas del compilador...${NC}\n"

# ---------------------------------------------------------
# Ejecutar pruebas (menos ruido)
# ---------------------------------------------------------

test_output=$(dotnet test "$Project_Ruta" --verbosity quiet 2>&1)
test_exit_code=$?

# ---------------------------------------------------------
# Extraer métricas
# ---------------------------------------------------------

total=$(echo "$test_output" | grep -Eo 'Total: *[0-9]+' | tail -1 | awk '{print $2}')
passed=$(echo "$test_output" | grep -Eo 'Superado: *[0-9]+' | tail -1 | awk '{print $2}')
failed=$(echo "$test_output" | grep -Eo 'Con error: *[0-9]+' | tail -1 | awk '{print $3}')
skipped=$(echo "$test_output" | grep -Eo 'Omitido: *[0-9]+' | tail -1 | awk '{print $2}')

# Fallback inglés
[[ -z "$total" ]] && total=$(echo "$test_output" | grep -Eo 'Total tests: *[0-9]+' | awk '{print $3}')
[[ -z "$passed" ]] && passed=$(echo "$test_output" | grep -Eo 'Passed: *[0-9]+' | awk '{print $2}')
[[ -z "$failed" ]] && failed=$(echo "$test_output" | grep -Eo 'Failed: *[0-9]+' | awk '{print $2}')
[[ -z "$skipped" ]] && skipped=$(echo "$test_output" | grep -Eo 'Skipped: *[0-9]+' | awk '{print $2}')

# ---------------------------------------------------------
# Mostrar tabla bonita
# ---------------------------------------------------------

echo -e "${CYAN}┌────────────────────────────┐${NC}"
echo -e "${CYAN}│      RESULTADOS TESTS      │${NC}"
echo -e "${CYAN}├──────────────┬─────────────┤${NC}"

printf "${CYAN}│${NC} %-12s ${CYAN}│${NC} ${GREEN}%11s${NC} ${CYAN}│${NC}\n" "Passed" "$passed"
printf "${CYAN}│${NC} %-12s ${CYAN}│${NC} ${RED}%11s${NC} ${CYAN}│${NC}\n" "Failed" "$failed"
printf "${CYAN}│${NC} %-12s ${CYAN}│${NC} ${YELLOW}%11s${NC} ${CYAN}│${NC}\n" "Skipped" "$skipped"

echo -e "${CYAN}├──────────────┼─────────────┤${NC}"

printf "${CYAN}│${NC} %-12s ${CYAN}│${NC} %11s ${CYAN}│${NC}\n" "Total" "$total"

echo -e "${CYAN}└──────────────┴─────────────┘${NC}"

# ---------------------------------------------------------
# Estado final
# ---------------------------------------------------------

echo ""

if [[ "$failed" == "0" ]]; then
  echo -e "${GREEN}${BOLD}✔ Todas las pruebas pasaron${NC}"
else
  echo -e "${RED}${BOLD}✖ Algunas pruebas fallaron${NC}"
fi

echo ""

# ---------------------------------------------------------
# Error handling
# ---------------------------------------------------------

if [[ $test_exit_code -ne 0 ]]; then
  exit $test_exit_code
fi

# ---------------------------------------------------------
# Ejecutar samples
# ---------------------------------------------------------

echo -e "${YELLOW}¿Deseas ejecutar las pruebas de samples? ([y]/n)${NC}"
read -r answer

echo ""

if [[ "$answer" == "y" || "$answer" == "Y" || -z "$answer" ]]; then
  echo -e "${CYAN}  Ejecutando pruebas de samples...${NC}\n"
  bash test-samples.sh
else
  echo -e "${YELLOW} Samples omitidos :(${NC}"
fi