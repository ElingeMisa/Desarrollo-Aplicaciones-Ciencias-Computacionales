# Herramientas de generación automática

Antes de implementar el scanner y el parser de Patito se evaluaron las herramientas disponibles para generarlos automáticamente. Escribirlos a mano (en C# puro) habría duplicado el esfuerzo y dejado las reglas mezcladas con la lógica de error, por lo que se descartó esa vía desde el inicio.

Esta investigación corresponde a la **Tarea 2 / Entrega 1** del proyecto y dejó como conclusión la elección de **ANTLR 4**.

## Candidatos evaluados

Se compararon tres familias de generadores que cubren los casos de uso típicos en proyectos de compiladores:

1. **Flex + Bison** — el estándar histórico del mundo C/UNIX.
2. **GPLEX + GPPG** — la traducción de Flex/Bison al ecosistema .NET.
3. **ANTLR 4** — un generador moderno con runtime oficial multilenguaje.

La siguiente tabla resume el análisis bajo los criterios más relevantes para implementar el front-end en **C# / .NET**.

| Criterio                  | Flex & Bison                          | GPLEX & GPPG                         | ANTLR 4                                  |
|---------------------------|---------------------------------------|--------------------------------------|------------------------------------------|
| Lenguaje generado         | C / C++                               | C# (.NET nativo)                     | C#, Java, Python, JS, Go, …              |
| Algoritmo de parser       | LALR(1) (+ GLR opcional)              | LALR(1)                              | ALL(*) adaptativo                        |
| Recursión izquierda       | Soportada                             | Soportada                            | Soportada (transformada)                 |
| Formato de entrada        | `.l` + `.y` separados                 | `.lex` + `.y` separados              | `.g4` unificado (lexer + parser)         |
| Integración C#/MSBuild    | No directa (requiere wrapper)         | YaccLexTools NuGet                   | `Antlr4BuildTasks` NuGet (auto)          |
| Soporte Unicode           | Limitado a 8-bit                      | Completo (21-bit)                    | Completo (21-bit)                        |
| Documentación general     | Abundante pero antigua                | Limitada, repos discontinuos         | Extensa, libro oficial, wiki activo      |
| Documentación C#          | Prácticamente nula (es C/C++)         | Pequeña, MIT pero baja actividad     | Grande, runtime oficial mantenido        |
| Licencia                  | BSD modificada / GPL3+ con excepción  | MIT                                  | BSD-3-Clause                             |
| IDE / herramientas        | CLI, plugins de editor                | CLI, reporte HTML de conflictos      | CLI, ANTLRWorks, plugins VS Code/IntelliJ |

## Por qué se descartaron Flex+Bison y GPLEX+GPPG

**Flex y Bison** generan código C que requiere un puente P/Invoke para integrarse en .NET. Aunque la integración es técnicamente posible, agrega complejidad al pipeline de `dotnet build`, complica el debugging (los breakpoints atraviesan dos runtimes) y obliga a mantener archivos `.l` y `.y` separados que sincronizar. Para un compilador académico el costo de mantenimiento no compensa.

**GPLEX y GPPG** son la traducción directa de Flex/Bison al ecosistema .NET. Su API es limpia y la integración con MSBuild funciona vía el paquete `YaccLexTools`, pero la **comunidad y la documentación son muy desiguales**: muchos repos llevan años sin actividad, los ejemplos suelen apuntar a versiones antiguas del SDK y, ante un error de plantilla, hay poca documentación de referencia. Para un proyecto que se va a extender entrega por entrega, ese vacío de soporte es un riesgo.

## ANTLR 4 — la elección

Se selecciona **ANTLR 4** como generador del front-end de Patito. Las razones son las siguientes:

### 1. Integración limpia con .NET

El paquete NuGet `Antlr4BuildTasks` añade el archivo `.g4` al ciclo normal de `dotnet build`. Cada compilación regenera `PatitoLexer.cs`, `PatitoParser.cs` y `PatitoBaseListener.cs` automáticamente. **No hay paso manual de generación ni Java instalado** en la máquina del desarrollador; el paquete trae su propio binario.

En el `.csproj` la integración se reduce a:

```xml
<ItemGroup>
  <PackageReference Include="Antlr4.Runtime.Standard" Version="4.13.1" />
  <PackageReference Include="Antlr4BuildTasks"        Version="12.8.0" PrivateAssets="all" />
</ItemGroup>

<ItemGroup>
  <Antlr4 Include="Patito.g4">
    <Package>Patito.Compiler.Generated</Package>
    <Listener>true</Listener>
    <Visitor>false</Visitor>
  </Antlr4>
</ItemGroup>
```

### 2. Gramática unificada en un solo archivo `.g4`

`Patito.g4` contiene en el mismo archivo las reglas del **lexer** (mayúsculas) y del **parser** (minúsculas). Esto reduce el costo cognitivo frente a tener `.l` y `.y` separados y mantiene cerca las definiciones que conceptualmente lo están (por ejemplo, `OP_EQ` y la regla `rel_op` que lo usa).

### 3. Algoritmo ALL(*) y manejo de recursión izquierda

ALL(*) realiza *lookahead* adaptativo, lo que permite escribir gramáticas tal como se piensan, sin reescritura para evitar conflictos `shift/reduce` ni LL(*k*) fijo. Esto es especialmente útil al añadir el manejador de errores y la generación de código intermedio en entregas futuras: una nueva producción no obliga a reorganizar reglas existentes.

### 4. Documentación y herramientas

ANTLR4 cuenta con:

- El libro de Terence Parr (*"The Definitive ANTLR 4 Reference"*).
- Ejemplos oficiales en GitHub para cada *target*, incluido C#.
- Herramientas visuales como **ANTLR Lab** y la extensión de **VS Code** que muestran el *parse tree* en vivo durante el desarrollo.

GPLEX/GPPG no tienen un equivalente con el mismo nivel de soporte.

## Cómo se organiza el archivo `.g4`

El archivo [`../src/Patito.Compiler/Patito.g4`](../src/Patito.Compiler/Patito.g4) sigue una estructura recomendada por la guía oficial:

1. **Cabecera** — declara el nombre con `grammar Patito;`.
2. **Reglas del parser** (minúsculas) — empezando por la regla raíz `programa` y bajando hasta los terminales más simples (`simple_atom`, `cte`).
3. **Reglas del lexer** (MAYÚSCULAS) — en este orden:
   1. Palabras reservadas (antes que `ID`).
   2. Operadores y delimitadores, con los de dos caracteres antes que los de uno.
   3. Constantes, con `CTE_FLOT` antes de `CTE_ENT`.
   4. `ID` y `LETRERO`.
4. **Fragments y reglas `-> skip`** — `WS`, `COMMENT_LINE`, `COMMENT_BLOCK`.

El lexer aplica la regla **longest-match** y, ante empates, prefiere la primera regla declarada. Por eso las palabras reservadas se definen **antes** que la regla `ID` y las versiones de dos caracteres (`==`, `!=`) antes de las de uno (`=`).

## Cómo se invoca el front-end desde el código

En tiempo de ejecución, el flujo del compilador es:

```
PatitoFrontEnd.Compile(source)
  ├── 1. PatitoLexer        ← Scanner (genera tokens)
  ├── 2. PatitoParser       ← Parser (genera parse tree)
  └── 3. SemanticAnalyzer   ← Listener semántico + generación de cuádruplos (Entrega 2 y 3)
```

El archivo [`../src/Patito.Compiler/PatitoFrontEnd.cs`](../src/Patito.Compiler/PatitoFrontEnd.cs) encadena las tres fases y devuelve un `CompileResult` con los tokens, el árbol, la lista de errores léxicos, sintácticos y semánticos, una referencia al `SemanticAnalyzer` ya poblado y la fila de cuádruplos (ver [`estructuras.md`](estructuras.md) y [`puntos_neuralgicos.md`](puntos_neuralgicos.md)).

### Campos relevantes de `CompileResult` (Entrega 3)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Semantic` | `SemanticAnalyzer?` | Analizador con tablas y cubo populados. `null` si el parser falló. |
| `Quads` | `IReadOnlyList<Quadruple>?` | Fila de cuádruplos generados. Acceso directo a `Semantic.Quads`. |
| `Success` | `bool` | `true` si no hay errores léxicos, sintácticos ni semánticos. |

### Flags del CLI (Entrega 3)

El driver de línea de comandos (`Program.cs`) acepta los siguientes flags:

| Flag | Descripción |
|------|-------------|
| `--tokens`  | Imprime la lista de tokens con número, línea, columna y texto. |
| `--tree`    | Imprime el parse tree en formato Lisp-like y las tablas de símbolos. |
| `--symbols` | Imprime la tabla global y el directorio de funciones. |
| `--quads`   | Imprime la fila de cuádruplos generados en formato tabular: `# Op Left Right Result`. |
| `--demo`    | Ejecuta un programa Patito embebido y muestra tokens + cuádruplos. |

Ejemplo de salida de `--quads`:

```
=== Fila de Cuádruplos ===
   #  Op        Left          Right         Result
------------------------------------------------------------
   0  =         10            _             x
   1  +         x             5             t0
   2  =         t0            _             y
   3  =         3.14          _             z
   4  >         x             y             t1
   5  GotoF     t1            _             8
   6  Print     _             _             "x es mayor"
   7  Goto      _             _             10
   8  Print     _             _             "y es mayor o igual"
   9  Print     _             _             y
  10  <         x             100           t2
  11  GotoF     t2            _             14
  12  +         x             1             t3
  13  =         t3            _             x
  14  Goto      _             _             10
```

## Conclusión

ANTLR4 ganó por una combinación de **integración limpia con .NET**, **un solo archivo de gramática**, **algoritmo de parsing flexible** y **documentación abundante**. Las alternativas no eran inviables, pero cada una implicaba un costo adicional (puente P/Invoke en el caso de Flex/Bison o soporte limitado en el de GPLEX/GPPG) que no se justificaba para un proyecto que se va a extender entrega por entrega.
