# Plan de Pruebas

Este documento consolida el **plan de pruebas** del compilador Patito a lo largo de las tres entregas. Cada caso se traduce en al menos un test de xUnit dentro del proyecto [`../tests/Patito.Tests`](../tests/Patito.Tests), de modo que `dotnet test` ejecuta toda la suite.

El diseño del plan persigue cuatro objetivos:

- **Pruebas dirigidas:** cada regla léxica, sintáctica, semántica o de generación de código clave tiene al menos un test que la ejercita.
- **Pruebas de proximidad:** pares como (`==` vs `=`) o (`KW_SI` vs `ID siempre`) verifican la prioridad del *longest-match* y las palabras reservadas.
- **Pruebas inversas:** programas diseñados deliberadamente para fallar en cada fase (léxica, sintáctica, semántica).
- **Pruebas en archivos:** suite parametrizada que carga cada `.patito` de [`../examples/`](../examples) y verifica el resultado esperado.

## Resumen por archivo de prueba

| Archivo                                         | Cobertura                                                                                  |
|-------------------------------------------------|--------------------------------------------------------------------------------------------|
| [`ScannerTests.cs`](../tests/Patito.Tests/ScannerTests.cs) | Tokens individuales, *longest-match*, prioridad de palabras reservadas, comentarios y whitespace. |
| [`ParserTests.cs`](../tests/Patito.Tests/ParserTests.cs)   | Producciones de la gramática, precedencia de operadores, programas válidos e inválidos.    |
| [`SemanticCubeTests.cs`](../tests/Patito.Tests/SemanticCubeTests.cs) | Todas las celdas del cubo semántico (combinaciones tipo × operador × tipo).               |
| [`VariableTableTests.cs`](../tests/Patito.Tests/VariableTableTests.cs) | Operaciones de la tabla de variables (declarar, duplicado, lookup).                      |
| [`FunctionDirectoryTests.cs`](../tests/Patito.Tests/FunctionDirectoryTests.cs) | Operaciones del directorio de funciones.                                                  |
| [`SemanticAnalyzerTests.cs`](../tests/Patito.Tests/SemanticAnalyzerTests.cs) | End-to-end: programa Patito → tablas pobladas + errores semánticos detectados.            |
| [`CodeGenTests.cs`](../tests/Patito.Tests/CodeGenTests.cs) | Generación de cuádruplos: PN-8 a PN-18, precedencia, Backfill, llamadas a función e integración. |

## Pruebas para el SCANNER

| Caso                                      | Test (xUnit)                                                  | Entrada                                                              | Resultado esperado                                         |
|-------------------------------------------|---------------------------------------------------------------|----------------------------------------------------------------------|------------------------------------------------------------|
| Palabras reservadas                       | `PalabraReservada_SeReconoceComoKeyword`                      | `programa`, `inicio`, `fin`, `vars`, `entero`, `flotante`, `nula`, `si`, `sino`, `mientras`, `haz`, `escribe` | Cada token se reconoce como su `KW_*` correspondiente, no como `ID`. |
| ID vs keyword (longest-match)             | `Identificador_NoColisionaConKeywords`                        | `siempre`, `inicios`, `contador`, `x1`                               | `siempre` se reconoce como `ID`, no como `KW_SI` + `empre`. |
| ID con mayúsculas                         | `Identificador_AceptaMayusculasYMinusculas`                   | `Hola`, `sumarHasta`, `MiVariable1`                                  | Acepta camelCase / PascalCase (`[a-zA-Z]`).                |
| `CTE_ENT`                                 | `ConstanteEntera_SeReconoce`                                  | `0`, `123`, `1039203`                                                | Números enteros sin punto.                                 |
| `CTE_FLOT` vs `CTE_ENT`                   | `ConstanteFlotante_PrefiereCteFlotSobreEntero`                | `3.14`, `0.0`, `100.0`, `3.14159265`                                 | `3.14` produce un solo `CTE_FLOT`, no `CTE_ENT . CTE_ENT`.  |
| `LETRERO` simple                          | `Letrero_AcceptaTextoSimple`                                  | `"Me llamo Misael"`                                                  | Cadena con espacios entre comillas.                        |
| `LETRERO` multilínea (error léxico)       | `Letrero_NoAceptaSaltoDeLinea`                                | `"linea1\nlinea2"`                                                   | Genera error léxico.                                       |
| Operadores individuales                   | `Operadores_TokenIndividual`                                  | `==`, `!=`, `=`, `<`, `>`, `+`, `-`, `*`, `/`                        | Cada operador es exactamente un token.                     |
| `==` sobre `=`                            | `OpEq_TienePrioridadSobreOpAsigna`                            | `==`                                                                 | Se elige la regla de 2 caracteres (longest-match).         |
| Comentarios                               | `ComentarioDeLinea_SeIgnora` / `ComentarioDeBloque_SeIgnora`  | `// ...` y `/* ... */`                                               | No producen tokens (skip).                                 |
| Whitespace                                | `Whitespace_NoGeneraTokens`                                   | `"  a   +\t b  "`                                                    | Solo se emiten `ID OP_MAS ID`.                             |
| Carácter inválido                         | `CaracterInvalido_GeneraErrorLexico`                          | `a @ b`                                                              | El símbolo `@` produce error léxico.                       |

## Pruebas para el PARSER

| Caso                                  | Test (xUnit)                                            | Resumen de entrada                                  | Resultado esperado                                            |
|---------------------------------------|---------------------------------------------------------|-----------------------------------------------------|---------------------------------------------------------------|
| Programa mínimo                       | `Programa_Minimo_SinVarsSinFuncs_Pasa`                  | `programa hola; inicio { escribe("hola"); } fin`    | Acepta sin errores.                                           |
| Vars con varios tipos                 | `Vars_VariosTiposYIds_Pasa`                             | `vars x,y,z: entero; pi: flotante;`                 | Acepta múltiples grupos `id:tipo`.                            |
| Precedencia de operadores             | `Expresion_RespectaPrecedenciaDeOperadores`             | `x = 2 + 3 * 4 - (1 + 1);`                          | Acepta y agrupa según precedencia (`*` > `+`).                |
| Operador relacional                   | `Expresion_OperadorRelacional_Pasa`                     | `si (a < 10) { ... };`                              | Acepta `rel_op` dentro de `expresion`.                        |
| Si-sino                               | `Condicion_ConSino_Pasa`                                | `si (x>0) {...} sino {...};`                         | Acepta rama `sino` opcional.                                  |
| Ciclo mientras-haz                    | `Ciclo_Mientras_Pasa`                                   | `mientras (i<5) haz {...};`                          | Acepta y consume el `;` final.                                |
| Escribe mixto                         | `Escribe_AceptaExpresionesYLetreros`                    | `escribe("la respuesta es", x, "fin", x+1);`        | Mezcla letreros y expresiones.                                |
| Función con params y vars locales     | `Funcion_Nula_ConParametros_Pasa`                       | `nula sumarHasta(n: entero) { vars i:entero; ... };` | Acepta declaración completa.                                  |
| Llamada como factor                   | `Llamada_PuedeUsarseComoFactor`                         | `x = dame() + 1;`                                    | `llamada` se usa dentro de `expresion`.                       |
| Falta de `;`                          | `FaltaPuntoYComa_TrasIdDelPrograma_Falla`               | `programa malo` (sin `;`)                            | Genera `ParseError`.                                          |
| Paréntesis sin cerrar                 | `ParentesisSinCerrar_Falla`                             | `si (x > 0 { ... }`                                  | Genera `ParseError`.                                          |
| Tipo no soportado                     | `TipoNoSoportado_Falla`                                 | `vars nombre: cadena;`                               | Genera `ParseError`.                                          |
| Escribe sin args                      | `EscribeSinArgumentos_Falla`                            | `escribe();`                                          | Genera `ParseError` (al menos un `imp` requerido).            |
| Suite de archivos válidos             | `Ejemplos_Validos_Pasan`                                | `01..07_*.patito`                                    | Todos parsean sin errores.                                    |
| Suite de archivos inválidos           | `Ejemplos_Invalidos_Fallan`                             | `invalido_01..05_*.patito`                           | Todos producen al menos un error.                             |

## Pruebas para el CUBO SEMÁNTICO

El cubo se prueba sin involucrar al parser: se construye `SemanticCube.Default` y se verifican las celdas. Esto aísla los bugs del cubo de los del listener.

| Caso                                              | Test (xUnit)                                          | Resultado esperado                                                 |
|---------------------------------------------------|-------------------------------------------------------|---------------------------------------------------------------------|
| `+`, `-`, `*` con enteros y flotantes             | `Suma_PropagaAFlotanteSiAlgunoEsFlotante`             | `entero + entero → entero`; cualquier mezcla con `flotante → flotante`. |
| División `/`                                       | `Division_SiempreFlotante`                            | Cualquier combinación de operandos numéricos devuelve `Flotante`.   |
| Relacionales `<`, `>`, `==`, `!=`                 | `Relacionales_DevuelvenBoolEntreNumericos`            | Resultado `Bool` con operandos numéricos.                           |
| Asignación válida                                  | `Asignacion_EnteroAEntero_Ok`                         | `entero ← entero` → `Entero`.                                       |
| Promoción implícita                                | `Asignacion_FlotanteAEntero_PermiteWidening`          | `flotante ← entero` → `Flotante`.                                   |
| Asignación flotante a flotante                    | `Asignacion_FlotanteAFlotante_Ok`                     | `flotante ← flotante` → `Flotante`.                                 |
| Narrowing prohibido                                | `Asignacion_EnteroAFlotante_EsError`                  | `entero ← flotante` → `Error`.                                      |
| `Bool` en aritmética                                | `Operacion_ConBool_EsError`                           | Cualquier `Bool` operando en `+`, `-`, `*`, `/` o relacional → `Error`. |
| Coherencia `Resolve`/`IsCompatible`               | `IsCompatible_ReflectaResolve`                        | Ambas APIs coinciden.                                                |

## Pruebas para las ESTRUCTURAS (tabla y directorio)

### `VariableTable`

| Caso                                          | Test (xUnit)                                            | Resultado esperado                                                 |
|-----------------------------------------------|---------------------------------------------------------|---------------------------------------------------------------------|
| Declaración nueva                              | `TryDeclare_PrimeraVez_RegresaTrue`                     | Devuelve `true`, `Count == 1`, `Contains(name)` es `true`.          |
| Declaración duplicada                          | `TryDeclare_NombreDuplicado_RegresaFalseYNoSobreescribe` | Devuelve `false`; el primer símbolo se conserva.                    |
| Lookup de nombre inexistente                  | `TryLookup_NoExistente_RegresaFalse`                    | Devuelve `false`; `Lookup` retorna `null`.                          |
| Orden de inserción                             | `Symbols_PreservaOrdenDeInsercion`                      | La enumeración respeta el orden en que se declararon los símbolos.  |

### `FunctionDirectory`

| Caso                                          | Test (xUnit)                                            | Resultado esperado                                                 |
|-----------------------------------------------|---------------------------------------------------------|---------------------------------------------------------------------|
| Declaración nueva                              | `TryDeclare_PrimeraVez_RegresaTrue`                     | Devuelve `true`, `Count == 1`.                                      |
| Declaración duplicada                          | `TryDeclare_NombreDuplicado_RegresaFalseYNoSobreescribe` | Devuelve `false`; la función original se conserva.                   |
| `FunctionInfo` inicial                        | `FunctionInfo_TablaLocal_ArrancaVacia`                  | `LocalTable.Count == 0`, `ParameterTypes` vacío.                    |
| Tabla global compartida                       | `GlobalTable_EsCompartidaPorElPrograma`                 | Se puede declarar y leer desde `directory.GlobalTable`.              |

## Pruebas para el ANALIZADOR SEMÁNTICO (end-to-end)

Estas pruebas alimentan código Patito al front-end completo y verifican que el árbol producido se procese correctamente: tablas pobladas, directorio lleno y errores con su `SemanticErrorCode` correspondiente. Se afirma sobre el **código** del error (no el texto) para que los tests sean inmunes a cambios de redacción.

| Caso                                              | Test (xUnit)                                       | Resultado esperado                                                                          |
|---------------------------------------------------|----------------------------------------------------|---------------------------------------------------------------------------------------------|
| Programa sin vars ni funciones                    | `Programa_SinVariables_PasaTodo`                   | Compila sin errores, tablas vacías, `ProgramName == "hola"`.                                |
| Globales con varios tipos                         | `Vars_GlobalesQuedanRegistradasConTipo`            | `GlobalTable.Count == 3` con `x, y: entero`, `pi: flotante`.                                |
| Función con params y locales                      | `Funcion_QuedaEnDirectorioConParametrosYLocales`   | Directorio contiene `sumar` con `ParameterTypes` correctos y `LocalTable` con 3 símbolos.    |
| Variable global doblemente declarada              | `VarGlobalDoblementeDeclarada_EmiteError`          | `SemanticErrorCode.VariableRedeclared` para `x`.                                            |
| Variable local doblemente declarada               | `VarLocalDoblementeDeclarada_EmiteError`           | `SemanticErrorCode.VariableRedeclared` para `i` en alcance de la función.                   |
| Parámetro duplicado                                | `ParametroDuplicado_EmiteError`                    | `SemanticErrorCode.ParameterRedeclared`.                                                    |
| Parámetro vs local con el mismo nombre            | `ParametroChocaConLocal_EmiteError`                | `SemanticErrorCode.VariableRedeclared` (el local choca con el parámetro previo).            |
| Función duplicada                                  | `FuncionDuplicada_EmiteError`                      | `SemanticErrorCode.FunctionRedeclared`.                                                     |
| Función con el nombre del programa                | `FuncionConNombreDelPrograma_EmiteError`           | `SemanticErrorCode.NameClashesWithProgram`.                                                 |
| Variable usada sin declarar (asignación)          | `VariableNoDeclaradaEnAsignacion_EmiteError`       | `SemanticErrorCode.UndeclaredVariable`.                                                     |
| Variable usada sin declarar (expresión)           | `VariableNoDeclaradaEnExpresion_EmiteError`        | `SemanticErrorCode.UndeclaredVariable` para `b` en `a = b + 1;`.                            |
| Llamada a función inexistente                     | `LlamadaAFuncionInexistente_EmiteError`            | `SemanticErrorCode.UndeclaredFunction`.                                                     |
| Local ensombrece a global                         | `VariableLocalEnsombreceALaGlobal`                 | Compila; la `x` local de la función es flotante, la global entero.                          |
| Función usa global sin error                      | `FuncionUsaGlobalSinError`                         | La función accede al global declarado en el programa sin error.                              |
| Cubo accesible desde el analizador                 | `AnalizadorExponeCubo`                             | `Semantic.Cube` no es null y responde correctamente a `Resolve`.                            |

## Pruebas de GENERACIÓN DE CÓDIGO (Entrega 3)

Archivo: [`CodeGenTests.cs`](../tests/Patito.Tests/CodeGenTests.cs)

Todas las pruebas de esta sección compilan el programa fuente con `PatitoFrontEnd.Compile` y validan la fila de cuádruplos resultante. Si la compilación falla por errores léxicos, sintácticos o semánticos, la prueba falla también antes de llegar a verificar los cuádruplos.

El helper central es:

```csharp
private static IReadOnlyList<Quadruple> Quads(string src)
{
    var r = PatitoFrontEnd.Compile(src, "<codegen-test>");
    Assert.True(r.Success, /* mensaje con errores */);
    return r.Quads!;
}
```

### Asignación (PN-8, PN-12)

| Caso                              | Entrada                 | Cuádruplos esperados                                    |
|-----------------------------------|-------------------------|---------------------------------------------------------|
| `AsignaConstanteEntera`           | `x = 42;`               | `(=, "42", null, "x")`                                  |
| `AsignaConstanteFlotante`         | `pi = 3.14;`            | `(=, "3.14", null, "pi")`                               |
| `AsignaVariable`                  | `a = 1; b = a;`         | `(=,"1",null,"a")`, `(=,"a",null,"b")`                  |

### Suma y resta (PN-10)

| Caso                              | Entrada                 | Cuádruplos esperados                                    |
|-----------------------------------|-------------------------|---------------------------------------------------------|
| `Suma_DosVariables`               | `x = a + b;`            | `(+,"a","b","t0")`, `(=,"t0",null,"x")`                 |
| `Resta_DosVariables`              | `x = a - b;`            | `(-,"a","b","t0")`, `(=,"t0",null,"x")`                 |

### Multiplicación y división (PN-9)

| Caso                              | Entrada                 | Cuádruplos esperados                                    |
|-----------------------------------|-------------------------|---------------------------------------------------------|
| `Multiplicacion`                  | `x = a * b;`            | `(*,"a","b","t0")`, `(=,"t0",null,"x")`                 |
| `Division`                        | `r = a / b;`            | `(/,"a","b","t0")`, `(=,"t0",null,"r")` — resultado `Flotante` |

### Precedencia (PN-9 + PN-10)

| Caso                                    | Entrada               | Cuádruplos esperados                                                        |
|-----------------------------------------|-----------------------|-----------------------------------------------------------------------------|
| `Precedencia_MulAntesQueSuma`           | `x = a + b * c;`      | `(*,"b","c","t0")`, `(+,"a","t0","t1")`, `(=,"t1",null,"x")`               |
| `Precedencia_ParentesisAnulanMul`       | `x = (a + b) * c;`    | `(+,"a","b","t0")`, `(*,"t0","c","t1")`, `(=,"t1",null,"x")`               |

### Negación unaria (PN-8)

| Caso                              | Entrada                 | Cuádruplos esperados                                    |
|-----------------------------------|-------------------------|---------------------------------------------------------|
| `NegacionUnaria_Variable`         | `y = -x;`               | `(neg,null,"x","t0")`, `(=,"t0",null,"y")`              |

### Operadores relacionales (PN-11)

Prueba parametrizada `[Theory]` sobre los cuatro operadores:

| `op` | `QuadOp` esperado | Cuádruplos verificados                                   |
|------|-------------------|----------------------------------------------------------|
| `<`  | `Lt`              | `(<,"a","b","t0")` en quad[0], `GotoF` en quad[1]        |
| `>`  | `Gt`              | `(>,"a","b","t0")` en quad[0], `GotoF` en quad[1]        |
| `==` | `Eq`              | `(==,"a","b","t0")` en quad[0], `GotoF` en quad[1]       |
| `!=` | `Neq`             | `(!=,"a","b","t0")` en quad[0], `GotoF` en quad[1]       |

### Impresión — `escribe` (PN-13)

| Caso                              | Entrada                        | Cuádruplos esperados                                              |
|-----------------------------------|--------------------------------|-------------------------------------------------------------------|
| `Escribe_Letrero`                 | `escribe("hola mundo");`       | `(Print,null,null,"\"hola mundo\"")`                             |
| `Escribe_Variable`                | `x = 7; escribe(x);`           | `(=,"7",null,"x")`, `(Print,null,null,"x")`                      |
| `Escribe_MixtoLetreroyVariable`   | `escribe("resp:", n);`         | `(Print,null,null,"\"resp:\"")`, `(Print,null,null,"n")`         |

### Condicional `si` sin `sino` (PN-11, PN-16)

```
si (x < 5) { y = 1; };
```

| # | Op    | Left | Right | Result |
|---|-------|------|-------|--------|
| 0 | `<`   | `x`  | `5`   | `t0`   |
| 1 | GotoF | `t0` | `_`   | `3`    | ← Backfill en PN-16 |
| 2 | `=`   | `1`  | `_`   | `y`    |

### Condicional `si/sino` (PN-11, PN-15, PN-16)

```
si (x < 5) { y = 1; } sino { y = 2; };
```

| # | Op    | Left | Right | Result |
|---|-------|------|-------|--------|
| 0 | `<`   | `x`  | `5`   | `t0`   |
| 1 | GotoF | `t0` | `_`   | `4`    | ← Backfill en PN-15 |
| 2 | `=`   | `1`  | `_`   | `y`    |
| 3 | Goto  | `_`  | `_`   | `5`    | ← Backfill en PN-16 |
| 4 | `=`   | `2`  | `_`   | `y`    |

### Ciclo `mientras` (PN-14, PN-17)

```
i = 0;
mientras (i < 5) haz { i = i + 1; };
```

| # | Op    | Left | Right | Result |
|---|-------|------|-------|--------|
| 0 | `=`   | `0`  | `_`   | `i`    |
| 1 | `<`   | `i`  | `5`   | `t0`   |
| 2 | GotoF | `t0` | `_`   | `6`    | ← Backfill en PN-17 |
| 3 | `+`   | `i`  | `1`   | `t1`   |
| 4 | `=`   | `t1` | `_`   | `i`    |
| 5 | Goto  | `_`  | `_`   | `1`    | ← ExitCiclo (PN-17) |

### Llamadas a función (PN-18)

| Caso                      | Entrada        | Cuádruplos (últimos de la función)                                    |
|---------------------------|----------------|-----------------------------------------------------------------------|
| `LlamadaSinArgs`          | `f();`         | `(Gosub,null,null,"f")`                                               |
| `LlamadaConUnArg`         | `f(n);`        | `(Param,null,null,"n")`, `(Gosub,null,null,"f")`                      |
| `LlamadaConDosArgs`       | `f(a, 7);`     | `(Param,null,null,"a")`, `(Param,null,null,"7")`, `(Gosub,...,"f")`   |

### Pruebas de integración

| Caso                                | Descripción                                                                                      |
|-------------------------------------|--------------------------------------------------------------------------------------------------|
| `Integracion_AsignacionYEscribe`    | 5 cuádruplos en orden exacto: dos asignaciones, un `+`, un `=` y un `Print`.                    |
| `Integracion_CicloConPrint`         | 7 cuádruplos; `Print` queda dentro del cuerpo del ciclo (entre `GotoF` y `Goto`-al-inicio).     |
| `Integracion_CondicionAnidada`      | `si/sino` anidados; ningún `GotoF` ni `Goto` queda con destino `"?"` tras el recorrido.         |
| `Integracion_EjemploFuncion`        | Smoke test con función `saludar` + ciclo interno; todos los saltos resueltos.                    |

---

## Cómo correr las pruebas

### Scripts de shell (recomendado)

Todos los scripts están en la raíz del repositorio. Deben darse permisos de ejecución la primera vez:

```bash
chmod +x build.sh compile-example.sh compile-examples.sh run-all.sh show-quads.sh
```

| Script | Qué hace |
|--------|----------|
| `./build.sh` | Compila `Patito.Compiler` (Release) y `Patito.Tests` (Debug) sin ejecutar tests. |
| `./build.sh --compiler` | Solo compila el compilador. |
| `./build.sh --tests` | Solo compila el proyecto de pruebas. |
| `./compile-example.sh <archivo.patito>` | Compila un programa Patito y muestra el resultado (tokens, tablas, errores). |
| `./compile-example.sh <archivo.patito> --quads` | Igual que el anterior pero además muestra la fila de cuádruplos. |
| `./compile-examples.sh` | Corre el compilador sobre todos los ejemplos **válidos** y verifica que pasen. |
| `./compile-examples.sh --all` | Igual, pero incluye también los `invalido_*` verificando que fallen. |
| `./show-quads.sh` | Muestra código fuente + cuádruplos de todos los ejemplos válidos con colores. |
| `./show-quads.sh <archivo.patito>` | Igual, solo para un archivo específico. |
| `./run-all.sh` | Orquesta las 4 fases: build → tests unitarios → demo quads → ejemplos. |
| `./run-all.sh --no-demo` | Igual pero omite la fase de demo de cuádruplos (más rápido). |

### Comandos `dotnet test` y filtros

```bash
# ── Toda la suite ──────────────────────────────────────────────────────────
dotnet test

# ── Por archivo de prueba ──────────────────────────────────────────────────
dotnet test --filter "FullyQualifiedName~ScannerTests"
dotnet test --filter "FullyQualifiedName~ParserTests"
dotnet test --filter "FullyQualifiedName~SemanticCubeTests"
dotnet test --filter "FullyQualifiedName~VariableTableTests"
dotnet test --filter "FullyQualifiedName~FunctionDirectoryTests"
dotnet test --filter "FullyQualifiedName~SemanticAnalyzerTests"
dotnet test --filter "FullyQualifiedName~CodeGenTests"

# ── Demo de cuadruplos (salida formateada en "Standard Output") ────────────
dotnet test --filter "FullyQualifiedName~QuadruplesDemoTests" -v normal

# ── Un test específico por nombre ──────────────────────────────────────────
dotnet test --filter "FullyQualifiedName~Funcion_Nula_ConParametros_Pasa"
dotnet test --filter "FullyQualifiedName~Demo_03_Condicion"

# ── Solo tests de una fase (usando DisplayName) ────────────────────────────
dotnet test --filter "FullyQualifiedName~CodeGenTests" -v normal

# ── Tests sin el demo (más rápido en CI) ──────────────────────────────────
dotnet test --filter "FullyQualifiedName!~QuadruplesDemoTests"
```

### Referencia de filtros por clase

| Clase de test | Filtro | Tests cubiertos |
|---|---|---|
| `ScannerTests` | `FullyQualifiedName~ScannerTests` | 12 — tokens, longest-match, comentarios |
| `ParserTests` | `FullyQualifiedName~ParserTests` | 14 — producciones, precedencia, archivos |
| `SemanticCubeTests` | `FullyQualifiedName~SemanticCubeTests` | 9 — todas las celdas del cubo |
| `VariableTableTests` | `FullyQualifiedName~VariableTableTests` | 4 — declarar, duplicado, lookup |
| `FunctionDirectoryTests` | `FullyQualifiedName~FunctionDirectoryTests` | 4 — directorio de funciones |
| `SemanticAnalyzerTests` | `FullyQualifiedName~SemanticAnalyzerTests` | 13 — end-to-end semántica |
| `CodeGenTests` | `FullyQualifiedName~CodeGenTests` | 24 — PN-8 a PN-18, Backfill |
| `QuadruplesDemoTests` | `FullyQualifiedName~QuadruplesDemoTests` | 12 — demo visual con cuádruplos |

### Flujo recomendado antes de cada entrega

```bash
# 1. Compilar todo
./build.sh

# 2. Correr suite completa sin demo (rápido)
dotnet test --filter "FullyQualifiedName!~QuadruplesDemoTests"

# 3. Verificar ejemplos .patito (válidos e inválidos)
./compile-examples.sh --all

# 4. Revisar cuadruplos visualmente
./show-quads.sh

# 5. O todo de una vez:
./run-all.sh
```

---

## Test Cases de la VM (TC-VM-01 a TC-VM-09)

Ubicados en [`VirtualMachineTests.cs`](../tests/Patito.Tests/VirtualMachineTests.cs).  
El helper `Run(source)` compila y ejecuta la VM; los tests verifican `VmResult.Output`.

| ID | Nombre | Código fuente Patito | Cuádruplos clave | Output esperado | Resultado |
|----|--------|----------------------|-----------------|-----------------|-----------|
| TC-VM-01 | Print de constante entera | `escribe(42);` | `Print _ _ 42` | `42` | ✅ |
| TC-VM-02 | Asignación y print de variable | `x = 10; escribe(x);` | `Assign 10 _ x`, `Print _ _ x` | `10` | ✅ |
| TC-VM-03 | Condicional si/sino | `si (x > 5) { escribe("mayor"); } sino { escribe("menor o igual"); };` | `Gt x 5 t0`, `GotoF t0 _ ?`, `Print _ _ "mayor"`, `Goto _ _ ?`, `Print _ _ "menor o igual"` | `mayor` (x=7), `menor o igual` (x=3) | ✅ |
| TC-VM-04 | Ciclo mientras/haz | `mientras (i < 4) haz { escribe(i); i = i + 1; };` | `Lt i 4 t0`, `GotoF t0`, `Print _ _ i`, `Plus i 1 t1`, `Assign t1 _ i`, `Goto _ _ startLoop` | `0`, `1`, `2`, `3` | ✅ |
| TC-VM-05 | Función void con parámetros | `nula imprimir(n: entero)` llamada con `a=99` y literal `7` | `ERA imprimir`, `Param _ _ a`, `Gosub imprimir startQ`, `EndFunc` | `valor:`, `99`, `valor:`, `7` | ✅ |
| TC-VM-06 | Función con retorno vía global | `nula cuadrado(base: entero)` escribe `retval = base * base` | `Times base base t0`, `Assign t0 _ retval` | `36` (x=6), `9` (x=3) | ✅ |
| TC-VM-07 | Aritmética mixta ent+flot | `a + b`, `a * b`, `f + 1.5` | `Plus a b t0`, `Times a b t1`, `Plus f 1.5 t2` | `13`, `30`, `4` | ✅ |
| TC-VM-08 | `regresa` + llamada como factor | `entero doble(n)` con `regresa d;`; `resultado = doble(x) + 1;` y `doble(3) + doble(10)` | `Return d _ doble_ret`, `Gosub doble _ startQ`, `Assign doble_ret _ t0`, `Plus t0 1 t1` | `11` (doble(5)+1), `26` (doble(3)+doble(10)) | ✅ |
| TC-VM-09 | Fibonacci recursivo con `regresa` | `entero fib(k)` recursiva, `valor = fib(k-1) + fib(k-2); regresa valor;` | dos `Gosub fib` dentro de una expresión + `Return valor _ fib_ret` | `21` (fib(8)) | ✅ |

### Notas de implementación

- **TC-VM-01/02**: verifica que `BuildConstValues()` convierte `"42"` en el entero `42` y lo carga en `_globalMemory[25000]`.
- **TC-VM-03**: ejercita `GotoF` (salto condicional) y `Goto` (salto incondicional del si-body antes del sino).
- **TC-VM-04**: ejercita el loop `GotoF` → body → `Goto` → re-evaluar condición.
- **TC-VM-05**: ejercita el protocolo ERA → Param → Gosub → EndFunc. El valor del parámetro se copia al `LocalMemory` del frame.
- **TC-VM-06**: la función `cuadrado` escribe en la variable global `retval` porque no está sombreada por ninguna local. El llamador luego lee `retval` del scope global.
- **TC-VM-07**: la promoción int→double ocurre automáticamente en los helpers `ArithAdd`, `ArithMul`. `4.0.ToString(InvariantCulture)` produce `"4"`.
- **TC-VM-08**: cubre el camino completo de `regresa` (cuádruplo `Return` + dirección global `"{func}_ret"`) y, sobre todo, valida el **fix del bug de aliasing**: `doble(3) + doble(10)` debe dar `26` y no `40` — si `ExitFactorLlamada` reusara el placeholder compartido `"doble_ret"` como operando de la suma, el segundo `Gosub` pisaría el resultado del primero antes del `Plus`.
- **TC-VM-09**: caso recursivo real (Fibonacci) que ejercita `regresa` dentro de `si/sino` y dos llamadas a la misma función (`fib(k-1) + fib(k-2)`) en cada nivel de recursión — el escenario exacto que el fix de aliasing de TC-VM-08 protege, ahora con profundidad de pila no trivial. `fib(8) = 21`.

---

## Resumen de fallos esperados

Los archivos en `examples/` con prefijo `invalido_` están diseñados para fallar en distintas etapas del pipeline:

| Archivo                                  | Falla esperada en…  | Motivo                                              |
|------------------------------------------|----------------------|------------------------------------------------------|
| `invalido_01_falta_punto_coma.patito`    | Parser               | Falta `;` después del id del programa.               |
| `invalido_02_parentesis.patito`          | Parser               | Paréntesis sin cerrar en un `si (...`.               |
| `invalido_03_letrero_multilinea.patito`  | Lexer + Parser       | `LETRERO` con `\n` interno.                          |
| `invalido_04_caracter_invalido.patito`   | Lexer + Parser       | Carácter no reconocido (`@`).                        |
| `invalido_05_tipo_invalido.patito`       | Parser               | Tipo `cadena` no existe en Patito.                   |

Cada uno está cubierto por la prueba parametrizada `Ejemplos_Invalidos_Fallan`.
