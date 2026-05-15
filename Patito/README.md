# Patito Compiler — Entregas 1 + 2 (Scanner + Parser + Análisis Semántico)

Compilador del lenguaje **Patito** (Víctor Misael Escalante Alvarado, A01741176).

- **Entrega 1**: análisis léxico (scanner) y sintáctico (parser), generados con **ANTLR 4** sobre **C# / .NET 8+**.
- **Entrega 2**: cubo semántico, tabla de variables, directorio de funciones y puntos neurálgicos para llenarlos con todas las validaciones pertinentes (variable doblemente declarada, función redeclarada, identificador no declarado, etc.).

## Descripcion del lenguaje

![Reglas](img/Reglas.png)

## Estructura

```
Patito-Compiler/
├── Patito.sln
├── README.md
├── docs/                                    Documentacion de las entregas
│   ├── entrega2_analisis_semantico.md       Resumen general de Entrega 2
│   ├── cubo_semantico.md                    Tabla de consideraciones semanticas
│   ├── estructuras.md                       Estructuras elegidas y operaciones
│   └── puntos_neuralgicos.md                Listener: cada Enter/Exit y su validacion
├── examples/                                Programas .patito de prueba
│   ├── 01_minimo.patito                    Casos validos
│   ├── 02_vars_y_asigna.patito
│   ├── 03_condicion.patito
│   ├── 04_ciclo.patito
│   ├── 05_funcion.patito
│   ├── 06_expresiones.patito
│   ├── 07_comentarios.patito
│   ├── invalido_01_falta_punto_coma.patito  Casos invalidos (deben fallar)
│   ├── invalido_02_parentesis.patito
│   ├── invalido_03_letrero_multilinea.patito
│   ├── invalido_04_caracter_invalido.patito
│   └── invalido_05_tipo_invalido.patito
├── src/Patito.Compiler/
│   ├── Patito.Compiler.csproj               Proyecto de consola con ANTLR4
│   ├── Patito.g4                            Gramatica unificada (lexer + parser)
│   ├── Program.cs                           Driver CLI (patitoc)
│   ├── PatitoFrontEnd.cs                    API in-process (corre scanner+parser+semantica)
│   ├── PatitoErrorListener.cs               Captura de errores lexicos/sintacticos
│   └── Semantic/                            ENTREGA 2
│       ├── SemanticType.cs                  Enum de tipos (Entero, Flotante, Bool, Nula, Error)
│       ├── SemanticOp.cs                    Operadores binarios + asignacion
│       ├── SemanticCube.cs                  Cubo semantico (tabla de compatibilidad de tipos)
│       ├── Symbol.cs                        Entrada de la tabla de variables
│       ├── VariableTable.cs                 Tabla de variables por alcance
│       ├── FunctionInfo.cs                  Firma + tabla local de una funcion
│       ├── FunctionDirectory.cs             Directorio de funciones (uno por programa)
│       ├── SemanticError.cs                 Error semantico con codigo estable
│       └── SemanticAnalyzer.cs              Listener con los puntos neuralgicos
└── tests/Patito.Tests/
    ├── Patito.Tests.csproj                  xUnit
    ├── ScannerTests.cs                      Pruebas lexicas (tokens, longest-match)
    ├── ParserTests.cs                       Pruebas sintacticas (validos / invalidos)
    ├── SemanticCubeTests.cs                 Verifica todas las celdas del cubo
    ├── VariableTableTests.cs                Pruebas unitarias de VariableTable
    ├── FunctionDirectoryTests.cs            Pruebas unitarias de FunctionDirectory
    └── SemanticAnalyzerTests.cs             Pruebas E2E del analisis semantico
```

## Cómo construir y correr

Requisitos: **.NET 8 SDK o superior** (`dotnet --version` → 8.x o mayor). El proyecto está configurado para `net10.0`; ajuste `TargetFramework` en los `.csproj` si necesita una versión menor.

ANTLR4 se descarga automáticamente vía NuGet (`Antlr4.Runtime.Standard` + `Antlr4BuildTasks`); no necesitas instalar Java.

```bash
# Restaurar y compilar (Patito.g4 -> *.cs se hace en este paso)
dotnet build

# Correr el ejecutable contra un archivo
dotnet run --project src/Patito.Compiler -- examples/02_vars_y_asigna.patito --tokens --tree

# Imprimir solo el directorio de funciones y las tablas
dotnet run --project src/Patito.Compiler -- examples/05_funcion.patito --symbols

# Demo embebido (sin archivos)
dotnet run --project src/Patito.Compiler -- --demo

# Correr la suite de pruebas (lex + parse + semantica)
dotnet test

# Corre los test pero en una tabla comparativa
source test-samples.sh
```

## Salida del CLI

```
patitoc <archivo.patito>            análisis léxico + sintáctico + semántico
patitoc <archivo.patito> --tokens   imprime además la lista de tokens
patitoc <archivo.patito> --tree     imprime además el parse tree y las tablas
patitoc <archivo.patito> --symbols  imprime la tabla global + directorio de funciones
patitoc --demo                      programa embebido (CI/smoke)
```

Categorías de errores reportados:

- `[LEX]` — error léxico (scanner).
- `[PARSE]` — error sintáctico (parser).
- `[SEM]` — error semántico (Entrega 2: doble declaración, identificador no declarado, etc.).

## Documentación de la Entrega 2

La documentación a profundidad vive en [`docs/`](docs/):

- [`entrega2_analisis_semantico.md`](docs/entrega2_analisis_semantico.md) — visión general.
- [`cubo_semantico.md`](docs/cubo_semantico.md) — la tabla de consideraciones semánticas (tipos × operadores → resultado).
- [`estructuras.md`](docs/estructuras.md) — qué estructuras se eligieron para `VariableTable` y `FunctionDirectory`, por qué, y qué operaciones se aplican sobre ellas.
- [`puntos_neuralgicos.md`](docs/puntos_neuralgicos.md) — cada `Enter…`/`Exit…` del listener mapeado a su acción semántica y a las validaciones que aplica.
