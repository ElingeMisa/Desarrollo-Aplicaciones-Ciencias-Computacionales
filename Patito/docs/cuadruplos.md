# Generación de Código Intermedio — Cuádruplos

> Documentación actualizada a la **Entrega 5** del compilador. Ver el [índice general](README.md) para más contexto.

Un **cuádruplo** es la unidad mínima de código intermedio: una instrucción de la forma `(Op, Left, Right, Result)` que puede ser ejecutada directamente por una máquina virtual o traducida a código objeto. Este documento explica el formato de los cuádruplos de Patito, los algoritmos que los generan y el resultado final para varios programas de prueba.

Las **estructuras de datos** que soportan la generación (pilas y fila) están descritas en [`estructuras.md`](estructuras.md#estructuras-de-la-entrega-3--generación-de-cuádruplos).  
Los **puntos neurálgicos** que disparan cada algoritmo están mapeados en [`puntos_neuralgicos.md`](puntos_neuralgicos.md), incluyendo **PN-0** (Goto inicial) que habilita la ejecución por la Máquina Virtual.  
La **Máquina Virtual** que interpreta estos cuádruplos está documentada en [`memoria_ejecucion.md`](memoria_ejecucion.md).

---

## Formato de un cuádruplo

```
(Op, Left, Right, Result)
```

| Campo    | Tipo        | Significado                                              |
|----------|-------------|----------------------------------------------------------|
| `Op`     | `QuadOp`    | La operación a realizar.                                 |
| `Left`   | `string?`   | Primer operando o condición. `null` si no aplica.        |
| `Right`  | `string?`   | Segundo operando. `null` si no aplica.                   |
| `Result` | `string`    | Destino de la operación, nombre del temporal o índice de salto. |

El campo `Index` (número de cuádruplo, base 0) lo asigna `FilaCuadruplos.Emit` automáticamente.

### Catálogo de operaciones (`QuadOp`)

| `QuadOp`        | Formato                               | Significado                                         |
|-----------------|---------------------------------------|-----------------------------------------------------|
| `Plus`          | `(+, L, R, T)`                        | `T = L + R`                                         |
| `Minus`         | `(-, L, R, T)`                        | `T = L - R`                                         |
| `Times`         | `(*, L, R, T)`                        | `T = L * R`                                         |
| `Divide`        | `(/, L, R, T)`                        | `T = L / R`  → resultado siempre `Flotante`         |
| `Lt`            | `(<, L, R, T)`                        | `T = L < R`  → resultado `Bool`                     |
| `Gt`            | `(>, L, R, T)`                        | `T = L > R`  → resultado `Bool`                     |
| `Eq`            | `(==, L, R, T)`                       | `T = L == R` → resultado `Bool`                     |
| `Neq`           | `(!=, L, R, T)`                       | `T = L != R` → resultado `Bool`                     |
| `Assign`        | `(=, expr, null, dest)`               | `dest = expr`                                       |
| `Neg`           | `(neg, null, var, T)`                 | `T = -var`  — negación unaria de variable           |
| `GotoF`         | `(GotoF, cond, null, N)`              | `if !cond goto quad[N]`                             |
| `Goto`          | `(Goto, null, null, N)`               | `goto quad[N]`                                      |
| `Print`         | `(Print, null, null, val)`            | `escribe(val)`                                      |
| `Era`           | `(ERA, null, null, func)`             | Reserva el Espacio de Registro de Activación para `func` antes de pasar argumentos |
| `Param`         | `(Param, null, null, arg)`            | Pasa el argumento `arg` a la función siguiente      |
| `Gosub`         | `(Gosub, func, null, startQ)`         | Llama a `func`; `Left=nombre`, `Result=startQuad`   |
| `EndFunc`       | `(EndFunc, null, null, func)`         | Marca el fin del cuerpo de `func`                   |
| `Return`        | `(Return, expr, null, "{func}_ret")`  | **[Entrega 6]** Copia el valor de `expr` a la dirección global reservada para el retorno de `func` (registrada en el `AddressBook` como `"{func}_ret"`). Emitido por `regresa <expr>;` (PN-19). |

---

## Algoritmo de traducción

La generación de cuádruplos ocurre **durante el mismo recorrido del árbol** que realiza el análisis semántico. Los algoritmos se implementan como métodos `Exit…` del listener `SemanticAnalyzer`, aprovechando que ANTLR4 los invoca cuando todos los hijos del nodo ya fueron visitados — es decir, cuando los operandos ya están disponibles en las pilas.

El orquestador central es `QuadrupleEmitter`, que posee las tres pilas y la fila y expone los métodos `PushOperand`, `NewTemp` y `EmitBinary`.

---

### PN-8 · `ExitFactorSimple` — Operandos hoja

Disparo: nodo `factor : (OP_MAS | OP_MENOS)? simple_atom  # FactorSimple`.

Este es el **punto de entrada** de los operandos a las pilas. Determina nombre y tipo del operando y llama `PushOperand(nombre, tipo)`:

```
Si atom es ID
    nombre ← id
    tipo   ← tabla de símbolos [id].Type  (Error si no existe)
Si atom es CTE_ENT
    nombre ← texto del token  (p.ej. "42")
    tipo   ← Entero
Si atom es CTE_FLOT
    nombre ← texto del token  (p.ej. "3.14")
    tipo   ← Flotante

Si tiene OP_MENOS (signo unario negativo):
    Si atom es ID   → emitir (Neg, null, nombre, t_k);  nombre ← t_k
    Si atom es cte  → nombre ← "-" + nombre  (p.ej. "-5")

PushOperand(nombre, tipo)
```

> El signo unario positivo (`OP_MAS`) no genera ningún cuádruplo: el valor se apila sin modificar.

---

### PN-9 · `ExitTermino` — Multiplicación y División

Disparo: nodo `termino : factor ((OP_POR | OP_DIV) factor)*`.

Si el término tiene más de un factor, los `n` operandos ya están en las pilas (apilados por los `ExitFactorSimple` hijos). El algoritmo los saca y procesa los operadores `*` y `/` de **izquierda a derecha**:

```
n ← número de factores en el término
Si n == 1: no hacer nada (el factor ya está en las pilas)

Si n > 1:
    Sacar los n pares (nombre, tipo) de las pilas en orden inverso → arrayNames, arrayTypes
    left ← (arrayNames[0], arrayTypes[0])
    Para cada OP_POR / OP_DIV entre factores consecutivos (índice opIdx):
        right ← (arrayNames[opIdx+1], arrayTypes[opIdx+1])
        (tempName, tempType) ← EmitBinary(Times|Divide, left.name, left.type, right.name, right.type)
        left ← (tempName, tempType)
    PushOperand(left.name, left.type)   // resultado final del término
```

`EmitBinary` consulta el cubo semántico, genera el nombre `t_k` con `NewTemp()` y emite el cuádruplo en `FilaCuadruplos`.

---

### PN-10 · `ExitExp` — Suma y Resta

Disparo: nodo `exp : termino ((OP_MAS | OP_MENOS) termino)*`.

Algoritmo idéntico a PN-9 pero para `+` y `-`. La clave es que **los `ExitTermino` se disparan antes** que `ExitExp` (porque `termino` es hijo de `exp`), por lo que la precedencia `*` / `/` > `+` / `-` está garantizada por la jerarquía de la gramática sin ninguna lógica adicional.

---

### PN-11 · `ExitExpresion` — Operadores relacionales

Disparo: nodo `expresion : exp ( rel_op exp )?`.

```
Si hay rel_op:
    right ← (Tipos.Pop(), Operandos.Pop())
    left  ← (Tipos.Pop(), Operandos.Pop())
    op    ← Lt | Gt | Eq | Neq  según el token rel_op
    (tempName, tempType) ← EmitBinary(op, left.name, left.type, right.name, right.type)
    PushOperand(tempName, tempType)   // tempType = Bool

Llamar MaybeEmitGotoF(ctx)
```

#### PN-11b · `MaybeEmitGotoF` (helper)

```
Si ctx.Parent NO es CondicionContext ni CicloContext → retornar
Si Tipos está vacía → retornar
Tipos.Pop(); condName ← Operandos.Pop()
gfIdx ← Fila.Emit(GotoF, condName, null, "?")   // destino desconocido
_pendingGotoF.Push(gfIdx)
```

---

### PN-12 · `ExitAsigna` — Asignación

Disparo: nodo `asigna : ID OP_ASIGNA expresion SEMICOLON`.

```
destName ← ID.text
destSym  ← LookupVariable(destName)
exprType ← Tipos.Pop()
exprName ← Operandos.Pop()

Si destSym es null → retornar  (UndeclaredVariable ya reportado en PN-4)

resultType ← SemanticCube.Resolve(destSym.Type, Assign, exprType)
Si resultType == Error:
    Reportar TypeMismatch
    Retornar
Emitir (Assign, exprName, null, destName)
```

---

### PN-13 · `ExitImp` — Impresión

Disparo: nodo `imp : expresion | LETRERO` (uno por cada argumento de `escribe`).

```
Si imp contiene LETRERO:
    Emitir (Print, null, null, "\"texto\"")
Si imp contiene expresion:
    Tipos.Pop(); exprName ← Operandos.Pop()
    Emitir (Print, null, null, exprName)
```

---

### PN-14 · `EnterCiclo` — Registro del inicio del ciclo

Disparo: nodo `ciclo : KW_MIENTRAS ...`, al **entrar** (antes de visitar hijos).

```
_cicloStart.Push(Fila.Count)
```

Guarda el índice del próximo cuádruplo que se emitirá (el primer cuádruplo de la condición del ciclo). Este valor será el destino del `Goto` de retorno.

---

### PN-15 · `ExitCuerpo` — Salto entre si-body y sino-body

Disparo: nodo `cuerpo`, solo cuando el padre es una `condicion` **con** rama `sino`, y únicamente para el **primer** `cuerpo` (el bloque del `si`).

```
gotoIdx ← Fila.Emit(Goto, null, null, "?")   // salto al final del sino, destino pendiente
_pendingGoto.Push(gotoIdx)

gfIdx ← _pendingGotoF.Pop()
Fila.Backfill(gfIdx, Fila.Count.ToString())   // GotoF apunta al inicio del bloque sino
```

---

### PN-16 · `ExitCondicion` — Resolución final del condicional

Disparo: nodo `condicion : KW_SI ...` al salir.

```
current ← Fila.Count
Si hay KW_SINO:
    Fila.Backfill(_pendingGoto.Pop(), current.ToString())   // Goto salta después del sino
Sino:
    Fila.Backfill(_pendingGotoF.Pop(), current.ToString())  // GotoF salta después del si
```

---

### PN-17 · `ExitCiclo` — Cierre del ciclo

Disparo: nodo `ciclo` al salir (después de visitar el cuerpo).

```
Fila.Emit(Goto, null, null, _cicloStart.Pop().ToString())    // regresa al inicio
Fila.Backfill(_pendingGotoF.Pop(), Fila.Count.ToString())    // GotoF sale del ciclo
```

---

### PN-18 · `ExitCall_stmt` — Llamadas a función

Disparo: nodo `call_stmt : llamada SEMICOLON`.

```
funcName ← llamada.ID().text
nArgs    ← llamada.args()?.expresion()?.Length ?? 0

argNames ← array de nArgs strings
Para i desde nArgs-1 hasta 0:
    Tipos.Pop()
    argNames[i] ← Operandos.Pop()    // LIFO → restituye orden original

Para cada arg en argNames:
    Emitir (Param, null, null, arg)

Emitir (Gosub, null, null, funcName)
```

---

## El mecanismo de Backfill

Los saltos condicionales (`GotoF`) e incondicionales (`Goto`) se emiten con destino `"?"` cuando aún no se conoce la posición de llegada. El mecanismo de **Backfill** los resuelve en O(1) una vez que se sabe la posición correcta:

```csharp
// FilaCuadruplos.Backfill
public void Backfill(int index, string newResult)
{
    _list[index] = _list[index] with { Result = newResult };
}
```

El flujo completo de un `si/sino` muestra la interacción de los cuatro puntos involucrados:

```
ExitExpresion  → Emit(Lt, "x", "5", "t0")       quad[0]
MaybeEmitGotoF → Emit(GotoF, "t0", null, "?")   quad[1]  ← _pendingGotoF.Push(1)

  [si-body: quad[2] ... quad[j-1]]

ExitCuerpo     → Emit(Goto, null, null, "?")     quad[j]  ← _pendingGoto.Push(j)
               → Backfill(1, (j+1).ToString())           ← GotoF apunta al sino

  [sino-body: quad[j+1] ... quad[k-1]]

ExitCondicion  → Backfill(j, k.ToString())               ← Goto salta después del sino
```

Y para el ciclo `mientras`:

```
EnterCiclo     → _cicloStart.Push(count)         count = índice de inicio
ExitExpresion  → Emit(Lt, "i", "n", "t0")        quad[inicio]
MaybeEmitGotoF → Emit(GotoF, "t0", null, "?")    ← _pendingGotoF.Push(...)

  [cuerpo del ciclo]

ExitCiclo      → Emit(Goto, null, null, inicio)  ← regresa al inicio
               → Backfill(gfIdx, Fila.Count)     ← GotoF sale del ciclo
```

---

## Programas de prueba — fila de cuádruplos

### Programa 1 · Expresiones aritméticas y asignación

```patito
programa aritmetica;
vars
    a, b, c: entero;
inicio {
    a = 2;
    b = 3;
    c = a + b * 4;
    escribe("resultado:", c);
} fin
```

**Traza de pilas para `c = a + b * 4`:**

| Evento | PilaOperandos | PilaTipos | FilaCuadruplos (nueva entrada) |
|--------|--------------|-----------|-------------------------------|
| ExitFactorSimple(`a`) | `[a]` | `[Ent]` | — |
| ExitFactorSimple(`b`) | `[a, b]` | `[Ent, Ent]` | — |
| ExitFactorSimple(`4`) | `[a, b, 4]` | `[Ent, Ent, Ent]` | — |
| ExitTermino(`b * 4`) | `[a, t0]` | `[Ent, Ent]` | `(*, "b", "4", "t0")` |
| ExitExp(`a + t0`) | `[t1]` | `[Ent]` | `(+, "a", "t0", "t1")` |
| ExitAsigna(`c = t1`) | `[]` | `[]` | `(=, "t1", null, "c")` |

**Fila de cuádruplos completa:**

```
#     Op        Left          Right         Result
────────────────────────────────────────────────────
   0  =         2             _             a
   1  =         3             _             b
   2  *         b             4             t0
   3  +         a             t0            t1
   4  =         t1            _             c
   5  Print     _             _             "resultado:"
   6  Print     _             _             c
```

> `t0` almacena `b * 4` (PN-9); `t1` almacena `a + t0` (PN-10). La precedencia está garantizada por la gramática.

---

### Programa 2 · Condicional `si/sino` (`03_condicion.patito`)

```patito
programa decide;
vars
    edad: entero;
inicio {
    edad = 18;
    si (edad < 18) {
        escribe("menor de edad");
    } sino {
        escribe("mayor o igual a 18");
    };
} fin
```

**Fila de cuádruplos:**

```
#     Op        Left          Right         Result
────────────────────────────────────────────────────
   0  =         18            _             edad
   1  <         edad          18            t0
   2  GotoF     t0            _             5     ← Backfill en PN-15
   3  Print     _             _             "menor de edad"
   4  Goto      _             _             6     ← Backfill en PN-16
   5  Print     _             _             "mayor o igual a 18"
```

> `GotoF[2]` apunta al cuádruplo 5 (inicio del `sino`). El `Goto[4]` apunta al 6 (después del estatuto). Ambos destinos se resuelven mediante Backfill: el `GotoF` en **PN-15** (`ExitCuerpo`) y el `Goto` en **PN-16** (`ExitCondicion`).

---

### Programa 3 · Ciclo `mientras/haz` (`04_ciclo.patito`)

```patito
programa cuenta;
vars
    i: entero;
inicio {
    i = 0;
    mientras (i < 5) haz {
        escribe("i =", i);
        i = i + 1;
    };
} fin
```

**Fila de cuádruplos:**

```
#     Op        Left          Right         Result
────────────────────────────────────────────────────
   0  =         0             _             i
   1  <         i             5             t0
   2  GotoF     t0            _             8     ← Backfill en PN-17
   3  Print     _             _             "i ="
   4  Print     _             _             i
   5  +         i             1             t1
   6  =         t1            _             i
   7  Goto      _             _             1     ← ExitCiclo (PN-17)
```

> **PN-14** (`EnterCiclo`) registra `_cicloStart = 1` (el índice del primer cuádruplo de la condición).  
> **PN-17** (`ExitCiclo`) emite `Goto(1)` y hace `Backfill(2, "8")`.

---

### Programa 4 · Función con ciclo interno (`05_funcion.patito`) — Entrega 4

```patito
programa concarga;
vars
    a: entero;

nula saludar (n: entero) {
    vars
        i: entero;
    i = 0;
    mientras (i < n) haz {
        escribe("hola numero", i);
        i = i + 1;
    };
};

inicio {
    a = 3;
    saludar(a);
} fin
```

**Fila de cuádruplos (Entrega 4):**

```
#     Op        Left          Right         Result
────────────────────────────────────────────────────
   0  =         0             _             i          ← cuerpo de saludar (StartQuad=0)
   1  <         i             n             t0
   2  GotoF     t0            _             8          ← Backfill en PN-17
   3  Print     _             _             "hola numero"
   4  Print     _             _             i
   5  +         i             1             t1
   6  =         t1            _             i
   7  Goto      _             _             1
   8  EndFunc   _             _             saludar    ← PN-7c: cierre del cuerpo
   9  =         3             _             a          ← cuerpo principal
  10  ERA       _             _             saludar    ← PN-18: reserva activación
  11  Param     _             _             a          ← PN-18: pasa argumento
  12  Gosub     saludar       _             0          ← PN-18: startQuad=0
```

> **PN-7b** (`EnterFunc_body`) registra `StartQuad = 0` para `saludar`.  
> **PN-7c** (`ExitFunc_body`) emite `EndFunc[8]`.  
> **PN-18** (`ExitCall_stmt`) emite la secuencia `ERA[10] + Param[11] + Gosub[12]`, con `Gosub.Left = "saludar"` y `Gosub.Result = "0"` (el `StartQuad` registrado en PN-7b).

### Programa 5 · Funciones múltiples (`14_cuadruplos_funciones.patito`) — Entrega 4

```patito
programa funciones;
vars
    a, b: entero;

nula doble (x: entero) {
    vars
        resultado: entero;
    resultado = x + x;
    escribe("doble:", resultado);
};

inicio {
    a = 5;
    doble(a);
    b = 2;
    doble(b);
} fin
```

**Fila de cuádruplos:**

```
#     Op        Left          Right         Result
────────────────────────────────────────────────────
   0  +         x             x             t0        ← cuerpo de doble (StartQuad=0)
   1  =         t0            _             resultado
   2  Print     _             _             "doble:"
   3  Print     _             _             resultado
   4  EndFunc   _             _             doble      ← PN-7c
   5  =         5             _             a          ← cuerpo principal
   6  ERA       _             _             doble      ← primera llamada
   7  Param     _             _             a
   8  Gosub     doble         _             0
   9  =         2             _             b          ← segunda llamada
  10  ERA       _             _             doble
  11  Param     _             _             b
  12  Gosub     doble         _             0
```

> Cada llamada genera su propia secuencia `ERA + Param + Gosub` independiente. El `StartQuad` es siempre el mismo `0` porque el cuerpo de `doble` no cambia entre llamadas.

---

## Precedencia de operadores — garantía estructural

La precedencia no requiere ninguna lógica explícita en los algoritmos. Está codificada directamente en la jerarquía de la gramática:

```
expresion
  └─ exp               ← nivel + / -   (ExitExp   = PN-10)
       └─ termino      ← nivel * / /   (ExitTermino = PN-9)
            └─ factor  ← hoja          (ExitFactorSimple = PN-8)
```

Como ANTLR4 visita en pre-orden y llama los `Exit…` en post-orden, los `ExitTermino` siempre se ejecutan **antes** que los `ExitExp` del nivel superior, lo que garantiza que `*` y `/` se evalúen primero.

---

## Cuádruplos con formato DIR(NOMBRE) — Entrega 5

A partir de la Entrega 5, cada operando puede imprimirse con su **dirección virtual** antepuesta, usando el método `Quadruple.Format(addressBook)`:

```
#     Op        Left                 Right                Result
----  --------  -------------------  -------------------  -------------------
   0  =         25000(10)            _                    18000(n)
   1  <         18000(n)             25001(5)             24000(t0)
   2  GotoF     24000(t0)            _                    6
   3  Print     _                    _                    27000("k =")
   4  Print     _                    _                    18000(n)
   5  Goto      _                    _                    1
   6  ERA        _                   _                    saludar
   7  Param     _                    _                    18000(n)
   8  Gosub     saludar              _                    0
```

### Regla de enrutamiento

| Segmento | Rango | Quién escribe | Ejemplo |
|----------|-------|---------------|---------|
| `GlobalInt` | 18000–18999 | Variables globales enteras | `n → 18000` |
| `GlobalFloat` | 19000–19999 | Variables globales flotantes | `pi → 19000` |
| `LocalInt` | 20000–20999 | Parámetros y locales de función | `limite → 20000` |
| `LocalFloat` | 21000–21999 | Parámetros y locales flotantes | — |
| `TempInt` | 22000–22999 | Temporales enteros (reset por función) | `t0 → 22000` |
| `TempFloat` | 23000–23999 | Temporales flotantes | — |
| `TempBool` | 24000–24999 | Resultados de operadores relacionales | `t0 → 24000` |
| `ConstInt` | 25000–25999 | Constantes enteras literales | `42 → 25000` |
| `ConstFloat` | 26000–26999 | Constantes flotantes literales | `3.14 → 26000` |
| `ConstString` | 27000–27999 | Cadenas literales | `"hola" → 27000` |

### Output real para `14_cuadruplos_funciones.patito`

Compilando con `patitoc examples/14_cuadruplos_funciones.patito --quads` se obtiene un listado en el formato arriba. Los operandos del programa principal usan el segmento `Global*` (18000+), los parámetros de `imprimirHasta` y `doble` usan `Local*` (20000+), y los temporales de las condiciones y ciclos usan `TempBool` (24000+).

---

## Ver también

- [`estructuras.md`](estructuras.md) — diseño de `PilaOperadores`, `PilaOperandos`, `PilaTipos`, `FilaCuadruplos`, `QuadrupleEmitter` y las nuevas estructuras de la VM.
- [`puntos_neuralgicos.md`](puntos_neuralgicos.md) — tabla completa PN-1 a PN-19 con disparadores y acciones (incluye PN-19 / `regresa`).
- [`cubo_semantico.md`](cubo_semantico.md) — tabla de tipos consultada en `EmitBinary`.
- [`pruebas.md`](pruebas.md) — suite de pruebas unitarias de generación de código (`CodeGenTests.cs`) y tests de la VM (`VirtualMachineTests.cs`).
- [`gramatica.md`](gramatica.md) — las producciones (`asigna`, `condicion`, `ciclo`, `factor`…) que disparan cada punto neurálgico.
