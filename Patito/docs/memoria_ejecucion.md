# Memoria de Ejecución y Máquina Virtual

> Documentación de la **Entrega 5** del compilador. Ver el [índice general](README.md) para más contexto.

La **Máquina Virtual (VM)** de Patito interpreta directamente los cuádruplos de código intermedio generados por el compilador. No produce código nativo: ejecuta cada instrucción en memoria, modificando un contador de programa (`pc`) y leyendo/escribiendo celdas de memoria indexadas por **dirección virtual**.

Las estructuras del compilador que alimentan la VM están en [`estructuras.md`](estructuras.md). El mapa de direcciones que la VM usa para enrutar accesos está en [`direcciones_virtuales.md`](direcciones_virtuales.md).

---

## Modelo de Memoria

La VM mantiene dos zonas de memoria durante la ejecución:

```
┌─────────────────────────────────────────────────────────────────────┐
│ Segmento            │ Rango dir. virtual │ Almacén en la VM         │
├─────────────────────┼────────────────────┼──────────────────────────┤
│ GlobalInt           │ 18 000 – 18 999    │ _globalMemory            │
│ GlobalFloat         │ 19 000 – 19 999    │ _globalMemory            │
│ LocalInt            │ 20 000 – 20 999    │ _activeLocal  (frame)    │
│ LocalFloat          │ 21 000 – 21 999    │ _activeLocal  (frame)    │
│ TempInt             │ 22 000 – 22 999    │ _activeLocal  (frame)    │
│ TempFloat           │ 23 000 – 23 999    │ _activeLocal  (frame)    │
│ TempBool            │ 24 000 – 24 999    │ _activeLocal  (frame)    │
│ ConstInt            │ 25 000 – 25 999    │ _globalMemory  (solo R)  │
│ ConstFloat          │ 26 000 – 26 999    │ _globalMemory  (solo R)  │
│ ConstString         │ 27 000 – 27 999    │ _globalMemory  (solo R)  │
└─────────────────────┴────────────────────┴──────────────────────────┘
```

La regla de enrutamiento es una sola comparación:

```csharp
private static bool IsLocalOrTemp(int addr)
    => addr >= VirtualMemoryMap.LocalIntBase &&   // 20 000
       addr <  VirtualMemoryMap.ConstIntBase;     // 25 000
```

- **`true`** → el acceso va a `_activeLocal` (memoria del frame activo).
- **`false`** → el acceso va a `_globalMemory` (globals + constantes).

### Carga inicial de constantes

Antes del primer cuádruplo, `Execute()` itera `constValues` (dirección → valor real) y carga cada constante en `_globalMemory`:

```csharp
foreach (var kvp in _constValues)
    _globalMemory.Set(kvp.Key, kvp.Value);
```

`constValues` proviene de `QuadrupleEmitter.BuildConstValues()`, que convierte cada literal del pool (`"42"` → `int 42`, `"3.14"` → `double 3.14`, `"\"hola\""` → `string "hola"`).

---

## ExecutionMemory

Almacén de memoria indexado por dirección virtual. Una instancia por zona (_globalMemory) o por frame (_activeLocal).

```
ExecutionMemory
  _cells : Dictionary<int, object>
  ─────────────────────────────────────────────────────────
  Set(addr, val)          Escribe val en addr.
  Get(addr)               Lee addr. Lanza InvalidOperationException
                          si no fue inicializado ("addr no inicializado").
  TryGet(addr, out val)   Lectura segura; devuelve bool.
  Reset()                 Borra todas las celdas (usado en tests).
```

**Tipos de valor almacenados:**

| Tipo C# | Corresponde a |
|---------|---------------|
| `int` | `SemanticType.Entero` |
| `double` | `SemanticType.Flotante` |
| `bool` | Resultado de operador relacional (`SemanticType.Bool`) |
| `string` | Literal de cadena (`ConstString`) |

### Diagrama de segmentos con valores de ejemplo

```
 _globalMemory                       _activeLocal  (frame de acumular)
 ┌───────────────────────────────┐   ┌──────────────────────────────────┐
 │ 18000 (n, GlobalInt)   →  5  │   │ 20000 (limite, LocalInt) →  5    │
 │ 18001 (acum, GlobalInt) →  0 │   │ 20001 (i, LocalInt)      →  1    │
 │ 25000 ("5", ConstInt)  →  5  │   │ 20002 (total, LocalInt)  →  0    │
 │ 25001 ("1", ConstInt)  →  1  │   │ 22000 (t0, TempBool)     → true  │
 │ 27000 ("\"hola\"", CS) → "…" │   └──────────────────────────────────┘
 └───────────────────────────────┘   ← nueva instancia por cada Gosub
```

---

## ActivationRecord

Frame de la pila de llamadas. Representa el contexto de una ejecución de función.

```
ActivationRecord
  FunctionName : string             Nombre de la función
  ReturnQuad   : int                Índice del quad de retorno (pc + 1 del Gosub)
  LocalMemory  : ExecutionMemory    Parámetros + locales + temporales del frame
  _pendingArgs : List<(addr,val)>   Args acumulados por Param antes del Gosub
  ─────────────────────────────────────────────────────────────────────────────
  PushArg(addr, val)   Agrega (addr_param_formal, valor_arg_real) a la lista.
  PopArgs()            Devuelve la lista y la vacía. Llamado una vez en Gosub.
```

### Ciclo de vida

```
ERA   func   →  new ActivationRecord("func")   // frame vacío, argCounter=0
Param arg1   →  PushArg(paramAddr0, val1)       // un Param por argumento
Param arg2   →  PushArg(paramAddr1, val2)
Gosub func N →  PopArgs() → LocalMemory.Set(addr, val) por cada arg
                callStack.Push(pc+1, _activeLocal)
                _activeLocal = frame.LocalMemory
                pc = N
EndFunc      →  (returnPc, savedLocal) = callStack.Pop()
                _activeLocal = savedLocal
                pc = returnPc
```

### Diagrama del call stack

Estado al ejecutar la segunda llamada a `imprimir(7)` en TC-VM-05:

```
  _callStack  (Stack — tope a la derecha)
  ╔══════════════════════════════════════╗
  ║ (vacío — sola llamada activa)        ║
  ╚══════════════════════════════════════╝

  _activeLocal = frame de imprimir
  ┌─────────────────────────────────────┐
  │ ReturnQuad = N+6                    │
  │ LocalMemory:                        │
  │   20000 (n, LocalInt) → 7           │
  └─────────────────────────────────────┘

  _globalMemory:
    18000 (a, GlobalInt) → 99
    25000 ("99", ConstInt) → 99
    25001 ("7",  ConstInt) → 7
    27000 ("\"valor:\"", ConstString) → "valor:"
```

Al ejecutar `EndFunc`:
1. `(returnPc, savedLocal) = callStack.Pop()`
2. `_activeLocal = savedLocal` (vuelve a `main_frame` — vacío)
3. `pc = returnPc = N+6`

---

## VirtualMachine

Intérprete de cuádruplos. Recibe los artefactos del compilador e implementa un loop `while(pc < quads.Count)` con un `switch` sobre `QuadOp`.

### Constructor

```csharp
new VirtualMachine(
    IReadOnlyList<Quadruple>         quads,
    IReadOnlyDictionary<string, int> addressBook,   // nombre → dirección virtual
    IReadOnlyDictionary<int, object> constValues,   // dirección → valor real
    FunctionDirectory                funcDir,
    TextWriter?                      output          // inyectable para tests
)
```

### Métodos públicos

| Firma | Descripción |
|-------|-------------|
| `Execute() → VmResult` | Carga constantes y ejecuta el loop completo. |
| `GetMemory(addr) → object?` | Lee una dirección virtual (global o local activo). Para tests de caja blanca. |

### Soporte de operaciones

| `QuadOp` | Implementación |
|----------|----------------|
| `Plus / Minus / Times / Divide` | Aritmética con promoción automática a `double` si algún operando es `double`. División entera si ambos son `int`. |
| `Lt / Gt / Eq / Neq` | Comparación numérica; resultado `bool` almacenado en segmento TempBool. |
| `Assign` | `SetValue(Result, GetValue(Left))` — copia entre celdas. |
| `Neg` | Negación unaria de `int` o `double`. |
| `GotoF` | Si la condición es `false` o `int 0`, `pc = int.Parse(Result)`. |
| `Goto` | `pc = int.Parse(Result)`. |
| `Print` | Resuelve el operando a su valor y lo escribe en el `TextWriter` inyectado. |
| `ERA` | Crea `ActivationRecord` pendiente; `_argCounter = 0`. |
| `Param` | Busca la dirección del parámetro formal en `funcDir.LocalTable`, llama `PushArg`. |
| `Gosub` | `PopArgs()` → copia args a `LocalMemory`; push call stack; `_activeLocal = frame.LocalMemory`; `pc = startQuad`. |
| `EndFunc` | Pop call stack; restaura `_activeLocal` y `pc`. |
| `Return` | **[Entrega 6]** `SetValue(Result, GetValue(Left))` — copia el valor de la expresión de `regresa` a la dirección **global** `"{func}_ret"`. Es deliberadamente simétrico a `Assign`; lo único que cambia es la semántica del nombre destino (vive en `GlobalInt`/`GlobalFloat`, sobrevive al `EndFunc` que sigue). |

### Cómo las Direcciones Virtuales indexan la memoria

La VM nunca trabaja con nombres de variables: todo acceso pasa por `GetValue(name)` / `SetValue(name, val)`, que primero resuelven el nombre a una dirección virtual vía `_addressBook`, y luego enrutan el acceso al almacén correcto:

```csharp
private object GetValue(string name)
{
    int addr = _addressBook[name];           // nombre → dirección virtual
    if (IsLocalOrTemp(addr))
        return _activeLocal.Get(addr);       // segmento local / temp
    return _globalMemory.Get(addr);          // global / constante
}
```

Esto elimina la necesidad de tablas de símbolos en tiempo de ejecución. La dirección virtual es, en sí misma, el índice de la celda de memoria.

---

## VmResult

Record inmutable que encapsula el resultado de una ejecución:

```csharp
sealed record VmResult(
    string     Output,   // todo lo impreso via escribe(), capturado en StringBuilder
    Exception? Error,    // excepción si la hubo; null en caso de éxito
    bool       Success   // true si Execute() terminó sin lanzar
)
```

---

## Ver también

- [`direcciones_virtuales.md`](direcciones_virtuales.md) — el mapa de segmentos y la API de `VirtualMemoryMap` que asigna cada dirección en compilación.
- [`cuadruplos.md`](cuadruplos.md) — el formato y catálogo de los cuádruplos que la VM ejecuta.
- [`puntos_neuralgicos.md`](puntos_neuralgicos.md) — especialmente PN-0 (Goto inicial), PN-7b/PN-18 (funciones con direcciones virtuales) y PN-19 (`regresa` y direcciones de retorno).
- [`pruebas.md`](pruebas.md) — test cases TC-VM-01 a TC-VM-09 que validan la VM de extremo a extremo (TC-VM-08/09 cubren `regresa` y el fix de aliasing en llamadas recursivas).
