# Documentación del Compilador Patito

Compilador del lenguaje **Patito** — *Víctor Misael Escalante Alvarado, A01741176*.

Esta carpeta contiene toda la documentación técnica del proyecto. Cada archivo está organizado por **tema** para que cualquier concepto se pueda consultar en un solo lugar

> El repositorio acompañante se encuentra en
> <https://github.com/ElingeMisa/Desarrollo-Aplicaciones-Ciencias-Computacionales>.

## Índice

### Visión general

| Documento                                    | Contenido                                                                                                  |
|----------------------------------------------|------------------------------------------------------------------------------------------------------------|
| [`lenguaje.md`](lenguaje.md)                 | Descripción del lenguaje Patito: características, tipos soportados, estructuras de control y diagramas de sintaxis. |
| [`herramientas.md`](herramientas.md)         | Comparación de generadores automáticos de scanners/parsers (Flex+Bison, GPLEX+GPPG, ANTLR4) y justificación de la elección. |

### Análisis léxico y sintáctico (Entregas 0 y 1)

| Documento                            | Contenido                                                                                          |
|--------------------------------------|----------------------------------------------------------------------------------------------------|
| [`lexico.md`](lexico.md)             | Expresiones regulares de cada token, elementos base, tabla completa de tokens, comentarios y skip. |
| [`gramatica.md`](gramatica.md)       | Gramática BNF original, adaptaciones al pasar a `.g4` y lista final de reglas que ejecuta ANTLR4.   |

### Análisis semántico (Entrega 2)

| Documento                                                         | Contenido                                                                                                                                        |
|-------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| [`cubo_semantico.md`](cubo_semantico.md)                          | Tabla de consideraciones semánticas: combinación tipo × operador × tipo → resultado.                                                             |
| [`estructuras.md`](estructuras.md)                                | Diseño de `VariableTable`, `Symbol`, `FunctionInfo` y `FunctionDirectory` (Entrega 2) y de `PilaOperadores`, `PilaOperandos`, `PilaTipos`, `FilaCuadruplos` y `QuadrupleEmitter` (Entrega 3), con justificación de cada elección y ciclo de vida completo. |
| [`directorio_y_tablas.md`](directorio_y_tablas.md)                | Descripción de las estructuras que representan el Directorio de Funciones y las Tablas de Variables: campos, operaciones y ejemplo concreto de poblado a partir de código fuente. |
| [`puntos_neuralgicos.md`](puntos_neuralgicos.md)                  | Recorrido del árbol con ANTLR4: mapeo de cada `Enter…`/`Exit…` del listener a su acción semántica (PN-1 a PN-7, Entrega 2) y a su acción de generación de cuádruplos (PN-8 a PN-18, Entrega 3). |

### Generación de código intermedio (Entrega 3)

| Documento                                      | Contenido                                                                                                                                                              |
|------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [`cuadruplos.md`](cuadruplos.md)               | Algoritmo completo de traducción a cuádruplos: formato `Quadruple`/`QuadOp`, algoritmos PN-8 a PN-18 con pseudocódigo y trazas de pilas, mecanismo de Backfill, y fila de cuádruplos generada para cuatro programas de prueba. |

### Calidad y verificación

| Documento                            | Contenido                                                                                          |
|--------------------------------------|----------------------------------------------------------------------------------------------------|
| [`pruebas.md`](pruebas.md)           | Plan de pruebas consolidado: casos de scanner, parser, semántica y generación de código con sus resultados esperados. |

## Mapa por entrega

Si lo que buscas es el material que corresponde a una **entrega específica**, esta es la equivalencia con los documentos por tema de arriba:

| Entrega                                    | Documentos relevantes                                                                                                                                              |
|--------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Entrega 0** — Definición del lenguaje    | [`lenguaje.md`](lenguaje.md), [`lexico.md`](lexico.md), [`gramatica.md`](gramatica.md) (sección "BNF original").                                                  |
| **Entrega 1** — Léxico y sintaxis          | [`herramientas.md`](herramientas.md), [`lexico.md`](lexico.md), [`gramatica.md`](gramatica.md) (secciones de ANTLR4), [`pruebas.md`](pruebas.md).                  |
| **Entrega 2** — Análisis semántico         | [`cubo_semantico.md`](cubo_semantico.md), [`estructuras.md`](estructuras.md) (§ Entrega 2), [`directorio_y_tablas.md`](directorio_y_tablas.md), [`puntos_neuralgicos.md`](puntos_neuralgicos.md) (§ Entregas 2), [`pruebas.md`](pruebas.md). |
| **Entrega 3** — Generación de cuádruplos  | [`cuadruplos.md`](cuadruplos.md), [`estructuras.md`](estructuras.md) (§ Entrega 3), [`puntos_neuralgicos.md`](puntos_neuralgicos.md) (§ Entrega 3), [`pruebas.md`](pruebas.md) (§ `CodeGenTests`). |

## Convenciones

- Los **no-terminales** de la gramática se escriben `<entre mayor y menor que>`.
- Los **terminales** van en minúsculas o `"entre comillas dobles"`.
- El símbolo `#` denota la **producción vacía** (epsilon).
- En notación regex (sección [`lexico.md`](lexico.md)) usamos `*`, `+`, `?`, `|`, `()` y `[a-z]` con el significado clásico.
- Los **identificadores de token** del lexer se escriben en `MAYUSCULAS` (p.ej. `KW_PROGRAMA`, `OP_EQ`) y siguen la convención de ANTLR4.
- Los **archivos de ejemplo** del lenguaje viven en [`../examples/`](../examples) y son la fuente de verdad para programas válidos e inválidos.

## Cómo se construyó esta documentación

La estructura **plana por tema** se eligió sobre una organización por entrega porque permite:

1. **Una única página por concepto.** Si en el futuro hay que actualizar un detalle de la gramática, se modifica un único documento ([`gramatica.md`](gramatica.md)) en lugar de buscar entre carpetas separadas.
2. **Trazabilidad por entrega preservada** vía el mapa de equivalencias de arriba.
3. **Compatibilidad con GitHub.** GitHub renderiza tablas, listas y enlaces relativos sin configuración adicional, lo que evita depender de un generador estático.

Cuando un documento incluye decisiones de diseño tomadas en una entrega específica, se anota expresamente (por ejemplo, "[Adaptación de Entrega 1]") para mantener la trazabilidad histórica.
