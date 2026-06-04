using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Patito.Compiler.Generated;
using Patito.Compiler.VM;

namespace Patito.Compiler;

/// <summary>
/// Driver de linea de comandos para el compilador Patito.
///
/// Uso:
///     patitoc &lt;archivo.patito&gt;            Tokeniza, parsea y analiza semanticamente.
///     patitoc &lt;archivo.patito&gt; --tree      Imprime ademas el arbol de derivacion.
///     patitoc &lt;archivo.patito&gt; --tokens    Imprime el listado de tokens.
///     patitoc &lt;archivo.patito&gt; --symbols   Imprime tablas de variables y directorio.
///     patitoc &lt;archivo.patito&gt; --quads     Imprime la fila de cuadruplos generados.
///     patitoc --demo                         Corre el ejemplo embebido (util para CI).
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

        bool printTokens  = args.Contains("--tokens");
        bool printTree    = args.Contains("--tree");
        bool printSymbols = args.Contains("--symbols");
        bool printQuads   = args.Contains("--quads");
        bool runVm        = args.Contains("--run") || args.Contains("-r");

        var source = File.ReadAllText(path);
        var result = PatitoFrontEnd.Compile(source, Path.GetFileName(path));

        if (printTokens)
            PrintTokens(result);

        foreach (var err in result.LexErrors)
            Console.Error.WriteLine($"[LEX] {err}");
        foreach (var err in result.ParseErrors)
            Console.Error.WriteLine($"[PARSE] {err}");
        foreach (var err in result.SemanticErrors)
            Console.Error.WriteLine($"[SEM] {err}");

        if (printTree && result.Tree is not null)
        {
            var ruleNames = PatitoParser.ruleNames;
            Console.WriteLine();
            Console.WriteLine("=== Parse Tree ===");
            Console.WriteLine(Trees.ToStringTree(result.Tree, ruleNames));
        }

        if (result.Semantic is not null && (printTree || printSymbols))
            PrintSymbolTables(result.Semantic);

        if (printQuads)
            PrintQuadruples(result);

        if (result.Success)
        {
            var nFuncs   = result.Semantic?.Directory.Count ?? 0;
            var nGlobals = result.Semantic?.GlobalTable.Count ?? 0;
            var nQuads   = result.Quads?.Count ?? 0;

            if (runVm)
            {
                // Modo --run: ejecutar la VM y mostrar su output
                var constValues = result.ConstValues
                    ?? new Dictionary<int, object>();
                var vm = new VirtualMachine(
                    result.Quads!,
                    result.AddressBook!,
                    constValues,
                    result.FunctionDirectory!,
                    Console.Out);
                var vmResult = vm.Execute();
                if (!vmResult.Success)
                {
                    Console.Error.WriteLine($"[VM ERROR] {vmResult.Error?.Message}");
                    return 3;
                }
                return 0;
            }

            Console.WriteLine(
                $"[OK] {path}: {result.Tokens.Count} tokens, " +
                $"{nGlobals} variable(s) global(es), {nFuncs} funcion(es), " +
                $"{nQuads} cuadruplo(s).");
            return 0;
        }
        else
        {
            Console.Error.WriteLine(
                $"[FAIL] {path}: {result.LexErrors.Count} error(es) lexico(s), " +
                $"{result.ParseErrors.Count} error(es) sintactico(s), " +
                $"{result.SemanticErrors.Count} error(es) semantico(s).");
            return 1;
        }
    }

    private static void PrintQuadruples(CompileResult result)
    {
        Console.WriteLine();
        Console.WriteLine("=== Fila de Cuadruplos ===");
        Console.WriteLine($"{"#",4}  {"Op",-8}  {"Left",-22}  {"Right",-22}  Result");
        Console.WriteLine(new string('-', 76));

        var quads = result.Quads;
        if (quads is null || quads.Count == 0)
        {
            Console.WriteLine("  (sin cuadruplos)");
        }
        else
        {
            var book = result.AddressBook;
            foreach (var q in quads)
                Console.WriteLine(book is not null && book.Count > 0
                    ? q.Format(book)
                    : q.ToString());
        }
        Console.WriteLine();
    }

    private static void PrintSymbolTables(Patito.Compiler.Semantic.SemanticAnalyzer sem)
    {
        Console.WriteLine();
        Console.WriteLine($"=== Programa: {sem.ProgramName ?? "<sin nombre>"} ===");
        Console.WriteLine();
        Console.WriteLine("--- Tabla de Variables Globales ---");
        if (sem.GlobalTable.Count == 0)
        {
            Console.WriteLine("  (vacia)");
        }
        else
        {
            foreach (var s in sem.GlobalTable.Symbols)
                Console.WriteLine($"  {s}");
        }
        Console.WriteLine();
        Console.WriteLine("--- Directorio de Funciones ---");
        if (sem.Directory.Count == 0)
        {
            Console.WriteLine("  (sin funciones)");
        }
        else
        {
            foreach (var f in sem.Directory.Functions)
            {
                Console.WriteLine($"  {f}");
                foreach (var s in f.LocalTable.Symbols)
                    Console.WriteLine($"    - {s}");
            }
        }
        Console.WriteLine();
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
        Console.WriteLine("Patito Compiler - Front End + Generacion de Cuadruplos + Maquina Virtual");
        Console.WriteLine();
        Console.WriteLine("Uso:");
        Console.WriteLine("  patitoc <archivo.patito>            Analisis lexico + sintactico + semantico.");
        Console.WriteLine("  patitoc <archivo.patito> --tokens   Imprime ademas la lista de tokens.");
        Console.WriteLine("  patitoc <archivo.patito> --tree     Imprime ademas el parse tree y las tablas.");
        Console.WriteLine("  patitoc <archivo.patito> --symbols  Imprime la tabla global y el directorio.");
        Console.WriteLine("  patitoc <archivo.patito> --quads    Imprime la fila de cuadruplos generados.");
        Console.WriteLine("  patitoc <archivo.patito> --run      Compila y ejecuta con la maquina virtual.");
        Console.WriteLine("  patitoc <archivo.patito> -r         Alias de --run.");
        Console.WriteLine("  patitoc --demo                      Corre un programa embebido.");
        Console.WriteLine();
    }

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
        PrintQuadruples(result);
        if (result.Success)
        {
            Console.WriteLine("[OK] demo: compilado correctamente.");
            return 0;
        }
        foreach (var e in result.LexErrors)    Console.Error.WriteLine($"[LEX] {e}");
        foreach (var e in result.ParseErrors)  Console.Error.WriteLine($"[PARSE] {e}");
        foreach (var e in result.SemanticErrors) Console.Error.WriteLine($"[SEM] {e}");
        return 1;
    }
}
