EXE="src/Patito.Compiler/bin/Debug/net10.0/patitoc"

GREEN='\033[32m'; 
RED='\033[31m'; 
NC='\033[0m' # No Color

if [[ ! -x "$EXE" ]]; then
  echo "Error: no se encontró el ejecutable '$EXE'. Compilando ..."
    dotnet build src/Patito.Compiler/Patito.Compiler.csproj -c Debug
    if [[ ! -x "$EXE" ]]; then
        echo "Error: no se pudo compilar el proyecto. Asegúrate de que el proyecto se compile correctamente."
        exit 1
    fi
fi

PASS=0; FAIL=0
# Print table header
printf "%-3s %-40s %-10s %-10s %-6s\n" "#" "File" "Expected" "Actual" "Result"
printf "%s\n" "---------------------------------------------------------------------------------------------"

idx=1
for f in examples/*.patito; do
  base=$(basename "$f")
  if [[ "$base" == invalido_* ]]; then expected=fail; else expected=ok; fi
  out=$("$EXE" "$f" 2>&1)
  if echo "$out" | grep -q '^\[OK\]'; then actual=ok; else actual=fail; fi
  if [[ "$actual" == "$expected" ]]; then
    result="OK"
    ((PASS++))
  else
    result="FAIL"
    ((FAIL++))
  fi

  if [[ "$result" == "OK" ]]; then
    printf "%-3s ${GREEN}%-40s${NC} %-10s ${GREEN}%-10s${NC} ${GREEN}%-6s${NC}\n" "$idx" "$base" "$expected" "$actual" "$result"
  else
    printf "%-3s ${RED}%-40s${NC} %-10s ${RED}%-10s${NC} ${RED}%-6s${NC}\n" "$idx" "$base" "$expected" "$actual" "$result"
    echo "Salida del compilador:"
    echo "$out"
  fi
  idx=$((idx+1))
done
echo
echo "Resumen: $PASS passed, $FAIL failed"