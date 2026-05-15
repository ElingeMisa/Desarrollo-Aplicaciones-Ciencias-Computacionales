# Cubo Semántico de Patito

El cubo semántico es la **tabla de consideraciones semánticas** del lenguaje. Centraliza, en una sola estructura consultable, todas las reglas de compatibilidad de tipos: dado un operador y los tipos de sus dos operandos, devuelve el tipo del resultado, o `Error` si la combinación está prohibida.

## Tipos modelados

| Símbolo en código   | Significado                                          | Origen                                   |
|---------------------|------------------------------------------------------|------------------------------------------|
| `SemanticType.Entero`   | Tipo `entero` (enteros con signo).                | Palabra reservada `entero`.              |
| `SemanticType.Flotante` | Tipo `flotante` (coma flotante).                  | Palabra reservada `flotante`.            |
| `SemanticType.Bool`     | Tipo booleano implícito (no declarable).          | Resultado de operadores relacionales.    |
| `SemanticType.Nula`     | Función sin valor de retorno.                     | Palabra reservada `nula`.                |
| `SemanticType.Error`    | Combinación inválida (se reporta como error).     | —                                        |

> Patito **no** permite declarar variables booleanas; `Bool` solo aparece como tipo de la condición en `si (…)` y `mientras (…)`.

## Operadores incluidos en el cubo

Aritméticos: `+`, `-`, `*`, `/`
Relacionales: `<`, `>`, `==`, `!=`
Asignación: `=` (también vive en el cubo; la celda `destino, =, fuente` indica si la asignación es legal).

## Tabla aritmética (`+`, `-`, `*`)

| izq \ der        | Entero      | Flotante     |
|------------------|-------------|--------------|
| **Entero**       | Entero      | Flotante     |
| **Flotante**     | Flotante    | Flotante     |

Regla: si **alguno** de los operandos es `Flotante`, el resultado se promueve a `Flotante`. Esto evita las truncaduras silenciosas típicas.

## División (`/`)

| izq \ der        | Entero      | Flotante     |
|------------------|-------------|--------------|
| **Entero**       | Flotante    | Flotante     |
| **Flotante**     | Flotante    | Flotante     |

Regla **de diseño**: la división siempre devuelve `Flotante`, incluso si ambos operandos son enteros. Sacrificamos un poco la "naturalidad" matemática a cambio de eliminar bugs por pérdida de fracción cuando el usuario olvida castear.

## Relacionales (`<`, `>`, `==`, `!=`)

| izq \ der        | Entero      | Flotante     |
|------------------|-------------|--------------|
| **Entero**       | Bool        | Bool         |
| **Flotante**     | Bool        | Bool         |

Cualquier otra combinación (`Bool` con algo, `Nula`, etc.) devuelve `Error`.

## Asignación (`=`)

La celda se interpreta como `tipo_destino = tipo_fuente`. El resultado de la celda es el tipo del destino (lo que termina almacenado).

| destino \ fuente   | Entero   | Flotante |
|--------------------|----------|----------|
| **Entero**         | Entero   | **Error**|
| **Flotante**       | Flotante | Flotante |

Es decir:

- `entero ← entero` ✔
- `flotante ← entero` ✔ (promoción implícita / widening)
- `flotante ← flotante` ✔
- `entero ← flotante` ✘ (perdería precisión; el usuario debe ser explícito)

## Representación interna

El cubo está implementado como un `Dictionary<(SemanticType, SemanticOp, SemanticType), SemanticType>` (ver `SemanticCube.cs`).

Por qué un `Dictionary` y no un arreglo 3D explícito:

- Solo registramos las celdas válidas. Una celda ausente equivale a `Error`, sin desperdiciar memoria.
- La lista de reglas se lee como tuplas en el código, lo que la hace fácil de auditar.
- El acceso es `O(1)` promedio, igual que un arreglo.
- Si en una entrega futura agregamos otro tipo (p.ej. `Cadena`), solo añadimos las celdas nuevas; no hay que redimensionar nada.

## API expuesta

```csharp
var cube = SemanticCube.Default;
var t = cube.Resolve(SemanticType.Entero, SemanticOp.Plus, SemanticType.Flotante);
// t == SemanticType.Flotante

bool ok = cube.IsCompatible(SemanticType.Entero, SemanticOp.Assign, SemanticType.Flotante);
// ok == false
```

`SemanticCube.Default` es un singleton inmutable; no es necesario instanciarlo manualmente.
