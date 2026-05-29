# Distribución de Direcciones Virtuales

> Documentación de las **Entregas 4 y 5** del compilador. Ver el [índice general](README.md) para más contexto.

Una **dirección virtual** es un número entero que identifica unívocamente una celda de memoria durante la ejecución de un programa Patito. La máquina virtual no trabaja con nombres de variables (`a`, `resultado`, `t0`) sino con sus direcciones. El compilador asigna una dirección a cada símbolo durante la fase semántica y la registra en el **libro de direcciones** (`AddressBook`).

> **Estado de implementación:** La asignación de direcciones está **completamente implementada** (Entrega 5). Los cuádruplos internamente siguen usando nombres de símbolos (para compatibilidad con los tests y la fase de backfill), pero el módulo de impresión formatea cada operando como `DIR(NOMBRE)` usando el `AddressBook`.

---

## Decisiones de diseño

### Base en 18 000

La memoria virtual comienza en **18 000** (en lugar de 0 o 1 000) para que las direcciones sean inmediatamente distinguibles de los índices de cuádruplos (que comienzan en 0) y de cualquier constante literal que pueda aparecer en el código fuente. Si en algún momento se imprime una dirección virtual en la fila de cuádruplos, su valor alto la hace visualmente evidente.

### Segmentos de tamaño fijo (1 000 direcciones)

Cada categoría de símbolo tiene exactamente **1 000 direcciones** reservadas. Esto simplifica:

- El cálculo de la dirección: `base_segmento + contador_interno`.
- La detección de desbordamiento: basta verificar `contador < 1 000`.
- La identificación del segmento a partir de una dirección: clasificación por división entera.

### Diez segmentos independientes

| # | Segmento              | Inicio  | Fin     | Contenido                                                    |
|---|-----------------------|---------|---------|--------------------------------------------------------------|
| 0 | **Global – Entero**   | 18 000  | 18 999  | Variables enteras declaradas en el alcance global.           |
| 1 | **Global – Flotante** | 19 000  | 19 999  | Variables flotantes declaradas en el alcance global.         |
| 2 | **Local – Entero**    | 20 000  | 20 999  | Variables y parámetros enteros de cualquier función.         |
| 3 | **Local – Flotante**  | 21 000  | 21 999  | Variables y parámetros flotantes de cualquier función.       |
| 4 | **Temp – Entero**     | 22 000  | 22 999  | Temporales `t0`, `t1`, … de tipo entero.                     |
| 5 | **Temp – Flotante**   | 23 000  | 23 999  | Temporales de tipo flotante.                                 |
| 6 | **Temp – Bool**       | 24 000  | 24 999  | Temporales de operaciones relacionales (`<`, `>`, `==`, `!=`). |
| 7 | **Const – Entero**    | 25 000  | 25 999  | Literales enteras (`0`, `42`, `-1`, …) del código fuente.    |
| 8 | **Const – Flotante**  | 26 000  | 26 999  | Literales flotantes (`3.14`, `0.5`, …).                      |
| 9 | **Const – Cadena**    | 27 000  | 27 999  | Literales de cadena (`"hola"`, `"resultado:"`, …).           |

### Separación local/global en lugar de por función

En Patito cada función tiene sus propias variables locales, pero la máquina virtual usa un modelo de registro de activación sencillo: al llamar a una función (`ERA`) se guarda el contexto previo y se reserva espacio para las locales de la nueva función. Esto significa que los segmentos **Local** cubren todas las funciones de usuario; la máquina virtual asigna los offsets dentro del segmento en tiempo de ejecución.

### Reinicio de temporales por función (`ResetTemps`)

Los temporales se generan durante la fase de código y viven en el registro de activación de la función que los produce. Por eso, al entrar al cuerpo de cada función (`EnterFunc_body` / PN-7b') el compilador reinicia los contadores de los segmentos Temp — cada función reutiliza las mismas direcciones `22 000`, `23 000`, `24 000` en su propio frame. Los contadores de segmentos Global y Const **nunca** se reinician (acumulan todas las declaraciones y literales del programa).

### Pool de constantes (deduplicación)

La misma literal (`"42"`, `"3.14"`, `"hola"`) siempre recibe la misma dirección virtual. El `QuadrupleEmitter` mantiene un diccionario `literal → dirección` (pool de constantes) que consulta antes de asignar, de modo que el segmento Const no desperdicia celdas en literales repetidas.

---

## Diagrama del mapa de memoria

```
 Dirección virtual
 ┌───────────────────────────────────────────────────────────────┐
 │  18 000 ──── Global Entero   (1 000 celdas)                   │  ← vars globales int
 │  19 000 ──── Global Flotante (1 000 celdas)                   │  ← vars globales float
 ├───────────────────────────────────────────────────────────────┤
 │  20 000 ──── Local Entero    (1 000 celdas)                   │  ← params + locales int
 │  21 000 ──── Local Flotante  (1 000 celdas)                   │  ← params + locales float
 ├───────────────────────────────────────────────────────────────┤
 │  22 000 ──── Temp Entero     (1 000 celdas) [reset/función]   │  ← temporales int
 │  23 000 ──── Temp Flotante   (1 000 celdas) [reset/función]   │  ← temporales float
 │  24 000 ──── Temp Bool       (1 000 celdas) [reset/función]   │  ← resultados relacionales
 ├───────────────────────────────────────────────────────────────┤
 │  25 000 ──── Const Entero    (1 000 celdas)                   │  ← literales enteras
 │  26 000 ──── Const Flotante  (1 000 celdas)                   │  ← literales flotantes
 │  27 000 ──── Const Cadena    (1 000 celdas)                   │  ← literales de cadena
 └───────────────────────────────────────────────────────────────┘
                                                        27 999
```

---

## Ciclo de vida de una dirección

| Fase | Quién asigna | Segmento destino |
|------|--------------|------------------|
| Declaración global (`vars`) | `DeclareVariable` (isGlobal=true) | Global-Int / Global-Float |
| Declaración local (params y vars de función) | `DeclareVariable` (isGlobal=false) | Local-Int / Local-Float |
| Literal numérica en expresión | `AllocateConstant` (pool, dedup) | Const-Int / Const-Float |
| Literal de cadena en `escribe` | `AllocateStringConst` (pool, dedup) | Const-Cadena |
| Temporal aritmético | `NewTemp(SemanticType)` en `EmitBinary` | Temp-Int / Temp-Float |
| Temporal relacional | `NewTemp(SemanticType.Bool)` | Temp-Bool |

---

## Libro de direcciones (`AddressBook`)

El `QuadrupleEmitter` mantiene un `Dictionary<string, int>` que mapea cada nombre de operando a su dirección virtual. Se expone públicamente como `IReadOnlyDictionary<string, int> AddressBook` y desde `CompileResult.AddressBook`.

El libro se puebla de forma incremental:

- `RegisterAddress(name, addr)` — llamado desde `DeclareVariable` para variables.
- `AllocateConstant` / `AllocateStringConst` — escriben directamente en el libro tras asignar.
- `NewTemp(type)` — escribe el temporal generado en el libro.

Los nombres de función (usados en `ERA`, `Gosub`, `EndFunc`) y los índices de salto (usados en `GotoF`/`Goto`) **no tienen dirección** en el libro y se imprimen sin prefijo.

---

## Formato de impresión de cuádruplos

A partir de la Entrega 5 el flag `--quads` imprime en el formato:

```
OPERACION  DIR(NOMBRE)  DIR(NOMBRE)  DIR(NOMBRE)
```

Ejemplo para `a = 1; b = 2; c = a + b; escribe("suma:", c);` con `a, b, c: entero`:

```
   #  Op        Left                    Right                   Result
────────────────────────────────────────────────────────────────────────────────
   0  =         25000(1)                _                       18000(a)
   1  =         25001(2)                _                       18001(b)
   2  +         18000(a)                18001(b)                22000(t0)
   3  =         22000(t0)               _                       18002(c)
   4  Print     _                       _                       27000("suma:")
   5  Print     _                       _                       18002(c)
```

Cuando un operando no tiene dirección asignada (nombre de función, índice de salto) se imprime solo el nombre sin prefijo.

---

## API de `VirtualMemoryMap`

La clase `VirtualMemoryMap` (en el paquete `Patito.Compiler.CodeGen`) expone:

### Constantes de base

```csharp
VirtualMemoryMap.GlobalIntBase    // 18_000
VirtualMemoryMap.GlobalFloatBase  // 19_000
VirtualMemoryMap.LocalIntBase     // 20_000
VirtualMemoryMap.LocalFloatBase   // 21_000
VirtualMemoryMap.TempIntBase      // 22_000
VirtualMemoryMap.TempFloatBase    // 23_000
VirtualMemoryMap.TempBoolBase     // 24_000
VirtualMemoryMap.ConstIntBase     // 25_000
VirtualMemoryMap.ConstFloatBase   // 26_000
VirtualMemoryMap.ConstStringBase  // 27_000
VirtualMemoryMap.SegmentSize      // 1_000
```

### Consultas estáticas (no modifican estado)

```csharp
int base = VirtualMemoryMap.BaseOf(MemorySegment.GlobalInt);   // 18_000
int end  = VirtualMemoryMap.EndOf(MemorySegment.GlobalInt);    // 18_999
MemorySegment? seg = VirtualMemoryMap.SegmentOf(18_500);       // GlobalInt
bool ok = VirtualMemoryMap.InSegment(18_500, MemorySegment.GlobalInt); // true
```

### Asignación de direcciones (estado mutable)

```csharp
var map = new VirtualMemoryMap();

// Por segmento
int dir1 = map.Allocate(MemorySegment.GlobalInt);   // 18_000
int dir2 = map.Allocate(MemorySegment.GlobalInt);   // 18_001

// Por tipo + alcance
int dirVar = map.AllocateFor(SemanticType.Entero,   isGlobal: true);   // GlobalInt
int dirLoc = map.AllocateFor(SemanticType.Flotante, isGlobal: false);  // LocalFloat

// Temporal según tipo semántico
int dirTmp = map.AllocateTemp(SemanticType.Entero);   // TempInt

// Reiniciar solo temporales (al entrar a cada función)
map.ResetTemps();

// Reiniciar todo (entre compilaciones en tests)
map.Reset();
```

---

## Ver también

- [`estructuras.md`](estructuras.md) — la clase `Symbol` que lleva la dirección asignada (`Symbol.Address`).
- [`puntos_neuralgicos.md`](puntos_neuralgicos.md) — PN-7b' (ResetTemps), PN-8 y PN-13 que registran constantes.
- [`cuadruplos.md`](cuadruplos.md) — formato de la fila de cuádruplos y el flag `--quads`.
