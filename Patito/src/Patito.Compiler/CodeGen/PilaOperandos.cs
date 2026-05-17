// =============================================================================
//  PilaOperandos.cs - Pila de nombres de operandos (codigo intermedio).
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Cada vez que se evalua un factor (constante, variable o resultado temporal)
//  su nombre se apila aqui. QuadrupleEmitter.EmitBinary() extrae los dos
//  operandos del tope para construir el cuadruplo. Esta pila se mantiene en
//  paralelo con PilaTipos: el elemento en la posicion N de PilaOperandos
//  tiene su tipo correspondiente en la posicion N de PilaTipos.
//
//  Estructura subyacente: Stack<string> del BCL de .NET.
// =============================================================================

using System.Collections.Generic;

namespace Patito.Compiler.CodeGen;

/// <summary>
/// Pila de nombres de operandos usados durante la generacion de cuadruplos.
/// Cada entrada es el nombre de una variable, constante o temporal (ej. "x", "3.14", "t0").
/// </summary>
public sealed class PilaOperandos
{
    private readonly Stack<string> _stack = new();

    /// <summary>Cantidad de operandos en la pila.</summary>
    public int Count => _stack.Count;

    /// <summary>True cuando la pila esta vacia.</summary>
    public bool IsEmpty => _stack.Count == 0;

    /// <summary>Apila un operando.</summary>
    public void Push(string operand) => _stack.Push(operand);

    /// <summary>Extrae y devuelve el operando del tope.</summary>
    public string Pop() => _stack.Pop();

    /// <summary>Devuelve el operando del tope sin extraerlo.</summary>
    public string Peek() => _stack.Peek();

    public override string ToString() => $"PilaOperandos(Count={Count})";
}
