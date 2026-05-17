# Análisis léxico

Este documento describe el **scanner** del lenguaje Patito: las expresiones regulares con las que se reconocen los tokens, la lista completa de tokens y las decisiones de implementación del lexer en ANTLR4.

La especificación regex original es de la **Entrega 0**; las decisiones de prioridad (longest-match, palabras reservadas antes que `ID`) y el manejo de comentarios se introdujeron en la **Entrega 1** al traducir las reglas a un archivo `.g4`.

## Notación

Las expresiones regulares se escriben con la siguiente convención:

| Notación   | Definición                          |
|------------|--------------------------------------|
| `[a-z]`    | Clase de caracteres (rango).         |
| `X*`       | Cero o más repeticiones de `X`.      |
| `X+`       | Una o más repeticiones de `X`.       |
| `X?`       | Cero o una ocurrencia de `X`.        |
| `X\|Y`      | Alternativa: `X` o `Y`.              |
| `(X)`      | Agrupación.                          |
| `"xyz"`    | Cadena literal (token exacto).        |

## Elementos base

Para no repetir clases de caracteres a lo largo de las reglas, se definen los siguientes **fragments** auxiliares. En ANTLR4 son fragmentos `fragment …` y no producen tokens por sí mismos.

| Elemento  | Definición       |
|-----------|-------------------|
| `letra`   | `[a-zA-Z]`        |
| `digito`  | `[0-9]`           |
| `alfanum` | `letra \| digito` |

> **[Adaptación de Entrega 1]** La Entrega 0 definía `letra` como `[a-z]` (solo minúsculas). En el archivo `.g4` final se amplió a `[a-zA-Z]` para soportar identificadores en *camelCase* y *PascalCase* (p. ej. `sumarHasta`, `MiVariable1`), lo cual es más coherente con el resto del ecosistema .NET. Esta decisión está cubierta por la prueba `Identificador_AceptaMayusculasYMinusculas` (ver [`pruebas.md`](pruebas.md)).

## Palabras reservadas

Cada palabra reservada representa una cadena de caracteres exacta y tiene **prioridad sobre la regla `ID`**. En ANTLR4 esta prioridad se obtiene declarando las palabras reservadas antes que `ID` dentro del `.g4`.

| Token         | Lexema      |
|---------------|-------------|
| `KW_PROGRAMA` | `programa`  |
| `KW_INICIO`   | `inicio`    |
| `KW_FIN`      | `fin`       |
| `KW_VARS`     | `vars`      |
| `KW_ENTERO`   | `entero`    |
| `KW_FLOTANTE` | `flotante`  |
| `KW_NULA`     | `nula`      |
| `KW_SI`       | `si`        |
| `KW_SINO`     | `sino`      |
| `KW_MIENTRAS` | `mientras`  |
| `KW_HAZ`      | `haz`       |
| `KW_ESCRIBE`  | `escribe`   |

El requisito de *longest-match* asegura que identificadores como `siempre` se reconozcan como `ID` y **no** como `KW_SI` seguido de `empre`; las pruebas `Identificador_NoColisionaConKeywords` y `PalabraReservada_SeReconoceComoKeyword` blindan ambos lados.

## Identificadores

| Token | Produce               |
|-------|------------------------|
| `ID`  | `letra alfanum*`       |

Sirven para nombrar variables, parámetros, funciones y el programa mismo. Como `letra` y `alfanum` están definidos sobre `[a-zA-Z]` y `[0-9]`, los identificadores **no admiten `_` ni acentos**. Esto se preserva intencionadamente respecto a la Entrega 0.

## Constantes

### Constante entera

| Token     | Produce  |
|-----------|----------|
| `CTE_ENT` | `digito+` |

Ejemplos: `0`, `24`, `1039203`.

### Constante flotante

| Token      | Produce              |
|------------|----------------------|
| `CTE_FLOT` | `digito+ "." digito+` |

Ejemplos: `3.14159265`, `0.1`, `100.0`.

> **Decisión de orden.** En el `.g4`, la regla `CTE_FLOT` se declara **antes** que `CTE_ENT`. Si fuera al revés, ante una entrada como `3.14` el lexer podría producir tres tokens (`CTE_ENT`, `.`, `CTE_ENT`) en lugar de un único `CTE_FLOT`. La prueba `ConstanteFlotante_PrefiereCteFlotSobreEntero` valida que `3.14` produce un solo token.

## Cadenas literales (letrero)

| Token     | Produce              |
|-----------|----------------------|
| `LETRERO` | `'"' ~["\r\n]* '"'`  |

Una cadena empieza y termina con comillas dobles, no permite comillas dobles internas ni saltos de línea, y no soporta secuencias de escape. La regla original de la Entrega 0 prohibía únicamente `\n`; en la Entrega 1 se amplió para excluir también `\r`, ganando portabilidad entre Windows y Unix. El caso negativo está cubierto por `invalido_03_letrero_multilinea.patito`.

## Operadores

Estos operadores aparecen dentro de expresiones, asignaciones y condiciones. El orden de declaración importa: las versiones de dos caracteres (`==`, `!=`) deben aparecer **antes** que la versión de un carácter (`=`), de modo que el principio de *longest-match* devuelva el operador más largo posible.

| Token       | Uso                                | Lexema |
|-------------|------------------------------------|--------|
| `OP_ASIGNA` | Asignación de un valor              | `=`    |
| `OP_EQ`     | Igual a un valor                    | `==`   |
| `OP_NEQ`    | Diferente de                        | `!=`   |
| `OP_LT`     | Menor que                           | `<`    |
| `OP_GT`     | Mayor que                           | `>`    |
| `OP_MAS`    | Suma o valor positivo               | `+`    |
| `OP_MENOS`  | Resta o valor negativo              | `-`    |
| `OP_POR`    | Multiplicación                      | `*`    |
| `OP_DIV`    | División                            | `/`    |

La prueba `OpEq_TienePrioridadSobreOpAsigna` verifica que la entrada `==` se tokenice como un único `OP_EQ` y no como dos `OP_ASIGNA` seguidos.

## Delimitadores y puntuación

Se les asigna un identificador propio para evitar confusiones con cualquier otra regla que use los mismos caracteres.

| Token       | Lexema |
|-------------|--------|
| `SEMICOLON` | `;`    |
| `COMA`      | `,`    |
| `LPAREN`    | `(`    |
| `RPAREN`    | `)`    |
| `LBRACE`    | `{`    |
| `RBRACE`    | `}`    |
| `COLON`     | `:`    |

## Comentarios y espacios en blanco

> **[Adaptación de Entrega 1]** La Entrega 0 dejaba la regla de comentarios marcada como `TBD`. En la Entrega 1 se eligieron las dos formas más comunes de lenguajes tipo C y se descartan mediante la directiva `-> skip` de ANTLR4 para que sean invisibles al parser sin tratamiento adicional.

| Token           | Definición regex      | Comentario                           |
|-----------------|------------------------|--------------------------------------|
| `COMMENT_LINE`  | `'//' ~[\r\n]*`        | Hasta fin de línea; se descarta.     |
| `COMMENT_BLOCK` | `'/*' .*? '*/'`        | Bloque al estilo C; no anidado.      |
| `WS`            | `[ \t\r\n]+`           | Espacios, tabs y saltos; se descartan. |

Las pruebas `ComentarioDeLinea_SeIgnora`, `ComentarioDeBloque_SeIgnora` y `Whitespace_NoGeneraTokens` confirman que ninguno produce tokens.

## Listado completo de tokens

Esta es la tabla maestra que consume el parser. La columna **Categoría** ayuda a entender qué tipo de elemento es cada token al imprimir la lista de tokens con `patitoc archivo.patito --tokens`.

| Token         | Produce              | Categoría             |
|---------------|----------------------|-----------------------|
| `KW_PROGRAMA` | `"programa"`         | Palabra reservada     |
| `KW_INICIO`   | `"inicio"`           | Palabra reservada     |
| `KW_FIN`      | `"fin"`              | Palabra reservada     |
| `KW_VARS`     | `"vars"`             | Palabra reservada     |
| `KW_ENTERO`   | `"entero"`           | Palabra reservada     |
| `KW_FLOTANTE` | `"flotante"`         | Palabra reservada     |
| `KW_NULA`     | `"nula"`             | Palabra reservada     |
| `KW_SI`       | `"si"`               | Palabra reservada     |
| `KW_SINO`     | `"sino"`             | Palabra reservada     |
| `KW_MIENTRAS` | `"mientras"`         | Palabra reservada     |
| `KW_HAZ`      | `"haz"`              | Palabra reservada     |
| `KW_ESCRIBE`  | `"escribe"`          | Palabra reservada     |
| `ID`          | `letra alfanum*`     | Identificador         |
| `CTE_ENT`     | `digito+`            | Constante entera      |
| `CTE_FLOT`    | `digito+ "." digito+` | Constante flotante   |
| `LETRERO`     | `"…"`                | Cadena de texto       |
| `OP_ASIGNA`   | `=`                  | Operador asignación   |
| `OP_EQ`       | `==`                 | Operador relacional   |
| `OP_NEQ`      | `!=`                 | Operador relacional   |
| `OP_LT`       | `<`                  | Operador relacional   |
| `OP_GT`       | `>`                  | Operador relacional   |
| `OP_MAS`      | `+`                  | Operador aritmético   |
| `OP_MENOS`    | `-`                  | Operador aritmético   |
| `OP_POR`      | `*`                  | Operador aritmético   |
| `OP_DIV`      | `/`                  | Operador aritmético   |
| `SEMICOLON`   | `;`                  | Puntuación            |
| `COMA`        | `,`                  | Puntuación            |
| `LPAREN`      | `(`                  | Puntuación            |
| `RPAREN`      | `)`                  | Puntuación            |
| `LBRACE`      | `{`                  | Puntuación            |
| `RBRACE`      | `}`                  | Puntuación            |
| `COLON`       | `:`                  | Puntuación            |

> **Nota:** En las producciones de la gramática (ver [`gramatica.md`](gramatica.md)) algunos tokens aparecen escritos directamente entre comillas (`"si"`, `"+"`, …) en lugar de con su nombre simbólico, sobre todo para los lectores que vienen del BNF original.

## Reglas implementadas en ANTLR4

El archivo [`../src/Patito.Compiler/Patito.g4`](../src/Patito.Compiler/Patito.g4) materializa todo lo anterior. La sección de lexer del `.g4` se organiza así:

1. **Palabras reservadas** — declaradas antes que `ID` para que ganen la prioridad de la regla *first-match*.
2. **Operadores y delimitadores** — con `OP_EQ`/`OP_NEQ` antes de `OP_ASIGNA` (longest-match).
3. **Constantes** — `CTE_FLOT` antes de `CTE_ENT` (longest-match).
4. **Identificador `ID`** — al final, para que ninguna palabra reservada caiga aquí por accidente.
5. **`LETRERO`** — cadena literal sin escapes ni saltos de línea.
6. **Fragments y reglas `-> skip`** — `WS`, `COMMENT_LINE`, `COMMENT_BLOCK`.

Las pruebas que verifican cada una de estas decisiones están listadas en [`pruebas.md`](pruebas.md) en la sección "Pruebas para el SCANNER".
