// =============================================================================
//  SemanticOp.cs - Operadores binarios reconocidos por el cubo semantico.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Distinguimos los operadores semanticos de los tokens del lexer porque el
//  cubo necesita un identificador estable (no depende del numero de token que
//  ANTLR generara en cada build). Asi, si manana cambiamos el lexer, el cubo
//  no se rompe.
//
//  El conjunto cubre los operadores binarios de Patito mas la asignacion, que
//  tambien participa del cubo (la "celda" assignment indica si la asignacion
//  de tipo X a tipo Y es valida y de que tipo es el resultado).
// =============================================================================

namespace Patito.Compiler.Semantic;

/// <summary>
/// Operadores que participan en el cubo semantico de Patito.
/// </summary>
public enum SemanticOp
{
    // --- Aritmeticos ---
    Plus,     // '+'
    Minus,    // '-'
    Times,    // '*'
    Divide,   // '/'

    // --- Relacionales ---
    Lt,       // '<'
    Gt,       // '>'
    Eq,       // '=='
    Neq,      // '!='

    // --- Asignacion ---
    Assign,   // '='
}

public static class SemanticOps
{
    public static string ToLexeme(this SemanticOp op) => op switch
    {
        SemanticOp.Plus   => "+",
        SemanticOp.Minus  => "-",
        SemanticOp.Times  => "*",
        SemanticOp.Divide => "/",
        SemanticOp.Lt     => "<",
        SemanticOp.Gt     => ">",
        SemanticOp.Eq     => "==",
        SemanticOp.Neq    => "!=",
        SemanticOp.Assign => "=",
        _                 => "?",
    };
}
