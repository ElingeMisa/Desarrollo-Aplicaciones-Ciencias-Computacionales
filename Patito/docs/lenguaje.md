# El lenguaje Patito

Patito es un mini lenguaje diseñado para ilustrar el comportamiento de un compilador. Su gramática es deliberadamente pequeña para que cada fase del compilador (análisis léxico, sintáctico y semántico) pueda explicarse con un esfuerzo razonable, pero al mismo tiempo es lo suficientemente rica como para tocar los puntos clásicos: variables tipadas, expresiones aritméticas, control de flujo y funciones.

## Características del lenguaje

Las decisiones de diseño se tomaron originalmente en la **Entrega 0** y son las siguientes:

- **Variables enteras y flotantes con declaración explícita de tipo.** No hay inferencia ni promoción implícita en declaraciones.
- **Funciones de tipo `nula`** (sin valor de retorno). Las funciones que sí devuelven valor declaran su tipo de retorno (`entero` o `flotante`).
- **Estructuras de control:** condicional `si`/`sino` y ciclo `mientras`/`haz`. No hay `para`, `casos`, `interrumpe` ni `continua`.
- **Instrucción de impresión `escribe`** que acepta una mezcla de expresiones y cadenas literales (`letrero`).
- **Expresiones aritméticas** con las cuatro operaciones (`+`, `-`, `*`, `/`) y **comparaciones relacionales** (`<`, `>`, `==`, `!=`).
- **Sin operadores lógicos.** No existen `y`, `o`, `no`; la sintaxis no permite combinar comparaciones.
- **Sin cadenas como valor de primera clase.** El token `LETRERO` solo aparece como argumento de `escribe`; no se puede asignar a una variable.

## Estructura general de un programa

Todo programa Patito sigue la misma plantilla:

```
programa <id>;
vars
    <declaraciones globales opcionales>

<definiciones de funciones opcionales>

inicio {
    <estatutos>
} fin
```

El identificador después de `programa` da nombre al módulo y vive en el directorio de funciones (ver [`estructuras.md`](estructuras.md)). Las secciones `vars` y de funciones son opcionales: un programa mínimo válido es

```patito
programa hola;
inicio {
    escribe("hola mundo");
} fin
```

(este es exactamente el archivo [`examples/01_minimo.patito`](../examples/01_minimo.patito)).

## Diagrama de sintaxis

El diagrama original que sirve como referencia visual está en [`img/Reglas.png`](../img/Reglas.png). Cubre las siguientes producciones de un vistazo:

`<Programa>`, `<VARS>`, `<TIPO>`, `<FUNCS>`, `<CUERPO>`, `<ESTATUTO>`, `<ASIGNA>`, `<CONDICIÓN>`, `<CICLO>`, `<IMPRIME>`, `<LLAMADA>`, `<EXPRESIÓN>`, `<EXP>`, `<TÉRMINO>`, `<FACTOR>` y `<CTE>`.

![Diagrama del lenguaje](../img/Reglas.png)

La traducción literal de cada caja del diagrama a notación BNF está en [`gramatica.md`](gramatica.md). Cuando hubo discrepancias entre el diagrama y la BNF original, se optó por la versión del diagrama por ser más legible; estas decisiones se documentan caso por caso en la sección de adaptaciones de [`gramatica.md`](gramatica.md).

## Tipos del lenguaje

| Tipo declarable    | Palabra reservada | Notación regex     | Ejemplo de literal |
|--------------------|-------------------|--------------------|--------------------|
| Entero             | `entero`          | `digito+`          | `0`, `42`, `1039203` |
| Flotante           | `flotante`        | `digito+ "." digito+` | `3.14`, `0.0`, `100.0` |

Hay dos tipos **implícitos** (no declarables por el usuario) que aparecen durante el análisis semántico:

| Tipo implícito | Origen                                                                |
|----------------|------------------------------------------------------------------------|
| `Bool`         | Resultado de los operadores relacionales (`<`, `>`, `==`, `!=`).        |
| `Nula`         | Tipo de retorno de funciones que no devuelven valor (`nula`).           |

El [`cubo_semantico.md`](cubo_semantico.md) define qué combinaciones de tipos son compatibles bajo cada operador.

## Alcances (scopes)

Patito tiene **dos niveles de alcance**, sin anidamiento adicional:

1. **Alcance global** — las variables declaradas en la sección `vars` justo después de `programa <id>;`. Son visibles desde cualquier lugar del programa.
2. **Alcance de función** — los parámetros y las variables declaradas en la sección `vars` de la función. Son visibles únicamente dentro de la función.

No hay bloques léxicos adicionales (un `si` o un `mientras` no abren un nuevo alcance), por lo que el modelo de tablas del compilador es deliberadamente simple: una `VariableTable` global y una por función. La justificación está en [`estructuras.md`](estructuras.md).

## Resolución de identificadores

Cuando dentro de una función se referencia un identificador, el compilador primero lo busca en la `VariableTable` local de la función y, si no lo encuentra, en la tabla global. Cuando se está fuera de una función (el cuerpo principal entre `inicio` y `fin`), solo se consulta la tabla global. Este comportamiento permite **ensombrecer** (shadowing) una variable global con una local del mismo nombre, lo que está cubierto por la prueba `VariableLocalEnsombreceALaGlobal` en [`pruebas.md`](pruebas.md).

## Lo que Patito *no* hace

Para evitar expectativas equivocadas, lo siguiente está **explícitamente fuera del alcance** del lenguaje en su versión actual:

- No hay arreglos ni estructuras compuestas.
- No hay manejo de cadenas más allá de pasarlas a `escribe`.
- No hay E/S de archivos.
- No hay módulos ni `import`/`include`.
- No hay manejo de excepciones.
- No hay funciones anidadas (toda función se declara al nivel del programa).
- No hay punteros, referencias ni paso por referencia.
- No hay operadores compuestos (`+=`, `++`, etc.).

Cualquiera de estos puntos podría agregarse como extensión en una entrega futura, pero exigirían tocar las tres fases del compilador.
