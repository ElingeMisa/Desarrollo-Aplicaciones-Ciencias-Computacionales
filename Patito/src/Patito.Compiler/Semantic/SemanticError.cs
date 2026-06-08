// =============================================================================
//  SemanticError.cs - Reporte de un error de analisis semantico.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Cada error se modela como un registro inmutable con:
//
//    * Code   : enum estable (no depende del texto). Esto es lo que afirman
//               los tests; si refraseamos el mensaje en espanol, los tests no
//               se rompen.
//    * Line   : numero de linea (base 1) en la fuente.
//    * Column : columna (base 1).
//    * Name   : identificador involucrado (o cadena vacia).
//    * Message: mensaje legible para el usuario.
//
//  Mantener el codigo separado del mensaje permite tambien internacionalizar
//  la salida sin tocar la logica del analizador.
// =============================================================================

namespace Patito.Compiler.Semantic;

/// <summary>Categorias de errores semanticos.</summary>
public enum SemanticErrorCode
{
    VariableRedeclared,
    ParameterRedeclared,
    FunctionRedeclared,
    UndeclaredVariable,
    UndeclaredFunction,
    NameClashesWithProgram,
    // Entrega 3: incompatibilidad de tipos detectada al generar cuadruplos.
    TypeMismatch,
    // 'regresa' usado fuera de una funcion, o en una funcion 'nula'.
    InvalidReturn,
}

public sealed record SemanticError(
    int Line,
    int Column,
    SemanticErrorCode Code,
    string Name,
    string Message)
{
    public override string ToString() =>
        $"Linea {Line}, Columna {Column}: [{Code}] {Message}";
}
