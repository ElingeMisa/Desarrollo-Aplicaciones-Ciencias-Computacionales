using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;

namespace Patito.Compiler;

/// <summary>
/// Representa un error detectado por el scanner o el parser.
/// </summary>
public sealed record CompileError(int Line, int Column, string Message, string Source)
{
    public override string ToString() =>
        $"[{Source}] Linea {Line}, Columna {Column}: {Message}";
}

/// <summary>
/// Listener compartido para los errores de Lexer y Parser de ANTLR4.
/// Acumula los errores en una lista para inspeccion posterior (util en tests).
/// Reemplaza al ConsoleErrorListener por defecto.
/// </summary>
public sealed class PatitoErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    private readonly List<CompileError> _errors = new();
    private readonly string _source;

    public PatitoErrorListener(string source) => _source = source;

    public IReadOnlyList<CompileError> Errors => _errors;
    public bool HasErrors => _errors.Count > 0;

    // ---- Errores de PARSER ----------------------------------------------------
    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        _errors.Add(new CompileError(line, charPositionInLine + 1, msg, _source));
    }

    // ---- Errores de LEXER -----------------------------------------------------
    public void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        int offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        _errors.Add(new CompileError(line, charPositionInLine + 1, msg, _source));
    }
}
