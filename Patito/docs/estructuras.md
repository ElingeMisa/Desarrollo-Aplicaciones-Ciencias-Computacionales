# Estructuras del Análisis Semántico

> Documentación de la **Entrega 2** del compilador. Ver el [índice general](README.md) para más contexto.

Este documento describe **qué** estructuras de datos elegimos para representar el Directorio de Funciones y las Tablas de Variables de Patito, **por qué** y **cuáles** operaciones se aplican sobre cada una. El modelo de dos niveles de alcance (global y de función) viene directamente de las decisiones del lenguaje resumidas en [`lenguaje.md`](lenguaje.md).

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

---

## Estructuras de la Entrega 3 — Generación de cuádruplos

La generación de código intermedio requiere cuatro estructuras nuevas que se alojan en el paquete `Patito.Compiler.CodeGen`. Todas ellas son instanciadas y orquestadas por `QuadrupleEmitter`, que el `SemanticAnalyzer` posee como campo privado y expone como propiedad `Emitter`.

### Mapa de clases (Entrega 3)

```
                         ┌─────────────────────────────────┐
                         │  QuadrupleEmitter                │
                         │                                  │
                         │  + Operadores : PilaOperadores   │
                         │  + Operandos  : PilaOperandos    │
                         │  + Tipos      : PilaTipos        │
                         │  + Fila       : FilaCuadruplos   │
                         │  + NewTemp()  : string           │
                         │  + PushOperand(name, type)       │
                         │  + EmitBinary(op, l, lt, r, rt)  │
                         └──────────────┬──────────────────┘
              ┌───────────┬─────────────┼─────────────────────┐
              ▼           ▼             ▼                      ▼
   ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐
   │PilaOperadores│ │PilaOperandos │ │  PilaTipos   │ │  FilaCuadruplos  │
   │Stack<QuadOp> │ │Stack<string> │ │Stack<Sem.T.> │ │ List<Quadruple>  │
   │              │ │              │ │              │ │                  │
   │+ Push(op)    │ │+ Push(name)  │ │+ Push(type)  │ │+ Emit(…) → int   │
   │+ Pop()       │ │+ Pop()       │ │+ Pop()       │ │+ Backfill(i,val) │
   │+ Peek()      │ │+ Peek()      │ │+ Peek()      │ │+ Count           │
   │+ IsEmpty     │ │+ IsEmpty     │ │+ IsEmpty     │ │+ Quads (lista)   │
   └──────────────┘ └──────────────┘ └──────────────┘ └──────────────────┘
                                                              │  contiene n
                                                              ▼
                                               ┌─────────────────────────┐
                                               │  Quadruple (record)     │
                                               │  + Index  : int         │
                                               │  + Op     : QuadOp      │
                                               │  + Left   : string?     │
                                               │  + Right  : string?     │
                                               │  + Result : string      │
                                               └─────────────────────────┘
```

### PilaOperadores

Apila los operadores (`QuadOp`) pendientes de aplicar durante la evaluación de una expresión. `QuadrupleEmitter.EmitBinary` hace `Push(op)` seguido de `Pop()` para procesar el operador de forma explícita a través de la pila antes de emitir el cuádruplo.

**Estructura subyacente:** `Stack<QuadOp>` (BCL de .NET).

| Operación | Complejidad | Propósito |
|-----------|-------------|-----------|
| `Push(op)` | O(1) | Registra el operador que se va a aplicar. |
| `Pop()`    | O(1) | Extrae el operador para emitir el cuádruplo. |
| `Peek()`   | O(1) | Consulta sin extraer (para algoritmos de precedencia). |
| `IsEmpty`  | O(1) | Guard para evitar pop sobre pila vacía. |

### PilaOperandos

Apila los **nombres** de los operandos (variables, constantes literales o temporales `t0`, `t1`, …) generados durante la evaluación de los factores. Se mantiene en paralelo con `PilaTipos`.

**Estructura subyacente:** `Stack<string>` (BCL de .NET).

| Operación | Complejidad | Propósito |
|-----------|-------------|-----------|
| `Push(name)` | O(1) | Registra el nombre del operando tras evaluar un factor. |
| `Pop()`      | O(1) | Extrae el operando para construir el cuádruplo. |

### PilaTipos

Apila los `SemanticType` correspondientes a cada entrada de `PilaOperandos`. El elemento en la posición *N* de `PilaTipos` tiene su nombre en la posición *N* de `PilaOperandos`. `QuadrupleEmitter.EmitBinary` extrae ambos en paralelo y consulta el cubo semántico antes de emitir.

**Estructura subyacente:** `Stack<SemanticType>` (BCL de .NET).

### FilaCuadruplos

Acumula los cuádruplos en el orden en que se ejecutarán. Conceptualmente es una **fila** (cola), pero se implementa sobre `List<Quadruple>` para permitir la operación `Backfill`, que rellena el destino de un salto condicional o incondicional una vez que se conoce la posición correcta.

**Estructura subyacente:** `List<Quadruple>` (BCL de .NET).

| Operación | Complejidad | Propósito |
|-----------|-------------|-----------|
| `Emit(op, left, right, result) → int` | O(1) amortizado | Agrega un cuádruplo al final; devuelve su índice. |
| `Backfill(index, newResult)` | O(1) | Rellena el campo `Result` del cuádruplo en `index`. |
| `Count` | O(1) | Número de cuádruplos emitidos. |
| `Quads` | O(n) iteración | Acceso de solo lectura para imprimir la fila. |

### Quadruple

Registro inmutable `(Index, Op, Left?, Right?, Result)` que representa una instrucción de código intermedio de Patito.

```
Formato:  (Op, Left, Right, Result)

Aritmético/relacional:  Result = Left op Right
Asignación:             Result = Left        (Right = null)
Negación unaria:        Result = -Right      (Left = null)
GotoF:                  if !Left goto Result (Right = null)
Goto:                   goto Result          (Left = Right = null)
Print:                  imprimir Result      (Left = Right = null)
Param:                  param Result         (Left = Right = null)
Gosub:                  gosub Result         (Left = Right = null)
```

### QuadrupleEmitter

Orquestador que posee las cuatro estructuras y centraliza las operaciones de generación de código:

- `NewTemp()` — genera nombres únicos `t0`, `t1`, `t2`, … para resultados intermedios.
- `PushOperand(name, type)` — apila un par (nombre, tipo) en `PilaOperandos` y `PilaTipos` simultáneamente.
- `EmitBinary(op, leftName, leftType, rightName, rightType)` — usa `PilaOperadores` para procesar el operador, consulta el cubo semántico y emite el cuádruplo en `FilaCuadruplos`. Devuelve `(resultName, resultType)`.

### Ciclo de vida (Entrega 3)

El ciclo de la Entrega 2 se extiende con una fase de generación de código que ocurre **durante el mismo recorrido** del árbol:

1. `ExitFactorSimple` → `_emitter.PushOperand(name, type)` por cada operando hoja.
2. `ExitTermino` y `ExitExp` → `_emitter.EmitBinary(op, …)` por cada operación aritmética.
3. `ExitExpresion` → `_emitter.EmitBinary(relOp, …)` si hay operador relacional.
4. `ExitAsigna` → `_emitter.Fila.Emit(Assign, exprName, null, destName)`.
5. `ExitImp` → `_emitter.Fila.Emit(Print, null, null, value)`.
6. `ExitCondicion` / `ExitCiclo` → `_emitter.Fila.Backfill(index, target)` para resolver saltos.

Resultado: al terminar el recorrido, `_emitter.Fila.Quads` contiene la fila completa de cuádruplos en orden de ejecución, accesible via `SemanticAnalyzer.Quads` y `CompileResult.Quads`.

---

## Estructuras de la Entrega 5 — Memoria de Ejecución

La Entrega 5 agrega una **Máquina Virtual** que interpreta los cuádruplos. Las nuevas estructuras viven en `src/Patito.Compiler/VM/`.

### ExecutionMemory

Almacén de memoria indexado por dirección virtual.

```
ExecutionMemory
  _cells : Dictionary<int, object>   addr → valor real (int/double/bool/string)
  ──────────────────────────────────────────────────────
  Set(addr, val)    → escribe val en addr
  Get(addr)         → lee addr (lanza si no inicializado)
  TryGet(addr, out) → intento de lectura sin excepción
  Reset()           → borra todas las celdas
```

#### Diagrama ASCII de segmentos con valores (ejemplo)

```
 _globalMemory          _activeLocal (frame activo)
 ┌──────────────────┐   ┌──────────────────────────┐
 │ 18000 → 3        │   │ 20000 → 5 (param 'n')    │
 │ 18001 → 7        │   │ 20001 → 0 (local 'i')    │
 │ 19000 → 3.14     │   │ 22000 → true (temp bool) │
 │ 25000 → 42       │◄──┤ 22001 → 8    (temp int)  │
 │ 27000 → "hola"   │   └──────────────────────────┘
 └──────────────────┘   ← nueva instancia por llamada
```

**Enrutamiento de acceso:**
- `addr ∈ [20000, 24999]` → `_activeLocal`
- `addr ∈ [18000, 19999] ∪ [25000, 27999]` → `_globalMemory`

La fórmula `addr - BaseOf(seg)` da el *offset* dentro del segmento (útil para inspección o límite de tamaño).

---

### ActivationRecord

Frame de pila de llamadas.

```
ActivationRecord
  FunctionName : string             nombre de la función que creó el frame
  ReturnQuad   : int                índice del cuádruplo de retorno
  LocalMemory  : ExecutionMemory    parámetros + locales + temporales del frame
  _pendingArgs : List<(addr,val)>   argumentos recopilados por Param, antes de Gosub
  ──────────────────────────────────────────────────────────────────────────────
  PushArg(addr, val)  → agrega (addr_param_formal, valor_arg_real)
  PopArgs()           → devuelve la lista y la vacía
```

#### Diagrama de la call stack en tiempo de ejecución

```
inicio { ... suma(a, b) ... }

  _callStack (Stack)
  ╔══════════════════════════════╗  ← tope
  ║  returnPc = 12               ║
  ║  savedLocal = main_frame     ║
  ╠══════════════════════════════╣
  ║  (vacío — sola llamada)      ║
  ╚══════════════════════════════╝

  _activeLocal = suma_frame.LocalMemory
    20000 → 3  (param 'a')
    20001 → 4  (param 'b')
```

Al ejecutar `EndFunc`:  
1. Pop → `(returnPc=12, savedLocal=main_frame)`  
2. `_activeLocal = main_frame`  
3. `pc = 12`

---

### VirtualMachine — métodos principales

| Firma | Descripción |
|-------|-------------|
| `VirtualMachine(quads, addressBook, constValues, funcDir, output?)` | Constructor. `output` es inyectable para tests. |
| `Execute() → VmResult` | Carga constantes, ejecuta el loop `while(pc < quads.Count)`. |
| `GetMemory(addr) → object?` | Lee la dirección virtual (global o local activo). Útil en tests. |

---

## Ver también

- [`puntos_neuralgicos.md`](puntos_neuralgicos.md) — los `Enter…`/`Exit…` que llenan estas estructuras durante el recorrido del árbol.
- [`cubo_semantico.md`](cubo_semantico.md) — la tabla de compatibilidad de tipos a la que estas estructuras dan contexto.
- [`gramatica.md`](gramatica.md) — las producciones (`vars`, `funcs`, `func_body`, etc.) que disparan la creación de cada entrada.
- [`pruebas.md`](pruebas.md) — pruebas unitarias de `VariableTable` y `FunctionDirectory`.
