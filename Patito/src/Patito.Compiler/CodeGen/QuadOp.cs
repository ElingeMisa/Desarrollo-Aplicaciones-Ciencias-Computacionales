// =============================================================================
//  QuadOp.cs - Operadores de los cuadruplos de Patito.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  QuadOp extiende a SemanticOp para incluir las operaciones de control de
//  flujo y de entrada/salida que aparecen en los cuadruplos pero que NO
//  participan en el cubo semantico (cuyo dominio es solo la verificacion
//  de tipos de las expresiones).
//
//  Formato de cada cuadruplo:  (Op, Left, Right, Result)
//    * Aritmeticos y relacionales:  Result = Left op Right
//    * Asignacion:                  Result = Left  (Right = null)
//    * Unario (Neg):                Result = -Right (Left = null)
//    * GotoF:                       if !Left goto Result  (Right = null)
//    * Goto:                        goto Result           (Left = Right = null)
//    * Print:                       imprimir Result       (Left = Right = null)
//    * Era:                         era Result (func)     (Left = Right = null)
//    * Param:                       param Result          (Left = Right = null)
//    * Gosub:                       gosub Left Result     (Left=func, Right=null, Result=startQuad)
//    * EndFunc:                     endfunc               (todos null / nombre en Result)
// =============================================================================

namespace Patito.Compiler.CodeGen;

/// <summary>
/// Operacion de un cuadruplo de codigo intermedio de Patito.
/// </summary>
public enum QuadOp
{
    // --- Aritmeticos ---
    Plus,
    Minus,
    Times,
    Divide,

    // --- Relacionales ---
    Lt,
    Gt,
    Eq,
    Neq,

    // --- Asignacion ---
    Assign,

    // --- Unario ---
    Neg,

    // --- Control de flujo ---
    GotoF,
    Goto,

    // --- Entrada / Salida ---
    Print,

    // --- Funciones ---
    Era,       // Espacio de Registro de Activacion: reserva memoria para la funcion
    Param,     // Pasa un argumento a la funcion que se va a llamar
    Gosub,     // Transfiere el control a la funcion (Left=nombre, Result=startQuad)
    EndFunc,   // Marca el fin del cuerpo de una funcion
}

public static class QuadOps
{
    /// <summary>Representacion textual del operador (para imprimir el listado de cuadruplos).</summary>
    public static string ToSymbol(this QuadOp op) => op switch
    {
        QuadOp.Plus    => "+",
        QuadOp.Minus   => "-",
        QuadOp.Times   => "*",
        QuadOp.Divide  => "/",
        QuadOp.Lt      => "<",
        QuadOp.Gt      => ">",
        QuadOp.Eq      => "==",
        QuadOp.Neq     => "!=",
        QuadOp.Assign  => "=",
        QuadOp.Neg     => "neg",
        QuadOp.GotoF   => "GotoF",
        QuadOp.Goto    => "Goto",
        QuadOp.Print   => "Print",
        QuadOp.Era     => "ERA",
        QuadOp.Param   => "Param",
        QuadOp.Gosub   => "Gosub",
        QuadOp.EndFunc => "EndFunc",
        _              => "?",
    };
}
