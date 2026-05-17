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

## Qué NO se hace (todavía)

Los siguientes son puntos neurálgicos planeados para la Entrega 3 (generación de cuadruplos con cubo activado):

- `ExitFactor` / `ExitTermino` / `ExitExp`: apilar el tipo del operando y disparar el cubo para validar la operación binaria.
- `EnterAsigna` / `ExitAsigna`: además de chequear que el destino exista, consultar el cubo en `Resolve(destinoType, Assign, expresionType)` y reportar incompatibilidad de tipos.
- `EnterCondicion` / `EnterCiclo`: verificar que la expresión de control tiene tipo `Bool`.
- `EnterImp`: validar que la expresión impresa tiene un tipo imprimible.
- `EnterLlamada` con args: validar la aridad y el tipo de cada argumento contra `FunctionInfo.ParameterTypes`.

Toda la infraestructura (cubo, pila de alcances, tablas pobladas) ya está en su lugar; lo que falta es apilar tipos durante el recorrido, que es la siguiente entrega.

## Ver también

- [`estructuras.md`](estructuras.md) — diseño de las tablas/directorio que cada punto neurálgico lee o escribe.
- [`cubo_semantico.md`](cubo_semantico.md) — la tabla de tipos que se consultará en los puntos neurálgicos de Entrega 3.
- [`gramatica.md`](gramatica.md) — las producciones (`asigna`, `llamada`, `factor`, etc.) que disparan cada `Enter…`.
- [`pruebas.md`](pruebas.md) — pruebas end-to-end que validan cada uno de los puntos neurálgicos.
