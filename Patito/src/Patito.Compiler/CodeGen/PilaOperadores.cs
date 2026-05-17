// =============================================================================
//  PilaOperadores.cs - Pila de operadores pendientes (codigo intermedio).
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Durante la evaluacion de expresiones, el compilador apila el operador
//  que se va a aplicar justo antes de emitir el cuadruplo correspondiente.
//  QuadrupleEmitter.EmitBinary() extrae el operador del tope de esta pila,
//  lo combina con los dos operandos del tope de PilaOperandos y PilaTipos,
//  y emite la instruccion en FilaCuadruplos.
//
//  Estructura subyacente: Stack<QuadOp> del BCL de .NET.
// =============================================================================

using System.Collections.Generic;

namespace Patito.Compiler.CodeGen;

/// <summary>
/// Pila de operadores (<see cref="QuadOp"/>) pendientes de aplicar durante
/// la generacion de cuadruplos de una expresion Patito.
/// </summary>
public sealed class PilaOperadores
{
    private readonly Stack<QuadOp> _stack = new();

    /// <summary>Cantidad de operadores en la pila.</summary>
    public int Count => _stack.Count;

    /// <summary>True cuando no hay operadores pendientes.</summary>
    public bool IsEmpty => _stack.Count == 0;

    /// <summary>Apila un operador.</summary>
    public void Push(QuadOp op) => _stack.Push(op);

    /// <summary>Extrae y devuelve el operador del tope.</summary>
    public QuadOp Pop() => _stack.Pop();

    /// <summary>Devuelve el operador del tope sin extraerlo.</summary>
    public QuadOp Peek() => _stack.Peek();

    public override string ToString() => $"PilaOperadores(Count={Count})";
}
