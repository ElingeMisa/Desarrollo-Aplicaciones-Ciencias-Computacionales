// =============================================================================
//  FilaCuadruplos.cs - Fila (lista indexada) de cuadruplos generados.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Los cuadruplos se generan en orden de ejecucion y se acumulan aqui.
//  Conceptualmente es una FILA (cola) porque los cuadruplos se agregan al
//  final en el mismo orden en que se ejecutaran. Sin embargo, necesitamos
//  acceso por indice para implementar la operacion de Backfill (rellenar el
//  destino de un salto condicional o incondicional una vez que se conoce
//  la posicion destino).
//
//  Por eso la estructura subyacente es List<Quadruple> en lugar de Queue<T>:
//    * Append al final -> O(1) amortizado (equivalente a Enqueue).
//    * Acceso por indice para Backfill -> O(1).
//    * Iteracion en orden de emision -> O(n).
// =============================================================================

using System.Collections.Generic;

namespace Patito.Compiler.CodeGen;

/// <summary>
/// Fila de cuadruplos de codigo intermedio. Los cuadruplos se emiten en orden
/// de ejecucion; el metodo <see cref="Backfill"/> permite actualizar el destino
/// de un salto una vez que se conoce.
/// </summary>
public sealed class FilaCuadruplos
{
    private readonly List<Quadruple> _list = new();

    /// <summary>Numero de cuadruplos emitidos hasta ahora.</summary>
    public int Count => _list.Count;

    /// <summary>Acceso de solo lectura a la lista de cuadruplos (para imprimir).</summary>
    public IReadOnlyList<Quadruple> Quads => _list;

    /// <summary>
    /// Emite un cuadruplo y lo agrega al final de la fila.
    /// Devuelve el indice del cuadruplo recien emitido (base 0).
    /// </summary>
    public int Emit(QuadOp op, string? left, string? right, string result)
    {
        int index = _list.Count;
        _list.Add(new Quadruple(index, op, left, right, result));
        return index;
    }

    /// <summary>
    /// Rellena el campo <c>Result</c> del cuadruplo en <paramref name="index"/>
    /// con un nuevo valor. Se usa para resolver destinos de saltos (GotoF/Goto)
    /// que al momento de emitirse aun no se conocian.
    /// </summary>
    public void Backfill(int index, string newResult)
    {
        if (index < 0 || index >= _list.Count) return;
        _list[index] = _list[index] with { Result = newResult };
    }
}
