using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Patito.Compiler.Generated;

namespace Patito.Compiler;

/// <summary>
/// Resultado de compilar (analizar lexica y sintacticamente) una fuente Patito.
/// </summary>
/// <param name="Tokens">Lista completa de tokens producidos por el scanner.</param>
/// <param name="Tree">Raiz del arbol de derivacion (regla 'programa') o null si fallo el parser.</param>
/// <param name="LexErrors">Errores reportados por el scanner (Lexer).</param>
/// <param name="ParseErrors">Errores reportados por el parser.</param>
public sealed record CompileResult(
    IReadOnlyList<IToken> Tokens,
    IParseTree? Tree,
    IReadOnlyList<CompileError> LexErrors,
    IReadOnlyList<CompileError> ParseErrors)
{
    public bool Success => LexErrors.Count == 0 && ParseErrors.Count == 0;
}

/// <summary>
/// Punto unico de entrada para correr el front-end (scanner + parser) sobre una
/// fuente Patito. Esta clase la consume tanto el ejecutable como los tests.
/// </summary>
public static class PatitoFrontEnd
{
    public static CompileResult Compile(string source, string sourceName = "<inline>")
    {
        // ---- 1. SCANNER -----------------------------------------------------
        var lexErrorListener = new PatitoErrorListener(sourceName);
        var input = new AntlrInputStream(source);
        var lexer = new PatitoLexer(input);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(lexErrorListener);

        // Materializamos la lista de tokens para poder reportarlos despues.
        var tokenStream = new CommonTokenStream(lexer);
        tokenStream.Fill();
        var tokens = new List<IToken>(tokenStream.GetTokens());

        // ---- 2. PARSER ------------------------------------------------------
        var parseErrorListener = new PatitoErrorListener(sourceName);
        var parser = new PatitoParser(tokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(parseErrorListener);

        IParseTree? tree = null;
        try
        {
            tree = parser.programa();
        }
        catch (RecognitionException ex)
        {
            // ANTLR normalmente reporta esto via el listener; este catch es defensivo.
            parseErrorListener.SyntaxError(
                System.Console.Error, parser,
                ex.OffendingToken,
                ex.OffendingToken?.Line ?? 0,
                ex.OffendingToken?.Column ?? 0,
                ex.Message, ex);
        }

        return new CompileResult(
            tokens,
            tree,
            lexErrorListener.Errors,
            parseErrorListener.Errors);
    }
}
