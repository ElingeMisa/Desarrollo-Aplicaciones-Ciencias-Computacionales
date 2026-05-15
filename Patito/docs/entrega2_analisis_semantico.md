# Entrega 2 — Análisis Semántico (Patito)

Autor: Víctor Misael Escalante Alvarado, A01741176
Fecha: mayo 2026

Esta entrega construye, sobre el front-end ya existente (scanner + parser de ANTLR4), las **estructuras y validaciones semánticas** del lenguaje **Patito**:

1. Cubo semántico.
2. Tabla de variables (por alcance) y Directorio de funciones (uno por programa).
3. Puntos neurálgicos que crean y llenan ambas estructuras durante el recorrido del árbol de derivación, con las validaciones pertinentes.
4. Documentación de las estructuras elegidas y de las operaciones expuestas.

Los detalles a profundidad están repartidos en tres documentos complementarios:

- [`cubo_semantico.md`](cubo_semantico.md) — tabla de consideraciones semánticas (resultado y compatibilidad de tipos por operador).
- [`estructuras.md`](estructuras.md) — diseño de `VariableTable`, `Symbol`, `FunctionInfo` y `FunctionDirectory`, con la justificación de la elección de estructura para cada uno.
- [`puntos_neuralgicos.md`](puntos_neuralgicos.md) — cada `Enter…` del listener `SemanticAnalyzer` mapeado a la acción semántica que realiza y a la validación que aplica.

## Cómo correr el análisis semántico

El analizador se ejecuta automáticamente después del parser. La CLI ahora reporta tres categorías de errores: `[LEX]`, `[PARSE]` y `[SEM]`.

```bash
# Compila el programa y muestra el directorio + tablas
dotnet run --project src/Patito.Compiler -- examples/05_funcion.patito --symbols

# El comando --tree imprime el parse tree y al final también las tablas.
dotnet run --project src/Patito.Compiler -- examples/05_funcion.patito --tree

# Ejecutar todas las pruebas (incluye las semánticas nuevas).
dotnet test
```

Un programa válido termina con un resumen como:

```
[OK] examples/05_funcion.patito: 78 tokens, 1 variable(s) global(es), 1 funcion(es) declarada(s).
```

Un programa con error semántico (p.ej. variable doblemente declarada) termina con:

```
[SEM] [<archivo>] Linea 4, Columna 5: [VariableRedeclared] Variable 'x' ya fue declarado en alcance '<global>' (declaracion previa en 3:5).
[FAIL] ...: 0 error(es) lexico(s), 0 error(es) sintactico(s), 1 error(es) semantico(s).
```

## Resumen de archivos nuevos

```
src/Patito.Compiler/Semantic/
├── SemanticType.cs        Enum de tipos (Entero, Flotante, Bool, Nula, Error).
├── SemanticOp.cs          Enum de operadores binarios + asignacion.
├── SemanticCube.cs        Cubo semantico (Dictionary<(T, op, T), T>).
├── Symbol.cs              Entrada de la tabla de variables.
├── VariableTable.cs       Dictionary<string, Symbol> por alcance.
├── FunctionInfo.cs        Tipo de retorno + parametros + tabla local.
├── FunctionDirectory.cs   Dictionary<string, FunctionInfo> (uno por programa).
├── SemanticError.cs       Reporte de error semantico con codigo estable.
└── SemanticAnalyzer.cs    Listener ANTLR con los puntos neuralgicos.

tests/Patito.Tests/
├── SemanticCubeTests.cs       Verifica todas las celdas del cubo.
├── VariableTableTests.cs      Pruebas unitarias de la tabla.
├── FunctionDirectoryTests.cs  Pruebas unitarias del directorio.
└── SemanticAnalyzerTests.cs   Pruebas end-to-end (programa Patito -> errores).
```

Y se modificaron:

- `src/Patito.Compiler/PatitoFrontEnd.cs` — corre el `SemanticAnalyzer` después del parser y agrega `SemanticErrors` y `Semantic` al `CompileResult`.
- `src/Patito.Compiler/Program.cs` — reporta `[SEM]` y soporta la opción `--symbols` para imprimir la tabla global y el directorio de funciones.
- `README.md` — actualizado a Entrega 2.
