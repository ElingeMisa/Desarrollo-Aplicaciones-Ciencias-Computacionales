// =============================================================================
//  QuadrupleEmitter.cs - Orquestador de las estructuras de generacion de codigo.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  QuadrupleEmitter centraliza las tres pilas y la fila de cuadruplos que
//  intervienen en la generacion de codigo intermedio de Patito:
//
//    PilaOperadores  -- operadores binarios pendientes de aplicar.
//    PilaOperandos   -- nombres de operandos/temporales.
//    PilaTipos       -- tipos semanticos paralelos a PilaOperandos.
//    FilaCuadruplos  -- lista ordenada de cuadruplos emitidos.
//
//  Tambien mantiene el contador de temporales (_tempCounter) que permite
//  generar nombres unicos "t0", "t1", "t2", ... para los resultados
//  intermedios de las expresiones.
//
//  El metodo central es EmitBinary(): extrae el operador de PilaOperadores
//  y los dos operandos y tipos de PilaOperandos/PilaTipos, luego consulta
//  el cubo semantico para obtener el tipo del resultado y emite el cuadruplo
//  en FilaCuadruplos.
// =============================================================================

using Patito.Compiler.Semantic;

namespace Patito.Compiler.CodeGen;

/// <summary>
/// Orquesta las pilas y la fila de cuadruplos durante la generacion de
/// codigo intermedio para el compilador Patito.
/// </summary>
public sealed class QuadrupleEmitter
{
    private int _tempCounter;

    /// <summary>Pila de operadores pendientes.</summary>
    public PilaOperadores Operadores { get; } = new();

    /// <summary>Pila de nombres de operandos.</summary>
    public PilaOperandos Operandos { get; } = new();

    /// <summary>Pila de tipos semanticos (paralela a <see cref="Operandos"/>).</summary>
    public PilaTipos Tipos { get; } = new();

    /// <summary>Fila (lista ordenada) de cuadruplos generados.</summary>
    public FilaCuadruplos Fila { get; } = new();

    /// <summary>
    /// Genera un nombre de temporal unico ("t0", "t1", …).
    /// </summary>
    public string NewTemp() => $"t{_tempCounter++}";

    /// <summary>
    /// Apila un par (nombre, tipo) en PilaOperandos y PilaTipos simultaneamente.
    /// Conveniente para ExitFactor*.
    /// </summary>
    public void PushOperand(string name, SemanticType type)
    {
        Operandos.Push(name);
        Tipos.Push(type);
    }

    /// <summary>
    /// Extrae el operador del tope de <see cref="Operadores"/> y los dos operandos
    /// del tope de <see cref="Operandos"/> / <see cref="Tipos"/>, consulta el cubo
    /// semantico y emite el cuadruplo. Devuelve el nombre y tipo del resultado
    /// (o ("?", Error) si los tipos son incompatibles).
    /// </summary>
    public (string name, SemanticType type) EmitBinary(
        QuadOp op, string leftName, SemanticType leftType,
        string rightName, SemanticType rightType)
    {
        // Usar PilaOperadores como mecanismo de transferencia del operador
        Operadores.Push(op);
        var quadOp = Operadores.Pop();

        var semOp = ToSemanticOp(quadOp);
        var resultType = SemanticCube.Default.Resolve(leftType, semOp, rightType);
        if (resultType == SemanticType.Error)
            return ("?", SemanticType.Error);

        var temp = NewTemp();
        Fila.Emit(quadOp, leftName, rightName, temp);
        return (temp, resultType);
    }

    // -------------------------------------------------------------------------
    //  Conversion QuadOp <-> SemanticOp (solo para los operadores que tienen
    //  representacion en ambos enums).
    // -------------------------------------------------------------------------
    private static SemanticOp ToSemanticOp(QuadOp op) => op switch
    {
        QuadOp.Plus   => SemanticOp.Plus,
        QuadOp.Minus  => SemanticOp.Minus,
        QuadOp.Times  => SemanticOp.Times,
        QuadOp.Divide => SemanticOp.Divide,
        QuadOp.Lt     => SemanticOp.Lt,
        QuadOp.Gt     => SemanticOp.Gt,
        QuadOp.Eq     => SemanticOp.Eq,
        QuadOp.Neq    => SemanticOp.Neq,
        QuadOp.Assign => SemanticOp.Assign,
        _ => throw new System.InvalidOperationException($"QuadOp {op} no tiene SemanticOp equivalente."),
    };
}
