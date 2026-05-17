using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Patito.Compiler.CodeGen;
using Patito.Compiler.Generated;
using Patito.Compiler.Semantic;

namespace Patito.Compiler;

/// <summary>
/// Resultado de compilar (analizar lexica, sintactica y semanticamente) una fuente Patito.
/// </summary>
/// <param name="Tokens">Lista completa de tokens producidos por el scanner.</param>
/// <param name="Tree">Raiz del arbol de derivacion (regla 'programa') o null si fallo el parser.</param>
/// <param name="LexErrors">Errores reportados por el scanner (Lexer).</param>
/// <param name="ParseErrors">Errores reportados por el parser.</param>
/// <param name="SemanticErrors">Errores reportados por el analizador semantico.</param>
/// <param name="Semantic">Instancia del analizador, con tablas y cuadruplos ya generados. Null si el parser fallo.</param>
public sealed record CompileResult(
    IReadOnlyList<IToken> Tokens,
    IParseTree? Tree,
    IReadOnlyList<CompileError> LexErrors,
    IReadOnlyList<CompileError> ParseErrors,
    IReadOnlyList<SemanticError> SemanticErrors,
    SemanticAnalyzer? Semantic)
{
    /// <summary>Compilacion totalmente sin errores (lex + parse + semantica).</summary>
    public bool Success =>
        LexErrors.Count == 0 && ParseErrors.Count == 0 && SemanticErrors.Count == 0;

    /// <summary>True si scanner y parser pasaron, aunque haya errores semanticos.</summary>
    public bool ParseSuccess =>
        LexErrors.Count == 0 && ParseErrors.Count == 0;

    /// <summary>
    /// Cuadruplos generados durante el analisis semantico (Entrega 3).
    /// Null si el parser fallo o si no se llego a la fase semantica.
    /// </summary>
    public IReadOnlyList<Quadruple>? Quads => Semantic?.Quads;
}

/// <summary>
/// Punto unico de entrada para correr el front-end (scanner + parser + analisis
/// semantico + generacion de cuadruplos) sobre una fuente Patito.
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
            parseErrorListener.SyntaxError(
                System.Console.Error, parser,
                ex.OffendingToken,
                ex.OffendingToken?.Line ?? 0,
                ex.OffendingToken?.Column ?? 0,
                ex.Message, ex);
        }

        // ---- 3. ANALISIS SEMANTICO + GENERACION DE CUADRUPLOS --------------
        SemanticAnalyzer? analyzer = null;
        IReadOnlyList<SemanticError> semErrors = System.Array.Empty<SemanticError>();
        if (tree is not null && parseErrorListener.Errors.Count == 0)
        {
            analyzer = new SemanticAnalyzer();
            ParseTreeWalker.Default.Walk(analyzer, tree);
            semErrors = analyzer.Errors;
        }

        return new CompileResult(
            tokens,
            tree,
            lexErrorListener.Errors,
            parseErrorListener.Errors,
            semErrors,
            analyzer);
    }
}
