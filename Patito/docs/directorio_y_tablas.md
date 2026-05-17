# Directorio de Funciones y Tablas de Variables

> Documentación complementaria de la **Entrega 2**. Ver el [índice general](README.md) para más contexto, y [`estructuras.md`](estructuras.md) para la justificación de la elección de estructuras de datos y el ciclo de vida completo.

Este documento describe las estructuras de datos que representan el **Directorio de Funciones** y las **Tablas de Variables** de Patito, con un enfoque en los campos concretos que guardan, sus relaciones y un ejemplo paso a paso de cómo se pueblan a partir de código fuente real.

---

## Modelo de dos niveles de alcance

Patito define exactamente **dos niveles de alcance**:

1. **Global** — variables declaradas antes de las funciones y accesibles desde todo el programa.
2. **Local por función** — parámetros y variables declaradas dentro de cada `func_body`; solo son visibles dentro de esa función.

Ese modelo se implementa con tres clases centrales:

```
FunctionDirectory
 ├─ ProgramName : string?
 ├─ GlobalTable : VariableTable          ← alcance global
 └─ _functions  : Dictionary<string, FunctionInfo>
                    └─ FunctionInfo
                        ├─ Name, ReturnType, ParameterTypes, Line, Column
                        └─ LocalTable : VariableTable   ← alcance local
                                          └─ _symbols : Dictionary<string, Symbol>
```

---

## Directorio de Funciones (`FunctionDirectory`)

### Estructura

El directorio es una tabla **única por programa** implementada como `Dictionary<string, FunctionInfo>` con clave el identificador de la función.

| Campo / Propiedad | Tipo C# | Descripción |
|---|---|---|
| `ProgramName` | `string?` | Identificador que aparece después de la palabra reservada `programa`. Se guarda para rechazar funciones con el mismo nombre. |
| `GlobalTable` | `VariableTable` | Tabla de variables del alcance global. |
| `_functions`  | `Dictionary<string, FunctionInfo>` | Mapa de nombre → metadatos para cada función declarada. |
| `Count`       | `int` | Número de funciones declaradas (no incluye el programa). |
| `Functions`   | `IEnumerable<FunctionInfo>` | Iterador sobre todas las funciones (en orden de declaración). |

### Operaciones expuestas

| Método | Complejidad | Propósito |
|---|---|---|
| `TryDeclare(FunctionInfo)` | O(1) prom. | Registra la función. Devuelve `false` si ya existía con ese nombre. |
| `TryLookup(name, out info)` | O(1) prom. | Resuelve el nombre al hacer una llamada a función. |
| `Lookup(name)` | O(1) prom. | Variante que devuelve `null` en lugar de un `bool`. |
| `Contains(name)` | O(1) prom. | Solo verifica presencia. |

---

## Tablas de Variables (`VariableTable` + `Symbol`)

### Estructura de `VariableTable`

Hay **una instancia por alcance**: una para los globales del programa y una por cada función. La estructura subyacente es `Dictionary<string, Symbol>` con clave el identificador de la variable.

| Campo / Propiedad | Tipo C# | Descripción |
|---|---|---|
| `ScopeName` | `string` | Nombre descriptivo del alcance (`"<global>"` o el nombre de la función). Solo para diagnóstico. |
| `_symbols`  | `Dictionary<string, Symbol>` | Mapa de nombre → datos del símbolo. |
| `Count`     | `int` | Número de símbolos en este alcance. |
| `Symbols`   | `IEnumerable<Symbol>` | Iterador en orden de inserción (útil para asignar direcciones consecutivas en Entrega 3). |

### Operaciones expuestas

| Método | Complejidad | Propósito |
|---|---|---|
| `TryDeclare(Symbol)` | O(1) prom. | Agrega el símbolo si no existe. `false` = variable ya declarada. |
| `TryLookup(name, out symbol)` | O(1) prom. | Busca por nombre; indica si se encontró. |
| `Lookup(name)` | O(1) prom. | Devuelve el símbolo o `null`. |
| `Contains(name)` | O(1) prom. | Solo verifica presencia. |

### Estructura de `Symbol`

Cada entrada de la tabla es un `sealed record` **inmutable** con los siguientes campos:

| Campo | Tipo C# | Descripción |
|---|---|---|
| `Name` | `string` | Lexema declarado en la fuente (ej. `"contador"`). |
| `Type` | `SemanticType` | Tipo del símbolo: `Entero` o `Flotante`. |
| `Kind` | `SymbolKind` | `Variable` (declaración en `vars`) o `Parameter` (parámetro de función). |
| `Line` | `int` | Línea de declaración en la fuente (para mensajes de error). |
| `Column` | `int` | Columna de declaración. |
| `Address` | `int` | Dirección de memoria virtual. Vale `-1` hasta la Entrega 3. |

El uso de `record` garantiza inmutabilidad: si en la Entrega 3 hay que asignar una dirección, se crea un nuevo `Symbol` con `with { Address = n }` en lugar de mutar el original.

### `SymbolKind`

```csharp
public enum SymbolKind { Variable, Parameter }
```

- **`Variable`** — declarado en una sección `vars`.
- **`Parameter`** — declarado como parámetro de una función. Vive en la `LocalTable` de esa función junto con las variables locales.

---

## Entrada del directorio: `FunctionInfo`

Cada función registrada en `FunctionDirectory` está representada por un `FunctionInfo`:

| Campo | Tipo C# | Descripción |
|---|---|---|
| `Name` | `string` | Identificador de la función (ej. `"sumarHasta"`). |
| `ReturnType` | `SemanticType` | Tipo de retorno: `Entero`, `Flotante` o `Nula` (sin retorno). |
| `ParameterTypes` | `List<SemanticType>` | Tipos de los parámetros **en orden de declaración**. El orden es obligatorio para validar las llamadas posicionalmente. |
| `LocalTable` | `VariableTable` | Tabla de variables del alcance local; contiene parámetros y locales. |
| `Line`, `Column` | `int` | Posición de la declaración en la fuente. |
| `StartQuad` | `int` | Índice del primer cuádruplo de la función. Vale `-1` hasta la Entrega 3. |

---

## Ejemplo concreto

Dado el siguiente programa Patito:

```
programa miProg;
vars
  x, y : entero;
  pi    : flotante;
funcs
  func sumarHasta(n : entero) : entero
  vars
    i, acc : entero;
  {
    acc = 0;
    ciclo { i = i + 1; acc = acc + i; } mientras (i < n);
    regresa acc;
  };
principal() {}
fin
```

El estado de las estructuras al finalizar el análisis semántico es el siguiente:

### `FunctionDirectory`

```
ProgramName : "miProg"
GlobalTable : VariableTable("<global>")    ← ver abajo
_functions  :
  "sumarHasta" → FunctionInfo(...)         ← ver abajo
```

### `GlobalTable` (alcance global)

| Name | Type    | Kind     | Line | Column | Address |
|------|---------|----------|------|--------|---------|
| `x`  | Entero  | Variable | 3    | 3      | -1      |
| `y`  | Entero  | Variable | 3    | 6      | -1      |
| `pi` | Flotante| Variable | 4    | 3      | -1      |

### `FunctionInfo` para `sumarHasta`

```
Name           : "sumarHasta"
ReturnType     : Entero
ParameterTypes : [Entero]
Line / Column  : 6 / 8
StartQuad      : -1
LocalTable     : VariableTable("sumarHasta")   ← ver abajo
```

### `LocalTable` de `sumarHasta`

| Name  | Type   | Kind      | Line | Column | Address |
|-------|--------|-----------|------|--------|---------|
| `n`   | Entero | Parameter | 6    | 19     | -1      |
| `i`   | Entero | Variable  | 7    | 5      | -1      |
| `acc` | Entero | Variable  | 7    | 8      | -1      |

> **Nota:** el parámetro `n` se registra primero en `LocalTable` (con `Kind = Parameter`) mientras el analizador procesa la lista de parámetros. Las variables locales (`i`, `acc`) se agregan después al procesar la sección `vars` del `func_body`. El orden de inserción queda preservado por el `Dictionary` de .NET.

---

## Resolución de nombres durante el análisis semántico

Cuando el analizador encuentra una referencia a un identificador (ej. `acc = acc + i`), aplica la siguiente búsqueda en dos pasos:

1. **Primero** consulta `LocalTable` de la función actual.
2. **Si no se encuentra**, consulta `GlobalTable`.
3. **Si tampoco está**, emite un `SemanticError` de tipo "variable no declarada".

```
referencia a "acc"
  → LocalTable("sumarHasta").TryLookup("acc")  ✓  → símbolo encontrado, tipo Entero
referencia a "x"
  → LocalTable("sumarHasta").TryLookup("x")    ✗
  → GlobalTable.TryLookup("x")                 ✓  → símbolo encontrado, tipo Entero
referencia a "z"
  → LocalTable("sumarHasta").TryLookup("z")    ✗
  → GlobalTable.TryLookup("z")                 ✗
  → SemanticError: variable 'z' no declarada
```

---

## Ver también

- [`estructuras.md`](estructuras.md) — justificación detallada de las estructuras de datos elegidas, ciclo de vida completo y alternativas consideradas.
- [`puntos_neuralgicos.md`](puntos_neuralgicos.md) — los `Enter…`/`Exit…` del listener que realizan las inserciones en el directorio y las tablas.
- [`cubo_semantico.md`](cubo_semantico.md) — la tabla de compatibilidad de tipos que se consulta al resolver cada expresión.
- [`pruebas.md`](pruebas.md) — pruebas unitarias de `VariableTable` y `FunctionDirectory`.
