# Plan de Pruebas

Este documento consolida el **plan de pruebas** del compilador Patito a lo largo de las tres entregas. Cada caso se traduce en al menos un test de xUnit dentro del proyecto [`../tests/Patito.Tests`](../tests/Patito.Tests), de modo que `dotnet test` ejecuta toda la suite.

El diseño del plan persigue cuatro objetivos:

- **Pruebas dirigidas:** cada regla léxica, sintáctica o semántica clave tiene al menos un test que la ejercita.
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

## Cómo correr las pruebas

```bash
# Toda la suite (Scanner + Parser + Semántica)
dotnet test

# Solo un archivo
dotnet test --filter "FullyQualifiedName~SemanticAnalyzerTests"

# Solo una prueba
dotnet test --filter "FullyQualifiedName~Funcion_Nula_ConParametros_Pasa"
```

También está disponible el script [`../test-samples.sh`](../test-samples.sh) que ejecuta el binario `patitoc` contra cada archivo de [`../examples/`](../examples) e imprime una tabla comparativa "esperado vs. obtenido". Es útil como *smoke-test* visual antes de cada entrega.

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
