// =============================================================================
//  SemanticAnalyzerTests.cs - Pruebas end-to-end del analizador semantico.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Estas pruebas alimentan codigo Patito al frontend completo y verifican
//  que:
//    * los simbolos terminen en las tablas correctas, con el tipo correcto;
//    * el directorio de funciones quede bien poblado;
//    * cada validacion ("punto neuralgico") emita el error esperado.
//
//  Los tests afirman codigos de error (SemanticErrorCode) en lugar de texto
//  para que el set de pruebas no dependa de la redaccion exacta del mensaje.
// =============================================================================

using System.Linq;
using Patito.Compiler;
using Patito.Compiler.Semantic;
using Xunit;

namespace Patito.Tests;

public class SemanticAnalyzerTests
{
    private static CompileResult Run(string src) => PatitoFrontEnd.Compile(src, "<test>");

    // -------------------------------------------------------------------------
    //  Programas validos: deben pasar lex, parse y semantica.
    // -------------------------------------------------------------------------

    [Fact]
    public void Programa_SinVariables_PasaTodo()
    {
        const string src = """
            programa hola;
            inicio {
                escribe("hola");
            } fin
            """;
        var r = Run(src);
        Assert.True(r.Success, FormatErrors(r));
        Assert.Equal(0, r.Semantic!.GlobalTable.Count);
        Assert.Equal(0, r.Semantic.Directory.Count);
        Assert.Equal("hola", r.Semantic.ProgramName);
    }

    [Fact]
    public void Vars_GlobalesQuedanRegistradasConTipo()
    {
        const string src = """
            programa demo;
            vars
                x, y: entero;
                pi: flotante;
            inicio {
                x = 1;
            } fin
            """;
        var r = Run(src);
        Assert.True(r.Success, FormatErrors(r));

        var gt = r.Semantic!.GlobalTable;
        Assert.Equal(3, gt.Count);

        Assert.Equal(SemanticType.Entero,   gt.Lookup("x")!.Type);
        Assert.Equal(SemanticType.Entero,   gt.Lookup("y")!.Type);
        Assert.Equal(SemanticType.Flotante, gt.Lookup("pi")!.Type);
    }

    [Fact]
    public void Funcion_QuedaEnDirectorioConParametrosYLocales()
    {
        const string src = """
            programa demo;
            vars
                a: entero;

            nula sumar (m: entero, n: flotante) {
                vars
                    i: entero;
                i = 0;
            };

            inicio {
                a = 5;
                sumar(a, 1.5);
            } fin
            """;
        var r = Run(src);
        Assert.True(r.Success, FormatErrors(r));

        var dir = r.Semantic!.Directory;
        Assert.Single(dir.Functions);
        var f = dir.Lookup("sumar")!;

        Assert.Equal(SemanticType.Nula, f.ReturnType);
        Assert.Equal(new[] { SemanticType.Entero, SemanticType.Flotante }, f.ParameterTypes.ToArray());

        // La tabla local debe contener m (param), n (param) y i (local).
        Assert.Equal(3, f.LocalTable.Count);
        Assert.Equal(SymbolKind.Parameter, f.LocalTable.Lookup("m")!.Kind);
        Assert.Equal(SymbolKind.Parameter, f.LocalTable.Lookup("n")!.Kind);
        Assert.Equal(SymbolKind.Variable,  f.LocalTable.Lookup("i")!.Kind);
    }

    // -------------------------------------------------------------------------
    //  Validaciones / errores
    // -------------------------------------------------------------------------

    [Fact]
    public void VarGlobalDoblementeDeclarada_EmiteError()
    {
        const string src = """
            programa demo;
            vars
                x: entero;
                x: flotante;
            inicio {
                x = 1;
            } fin
            """;
        var r = Run(src);
        Assert.False(r.Success);
        Assert.Contains(r.SemanticErrors, e => e.Code == SemanticErrorCode.VariableRedeclared && e.Name == "x");
    }

    [Fact]
    public void VarLocalDoblementeDeclarada_EmiteError()
    {
        const string src = """
            programa demo;
            nula f () {
                vars
                    i: entero;
                    i: flotante;
            };
            inicio { f(); } fin
            """;
        var r = Run(src);
        Assert.False(r.Success);
        Assert.Contains(r.SemanticErrors, e => e.Code == SemanticErrorCode.VariableRedeclared && e.Name == "i");
    }

    [Fact]
    public void ParametroDuplicado_EmiteError()
    {
        const string src = """
            programa demo;
            nula f (x: entero, x: flotante) { };
            inicio { f(1, 2); } fin
            """;
        var r = Run(src);
        Assert.False(r.Success);
        Assert.Contains(r.SemanticErrors, e => e.Code == SemanticErrorCode.ParameterRedeclared && e.Name == "x");
    }

    [Fact]
    public void ParametroChocaConLocal_EmiteError()
    {
        const string src = """
            programa demo;
            nula f (x: entero) {
                vars
                    x: entero;
            };
            inicio { f(1); } fin
            """;
        var r = Run(src);
        Assert.False(r.Success);
        // El parametro 'x' se declara primero, asi que el local choca y se reporta
        // como VariableRedeclared.
        Assert.Contains(r.SemanticErrors, e => e.Code == SemanticErrorCode.VariableRedeclared && e.Name == "x");
    }

    [Fact]
    public void FuncionDuplicada_EmiteError()
    {
        const string src = """
            programa demo;
            nula f () { };
            nula f () { };
            inicio { f(); } fin
            """;
        var r = Run(src);
        Assert.False(r.Success);
        Assert.Contains(r.SemanticErrors, e => e.Code == SemanticErrorCode.FunctionRedeclared && e.Name == "f");
    }

    [Fact]
    public void FuncionConNombreDelPrograma_EmiteError()
    {
        const string src = """
            programa demo;
            nula demo () { };
            inicio { demo(); } fin
            """;
        var r = Run(src);
        Assert.False(r.Success);
        Assert.Contains(r.SemanticErrors, e => e.Code == SemanticErrorCode.NameClashesWithProgram);
    }

    [Fact]
    public void VariableNoDeclaradaEnAsignacion_EmiteError()
    {
        const string src = """
            programa demo;
            inicio {
                x = 1;
            } fin
            """;
        var r = Run(src);
        Assert.False(r.Success);
        Assert.Contains(r.SemanticErrors, e => e.Code == SemanticErrorCode.UndeclaredVariable && e.Name == "x");
    }

    [Fact]
    public void VariableNoDeclaradaEnExpresion_EmiteError()
    {
        const string src = """
            programa demo;
            vars
                a: entero;
            inicio {
                a = b + 1;
            } fin
            """;
        var r = Run(src);
        Assert.False(r.Success);
        Assert.Contains(r.SemanticErrors, e => e.Code == SemanticErrorCode.UndeclaredVariable && e.Name == "b");
    }

    [Fact]
    public void LlamadaAFuncionInexistente_EmiteError()
    {
        const string src = """
            programa demo;
            inicio {
                noExiste();
            } fin
            """;
        var r = Run(src);
        Assert.False(r.Success);
        Assert.Contains(r.SemanticErrors, e => e.Code == SemanticErrorCode.UndeclaredFunction && e.Name == "noExiste");
    }

    [Fact]
    public void VariableLocalEnsombreceALaGlobal()
    {
        // Una funcion declara 'x' local; la global tambien tiene 'x'.
        // El uso dentro de la funcion debe resolver al simbolo local
        // (no debe reportar nada como no declarado).
        const string src = """
            programa demo;
            vars
                x: entero;

            nula f () {
                vars
                    x: flotante;
                x = 3.14;
            };

            inicio {
                x = 1;
                f();
            } fin
            """;
        var r = Run(src);
        Assert.True(r.Success, FormatErrors(r));
        Assert.Equal(SemanticType.Entero,   r.Semantic!.GlobalTable.Lookup("x")!.Type);
        Assert.Equal(SemanticType.Flotante, r.Semantic.Directory.Lookup("f")!.LocalTable.Lookup("x")!.Type);
    }

    [Fact]
    public void FuncionUsaGlobalSinError()
    {
        const string src = """
            programa demo;
            vars
                contador: entero;

            nula incrementar () {
                contador = contador + 1;
            };

            inicio {
                contador = 0;
                incrementar();
            } fin
            """;
        var r = Run(src);
        Assert.True(r.Success, FormatErrors(r));
    }

    // -------------------------------------------------------------------------
    //  Cubo expuesto desde el analizador (sanity)
    // -------------------------------------------------------------------------

    [Fact]
    public void AnalizadorExponeCubo()
    {
        var r = Run("programa demo; inicio { escribe(\"x\"); } fin");
        Assert.NotNull(r.Semantic);
        Assert.NotNull(r.Semantic!.Cube);
        Assert.Equal(SemanticType.Entero,
            r.Semantic.Cube.Resolve(SemanticType.Entero, SemanticOp.Plus, SemanticType.Entero));
    }

    private static string FormatErrors(CompileResult r)
    {
        var lex = string.Join("\n  ", r.LexErrors);
        var par = string.Join("\n  ", r.ParseErrors);
        var sem = string.Join("\n  ", r.SemanticErrors);
        return $"LEX:\n  {lex}\nPARSE:\n  {par}\nSEM:\n  {sem}";
    }
}
