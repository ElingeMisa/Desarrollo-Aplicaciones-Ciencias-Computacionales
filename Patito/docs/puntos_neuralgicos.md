# Puntos Neurálgicos del Análisis Semántico

> Documentación actualizada a la **Entrega 5** del compilador. Ver el [índice general](README.md) para más contexto.

Un **punto neurálgico** es un instante específico durante el recorrido del árbol de derivación en el que el compilador realiza una acción semántica (registrar un símbolo, validar un tipo, emitir un cuadruplo, etc.). En la implementación con ANTLR4, cada método `EnterX`/`ExitX` del listener `SemanticAnalyzer` puede ser un punto neurálgico. Las reglas gramaticales en las que se enganchan estos métodos están detalladas en [`gramatica.md`](gramatica.md), y las estructuras que se llenan en cada punto, en [`estructuras.md`](estructuras.md).

La siguiente tabla resume todos los puntos neurálgicos implementados, incluyendo el **PN-0** de la Entrega 5 que habilita la Máquina Virtual.

| #     | Punto neurálgico         | Disparador en la gramática              | Acción semántica                                                              | Validaciones                                                                                        |
|-------|--------------------------|------------------------------------------|-------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| PN-0  | `EnterPrograma` / `EnterCuerpo` | `programa`, `cuerpo` (del programa) | **[Entrega 5]** Emite `Goto "?"` como quad 0 al final de `EnterPrograma`; hace backfill en `EnterCuerpo` cuando el padre es `ProgramaContext`. Garantiza que la VM salte los cuerpos de función y arranque en `inicio{}`. | — |
| PN-1  | `EnterPrograma`          | `programa : KW_PROGRAMA ID ; …`          | Registra `ProgramName` en el directorio. Dispara PN-2 (globales) y PN-3.      | —                                                                                                   |
| PN-2  | `ProcessVars`            | `vars`, `listado_vars`                   | Recorre cada `ids : tipo ;` y añade cada `ID` a la `VariableTable` activa.    | **Variable doblemente declarada** → `VariableRedeclared`.                                          |
| PN-3  | `ProcessFuncs`           | `funcs` (cada función)                   | Crea `FunctionInfo(nombre, returnType)`, lo registra en el directorio, llena su `LocalTable` con parámetros y locales. | **Nombre choca con el del programa** → `NameClashesWithProgram`. **Función ya declarada** → `FunctionRedeclared`. **Parámetro duplicado** → `ParameterRedeclared`. **Local choca con otro local o con un parámetro** → `VariableRedeclared`. |
| PN-4  | `EnterAsigna`            | `asigna : ID = expresion ;`              | Verifica que el `ID` destino exista en el alcance visible (local → global).  | **Variable usada sin declarar** → `UndeclaredVariable`.                                            |
| PN-5  | `EnterFactorSimple`      | `factor : … (OP_MAS|OP_MENOS)? simple_atom` con `simple_atom : ID` | Verifica que el `ID` referenciado en una expresión exista. | **Variable usada sin declarar** → `UndeclaredVariable`.                                            |
| PN-6  | `EnterLlamada`           | `llamada : ID ( args? )`                 | Verifica que la función invocada exista en el directorio.                    | **Función invocada sin declarar** → `UndeclaredFunction`.                                          |
| PN-7  | `EnterFunc_body` / `ExitFunc_body` | `func_body : { vars estatuto* }` | Empuja/saca la `FunctionInfo` activa de una pila de alcances, para que PN-4/PN-5 resuelvan locales antes que globales. | — (es estructural)                                                                                  |
| PN-7b | `EnterFunc_body`                   | `func_body : { vars estatuto* }` | **[Entrega 4]** Registra `FunctionInfo.StartQuad = FilaCuadruplos.Count` justo antes de visitar el cuerpo. Ese índice es el punto de entrada al que apunta `Gosub`. | — |
| PN-7c | `ExitFunc_body`                    | `func_body : { vars estatuto* }` | **[Entrega 4]** Emite `EndFunc(_, _, funcName)` al terminar el cuerpo para que la MV pueda restaurar el contexto de ejecución. | — |

## Orden de ejecución

ANTLR4 recorre el árbol en pre-orden (Enter primero, luego hijos, luego Exit). El listener semántico aprovecha esto así:

1. **`EnterPrograma` (PN-1)** se dispara **antes** que cualquier otro nodo. En ese momento, el contexto `ProgramaContext` ya contiene **todo** el subárbol (porque la fase de parsing terminó), así que PN-1 puede caminar globalmente y poblar el directorio + la tabla global de un golpe (vía PN-2 y PN-3).

2. Cuando el walker desciende a cada `func_body`, **PN-7** empuja la función al stack. A partir de ahí, **PN-4/PN-5/PN-6** que se disparan al visitar estatutos dentro del cuerpo resuelven nombres contra la `LocalTable` activa primero.

3. Al salir del `func_body`, **PN-7** hace pop. Cuando el walker llega al `cuerpo` principal (entre `inicio` y `fin`), el stack está vacío, así que PN-4/PN-5 resuelven solo contra la `GlobalTable`.

## Ejemplo paso a paso

Programa:

```patito
programa demo;
vars
    a: entero;

nula incrementar (n: entero) {
    vars
        i: entero;
    i = 0;
    mientras (i < n) haz {
        i = i + 1;
    };
};

inicio {
    a = 5;
    incrementar(a);
} fin
```

Eventos del walker en orden, y qué hace el analyzer:

1. `EnterPrograma` (**PN-1**) →
   - `ProgramName = "demo"`.
   - **PN-2** sobre vars globales → registra `a: entero` en `GlobalTable`.
   - **PN-3** sobre `funcs`:
     - Crea `FunctionInfo("incrementar", Nula)`, registra en el directorio.
     - **PN-2** sobre parámetros → `n: entero` en `incrementar.LocalTable` (con `Kind=Parameter`).
     - **PN-2** sobre vars locales (`vars i: entero;`) → `i: entero` en la misma tabla.
2. `EnterFunc_body` de `incrementar` (**PN-7**) → pila: `[incrementar]`.
3. `EnterAsigna(i = 0)` (**PN-4**) → busca `i`: encontrada en local. OK.
4. `EnterFactorSimple(0)` → es constante, no aplica.
5. `EnterFactorSimple(i)` (**PN-5**) → encontrada. OK.
6. `EnterFactorSimple(n)` (**PN-5**) → encontrada (parámetro). OK.
7. `EnterAsigna(i = i + 1)` (**PN-4**) y dos `EnterFactorSimple` → todo OK.
8. `ExitFunc_body` (**PN-7**) → pila vacía.
9. `EnterAsigna(a = 5)` (**PN-4**) → busca `a`: no hay alcance local activo, busca en `GlobalTable`. Encontrada. OK.
10. `EnterLlamada(incrementar(a))` (**PN-6**) → presente en directorio. OK.
11. `EnterFactorSimple(a)` (**PN-5**) → global. OK.

Salida final: tablas pobladas, 0 errores semánticos.

## Validaciones — códigos de error

Cada validación emite un `SemanticError` con un `SemanticErrorCode` estable. Esto desacopla los tests del texto del mensaje, y nos permite traducir mensajes sin romper la suite.

| Código                    | Significado                                              |
|---------------------------|----------------------------------------------------------|
| `VariableRedeclared`      | El nombre ya existía en la misma `VariableTable`.        |
| `ParameterRedeclared`     | Dos parámetros con el mismo nombre en la misma función.  |
| `FunctionRedeclared`      | Dos funciones con el mismo nombre.                       |
| `NameClashesWithProgram`  | Una función usa el mismo identificador que el programa.  |
| `UndeclaredVariable`      | Uso de una variable sin declararla antes.                |
| `UndeclaredFunction`      | Llamada a una función no registrada en el directorio.    |

---

## Puntos neurálgicos de la Entrega 3 — Generación de cuádruplos

Los siguientes puntos neurálgicos extienden la tabla de la Entrega 2. Todos operan sobre las estructuras de `QuadrupleEmitter` (`PilaOperadores`, `PilaOperandos`, `PilaTipos`, `FilaCuadruplos`) descritas en [`estructuras.md`](estructuras.md).

| #      | Punto neurálgico          | Disparador en la gramática                                   | Acción semántica                                                                                                   |
|--------|---------------------------|--------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------|
| PN-8   | `ExitFactorSimple`        | `factor : (±)? simple_atom  # FactorSimple`                  | Determina nombre y tipo del operando (ID, constante entera o flotante). Maneja signo unario negativo. Llama `PushOperand(name, type)` en PilaOperandos y PilaTipos. |
| PN-9   | `ExitTermino`             | `termino : factor ((* \| /) factor)*`                        | Si hay más de un factor: saca todos los operandos de las pilas, itera los operadores `*`/`/` de `ctx.children`, llama `EmitBinary` por cada par (izquierda-derecha), empuja el temporal resultante. |
| PN-10  | `ExitExp`                 | `exp : termino ((+ \| -) termino)*`                          | Idéntico a PN-9 pero para `+`/`-` (menor precedencia). Un solo termino → no hace nada. |
| PN-11  | `ExitExpresion`           | `expresion : exp ( rel_op exp )?`                            | Si hay `rel_op`: saca los dos resultados de las pilas, llama `EmitBinary` con el operador relacional (`<`,`>`,`==`,`!=`), empuja el temporal Bool. Siempre llama `MaybeEmitGotoF`. |
| PN-11b | `MaybeEmitGotoF` (helper) | Llamado al final de `ExitExpresion`                          | Si `ctx.Parent` es `CondicionContext` o `CicloContext`: saca el resultado de la condición de las pilas, emite `GotoF(cond, _, ?)` en FilaCuadruplos, apila el índice del cuádruplo en `_pendingGotoF` para Backfill posterior. |
| PN-12  | `ExitAsigna`              | `asigna : ID = expresion ;`                                  | Saca el resultado de la expresión de las pilas. Consulta `SemanticCube.Resolve(destType, Assign, exprType)`: si es Error → reporta `TypeMismatch`; si es OK → emite `Assign(exprName, _, destID)` en FilaCuadruplos. |
| PN-13  | `ExitImp`                 | `imp : expresion \| LETRERO`                                 | Si es LETRERO: emite `Print(_, _, "texto")`. Si es expresión: saca el resultado de las pilas y emite `Print(_, _, resultName)`. |
| PN-14  | `EnterCiclo`              | `ciclo : mientras ( expresion ) haz cuerpo ;`                | Registra el índice actual de FilaCuadruplos en `_cicloStart` (pila). Ese índice es el destino del Goto de retorno al comienzo del ciclo. |
| PN-15  | `ExitCuerpo`              | `cuerpo : { estatuto* }`  (cuando padre es CondicionContext) | Solo para el **primer** `cuerpo` de una condición que tiene `sino`: emite `Goto(_, _, ?)` (para saltar el bloque sino), apila su índice en `_pendingGoto`, y hace `Backfill` del GotoF pendiente apuntando al inicio del sino. |
| PN-16  | `ExitCondicion`           | `condicion : si ( expresion ) cuerpo (sino cuerpo)? ;`       | **Con sino**: hace `Backfill(_pendingGoto.Pop(), current)` para que el Goto salte después del sino. **Sin sino**: hace `Backfill(_pendingGotoF.Pop(), current)` para que el GotoF salte después del si. |
| PN-17  | `ExitCiclo`               | `ciclo : mientras ( expresion ) haz cuerpo ;`                | Emite `Goto(_, _, start)` donde `start = _cicloStart.Pop()`. Luego hace `Backfill(_pendingGotoF.Pop(), current)` para que el GotoF salte después del ciclo completo. |
| PN-18  | `ExitCall_stmt`           | `call_stmt : llamada ;`                                      | **[Entrega 4]** Saca los argumentos de las pilas. Emite `ERA(_, _, funcName)` para reservar el espacio de activación, luego `Param(_, _, argName)` por cada argumento y finalmente `Gosub(funcName, _, startQuad)` donde `startQuad = FunctionInfo.StartQuad`. |

---

## Puntos neurálgicos de la Entrega 4 — Funciones completas

La Entrega 4 extiende PN-7 y actualiza PN-18 para completar el ciclo de vida de las funciones en la fila de cuádruplos.

### Diagrama de ciclo de vida de una función

```
DECLARACIÓN (antes de inicio)
   EnterFunc_body ──[PN-7b]──→ info.StartQuad = FilaCuadruplos.Count
   [cuadruplos del cuerpo]
   ExitFunc_body  ──[PN-7c]──→ Emit(EndFunc, _, _, funcName)

INVOCACIÓN (dentro de inicio o de otra función)
   ExitCall_stmt  ──[PN-18]──→ Emit(ERA, _, _, funcName)
                               Emit(Param, _, _, arg_1)
                               ...
                               Emit(Param, _, _, arg_n)
                               Emit(Gosub, funcName, _, startQuad)
```

### Tabla de puntos neurálgicos de Entrega 4

| #      | Punto neurálgico  | Disparador                        | Acción semántica                                                                           |
|--------|-------------------|-----------------------------------|--------------------------------------------------------------------------------------------|
| PN-7b  | `EnterFunc_body`  | `func_body : { vars estatuto* }`  | Registra `FunctionInfo.StartQuad = FilaCuadruplos.Count` (índice del primer cuádruplo del cuerpo). |
| PN-7c  | `ExitFunc_body`   | `func_body : { vars estatuto* }`  | Emite `EndFunc(_, _, funcName)` para señalar el fin del cuerpo de la función.              |
| PN-18  | `ExitCall_stmt`   | `call_stmt : llamada ;`           | Emite `ERA + Param* + Gosub` con `Gosub.Left = funcName` y `Gosub.Result = startQuad`.    |

### Ejemplo de secuencia para `saludar(a)`

```
// Declaración de saludar:
EnterFunc_body(saludar) [PN-7b] → saludar.StartQuad = 0
  quad[0]  =       0     _    i
  quad[1]  <       i     n    t0
  quad[2]  GotoF   t0    _    8
  quad[3]  Print   _     _    "hola numero"
  quad[4]  Print   _     _    i
  quad[5]  +       i     1    t1
  quad[6]  =       t1    _    i
  quad[7]  Goto    _     _    1
ExitFunc_body(saludar) [PN-7c] → emit quad[8] EndFunc _ _ saludar

// Cuerpo principal:
  quad[9]  =       3     _    a
ExitCall_stmt(saludar(a)) [PN-18] →
  quad[10] ERA     _     _    saludar
  quad[11] Param   _     _    a
  quad[12] Gosub   saludar  _  0     ← Result = saludar.StartQuad
```

### Orden de ejecución (Entrega 3)

Los nuevos puntos neurálgicos se encadenan en el recorrido del árbol de la siguiente manera para la expresión `a + b * c`:

```
EnterFactorSimple(a) → ExitFactorSimple(a)  → PilaOperandos=[a]  PilaTipos=[Entero]
EnterFactorSimple(b) → ExitFactorSimple(b)  → PilaOperandos=[a,b]  PilaTipos=[Entero,Entero]
EnterFactorSimple(c) → ExitFactorSimple(c)  → PilaOperandos=[a,b,c]  PilaTipos=[Entero,Entero,Entero]
ExitTermino(b*c)     → EmitBinary(*, b, c)  → emit "t0 = b * c"  PilaOperandos=[a,t0]
ExitExp(a + t0)      → EmitBinary(+, a, t0) → emit "t1 = a + t0" PilaOperandos=[t1]
ExitExpresion(...)   → no rel_op, MaybeEmitGotoF → parent no es si/mientras → nada
```

### Ejemplo de control de flujo — `si/sino`

Para `si (x > 0) { ... } sino { ... };`:

```
ExitExpresion(x > 0)  → emit "t0 = x > 0"
MaybeEmitGotoF        → emit "GotoF t0 _ ?"   [índice=3, guardado en _pendingGotoF]
  ... (quads del si-body) ...
ExitCuerpo(si-body)   → emit "Goto _ _ ?"      [índice=7, guardado en _pendingGoto]
                        Backfill(3, "8")        [GotoF apunta al inicio del sino]
  ... (quads del sino-body) ...
ExitCondicion         → Backfill(7, "10")       [Goto apunta después del sino]
```

### Ejemplo de control de flujo — `mientras`

Para `mientras (i < n) haz { ... };`:

```
EnterCiclo            → _cicloStart.Push(0)    [inicio = cuad #0]
ExitExpresion(i < n)  → emit "t0 = i < n"
MaybeEmitGotoF        → emit "GotoF t0 _ ?"   [índice=1, guardado en _pendingGotoF]
  ... (quads del cuerpo) ...
ExitCiclo             → emit "Goto _ _ 0"      [regresa al inicio]
                        Backfill(1, "5")        [GotoF apunta después del ciclo]
```

---

## PN-7b y PN-18 con direcciones virtuales (Entrega 5)

### PN-7b — `ResetTemps()` por función

Al entrar al `func_body` de cada función, `_emitter.ResetTemps()` reinicia los contadores de `TempInt` (22000), `TempFloat` (23000) y `TempBool` (24000). Esto garantiza que cada activación de la función reutiliza las mismas direcciones del segmento Temp desde el offset 0, sin solaparse con los temporales del llamador (que se salvan en la call stack junto con el `_activeLocal` del frame).

### PN-18 — `ExitCall_stmt` con direcciones virtuales

El cuádruplo `Param _ _ argName` lleva el **nombre** del argumento real (e.g. `a`). La VM resuelve su dirección virtual en tiempo de ejecución consultando el `AddressBook`. Esto es correcto porque:

1. `a` siempre tiene una dirección virtual fija asignada en compilación.
2. Al procesar `Param`, la VM conoce qué función es la pendiente (del ERA previo) y puede buscar la dirección del parámetro formal correspondiente en `funcDir.LocalTable`.
3. El valor leído de la dirección de `a` se copia al `LocalMemory` del nuevo frame en la dirección del parámetro formal.

Ejemplo para `suma(a, b)` donde `a → 18000`, `b → 18001`, `x → 20000`, `y → 20001`:

```
ERA   _  _  suma          → crear pendingRecord("suma")
Param _  _  18000(a)      → GetValue("a")=3 → PushArg(20000, 3)
Param _  _  18001(b)      → GetValue("b")=4 → PushArg(20001, 4)
Gosub suma  _  startQ     → LocalMemory[20000]=3, LocalMemory[20001]=4
                             callStack.push(pc+1, mainFrame)
                             _activeLocal = suma.LocalMemory
                             pc = startQ
```

---

## PN-0 — Goto inicial al inicio{} (Entrega 5)

Este punto neurálgico fue necesario para que la **Máquina Virtual** pudiera ejecutar programas con funciones. Sin él, la VM comenzaba en el cuádruplo 0 — que pertenecía al cuerpo de la primera función declarada — sin que nadie hubiera preparado los parámetros, causando el error:

```
[VM ERROR] La dirección virtual 20000 no fue inicializada antes de su primer uso.
```

### Causa raíz

ANTLR4 recorre el árbol en profundidad. Como `funcs` precede a `inicio` en la producción `programa`, los cuerpos de función se visitan antes que el bloque `inicio{}`. Los cuádruplos de esos cuerpos quedan al inicio de la lista desde el cuádruplo 0, y la VM arranca ahí.

### Solución

El patrón estándar para este problema es emitir un `Goto` incondicionado como **primer cuádruplo** que salte sobre todos los cuerpos de función y aterrice justo en el primer cuádruplo de `inicio{}`. El destino no se conoce todavía al emitirlo, así que se usa Backfill.

**Implementación en `SemanticAnalyzer.cs`:**

```csharp
// Campo nuevo
private int _mainGotoIdx = -1;

// Al final de EnterPrograma (después de registrar vars y funciones):
_mainGotoIdx = _emitter.Fila.Emit(QuadOp.Goto, null, null, "?");

// Nuevo override:
public override void EnterCuerpo(PatitoParser.CuerpoContext ctx)
{
    // Solo aplica cuando el padre es ProgramaContext (el cuerpo del inicio{})
    if (ctx.Parent is PatitoParser.ProgramaContext && _mainGotoIdx >= 0)
        _emitter.Fila.Backfill(_mainGotoIdx, _emitter.Fila.Count.ToString());
}
```

### Estructura resultante de la fila de cuádruplos

```
quad[0]   Goto  _  _  N          ← salta sobre todos los cuerpos de función
quad[1]   ...                    ← cuerpo de función 1
quad[K]   EndFunc  _  _  func1
quad[K+1] ...                    ← cuerpo de función 2
quad[M]   EndFunc  _  _  func2
quad[N]   ...                    ← primer cuádruplo de inicio{} ← la VM empieza aquí
```

Las funciones son alcanzables únicamente vía `Gosub`, que salta directamente a su `StartQuad`. El `Goto` inicial es inocuo en programas sin funciones (salta a N=1).

---

## Ver también

- [`estructuras.md`](estructuras.md) — diseño de las tablas/directorio que cada punto neurálgico lee o escribe.
- [`memoria_ejecucion.md`](memoria_ejecucion.md) — cómo la VM usa el Goto inicial para arrancar en el lugar correcto.
- [`cubo_semantico.md`](cubo_semantico.md) — la tabla de tipos que se consulta en los puntos neurálgicos de Entrega 3.
- [`gramatica.md`](gramatica.md) — las producciones (`asigna`, `llamada`, `factor`, etc.) que disparan cada `Enter…`.
- [`pruebas.md`](pruebas.md) — pruebas end-to-end que validan cada uno de los puntos neurálgicos.
