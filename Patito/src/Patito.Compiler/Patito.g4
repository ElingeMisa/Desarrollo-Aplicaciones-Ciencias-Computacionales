// 
//  Patito.g4 - Gramatica unificada (lexer + parser) del lenguaje Patito.
//  Autor: Victor Misael Escalante Alvarado, A01741176
//  Generador: ANTLR 4.13 (target: CSharp)
//
//  Convenciones de ANTLR4:
//    * Las reglas de PARSER inician con minuscula.
//    * Las reglas de LEXER  inician con MAYUSCULA.
//    * 'fragment' marca reglas auxiliares que NO producen tokens.
//    * El lexer aplica "first-match longest"; por eso las palabras reservadas
//      van ANTES que la regla ID para que tengan prioridad.
//
//  Esta gramatica deriva de las definiciones de la Entrega 0:
//    - Expresiones regulares (palabras reservadas, ID, CTE_ENT, CTE_FLOT,
//      LETRERO, operadores, delimitadores).
//    - Reglas BNF (programa, vars, funcs, cuerpo, estatuto, asigna,
//      condicion, ciclo, imprime, llamada, expresion, exp, termino, factor).
// 

grammar Patito;

// 
//  REGLAS DEL PARSER
// 

// Punto de entrada del parser. Se nombra 'programa' para coincidir con la BNF.
programa
    : KW_PROGRAMA ID SEMICOLON vars funcs KW_INICIO cuerpo KW_FIN EOF
    ;

//  Declaracion de variables -
// La seccion 'vars' es opcional: puede omitirse por completo.
// NOTA: la BNF de la Entrega 0 escribia "vars : <listado_vars>" (con un ':'
// despues de la palabra 'vars') pero el diagrama de sintaxis NO incluye dicho
// ':'. Se adopta la version del diagrama (sin colon) por ser mas natural y
// consistente con los ejemplos. Esta decision queda documentada en el .docx.
vars
    : KW_VARS listado_vars
    | /* vacio */
    ;

// Cero o mas grupos "ids : tipo ;"
listado_vars
    : (lista_ids COLON tipo SEMICOLON)+
    | /* vacio */
    ;

lista_ids
    : ID (COMA ID)*
    ;

tipo
    : KW_ENTERO
    | KW_FLOTANTE
    ;

//  Funciones 
// Cero o mas definiciones de funcion. Interpretamos la BNF de modo que el
// cuerpo de la funcion lleva sus propias 'vars' locales seguidas del bloque
// de instrucciones, todo dentro de un par unico de llaves (ver doc).
funcs
    : (typo_fun ID LPAREN params RPAREN func_body SEMICOLON)*
    ;

typo_fun
    : KW_NULA
    | tipo
    ;

params
    : ID COLON tipo (COMA ID COLON tipo)*
    | /* vacio - funcion sin parametros */
    ;

// Cuerpo de funcion: { vars  estatuto* }
func_body
    : LBRACE vars estatuto* RBRACE
    ;

//  Cuerpo (bloques de control) -
// Cuerpo usado por 'si', 'mientras' y por el bloque principal entre
// 'inicio' y 'fin'. La gramatica original lo define como { list_estatutos }.
cuerpo
    : LBRACE estatuto* RBRACE
    ;

//  Estatutos 
estatuto
    : asigna
    | condicion
    | ciclo
    | imprime
    | call_stmt
    | retorno
    ;

asigna
    : ID OP_ASIGNA expresion SEMICOLON
    ;

// 'regresa <expr>;' - solo tiene sentido dentro del cuerpo de una funcion con
// tipo de retorno distinto de 'nula'; esa validacion (PN-19) se hace en el
// analizador semantico, no aqui (el parser solo reconoce la sintaxis).
retorno
    : KW_REGRESA expresion SEMICOLON
    ;

// 'si (expr) { ... }' con rama 'sino' opcional. Termina en ';' segun la BNF.
condicion
    : KW_SI LPAREN expresion RPAREN cuerpo (KW_SINO cuerpo)? SEMICOLON
    ;

// 'mientras (expr) haz { ... };'
ciclo
    : KW_MIENTRAS LPAREN expresion RPAREN KW_HAZ cuerpo SEMICOLON
    ;

// 'escribe ( imp (, imp)* );'
imprime
    : KW_ESCRIBE LPAREN imp (COMA imp)* RPAREN SEMICOLON
    ;

imp
    : expresion
    | LETRERO
    ;

// Llamada a funcion como instruccion (con ;).
call_stmt
    : llamada SEMICOLON
    ;

// Llamada a funcion (puede aparecer tambien como factor en una expresion).
llamada
    : ID LPAREN args? RPAREN
    ;

args
    : expresion (COMA expresion)*
    ;

//  Expresiones 
// Una expresion es una 'exp' opcionalmente seguida de un operador relacional
// y otra 'exp'. Coincide con el diagrama (rel_op no se encadena).
expresion
    : exp ( rel_op exp )?
    ;

rel_op
    : OP_LT
    | OP_GT
    | OP_NEQ
    | OP_EQ
    ;

// Suma y resta - asociatividad izquierda gracias al '*'.
exp
    : termino ( (OP_MAS | OP_MENOS) termino )*
    ;

// Multiplicacion y division - mayor precedencia que +/-.
termino
    : factor ( (OP_POR | OP_DIV) factor )*
    ;

// 'factor' acepta: '(' expresion ')', llamada, o (signo? ID/CTE).
// El orden importa: ANTLR4 usa ALL(*) y prueba 'llamada' (que requiere ID '(')
// antes de la alternativa simple para evitar ambiguedad con ID.
factor
    : LPAREN expresion RPAREN                       # FactorParen
    | llamada                                       # FactorLlamada
    | (OP_MAS | OP_MENOS)? simple_atom              # FactorSimple
    ;

// 'simple_atom' = id o constante. Equivalente a <factor_base_b> de la BNF.
simple_atom
    : ID
    | cte
    ;

cte
    : CTE_ENT
    | CTE_FLOT
    ;

// 
//  REGLAS DEL LEXER
// 

//  Palabras reservadas (deben ir ANTES de ID) -
KW_PROGRAMA  : 'programa' ;
KW_INICIO    : 'inicio'   ;
KW_FIN       : 'fin'      ;
KW_VARS      : 'vars'     ;
KW_ENTERO    : 'entero'   ;
KW_FLOTANTE  : 'flotante' ;
KW_NULA      : 'nula'     ;
KW_SI        : 'si'       ;
KW_SINO      : 'sino'     ;
KW_MIENTRAS  : 'mientras' ;
KW_HAZ       : 'haz'      ;
KW_ESCRIBE   : 'escribe'  ;
KW_REGRESA   : 'regresa'  ;

//  Operadores -
// '==' y '!=' deben ir antes que '=' para que el lexer prefiera la version
// de dos caracteres (regla longest-match).
OP_EQ        : '==' ;
OP_NEQ       : '!=' ;
OP_ASIGNA    : '='  ;
OP_LT        : '<'  ;
OP_GT        : '>'  ;
OP_MAS       : '+'  ;
OP_MENOS     : '-'  ;
OP_POR       : '*'  ;
OP_DIV       : '/'  ;

//  Delimitadores / puntuacion 
SEMICOLON    : ';' ;
COMA         : ',' ;
LPAREN       : '(' ;
RPAREN       : ')' ;
LBRACE       : '{' ;
RBRACE       : '}' ;
COLON        : ':' ;

//  Constantes -
// CTE_FLOT debe ir ANTES que CTE_ENT para que '3.14' no se tokenize como
// CTE_ENT('3') . CTE_ENT('14').
CTE_FLOT     : DIGITO+ '.' DIGITO+ ;
CTE_ENT      : DIGITO+ ;

//  Identificadores 
// Coincide con: letra alfanum*  donde letra = [a-zA-Z], alfanum = letra | digito.
// Se permiten mayusculas para soportar identificadores camelCase (p.ej. sumarHasta).
ID           : LETRA ALFANUM* ;

//  Cadenas literales ('letrero') 
// Comilla doble, cualquier caracter excepto otra comilla o salto de linea,
// cerrada con comilla doble. No permite escapes.
LETRERO      : '"' ~["\r\n]* '"' ;

//  Fragments (definiciones auxiliares, NO producen tokens) 
fragment LETRA   : [a-zA-Z] ;
fragment DIGITO  : [0-9] ;
fragment ALFANUM : LETRA | DIGITO ;

//  Espacios y comentarios -> skip -
// Comentarios de linea estilo //  y de bloque  /* ... */ - ambos se descartan.
WS            : [ \t\r\n]+   -> skip ;
COMMENT_LINE  : '//' ~[\r\n]* -> skip ;
COMMENT_BLOCK : '/*' .*? '*/' -> skip ;
