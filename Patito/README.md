# Compilador Patito — Entregas 0 a 5

Compilador completo del lenguaje **Patito** (Víctor Misael Escalante Alvarado, A01741176).

| Entrega | Alcance |
|---------|---------|
| **0** | Definición del lenguaje: expresiones regulares, BNF y diagramas de sintaxis. |
| **1** | Front-end con **ANTLR 4** sobre **C# / .NET 10** — scanner y parser. |
| **2** | Análisis semántico: cubo semántico, tabla de variables, directorio de funciones y puntos neurálgicos con todas las validaciones de tipos e identificadores. |
| **3** | Generación de cuádruplos para expresiones, asignaciones, `si`/`sino` y `mientras`. |
| **4** | Generación de cuádruplos para declaración e invocación de funciones (ERA / Param / Gosub / EndFunc). |
| **5** | Direcciones virtuales completas + **Máquina Virtual** que interpreta todos los cuádruplos de código intermedio. |

## Documentación

Toda la documentación técnica vive en **[`docs/`](docs/)**. El [índice principal](docs/README.md) lleva a cada página, organizadas por **tema**:

| Tema | Documento |
|------|-----------|
| Descripción del lenguaje | [`docs/lenguaje.md`](docs/lenguaje.md) |
| Análisis léxico (regex, tokens) | [`docs/lexico.md`](docs/lexico.md) |
| Gramática (BNF y `.g4`) | [`docs/gramatica.md`](docs/gramatica.md) |
| Comparación y elección de tooling | [`docs/herramientas.md`](docs/herramientas.md) |
| Cubo semántico | [`docs/cubo_semantico.md`](docs/cubo_semantico.md) |
| Estructuras (tablas, directorio, pilas, emitter, VM) | [`docs/estructuras.md`](docs/estructuras.md) |
| Puntos neurálgicos del listener | [`docs/puntos_neuralgicos.md`](docs/puntos_neuralgicos.md) |
| Cuádruplos de código intermedio | [`docs/cuadruplos.md`](docs/cuadruplos.md) |
| Mapa de Direcciones Virtuales | [`docs/direcciones_virtuales.md`](docs/direcciones_virtuales.md) |
| **Memoria de Ejecución y Máquina Virtual** | [`docs/memoria_ejecucion.md`](docs/memoria_ejecucion.md) |
| Plan de pruebas | [`docs/pruebas.md`](docs/pruebas.md) |

## Descripción del lenguaje

![Reglas](img/Reglas.png)

Para los detalles, ver [`docs/lenguaje.md`](docs/lenguaje.md).

## Estructura del repositorio

```
Patito/
├── Patito.sln
├── README.md                                Este archivo
├── docs/                                    Documentación técnica, por tema
│   ├── README.md                            Índice principal
│   ├── lenguaje.md
│   ├── lexico.md
│   ├── gramatica.md
│   ├── herramientas.md
│   ├── cubo_semantico.md
│   ├── estructuras.md
│   ├── directorio_y_tablas.md
│   ├── puntos_neuralgicos.md
│   ├── cuadruplos.md
│   ├── direcciones_virtuales.md
│   ├── memoria_ejecucion.md                 NUEVO — Entrega 5
│   └── pruebas.md
├── examples/
│   ├── 01_minimo.patito … 14_cuadruplos_funciones.patito   Casos válidos
│   ├── 15_maquina_virtual.patito            NUEVO — demo completo de la VM
│   └── invalido_01 … invalido_11.patito     Casos inválidos (deben fallar)
├── src/Patito.Compiler/
│   ├── Patito.Compiler.csproj
│   ├── Patito.g4                            Gramática unificada (lexer + parser)
│   ├── Program.cs                           Driver CLI (patitoc)
│   ├── PatitoFrontEnd.cs                    API in-process (compilar + ejecutar)
│   ├── PatitoErrorListener.cs
│   ├── Semantic/
│   │   ├── SemanticType.cs
│   │   ├── SemanticOp.cs
│   │   ├── SemanticCube.cs
│   │   ├── Symbol.cs
│   │   ├── VariableTable.cs
│   │   ├── FunctionInfo.cs
│   │   ├── FunctionDirectory.cs
│   │   ├── SemanticError.cs
│   │   └── SemanticAnalyzer.cs              Listener con todos los puntos neurálgicos
│   ├── CodeGen/
│   │   ├── QuadOp.cs
│   │   ├── Quadruple.cs
│   │   ├── FilaCuadruplos.cs
│   │   ├── PilaOperadores.cs
│   │   ├── PilaOperandos.cs
│   │   ├── PilaTipos.cs
│   │   ├── QuadrupleEmitter.cs
│   │   └── VirtualMemoryMap.cs
│   └── VM/                                  NUEVO — Entrega 5
│       ├── ExecutionMemory.cs               Almacén dirección → valor en tiempo de ejecución
│       ├── ActivationRecord.cs              Frame de la pila de llamadas
│       ├── VirtualMachine.cs                Intérprete de cuádruplos
│       └── VmResult.cs                      Resultado de ejecución (output, error, success)
└── tests/Patito.Tests/
    ├── Patito.Tests.csproj
    ├── ScannerTests.cs
    ├── ParserTests.cs
    ├── SemanticCubeTests.cs
    ├── VariableTableTests.cs
    ├── FunctionDirectoryTests.cs
    ├── SemanticAnalyzerTests.cs
    ├── CodeGenTests.cs
    ├── QuadruplesDemoTests.cs
    └── VirtualMachineTests.cs               TC-VM-01 a TC-VM-09 (incluye `regresa` y aliasing en recursión)
```

## Cómo construir y ejecutar

**Requisitos:** .NET 10 SDK (`dotnet --version` → 10.x). ANTLR4 se descarga automáticamente vía NuGet.

```bash
# Compilar (genera lexer/parser desde Patito.g4)
dotnet build

# Analizar un archivo (léxico + sintáctico + semántico)
dotnet run --project src/Patito.Compiler -- examples/05_funcion.patito

# Imprimir tokens
dotnet run --project src/Patito.Compiler -- examples/05_funcion.patito --tokens

# Imprimir árbol de derivación y tablas de símbolos
dotnet run --project src/Patito.Compiler -- examples/05_funcion.patito --tree

# Imprimir tabla global y directorio de funciones
dotnet run --project src/Patito.Compiler -- examples/05_funcion.patito --symbols

# Imprimir fila de cuádruplos con direcciones virtuales
dotnet run --project src/Patito.Compiler -- examples/14_cuadruplos_funciones.patito --quads

# Compilar Y ejecutar con la Máquina Virtual
dotnet run --project src/Patito.Compiler -- examples/15_maquina_virtual.patito --run

# Demo embebido (smoke test sin archivos)
dotnet run --project src/Patito.Compiler -- --demo
```

## Suite de pruebas

```bash
# Toda la suite
dotnet test

# Solo test cases de la Máquina Virtual
dotnet test --filter "FullyQualifiedName~VirtualMachineTests" -v normal

# Demo visual de cuádruplos
dotnet test --filter "FullyQualifiedName~QuadruplesDemoTests" -v normal

# Sin demos (más rápido en CI)
dotnet test --filter "FullyQualifiedName!~QuadruplesDemoTests"
```

| Clase | Tests | Cubre |
|-------|-------|-------|
| `ScannerTests` | 12 | Tokens, longest-match, comentarios |
| `ParserTests` | 14 | Producciones, precedencia, archivos |
| `SemanticCubeTests` | 9 | Todas las celdas del cubo |
| `VariableTableTests` | 4 | Declarar, duplicado, lookup |
| `FunctionDirectoryTests` | 4 | Directorio de funciones |
| `SemanticAnalyzerTests` | 13 | Semántica end-to-end |
| `CodeGenTests` | 24 | PN-8 a PN-18, Backfill |
| `QuadruplesDemoTests` | 12 | Demo visual con cuádruplos |
| **`VirtualMachineTests`** | **9** | **TC-VM-01 a TC-VM-09 — VM completa, incluyendo `regresa`/`Return` y el fix de aliasing en llamadas recursivas (`fib`)** |

## Mensajes del CLI

| Prefijo | Origen |
|---------|--------|
| `[LEX]` | Error léxico (scanner) |
| `[PARSE]` | Error sintáctico (parser) |
| `[SEM]` | Error semántico (doble declaración, tipo incompatible, etc.) |
| `[VM ERROR]` | Excepción en tiempo de ejecución de la Máquina Virtual |

## Repositorio remoto

<https://github.com/ElingeMisa/Desarrollo-Aplicaciones-Ciencias-Computacionales>
