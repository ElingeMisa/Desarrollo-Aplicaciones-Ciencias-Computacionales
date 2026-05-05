# Patito Compiler — Entrega 1 (Scanner + Parser)

Compilador del lenguaje **Patito** (Victor Misael Escalante Alvarado, A01741176).
Esta entrega cubre el **front-end**: análisis léxico (scanner) y análisis
sintáctico (parser), generados con **ANTLR 4** sobre **C# / .NET 8**.

## Estructura

```
Patito-Compiler/
├── Patito.sln
├── README.md
├── examples/                       Programas .patito de prueba
│   ├── 01_minimo.patito            Casos válidos
│   ├── 02_vars_y_asigna.patito
│   ├── 03_condicion.patito
│   ├── 04_ciclo.patito
│   ├── 05_funcion.patito
│   ├── 06_expresiones.patito
│   ├── 07_comentarios.patito
│   ├── invalido_01_falta_punto_coma.patito   Casos inválidos (deben fallar)
│   ├── invalido_02_parentesis.patito
│   ├── invalido_03_letrero_multilinea.patito
│   ├── invalido_04_caracter_invalido.patito
│   └── invalido_05_tipo_invalido.patito
├── src/Patito.Compiler/
│   ├── Patito.Compiler.csproj      Proyecto de consola con ANTLR4
│   ├── Patito.g4                   Gramática unificada (lexer + parser)
│   ├── Program.cs                  Driver CLI (patitoc)
│   ├── PatitoFrontEnd.cs           API in-process para tests
│   └── PatitoErrorListener.cs      Captura de errores léxicos/sintácticos
└── tests/Patito.Tests/
    ├── Patito.Tests.csproj         xUnit
    ├── ScannerTests.cs             Pruebas léxicas (tokens, longest-match)
    └── ParserTests.cs              Pruebas sintácticas (válidos / inválidos)
```

## Cómo construir y correr

Requisitos: **.NET 8 SDK** (`dotnet --version` → 8.x).
ANTLR4 se descarga automáticamente vía NuGet (`Antlr4.Runtime.Standard` + `Antlr4BuildTasks`); no necesitas instalar Java.

```bash
# Restaurar y compilar (Patito.g4 -> *.cs se hace en este paso)
dotnet build

# Correr el ejecutable contra un archivo
dotnet run --project src/Patito.Compiler -- examples/02_vars_y_asigna.patito --tokens --tree

# Demo embebido (sin archivos)
dotnet run --project src/Patito.Compiler -- --demo

# Correr la suite de pruebas
dotnet test
```

## Salida del CLI

```
patitoc <archivo.patito>            análisis léxico + sintáctico
patitoc <archivo.patito> --tokens   imprime además la lista de tokens
patitoc <archivo.patito> --tree     imprime además el parse tree
patitoc --demo                      programa embebido (CI/smoke)
```
