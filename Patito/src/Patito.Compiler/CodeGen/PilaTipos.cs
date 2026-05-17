// =============================================================================
//  PilaTipos.cs - Pila de tipos semanticos (codigo intermedio).
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Se mantiene en paralelo con PilaOperandos: cada vez que un nombre de
//  operando se apila en PilaOperandos, su tipo semantico se apila aqui.
//  QuadrupleEmitter.EmitBinary() extrae el tipo del tope de esta pila para
//  consultar el cubo semantico antes de emitir el cuadruplo.
//
//  Estructura subyacente: Stack<SemanticType> del BCL de .NET.
// =============================================================================

using System.Collections.Generic;
using Patito.Compiler.Semantic;

namespace Patito.Compiler.CodeGen;

/// <summary>
/// Pila de tipos semanticos en paralelo con <see cref="PilaOperandos"/>.
/// El tipo en la posicion N corresponde al operando en la posicion N de PilaOperandos.
/// </summary>
public sealed class PilaTipos
{
    private readonly Stack<SemanticType> _stack = new();

    /// <summary>Cantidad de tipos en la pila.</summary>
    public int Count => _stack.Count;

    /// <summary>True cuando la pila esta vacia.</summary>
    public bool IsEmpty => _stack.Count == 0;

    /// <summary>Apila un tipo.</summary>
    public void Push(SemanticType type) => _stack.Push(type);

    /// <summary>Extrae y devuelve el tipo del tope.</summary>
    public SemanticType Pop() => _stack.Pop();

    /// <summary>Devuelve el tipo del tope sin extraerlo.</summary>
    public SemanticType Peek() => _stack.Peek();

    public override string ToString() => $"PilaTipos(Count={Count})";
}
