# Estructuras del Análisis Semántico

Este documento describe **qué** estructuras de datos elegimos para representar el Directorio de Funciones y las Tablas de Variables de Patito, **por qué** y **cuáles** operaciones se aplican sobre cada una.

## Mapa de clases

```
                                  ┌────────────────────────────────────────┐
                                  │  FunctionDirectory                     │
                                  │  Dictionary<string, FunctionInfo>      │
                                  │                                        │
                                  │  + ProgramName : string?               │
                                  │  + GlobalTable : VariableTable         │
                                  │  + TryDeclare(FunctionInfo)            │
                                  │  + TryLookup(name, out info)           │
                                  │  + Contains(name)                      │
                                  │  + Functions  : IEnumerable            │
                                  └─────────────────┬──────────────────────┘
                                                    │  contiene 0..n
                                                    ▼
                                  ┌────────────────────────────────────────┐
                                  │  FunctionInfo                          │
                                  │                                        │
                                  │  + Name           : string             │
                                  │  + ReturnType     : SemanticType       │
                                  │  + ParameterTypes : List<SemanticType> │
                                  │  + LocalTable     : VariableTable      │
                                  │  + Line, Column   : int                │
                                  │  + StartQuad      : int   (Entrega 3)  │
                                  └─────────────────┬──────────────────────┘
                                                    │  contiene una
                                                    ▼
                                  ┌────────────────────────────────────────┐
                                  │  VariableTable                         │
                                  │  Dictionary<string, Symbol>            │
                                  │                                        │
                                  │  + ScopeName : string                  │
                                  │  + TryDeclare(Symbol)                  │
                                  │  + TryLookup(name, out symbol)         │
                                  │  + Lookup(name) : Symbol?              │
                                  │  + Contains(name)                      │
                                  │  + Symbols   : IEnumerable             │
                                  └─────────────────┬──────────────────────┘
                                                    │  contiene 0..n
                                                    ▼
                                  ┌────────────────────────────────────────┐
                                  │  Symbol (record)                       │
                                  │  + Name    : string                    │
                                  │  + Type    : SemanticType              │
                                  │  + Kind    : Variable | Parameter      │
                                  │  + Line, Column : int                  │
                                  │  + Address : int   (Entrega 3, -1 ahora)│
                                  └────────────────────────────────────────┘
```

## VariableTable (por alcance)

Cada alcance (global o función) tiene **una** instancia de `VariableTable`. La estructura subyacente es `Dictionary<string, Symbol>`.

### Justificación

1. **Búsqueda O(1)**. El análisis semántico (y la generación de código en Entrega 3) consulta el nombre de una variable cada vez que aparece en el programa. La búsqueda por nombre debe ser barata.
2. **Detección de duplicados gratis**. `TryDeclare` retorna `false` si el nombre ya existía, lo que coincide directamente con la validación "variable doblemente declarada". No necesitamos un segundo `HashSet`.
3. **Preserva el orden de inserción**. El `Dictionary<TKey, TValue>` de .NET enumera en orden de inserción, lo que nos permitirá asignar direcciones consecutivas en la Entrega 3 sin estructura adicional.
4. **Inmutabilidad por símbolo**. Cada `Symbol` es un `record` inmutable; si en el futuro necesitamos asignarle una dirección, creamos un nuevo `Symbol` con `with` en lugar de mutar el original. Esto evita aliasing accidental.

### Operaciones

| Operación                       | Complejidad      | Propósito                                            |
|---------------------------------|------------------|------------------------------------------------------|
| `TryDeclare(Symbol)`            | O(1) promedio    | Agrega el símbolo si no existe; reporta duplicado.   |
| `TryLookup(name, out Symbol)`   | O(1) promedio    | Encuentra un símbolo por nombre.                     |
| `Lookup(name)`                  | O(1) promedio    | Variante que devuelve `null` en lugar de bool.       |
| `Contains(name)`                | O(1) promedio    | Solo presencia, sin sacar el valor.                  |
| `Symbols`                       | O(n) iteración   | Para imprimir el dump o asignar direcciones.         |
| `Count`                         | O(1)             | Tamaño del alcance.                                  |

## FunctionDirectory (uno por programa)

Estructura subyacente: `Dictionary<string, FunctionInfo>`.

Razones idénticas a las de `VariableTable`: lookups O(1), detección de duplicados al insertar y orden de iteración consistente para imprimir.

`FunctionDirectory` también expone:

- `ProgramName : string?` — el ID que aparece justo después de la palabra `programa`. Lo guardamos aquí para poder rechazar funciones con el mismo nombre.
- `GlobalTable : VariableTable` — la tabla de variables globales. Conceptualmente el "alcance del programa" es también una función especial sin parámetros, pero la modelamos por separado para que el directorio liste solo funciones de usuario (más limpio al imprimir y al recorrer).

### Operaciones

| Operación                          | Complejidad      | Propósito                                             |
|------------------------------------|------------------|-------------------------------------------------------|
| `TryDeclare(FunctionInfo)`         | O(1) promedio    | Registra la función; reporta si ya existía.           |
| `TryLookup(name, out FunctionInfo)`| O(1) promedio    | Resuelve un nombre de función al hacer una llamada.   |
| `Contains(name)`                   | O(1) promedio    | Solo presencia.                                       |
| `Functions`                        | O(n) iteración   | Para imprimir el directorio o recorrer en codegen.    |

## FunctionInfo

Un `FunctionInfo` agrupa **toda** la información que el compilador necesita saber de una función:

- `Name`, `ReturnType` y la lista ordenada de `ParameterTypes` (importante: el orden define la firma).
- `LocalTable` — la `VariableTable` propia que contiene tanto los parámetros (con `SymbolKind.Parameter`) como las variables locales declaradas en `vars` dentro del `func_body`.
- `Line, Column` — para mensajes de error que apuntan a la declaración original.
- `StartQuad` — placeholder para la Entrega 3 (índice del primer cuadruplo de la función). Por ahora vale `-1`.

## Symbol (entrada de la tabla)

```csharp
public sealed record Symbol(
    string Name,
    SemanticType Type,
    SymbolKind Kind,
    int Line, int Column,
    int Address = -1);
```

Es un `record` inmutable. El campo `Address` es el "hueco" reservado para la Entrega 3 (asignación de memoria virtual); el resto es lo que ya necesita el análisis semántico de la Entrega 2.

## Alternativas consideradas

- **Listas (`List<Symbol>`).** Más simples pero requieren `O(n)` por lookup. En programas grandes con muchas referencias por variable, esto se nota.
- **Pila de scopes anidados (Dictionary<string, Stack<Symbol>>).** Útil si Patito permitiera bloques anidados con scoping léxico fino. La gramática actual define solo dos niveles de alcance (global y función), así que no compensa la complejidad extra.
- **Una sola tabla con prefijos (`"funcion::variable"`)** — funciona en lenguajes simples, pero rompe la operación clave: "darme todas las variables locales de esta función". Mantener tablas separadas hace ese caso trivial.

## Ciclo de vida durante una compilación

1. Se crea un `SemanticAnalyzer`. Internamente instancia un `FunctionDirectory` con `GlobalTable` vacía.
2. El walker entra a `programa`: el analyzer **graba** `ProgramName`, **llena** `GlobalTable` con cada `vars` global, y **llena** el directorio recorriendo `funcs`.
3. Para cada función, el analyzer construye su `FunctionInfo`, llena su `LocalTable` con parámetros y locales, y la registra.
4. Mientras el walker visita estatutos dentro de `func_body`, el analyzer empuja la función actual a una pila de alcances. Esto permite que `LookupVariable` resuelva primero contra la `LocalTable` activa y solo si falla consulte la `GlobalTable`.

Resultado: al terminar el recorrido, el `FunctionDirectory` y la `GlobalTable` están **pobladas**, el cubo semántico está disponible para consultas, y la lista de errores semánticos contiene todas las violaciones detectadas.
