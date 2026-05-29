// =============================================================================
//  Quadruple.cs - Instruccion de codigo intermedio (cuadruplo).
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Un cuadruplo es la unidad minima de codigo intermedio. Su forma canonica
//  es la cuadrupla  (Op, Left, Right, Result):
//
//    * Op      : la operacion a realizar (QuadOp).
//    * Left    : primer operando o condicion (null si no aplica).
//    * Right   : segundo operando (null si no aplica).
//    * Result  : destino de la operacion o target del salto.
//
//  Ejemplos:
//    (Plus,   "a",  "b",   "t0")   ->  t0 = a + b
//    (Assign, "t0", null,  "x")    ->  x = t0
//    (GotoF,  "t1", null,  "8")    ->  if !t1 goto quad[8]
//    (Goto,   null, null,  "3")    ->  goto quad[3]
//    (Print,  null, null,  "\"Hola\"") -> escribe("Hola")
//
//  El Index lo asigna FilaCuadruplos automaticamente; es inmutable una vez
//  emitido (la ficha se puede reemplazar con Backfill solo en el campo Result).
// =============================================================================

using System.Collections.Generic;

namespace Patito.Compiler.CodeGen;

/// <summary>
/// Instruccion de codigo intermedio de Patito. El campo <see cref="Index"/>
/// es el numero de cuadruplo (base 0) asignado por <see cref="FilaCuadruplos"/>.
/// </summary>
public sealed record Quadruple(int Index, QuadOp Op, string? Left, string? Right, string Result)
{
    /// <summary>
    /// Representacion compacta con nombres planos (sin direccion). Usada
    /// internamente y en los tests unitarios.
    /// </summary>
    public override string ToString()
    {
        var l = Left   ?? "_";
        var r = Right  ?? "_";
        return $"{Index,4}  {Op.ToSymbol(),-8}  {l,-12}  {r,-12}  {Result}";
    }

    /// <summary>
    /// Representacion con direcciones virtuales en el formato
    ///   <c>OPERACION  DIR(NOMBRE)  DIR(NOMBRE)  DIR(NOMBRE)</c>
    /// Si un operando no tiene direccion asignada (nombre de funcion, indice
    /// de salto, etc.) se muestra solo el nombre sin prefijo de direccion.
    /// </summary>
    public string Format(IReadOnlyDictionary<string, int> addressBook)
    {
        static string Fmt(string? s, IReadOnlyDictionary<string, int> book)
        {
            if (s is null) return "_";
            return book.TryGetValue(s, out int addr) ? $"{addr}({s})" : s;
        }
        var l   = Fmt(Left,   addressBook);
        var r   = Fmt(Right,  addressBook);
        var res = Fmt(Result, addressBook);
        return $"{Index,4}  {Op.ToSymbol(),-8}  {l,-22}  {r,-22}  {res}";
    }
}
