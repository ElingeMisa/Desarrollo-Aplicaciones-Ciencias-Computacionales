# Gramática

Este documento describe la **gramática libre de contexto** del lenguaje Patito en dos formatos complementarios:

1. **BNF original** — La especificación tal cual quedó en la Entrega 0, útil como referencia conceptual y para comparar contra los diagramas de sintaxis.
2. **Reglas ANTLR4 (`.g4`)** — La forma final con la que el parser efectivamente se genera, incluyendo las adaptaciones que se hicieron en la Entrega 1 al traducir la BNF.

Para la lista de tokens (terminales) que estas reglas referencian, ver [`lexico.md`](lexico.md).

## Notación BNF

- Los **no-terminales** se escriben `<entre mayor y menor que>`.
- Los **terminales** en minúsculas o `"entre comillas"`.
- El símbolo `#` denota la **producción vacía** (epsilon).

## BNF original (Entrega 0)

### Definición base del programa

| No-Terminal   | →    | Produce                                                            |
|---------------|------|--------------------------------------------------------------------|
| `<programa>`  | →    | `programa id ";" <vars> <funcs> "inicio" <cuerpo> "fin"`           |

### Declaración de variables

| No-Terminal       | →   | Produce                                              | Descripción              |
|-------------------|-----|------------------------------------------------------|--------------------------|
| `<vars>`          | →   | `vars : <listado_vars>`                              | Sección de variables     |
| `<vars>`          | →   | `#`                                                  | Sin variables            |
| `<listado_vars>`  | →   | `<lista_ids> : <tipo> ; <listado_vars>`              | Un grupo `id:tipo`       |
| `<listado_vars>`  | →   | `#`                                                  | Fin de declaraciones     |
| `<lista_ids>`     | →   | `id`                                                 | Una variable             |
| `<lista_ids>`     | →   | `id , <lista_ids>`                                   | Múltiples variables      |
| `<tipo>`          | →   | `KW_ENTERO`                                          | Tipo entero              |
| `<tipo>`          | →   | `KW_FLOTANTE`                                        | Tipo flotante            |

### Funciones

| No-Terminal     | →   | Produce                                                                       | Descripción            |
|-----------------|-----|-------------------------------------------------------------------------------|------------------------|
| `<funcs>`       | →   | `<typo_fun> id (<params>) <vars> {<cuerpo>} <funcs>`                          | Definición de función  |
| `<funcs>`       | →   | `#`                                                                            | Sin más funciones      |
| `<params>`      | →   | `id : <tipo> <params_cont>`                                                   | Al menos un parámetro  |
| `<params>`      | →   | `#`                                                                            | Sin parámetros         |
| `<params_cont>` | →   | `, id : <tipo> <params_cont>`                                                 | Más parámetros         |
| `<params_cont>` | →   | `#`                                                                            |                        |
| `<typo_fun>`    | →   | `KW_NULA`                                                                     | Puede ser tipo void    |
| `<typo_fun>`    | →   | `<tipo>`                                                                       | Puede devolver un tipo |

### Llamada a función

| No-Terminal     | →   | Produce                                | Descripción              |
|-----------------|-----|----------------------------------------|--------------------------|
| `<llamada>`     | →   | `id ( <args> ) ;`                      | Llamada con argumentos   |
| `<args>`        | →   | `<expresion> <args_cont>`              | Al menos un argumento    |
| `<args>`        | →   | `#`                                    | Sin argumentos           |
| `<args_cont>`   | →   | `, <expresion> <args_cont>`            | Más argumentos           |
| `<args_cont>`   | →   | `#`                                    | Fin de argumentos        |

### Cuerpo

| No-Terminal         | →   | Produce                                  | Descripción                          |
|---------------------|-----|------------------------------------------|--------------------------------------|
| `<cuerpo>`          | →   | `{ <list_estatutos> }`                   | Forzosamente entre llaves            |
| `<list_estatutos>`  | →   | `<estatuto> <list_estatutos>`            | Lista de estatutos                   |
| `<list_estatutos>`  | →   | `#`                                      | Lista vacía                          |

### Estatuto

| No-Terminal | →   | Produce              | Descripción              |
|-------------|-----|----------------------|--------------------------|
| `<estatuto>`| →   | `<asigna>`           | Asignación               |
| `<estatuto>`| →   | `<condicion>`        | Condicional              |
| `<estatuto>`| →   | `<ciclo>`            | Ciclo                    |
| `<estatuto>`| →   | `<imprime>`          | Impresión                |
| `<estatuto>`| →   | `<llamada> ;`        | Llamada a función        |
| `<estatuto>`| →   | `[<list_estatutos>]` | Genera lista de estatutos |

### Asignación, condición y ciclo

| No-Terminal     | →   | Produce                                                          | Descripción                  |
|-----------------|-----|------------------------------------------------------------------|------------------------------|
| `<asigna>`      | →   | `id OP_ASIGNA <expresion> ;`                                     | El id recibe el valor        |
| `<condicion>`   | →   | `KW_SI ( <expresion> ) <cuerpo> <sino_opt> ;`                    | Con o sin else               |
| `<sino_opt>`    | →   | `KW_SINO <cuerpo>`                                                | Rama else                    |
| `<sino_opt>`    | →   | `#`                                                                | Sin rama sino                |
| `<ciclo>`       | →   | `KW_MIENTRAS ( <expresion> ) KW_HAZ <cuerpo> ;`                  | Ciclo mientras-haz           |

### Impresión

| No-Terminal       | →   | Produce                                                         |
|-------------------|-----|-----------------------------------------------------------------|
| `<imprime>`       | →   | `KW_ESCRIBE ( <lista_imp> ) ;`                                  |
| `<lista_imp>`     | →   | `<imp> <mas_lista_imp>`                                          |
| `<mas_lista_imp>` | →   | `, <imp> <mas_lista_imp>`                                        |
| `<mas_lista_imp>` | →   | `#`                                                              |
| `<imp>`           | →   | `<expresion>`                                                    |
| `<imp>`           | →   | `LETRERO`                                                        |

### Expresión

| No-Terminal     | →   | Produce                       | Descripción              |
|-----------------|-----|-------------------------------|--------------------------|
| `<expresion>`   | →   | `<exp> <list_op>`             |                          |
| `<list_op>`     | →   | `<rel_op>`                    | Una comparación opcional |
| `<list_op>`     | →   | `#`                            |                          |
| `<rel_op>`      | →   | `OP_LT <exp>`                 | Menor que                |
| `<rel_op>`      | →   | `OP_GT <exp>`                 | Mayor que                |
| `<rel_op>`      | →   | `OP_NEQ <exp>`                | Diferente de             |
| `<rel_op>`      | →   | `OP_EQ <exp>`                 | Igual a                  |

### Aritmética: exp, término y factor

| No-Terminal       | →   | Produce                                  | Descripción           |
|-------------------|-----|------------------------------------------|-----------------------|
| `<exp>`           | →   | `<termino> <exp_cont>`                   |                       |
| `<exp_cont>`      | →   | `+ <termino> <exp_cont>`                 | Suma                  |
| `<exp_cont>`      | →   | `- <termino> <exp_cont>`                 | Resta                 |
| `<exp_cont>`      | →   | `#`                                       | Sin más términos      |
| `<termino>`       | →   | `<factor> <term_cont>`                   |                       |
| `<term_cont>`     | →   | `* <factor> <term_cont>`                 | Multiplicación        |
| `<term_cont>`     | →   | `/ <factor> <term_cont>`                 | División              |
| `<term_cont>`     | →   | `#`                                       | Sin más factores      |
| `<factor>`        | →   | `+ <factor_base_b>`                       | Positivo unario       |
| `<factor>`        | →   | `- <factor_base_b>`                       | Negativo unario       |
| `<factor>`        | →   | `<factor_base_a>`                         | Sin signo             |
| `<factor>`        | →   | `<factor_base_b>`                         | Unidad sin signo      |
| `<factor>`        | →   | `<llamada>`                               | Llamada como factor   |
| `<factor_base_a>` | →   | `( <expresion> )`                         | Sub-expresión         |
| `<factor_base_b>` | →   | `<cte>`                                   | Constante             |
| `<factor_base_b>` | →   | `id`                                      | Variable              |
| `<cte>`           | →   | `CTE_ENT`                                 | Constante entera      |
| `<cte>`           | →   | `CTE_FLOT`                                | Constante flotante    |

## Adaptaciones al traducir a ANTLR4 (Entrega 1)

ANTLR4 acepta operadores de repetición (`*`, `+`, `?`) y alternativas (`|`) directamente en el lado derecho de una regla, por lo que la recursión explícita del BNF se reescribe de forma compacta. En la traducción surgieron **cinco puntos** que requirieron una decisión de diseño consciente:

### 1. Colon después de `vars`

La BNF original escribía `vars : <listado_vars>` (con un `:` entre la palabra `vars` y el listado), pero el diagrama de sintaxis **no** incluye dicho `:`. Se eligió la versión sin `:` por tres razones:

- Coincide con el diagrama.
- Elimina un token redundante (la primera declaración ya empieza con `id`).
- Hace que los ejemplos sean más legibles.

Es decir, un programa pasa directamente de `vars` a la primera declaración `id : tipo ;`.

### 2. Llamada a función: expresión vs. instrucción

La BNF declara `<llamada> id ( <args> ) ;` con punto y coma terminal, y además `<estatuto> <llamada> ;`. Eso duplica el `;`. Para arreglarlo:

- Se redefinió `llamada` **sin** el punto y coma final (queda `ID LPAREN args? RPAREN`).
- Se introdujo una regla auxiliar `call_stmt : llamada SEMICOLON ;`.

De esta manera `llamada` puede aparecer como factor en expresiones (`x = dame() + 1;`) y `call_stmt` cumple su función como instrucción con su único `;`.

### 3. Cuerpo de función

El diagrama de `<FUNCS>` muestra el cuerpo como `{ VARS CUERPO }`, y como `CUERPO` a su vez es `{ list_estatutos }`, una lectura literal produciría llaves duplicadas. Se introdujo la regla auxiliar `func_body : LBRACE vars estatuto* RBRACE ;`, que conserva la idea de "vars dentro del cuerpo de la función" pero usa un único par de llaves.

### 4. Letrero

La regex `"\"[^\"\\n]*\""` de la Entrega 0 prohíbe `\n`, pero deja implícito si `\r` también está excluido. La regla del lexer excluye ambos (`\r` y `\n`) y la prueba `invalido_03_letrero_multilinea.patito` verifica el rechazo.

### 5. Comentarios

La Entrega 0 dejaba la regla de comentarios marcada como `TBD`. Se eligieron las dos formas más comunes en lenguajes tipo C: `//` hasta fin de línea y `/* ... */` sin anidamiento. Ambas se descartan con la directiva `-> skip` de ANTLR4, lo que las hace invisibles al parser sin tratamiento adicional.

## Reglas finales en `.g4`

Esta es la lista final, tal cual aparece en [`../src/Patito.Compiler/Patito.g4`](../src/Patito.Compiler/Patito.g4):

| No-terminal     | Producción (en notación `.g4`)                                                    |
|-----------------|-----------------------------------------------------------------------------------|
| `programa`      | `KW_PROGRAMA ID SEMICOLON vars funcs KW_INICIO cuerpo KW_FIN EOF`                 |
| `vars`          | `KW_VARS listado_vars \| /* vacío */`                                              |
| `listado_vars`  | `(lista_ids COLON tipo SEMICOLON)+ \| /* vacío */`                                 |
| `lista_ids`     | `ID (COMA ID)*`                                                                    |
| `tipo`          | `KW_ENTERO \| KW_FLOTANTE`                                                          |
| `funcs`         | `( typo_fun ID LPAREN params RPAREN func_body SEMICOLON )*`                       |
| `typo_fun`      | `KW_NULA \| tipo`                                                                  |
| `params`        | `ID COLON tipo (COMA ID COLON tipo)* \| /* vacío */`                              |
| `func_body`     | `LBRACE vars estatuto* RBRACE`                                                    |
| `cuerpo`        | `LBRACE estatuto* RBRACE`                                                         |
| `estatuto`      | `asigna \| condicion \| ciclo \| imprime \| call_stmt`                             |
| `asigna`        | `ID OP_ASIGNA expresion SEMICOLON`                                                |
| `condicion`     | `KW_SI LPAREN expresion RPAREN cuerpo (KW_SINO cuerpo)? SEMICOLON`                |
| `ciclo`         | `KW_MIENTRAS LPAREN expresion RPAREN KW_HAZ cuerpo SEMICOLON`                     |
| `imprime`       | `KW_ESCRIBE LPAREN imp (COMA imp)* RPAREN SEMICOLON`                              |
| `imp`           | `expresion \| LETRERO`                                                             |
| `call_stmt`     | `llamada SEMICOLON`                                                               |
| `llamada`       | `ID LPAREN args? RPAREN`                                                           |
| `args`          | `expresion (COMA expresion)*`                                                      |
| `expresion`     | `exp ( rel_op exp )?`                                                              |
| `rel_op`        | `OP_LT \| OP_GT \| OP_NEQ \| OP_EQ`                                                |
| `exp`           | `termino ( (OP_MAS \| OP_MENOS) termino )*`                                        |
| `termino`       | `factor ( (OP_POR \| OP_DIV) factor )*`                                            |
| `factor`        | `LPAREN expresion RPAREN \| llamada \| (OP_MAS \| OP_MENOS)? simple_atom`         |
| `simple_atom`   | `ID \| cte`                                                                        |
| `cte`           | `CTE_ENT \| CTE_FLOT`                                                              |

## Notas sobre el parser

ANTLR4 usa el algoritmo **ALL(*)** (adaptive LL with arbitrary lookahead). En la práctica esto significa:

- No hace falta reescribir la gramática para evitar **recursión izquierda** ni conflictos `shift/reduce` como en LALR(1) clásico.
- La regla `factor` lista la alternativa `llamada` antes que la alternativa `simple_atom` para que el parser prefiera la interpretación de "llamada a función" cuando el input empieza con un `ID` seguido de `(`.
- La regla `condicion` lleva el `(KW_SINO cuerpo)?` opcional, que el parser resuelve sin ambigüedad gracias al `SEMICOLON` final.
- El parser produce un *parse tree* concreto (no un AST). El análisis semántico ([`puntos_neuralgicos.md`](puntos_neuralgicos.md)) trabaja directamente sobre ese árbol vía un `Listener`.

Si en una futura entrega se necesita un AST compacto, la opción más natural es agregar un visitor que transforme el parse tree; por el momento no es necesario.
