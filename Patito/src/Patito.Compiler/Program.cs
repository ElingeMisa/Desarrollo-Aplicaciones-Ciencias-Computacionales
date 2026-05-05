using System;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Patito.Compiler.Generated;

namespace Patito.Compiler;

/// <summary>
/// Driver de linea de comandos para el compilador Patito.
///
/// Uso:
///     patitoc <archivo.patito>           Tokeniza y parsea el archivo y reporta resultado.
///     patitoc <archivo.patito> --tree    Imprime ademas el arbol de derivacion.
///     patitoc <archivo.patito> --tokens  Imprime el listado de tokens.
///     patitoc --demo                     Corre el ejemplo embebido (util para CI).
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return 0;
        }

        if (args[0] == "--demo")
        {
            return RunDemo();
        }

        var path = args[0];
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"[ERROR] No se encontro el archivo: {path}");
            return 2;
        }

        bool printTokens = args.Contains("--tokens");
        bool printTree   = args.Contains("--tree");

        var source = File.ReadAllText(path);
        var result = PatitoFrontEnd.Compile(source, Path.GetFileName(path));

        if (printTokens)
        {
            PrintTokens(result);
        }

        // Reporte de errores lexicos
        foreach (var err in result.LexErrors)
        {
            Console.Error.WriteLine($"[LEX] {err}");
        }
        // Reporte de errores sintacticos
        foreach (var err in result.ParseErrors)
        {
            Console.Error.WriteLine($"[PARSE] {err}");
        }

        if (printTree && result.Tree is not null)
        {
            // ToStringTree con el array de reglas produce un arbol legible (Lisp-like).
            var ruleNames = PatitoParser.ruleNames;
            Console.WriteLine();
            Console.WriteLine("=== Parse Tree ===");
            Console.WriteLine(Trees.ToStringTree(result.Tree, ruleNames));
        }

        if (result.Success)
        {
            Console.WriteLine($"[OK] {path}: {result.Tokens.Count} tokens, parser sin errores.");
            return 0;
        }
        else
        {
            Console.Error.WriteLine(
                $"[FAIL] {path}: {result.LexErrors.Count} error(es) lexico(s), " +
                $"{result.ParseErrors.Count} error(es) sintactico(s).");
            return 1;
        }
    }

    private static void PrintTokens(CompileResult result)
    {
        var symbolicNames = PatitoLexer.DefaultVocabulary;
        Console.WriteLine("=== Tokens ===");
        Console.WriteLine($"{"#",4}  {"Linea",5}:{"Col",-3}  {"Token",-14}  Texto");
        Console.WriteLine(new string('-', 60));
        int i = 0;
        foreach (var t in result.Tokens)
        {
            if (t.Type == TokenConstants.EOF) { Console.WriteLine($"{i++,4}  {t.Line,5}:{t.Column,-3}  EOF"); break; }
            var name = symbolicNames.GetSymbolicName(t.Type);
            var text = t.Text.Replace("\n", "\\n").Replace("\r", "\\r");
            Console.WriteLine($"{i++,4}  {t.Line,5}:{t.Column,-3}  {name,-14}  {text}");
        }
        Console.WriteLine();
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Patito Compiler - Front End (Scanner + Parser)");
        Console.WriteLine();
        Console.WriteLine("Uso:");
        Console.WriteLine("  patitoc <archivo.patito>            Analisis lexico + sintactico.");
        Console.WriteLine("  patitoc <archivo.patito> --tokens   Imprime ademas la lista de tokens.");
        Console.WriteLine("  patitoc <archivo.patito> --tree     Imprime ademas el parse tree.");
        Console.WriteLine("  patitoc --demo                       Corre un programa embebido.");
        Console.WriteLine();
    }

    /// <summary>
    /// Ejecuta un programa Patito embebido (util para CI o smoke-test sin archivos).
    /// </summary>
    private static int RunDemo()
    {
        const string demo = """
            programa demo;
            vars
                x, y: entero;
                z: flotante;
            inicio {
                x = 10;
                y = x + 5;
                z = 3.14;
                si (x > y) {
                    escribe("x es mayor", x);
                } sino {
                    escribe("y es mayor o igual", y);
                };
                mientras (x < 100) haz {
                    x = x + 1;
                };
            } fin
            """;
        var result = PatitoFrontEnd.Compile(demo, "demo.patito");
        PrintTokens(result);
        if (result.Success)
        {
            Console.WriteLine("[OK] demo: parseado correctamente.");
            return 0;
        }
        foreach (var e in result.LexErrors) Console.Error.WriteLine($"[LEX] {e}");
        foreach (var e in result.ParseErrors) Console.Error.WriteLine($"[PARSE] {e}");
        return 1;
    }
}
