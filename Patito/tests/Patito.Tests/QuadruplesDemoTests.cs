// =============================================================================
//  QuadruplesDemoTests.cs - Demo visual de generacion de cuadruplos.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Este archivo contiene pruebas de demostracion que compilan cada programa de
//  ejemplo valido y muestran, de forma formateada, el codigo fuente junto con
//  la fila completa de cuadruplos generados.
//
//  Como correr:
//      dotnet test --filter "FullyQualifiedName~QuadruplesDemoTests" -v normal
//
//  La salida formateada aparece en la seccion "Standard Output" de cada test.
//  Cada prueba [Fact] representa un programa de ejemplo del repositorio.
//
//  Los programas estan ordenados por complejidad creciente:
//    01 - Minimo (solo escribe)
//    02 - Variables, aritmetica y asignacion
//    03 - Condicional si/sino
//    04 - Ciclo mientras/haz
//    05 - Funcion con ciclo interno
//    06 - Expresiones mixtas y relacionales
//    07 - Comentarios (misma semantica que el ciclo)
//    08 - Multiples funciones y llamadas
//    09 - Anidamiento de si/sino y mientras
//    10 - Tipos flotantes y division
//    11 - Funcion con tipo de retorno usada en expresion
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Patito.Compiler;
using Patito.Compiler.CodeGen;
using Xunit;
using Xunit.Abstractions;

namespace Patito.Tests;

public class QuadruplesDemoTests
{
    private readonly ITestOutputHelper _out;

    // Ruta de la carpeta examples/ relativa al directorio de ejecucion del test.
    // dotnet test corre desde tests/Patito.Tests/bin/Debug/net*/
    // por lo que subimos cuatro niveles hasta la raiz del repo.
    private static readonly string ExamplesDir =
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "..", "examples"));

    public QuadruplesDemoTests(ITestOutputHelper output) => _out = output;

    // =========================================================================
    //  Helpers de formato
    // =========================================================================

    /// <summary>
    /// Compila el programa, imprime fuente + fila de cuadruplos y verifica
    /// que la compilacion fue exitosa y que todos los saltos estan resueltos.
    /// </summary>
    private void RunDemo(string source, string label)
    {
        var result = PatitoFrontEnd.Compile(source, label);

        PrintBanner(label);
        PrintSource(source);

        if (!result.Success)
        {
            _out.WriteLine("  *** ERRORES DE COMPILACION ***");
            foreach (var e in result.LexErrors)    _out.WriteLine($"  [LEX]   {e}");
            foreach (var e in result.ParseErrors)  _out.WriteLine($"  [PARSE] {e}");
            foreach (var e in result.SemanticErrors) _out.WriteLine($"  [SEM]   {e}");
            _out.WriteLine("");

            // Hacemos fallar el test para que se note en el runner.
            Assert.Fail($"El programa '{label}' no compilo correctamente.");
            return;
        }

        PrintQuads(result.Quads!);
        PrintStats(result);

        // Verificacion: ningun salto debe quedar sin resolver ("?").
        var unresolvedJumps = result.Quads!
            .Where(q => (q.Op == QuadOp.GotoF || q.Op == QuadOp.Goto) && q.Result == "?")
            .ToList();
        Assert.True(
            unresolvedJumps.Count == 0,
            $"Saltos sin resolver en '{label}': {string.Join(", ", unresolvedJumps.Select(q => q.ToString()))}");
    }

    private void PrintBanner(string label)
    {
        var title = $"  PROGRAMA: {label}  ";
        var line  = new string('=', Math.Max(60, title.Length + 4));
        _out.WriteLine("");
        _out.WriteLine(line);
        _out.WriteLine(title);
        _out.WriteLine(line);
    }

    private void PrintSource(string source)
    {
        _out.WriteLine("");
        _out.WriteLine("  --- Codigo fuente ---");
        var lines = source.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var l = lines[i].TrimEnd('\r');
            if (!string.IsNullOrWhiteSpace(l))
                _out.WriteLine($"  {i + 1,3}  {l}");
        }
        _out.WriteLine("");
    }

    private void PrintQuads(IReadOnlyList<Quadruple> quads)
    {
        _out.WriteLine("  --- Fila de cuadruplos generados ---");
        _out.WriteLine("");
        _out.WriteLine($"  {"#",4}  {"Op",-8}  {"Left",-14}  {"Right",-14}  Result");
        _out.WriteLine($"  {new string('-', 58)}");

        if (quads.Count == 0)
        {
            _out.WriteLine("  (sin cuadruplos)");
        }
        else
        {
            foreach (var q in quads)
            {
                var left   = q.Left   ?? "_";
                var right  = q.Right  ?? "_";
                var op     = q.Op.ToSymbol();
                _out.WriteLine($"  {q.Index,4}  {op,-8}  {left,-14}  {right,-14}  {q.Result}");
            }
        }
        _out.WriteLine("");
    }

    private void PrintStats(CompileResult result)
    {
        var nGlobals = result.Semantic?.GlobalTable.Count ?? 0;
        var nFuncs   = result.Semantic?.Directory.Count  ?? 0;
        var nQuads   = result.Quads?.Count ?? 0;
        var nTokens  = result.Tokens.Count;
        _out.WriteLine($"  Estadisticas: {nTokens} tokens  |  {nGlobals} global(es)  |  {nFuncs} funcion(es)  |  {nQuads} cuadruplo(s)");
        _out.WriteLine("");
    }

    // =========================================================================
    //  01 - Programa minimo
    // =========================================================================
    [Fact(DisplayName = "01 · Programa mínimo — solo escribe")]
    public void Demo_01_Minimo()
    {
        const string src = """
            programa hola;
            inicio {
                escribe("hola mundo");
            } fin
            """;
        RunDemo(src, "01_minimo");
    }

    // =========================================================================
    //  02 - Variables, aritmetica y asignacion
    // =========================================================================
    [Fact(DisplayName = "02 · Variables, aritmética y asignación")]
    public void Demo_02_VarsYAsigna()
    {
        const string src = """
            programa aritmetica;
            vars
                x, y, z: entero;
                pi: flotante;
            inicio {
                x = 10;
                y = 20;
                z = x + y * 2;
                pi = 3.14159;
                escribe("z =", z, "  pi =", pi);
            } fin
            """;
        RunDemo(src, "02_vars_y_asigna");
    }

    // =========================================================================
    //  03 - Condicional si / sino
    // =========================================================================
    [Fact(DisplayName = "03 · Condicional si/sino con Backfill")]
    public void Demo_03_Condicion()
    {
        const string src = """
            programa decide;
            vars
                edad: entero;
            inicio {
                edad = 18;
                si (edad < 18) {
                    escribe("menor de edad");
                } sino {
                    escribe("mayor o igual a 18");
                };
            } fin
            """;
        RunDemo(src, "03_condicion");
    }

    // =========================================================================
    //  04 - Ciclo mientras / haz
    // =========================================================================
    [Fact(DisplayName = "04 · Ciclo mientras/haz con Backfill")]
    public void Demo_04_Ciclo()
    {
        const string src = """
            programa cuenta;
            vars
                i: entero;
            inicio {
                i = 0;
                mientras (i < 5) haz {
                    escribe("i =", i);
                    i = i + 1;
                };
            } fin
            """;
        RunDemo(src, "04_ciclo");
    }

    // =========================================================================
    //  05 - Funcion con ciclo interno
    // =========================================================================
    [Fact(DisplayName = "05 · Función con ciclo interno — Param + Gosub")]
    public void Demo_05_Funcion()
    {
        const string src = """
            programa concarga;
            vars
                a: entero;

            nula saludar (n: entero) {
                vars
                    i: entero;
                i = 0;
                mientras (i < n) haz {
                    escribe("hola numero", i);
                    i = i + 1;
                };
            };

            inicio {
                a = 3;
                saludar(a);
            } fin
            """;
        RunDemo(src, "05_funcion");
    }

    // =========================================================================
    //  06 - Expresiones con parentesis y relacionales
    // =========================================================================
    [Fact(DisplayName = "06 · Expresiones mixtas — paréntesis, signos y relacionales")]
    public void Demo_06_Expresiones()
    {
        const string src = """
            programa expresiones;
            vars
                a, b, c: entero;
            inicio {
                a = -5;
                b = 3;
                c = (a + b) * 2;
                si (c == -4) {
                    escribe("c es -4 como esperabamos");
                };
                si ((a + b) != 0) {
                    escribe("la suma no es cero");
                };
            } fin
            """;
        RunDemo(src, "06_expresiones");
    }

    // =========================================================================
    //  07 - Comentarios (misma semantica que un ciclo)
    // =========================================================================
    [Fact(DisplayName = "07 · Comentarios — lexer los descarta, código igual a ciclo")]
    public void Demo_07_Comentarios()
    {
        const string src = """
            /* Ejemplo con comentarios de bloque y de linea */
            programa coments;
            vars
                n: entero;     // contador
            inicio {
                n = 0;            // inicializacion
                // bucle simple
                mientras (n < 3) haz {
                    escribe("n vale", n);
                    n = n + 1;
                };
            } fin
            """;
        RunDemo(src, "07_comentarios");
    }

    // =========================================================================
    //  08 - Multiples funciones y llamadas
    // =========================================================================
    [Fact(DisplayName = "08 · Múltiples funciones — imprimirDoble + contar")]
    public void Demo_08_FuncionesMultiples()
    {
        const string src = """
            programa multiFunc;
            vars
                resultado: entero;

            nula imprimirDoble (n: entero) {
                vars
                    doble: entero;
                doble = n + n;
                escribe("doble:", doble);
            };

            nula contar (limite: entero) {
                vars
                    i: entero;
                i = 0;
                mientras (i < limite) haz {
                    escribe(i);
                    i = i + 1;
                };
            };

            inicio {
                resultado = 5;
                imprimirDoble(resultado);
                contar(3);
            } fin
            """;
        RunDemo(src, "08_funciones_multiples");
    }

    // =========================================================================
    //  09 - Anidamiento si/sino + mientras con si interior
    // =========================================================================
    [Fact(DisplayName = "09 · Anidamiento — si/sino anidados + mientras con si interior")]
    public void Demo_09_Anidamiento()
    {
        const string src = """
            programa anidado;
            vars
                x, y, i: entero;

            inicio {
                x = 10;
                y = 5;

                si (x > y) {
                    si (y > 0) {
                        escribe("x mayor, y positivo");
                    } sino {
                        escribe("x mayor, y no positivo");
                    };
                } sino {
                    escribe("x no mayor que y");
                };

                i = 0;
                mientras (i < x) haz {
                    si (i == y) {
                        escribe("i igual a y");
                    };
                    i = i + 1;
                };
            } fin
            """;
        RunDemo(src, "09_anidamiento");
    }

    // =========================================================================
    //  10 - Tipos flotantes y division
    // =========================================================================
    [Fact(DisplayName = "10 · Tipos flotantes — división siempre produce Flotante")]
    public void Demo_10_TipoFlotante()
    {
        const string src = """
            programa flotantes;
            vars
                a, b: entero;
                r, pi: flotante;

            inicio {
                a = 10;
                b = 3;
                r = a / b;
                pi = 3.14159;
                escribe("division:", r);
                escribe("pi:", pi);
                si (r < pi) {
                    escribe("r es menor que pi");
                };
            } fin
            """;
        RunDemo(src, "10_tipo_flotante");
    }

    // =========================================================================
    //  11 - Funcion con retorno usada como factor en una expresion
    // =========================================================================
    [Fact(DisplayName = "11 · Retorno de función usado como factor en expresión")]
    public void Demo_11_RetornoTipo()
    {
        const string src = """
            programa retorno;
            vars
                x, resultado: entero;

            entero doble (n: entero) {
                vars
                    d: entero;
                d = n + n;
            };

            inicio {
                x = 7;
                resultado = doble(x) + 1;
                escribe("resultado:", resultado);
            } fin
            """;
        RunDemo(src, "11_retorno_tipo");
    }

    // =========================================================================
    //  Prueba de integracion: todos los ejemplos en un solo test
    // =========================================================================
    [Fact(DisplayName = "ALL · Todos los ejemplos válidos — resumen de cuádruplos")]
    public void Demo_TodosLosEjemplos_Resumen()
    {
        var ejemplos = new (string label, string src)[]
        {
            ("01_minimo",
             "programa hola;\ninicio {\n    escribe(\"hola mundo\");\n} fin"),
            ("02_vars_y_asigna",
             "programa aritmetica;\nvars\n    x, y, z: entero;\n    pi: flotante;\ninicio {\n    x = 10;\n    y = 20;\n    z = x + y * 2;\n    pi = 3.14159;\n    escribe(\"z =\", z);\n} fin"),
            ("03_condicion",
             "programa decide;\nvars\n    edad: entero;\ninicio {\n    edad = 18;\n    si (edad < 18) {\n        escribe(\"menor\");\n    } sino {\n        escribe(\"mayor\");\n    };\n} fin"),
            ("04_ciclo",
             "programa cuenta;\nvars\n    i: entero;\ninicio {\n    i = 0;\n    mientras (i < 5) haz {\n        escribe(i);\n        i = i + 1;\n    };\n} fin"),
            ("05_funcion",
             "programa concarga;\nvars\n    a: entero;\nnula saludar (n: entero) {\n    vars\n        i: entero;\n    i = 0;\n    mientras (i < n) haz {\n        escribe(i);\n        i = i + 1;\n    };\n};\ninicio {\n    a = 3;\n    saludar(a);\n} fin"),
        };

        _out.WriteLine("");
        _out.WriteLine("========================================================");
        _out.WriteLine("  RESUMEN — Cuadruplos de los primeros 5 ejemplos        ");
        _out.WriteLine("========================================================");

        var totalQuads = 0;
        foreach (var (label, src) in ejemplos)
        {
            var r = PatitoFrontEnd.Compile(src, label);
            Assert.True(r.Success, $"'{label}' no compilo: {string.Join("; ", r.SemanticErrors)}");

            _out.WriteLine("");
            _out.WriteLine($"  [{label}]  → {r.Quads!.Count} cuadruplo(s)");
            _out.WriteLine($"  {"#",4}  {"Op",-8}  {"Left",-12}  {"Right",-12}  Result");
            _out.WriteLine($"  {new string('-', 54)}");
            foreach (var q in r.Quads!)
            {
                var l = q.Left  ?? "_";
                var rv = q.Right ?? "_";
                _out.WriteLine($"  {q.Index,4}  {q.Op.ToSymbol(),-8}  {l,-12}  {rv,-12}  {q.Result}");
            }
            totalQuads += r.Quads!.Count;
        }

        _out.WriteLine("");
        _out.WriteLine($"  Total de cuadruplos generados: {totalQuads}");
        _out.WriteLine("========================================================");
        _out.WriteLine("");
    }
}
