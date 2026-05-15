// =============================================================================
//  Symbol.cs - Entrada de la Tabla de Variables.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Un Symbol es la informacion que el compilador guarda por cada identificador
//  declarado en un alcance (global o de funcion). Para esta entrega (Entrega 2)
//  solo necesitamos:
//
//    * Name     : el lexema declarado en la fuente.
//    * Type     : entero o flotante (los unicos tipos declarables en Patito).
//    * Kind     : si es variable normal o parametro de una funcion. Saberlo
//                 es util al imprimir el directorio para debugging y prepara
//                 el terreno para Entrega 3 (donde los parametros se asocian
//                 a memoria parametrica vs. local).
//    * Line/Col : posicion declarada en la fuente; nos permite reportar
//                 "previamente declarada aqui" en errores de re-declaracion.
//    * Address  : direccion de memoria virtual. Por ahora se deja como -1
//                 (asignacion diferida a Entrega 3 - generacion de codigo).
//
//  Symbol es 'sealed record' (inmutable). Esto refleja la idea de que un
//  identificador, una vez declarado, no muta su tipo. Si necesitamos
//  asignarle direccion mas adelante, creamos un nuevo Symbol via 'with'.
// =============================================================================

namespace Patito.Compiler.Semantic;

/// <summary>Tipo de simbolo dentro de la tabla.</summary>
public enum SymbolKind
{
    Variable,
    Parameter,
}

/// <summary>
/// Entrada inmutable de la Tabla de Variables.
/// </summary>
public sealed record Symbol(
    string Name,
    SemanticType Type,
    SymbolKind Kind,
    int Line,
    int Column,
    int Address = -1)
{
    /// <summary>Representacion compacta para debugging.</summary>
    public override string ToString() =>
        $"{Name}: {Type.ToLexeme()} ({Kind}) @ {Line}:{Column}";
}
