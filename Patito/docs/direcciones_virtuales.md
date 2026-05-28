# Distribución de Direcciones Virtuales

> Documentación de la **Entrega 4** del compilador. Ver el [índice general](README.md) para más contexto.

Una **dirección virtual** es un número entero que identifica unívocamente una celda de memoria durante la ejecución de un programa Patito. La máquina virtual no trabaja con nombres de variables (`a`, `resultado`, `t0`) sino con sus direcciones. El compilador es el encargado de asignar una dirección a cada símbolo antes de generar el código objeto.

> **Nota (Entrega 4):** La *asignación* de direcciones concretas a cada símbolo se realiza en la Entrega 5. En esta entrega se define la **estructura del mapa** y se implementa la clase `VirtualMemoryMap` (en `src/Patito.Compiler/CodeGen/VirtualMemoryMap.cs`) que será usada por los algoritmos de traducción futuros.

---

## Decisiones de diseño

### Base en 18 000

La memoria virtual comienza en **18 000** (en lugar de 0 o 1 000) para que las direcciones sean inmediatamente distinguibles de los índices de cuádruplos (que comienzan en 0) y de cualquier constante literal que pueda aparecer en el código fuente. Si en algún momento se imprime una dirección virtual en la fila de cuádruplos, su valor alto la hace visualmente evidente.

### Segmentos de tamaño fijo (1 000 direcciones)

Cada categoría de símbolo tiene exactamente **1 000 direcciones** reservadas. Esto simplifica:

- El cálculo de la dirección: `base_segmento + contador_interno`.
- La detección de desbordamiento: basta verificar `contador < 1 000`.
- La identificación del segmento a partir de una dirección: la clasificación es una división entera.

### Diez segmentos independientes

| # | Segmento              | Inicio  | Fin     | Contenido                                                    |
|---|-----------------------|---------|---------|--------------------------------------------------------------|
| 0 | **Global – Entero**   | 18 000  | 18 999  | Variables enteras declaradas en el alcance global.           |
| 1 | **Global – Flotante** | 19 000  | 19 999  | Variables flotantes declaradas en el alcance global.         |
| 2 | **Local – Entero**    | 20 000  | 20 999  | Variables y parámetros enteros de cualquier función.         |
| 3 | **Local – Flotante**  | 21 000  | 21 999  | Variables y parámetros flotantes de cualquier función.       |
| 4 | **Temp – Entero**     | 22 000  | 22 999  | Temporales `t0`, `t1`, … de tipo entero.                     |
| 5 | **Temp – Flotante**   | 23 000  | 23 999  | Temporales de tipo flotante.                                 |
| 6 | **Temp – Bool**       | 24 000  | 24 999  | Temporales que resultan de operaciones relacionales (`<`, `>`, `==`, `!=`). |
| 7 | **Const – Entero**    | 25 000  | 25 999  | Literales enteras (`0`, `42`, `-1`, …) que aparecen en el código fuente. |
| 8 | **Const – Flotante**  | 26 000  | 26 999  | Literales flotantes (`3.14`, `0.5`, …).                      |
| 9 | **Const – Cadena**    | 27 000  | 27 999  | Literales de cadena (`"hola"`, `"resultado:"`, …).           |

### Separación local/global en lugar de por función

En Patito cada función tiene sus propias variables locales, **pero** la máquina virtual de Patito utiliza un modelo de registro de activación sencillo: al llamar a una función (cuádruplo `ERA`) se guarda el contexto previo y se reserva espacio para las locales de la nueva función. Esto significa que los segmentos **Local** cubren todas las funciones de usuario; la máquina virtual es quien asigna los offsets dentro del segmento en tiempo de ejecución.

---

## Diagrama del mapa de memoria

```
 Dirección virtual
 ┌───────────────────────────────────────────────────────────────┐
 │  18 000 ──── Global Entero   (1 000 celdas)                   │  ← variables globales int
 │  19 000 ──── Global Flotante (1 000 celdas)                   │  ← variables globales float
 ├───────────────────────────────────────────────────────────────┤
 │  20 000 ──── Local Entero    (1 000 celdas)                   │  ← params + locales int
 │  21 000 ──── Local Flotante  (1 000 celdas)                   │  ← params + locales float
 ├───────────────────────────────────────────────────────────────┤
 │  22 000 ──── Temp Entero     (1 000 celdas)                   │  ← temporales int
 │  23 000 ──── Temp Flotante   (1 000 celdas)                   │  ← temporales float
 │  24 000 ──── Temp Bool       (1 000 celdas)                   │  ← resultados relacionales
 ├───────────────────────────────────────────────────────────────┤
 │  25 000 ──── Const Entero    (1 000 celdas)                   │  ← literales enteras
 │  26 000 ──── Const Flotante  (1 000 celdas)                   │  ← literales flotantes
 │  27 000 ──── Const Cadena    (1 000 celdas)                   │  ← literales de cadena
 └───────────────────────────────────────────────────────────────┘
                                                        27 999
```

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
// Base y tope del segmento
int base = VirtualMemoryMap.BaseOf(MemorySegment.GlobalInt);   // 18_000
int end  = VirtualMemoryMap.EndOf(MemorySegment.GlobalInt);    // 18_999

// ¿A qué segmento pertenece esta dirección?
MemorySegment? seg = VirtualMemoryMap.SegmentOf(18_500);       // GlobalInt
MemorySegment? nil = VirtualMemoryMap.SegmentOf(1_000);        // null

// ¿Está dentro del segmento?
bool ok = VirtualMemoryMap.InSegment(18_500, MemorySegment.GlobalInt); // true
```

### Asignación de direcciones (estado mutable, para Entrega 5)

```csharp
var map = new VirtualMemoryMap();

// Asignar por segmento
int dir1 = map.Allocate(MemorySegment.GlobalInt);   // 18_000
int dir2 = map.Allocate(MemorySegment.GlobalInt);   // 18_001

// Asignar por tipo + alcance
int dirVar = map.AllocateFor(SemanticType.Entero,   isGlobal: true);   // segmento GlobalInt
int dirLoc = map.AllocateFor(SemanticType.Flotante, isGlobal: false);  // segmento LocalFloat

// Asignar temporal
int dirTmp = map.AllocateTemp(SemanticType.Entero);   // segmento TempInt

// Reiniciar contadores (útil entre compilaciones en tests)
map.Reset();
```

---

## Ejemplo de traducción (Entrega 5 — adelanto)

Con el mapa definido, en la Entrega 5 la frase:

```patito
vars
    a, b: entero;
    x: flotante;
```

…generará la asignación:

| Símbolo | Segmento       | Dirección virtual |
|---------|----------------|-------------------|
| `a`     | Global Entero  | 18 000            |
| `b`     | Global Entero  | 18 001            |
| `x`     | Global Flotante| 19 000            |

Y los cuádruplos ya no contendrán el nombre `a` sino su dirección `18000`:

```
   0  =         18000   _    18000    ← Antes: (=, "42", null, "a")
```

---

## Ver también

- [`estructuras.md`](estructuras.md) — la clase `Symbol` que llevará la dirección asignada.
- [`puntos_neuralgicos.md`](puntos_neuralgicos.md) — los puntos neurálgicos PN-8/PN-9/PN-12 que generan temporales (cuyas direcciones vendrán del segmento Temp).
- [`cuadruplos.md`](cuadruplos.md) — formato de la fila de cuádruplos que recibirá las direcciones virtuales.
