using System.IO;
using Patito.Compiler;
using Xunit;

namespace Patito.Tests;

/// <summary>
/// Pruebas a nivel de PARSER: verifican que las construcciones de la BNF de
/// Patito se acepten o rechacen correctamente. Para los casos validos solo
/// importa que no se generen errores; los inputs son intencionalmente
/// minimalistas para aislar la regla bajo prueba.
/// </summary>
public class ParserTests
{
    private static CompileResult Run(string source) =>
        PatitoFrontEnd.Compile(source, "<test>");

    // ---- 1. Programa minimo ----------------------------------------------------

    [Fact]
    public void Programa_Minimo_SinVarsSinFuncs_Pasa()
    {
        const string src = """
            programa hola;
            inicio {
                escribe("hola");
            } fin
            """;
        var r = Run(src);
        Assert.True(r.Success, FormatErrors(r));
    }

    // ---- 2. Vars ---------------------------------------------------------------

    [Fact]
    public void Vars_VariosTiposYIds_Pasa()
    {
        const string src = """
            programa demo;
            vars
                x, y, z: entero;
                pi: flotante;
            inicio {
                x = 1;
            } fin
            """;
        var r = Run(src);
        Assert.True(r.Success, FormatErrors(r));
    }

    // ---- 3. Asignacion y expresion con precedencia -----------------------------

    [Fact]
    public void Expresion_RespectaPrecedenciaDeOperadores()
    {
        // No probamos el AST aqui (eso es para semantica), pero confirmamos
        // que la expresion se acepta sin errores.
        const string src = """
            programa demo;
            vars
                x: entero;
            inicio {
                x = 2 + 3 * 4 - (1 + 1);
            } fin
            """;
        var r = Run(src);
        Assert.True(r.Success, FormatErrors(r));
    }

    [Fact]
    public void Expresion_OperadorRelacional_Pasa()
    {
        const string src = """
            programa demo;
            vars
                a: entero;
            inicio {
                si (a < 10) {
                    escribe("pequeno");
                };
            } fin
            """;
        var r = Run(src);
        Assert.True(r.Success, FormatErrors(r));
    }

    // ---- 4. Condicion / ciclo / imprime ----------------------------------------

    [Fact]
    public void Condicion_ConSino_Pasa()
    {
        const string src = """
            programa demo;
            vars
                x: entero;
            inicio {
                si (x > 0) {
                    escribe("pos");
                } sino {
                    escribe("neg");
                };
            } fin
            """;
        Assert.True(Run(src).Success);
    }

    [Fact]
    public void Ciclo_Mientras_Pasa()
    {
        const string src = """
            programa demo;
            vars
                i: entero;
            inicio {
                i = 0;
                mientras (i < 5) haz {
                    i = i + 1;
                };
            } fin
            """;
        Assert.True(Run(src).Success);
    }

    [Fact]
    public void Escribe_AceptaExpresionesYLetreros()
    {
        const string src = """
            programa demo;
            vars
                x: entero;
            inicio {
                x = 42;
                escribe("la respuesta es", x, "fin", x + 1);
            } fin
            """;
        Assert.True(Run(src).Success);
    }

    // ---- 5. Funciones y llamadas -----------------------------------------------

    [Fact]
    public void Funcion_Nula_ConParametros_Pasa()
    {
        const string src = """
            programa demo;
            vars
                a: entero;

            nula sumarHasta (n: entero) {
                vars
                    i: entero;
                i = 0;
                mientras (i < n) haz {
                    i = i + 1;
                };
            };

            inicio {
                a = 5;
                sumarHasta(a);
            } fin
            """;
        var r = Run(src);
        Assert.True(r.Success, FormatErrors(r));
    }

    [Fact]
    public void Llamada_PuedeUsarseComoFactor()
    {
        const string src = """
            programa demo;
            vars
                x: entero;

            entero dame () {
                x = 1;
            };

            inicio {
                x = dame() + 1;
            } fin
            """;
        var r = Run(src);
        Assert.True(r.Success, FormatErrors(r));
    }

    // ---- 6. Casos invalidos: deben fallar --------------------------------------

    [Fact]
    public void FaltaPuntoYComa_TrasIdDelPrograma_Falla()
    {
        const string src = """
            programa malo
            inicio { escribe("x"); } fin
            """;
        var r = Run(src);
        Assert.False(r.Success);
        Assert.NotEmpty(r.ParseErrors);
    }

    [Fact]
    public void ParentesisSinCerrar_Falla()
    {
        const string src = """
            programa malo;
            vars x: entero;
            inicio {
                si (x > 0 {
                    escribe("oops");
                };
            } fin
            """;
        var r = Run(src);
        Assert.False(r.Success);
    }

    [Fact]
    public void TipoNoSoportado_Falla()
    {
        const string src = """
            programa malo;
            vars
                nombre: cadena;
            inicio {
                nombre = "ana";
            } fin
            """;
        var r = Run(src);
        Assert.False(r.Success);
    }

    [Fact]
    public void EscribeSinArgumentos_Falla()
    {
        const string src = """
            programa malo;
            inicio {
                escribe();
            } fin
            """;
        var r = Run(src);
        Assert.False(r.Success);
    }

    // ---- 7. Smoke tests: corre la suite de archivos en disco --------------------

    [Theory]
    [InlineData("01_minimo.patito")]
    [InlineData("02_vars_y_asigna.patito")]
    [InlineData("03_condicion.patito")]
    [InlineData("04_ciclo.patito")]
    [InlineData("05_funcion.patito")]
    [InlineData("06_expresiones.patito")]
    [InlineData("07_comentarios.patito")]
    public void Ejemplos_Validos_Pasan(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "examples", fileName);
        Assert.True(File.Exists(path), $"No se encontro {path}");
        var src = File.ReadAllText(path);
        var r = Run(src);
        Assert.True(r.Success, $"{fileName} fallo:\n{FormatErrors(r)}");
    }

    [Theory]
    [InlineData("invalido_01_falta_punto_coma.patito")]
    [InlineData("invalido_02_parentesis.patito")]
    [InlineData("invalido_03_letrero_multilinea.patito")]
    [InlineData("invalido_04_caracter_invalido.patito")]
    [InlineData("invalido_05_tipo_invalido.patito")]
    public void Ejemplos_Invalidos_Fallan(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "examples", fileName);
        Assert.True(File.Exists(path), $"No se encontro {path}");
        var src = File.ReadAllText(path);
        var r = Run(src);
        Assert.False(r.Success, $"Se esperaba error en {fileName} pero el parser/lexer lo aceptaron.");
    }

    // ---------------------------------------------------------------------------

    private static string FormatErrors(CompileResult r)
    {
        var errs = string.Join("\n  ", r.LexErrors) + "\n  " + string.Join("\n  ", r.ParseErrors);
        return $"Errores:\n  {errs}";
    }
}
