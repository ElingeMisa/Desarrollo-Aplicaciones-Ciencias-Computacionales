# Puntos Neurálgicos del Análisis Semántico

> Documentación de la **Entrega 2** del compilador. Ver el [índice general](README.md) para más contexto.

Un **punto neurálgico** es un instante específico durante el recorrido del árbol de derivación en el que el compilador realiza una acción semántica (registrar un símbolo, validar un tipo, emitir un cuadruplo, etc.). En la implementación con ANTLR4, cada método `EnterX`/`ExitX` del listener `SemanticAnalyzer` puede ser un punto neurálgico. Las reglas gramaticales en las que se enganchan estos métodos están detalladas en [`gramatica.md`](gramatica.md), y las estructuras que se llenan en cada punto, en [`estructuras.md`](estructuras.md).

La siguiente tabla resume los puntos neurálgicos implementados en la Entrega 2, mapeando cada uno a su regla gramatical y a la(s) validación(es) que aplica.

| #     | Punto neurálgico         | Disparador en la gramática              | Acción semántica                                                              | Validaciones                                                                                        |
|-------|--------------------------|------------------------------------------|-------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| PN-1  | `EnterPrograma`          | `programa : KW_PROGRAMA ID ; …`          | Registra `ProgramName` en el directorio. Dispara PN-2 (globales) y PN-3.      | —                                                                                                   |
| PN-2  | `ProcessVars`            | `vars`, `listado_vars`                   | Recorre cada `ids : tipo ;` y añade cada `ID` a la `VariableTable` activa.    | **Variable doblemente declarada** → `VariableRedeclared`.                                          |
| PN-3  | `ProcessFuncs`           | `funcs` (cada función)                   | Crea `FunctionInfo(nombre, returnType)`, lo registra en el directorio, llena su `LocalTable` con parámetros y locales. | **Nombre choca con el del programa** → `NameClashesWithProgram`. **Función ya declarada** → `FunctionRedeclared`. **Parámetro duplicado** → `ParameterRedeclared`. **Local choca con otro local o con un parámetro** → `VariableRedeclared`. |
| PN-4  | `EnterAsigna`            | `asigna : ID = expresion ;`              | Verifica que el `ID` destino exista en el alcance visible (local → global).  | **Variable usada sin declarar** → `UndeclaredVariable`.                                            |
| PN-5  | `EnterFactorSimple`      | `factor : … (OP_MAS|OP_MENOS)? simple_atom` con `simple_atom : ID` | Verifica que el `ID` referenciado en una expresión exista. | **Variable usada sin declarar** → `UndeclaredVariable`.                                            |
| PN-6  | `EnterLlamada`           | `llamada : ID ( args? )`                 | Verifica que la función invocada exista en el directorio.                    | **Función invocada sin declarar** → `UndeclaredFunction`.                                          |
| PN-7  | `EnterFunc_body` / `ExitFunc_body` | `func_body : { vars estatuto* }` | Empuja/saca la `FunctionInfo` activa de una pila de alcances, para que PN-4/PN-5 resuelvan locales antes que globales. | — (es estructural)                                                                                  |

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
| PN-18  | `ExitCall_stmt`           | `call_stmt : llamada ;`                                      | Saca los argumentos de las pilas (en orden inverso al push → LIFO → restituye orden correcto), emite `Param(_, _, argName)` por cada argumento y `Gosub(_, _, funcName)`. |

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

## Ver también

- [`estructuras.md`](estructuras.md) — diseño de las tablas/directorio que cada punto neurálgico lee o escribe.
- [`cubo_semantico.md`](cubo_semantico.md) — la tabla de tipos que se consultará en los puntos neurálgicos de Entrega 3.
- [`gramatica.md`](gramatica.md) — las producciones (`asigna`, `llamada`, `factor`, etc.) que disparan cada `Enter…`.
- [`pruebas.md`](pruebas.md) — pruebas end-to-end que validan cada uno de los puntos neurálgicos.
