using System.Linq;
using Antlr4.Runtime;
using Patito.Compiler;
using Patito.Compiler.Generated;
using Xunit;

namespace Patito.Tests;

/// <summary>
/// Pruebas a nivel de SCANNER: verifican que la lista de tokens emitida coincide
/// con lo esperado segun las expresiones regulares de Patito.
/// </summary>
public class ScannerTests
{
    private static int[] TokenTypes(string source)
    {
        var result = PatitoFrontEnd.Compile(source, "<test>");
        // Ignoramos el EOF para que las aserciones lean mas naturales.
        return result.Tokens
            .Where(t => t.Type != TokenConstants.EOF)
            .Select(t => t.Type)
            .ToArray();
    }

    // ---- 1. Palabras reservadas y keyword vs identificador ---------------------

    [Theory]
    [InlineData("programa", PatitoLexer.KW_PROGRAMA)]
    [InlineData("inicio",   PatitoLexer.KW_INICIO)]
    [InlineData("fin",      PatitoLexer.KW_FIN)]
    [InlineData("vars",     PatitoLexer.KW_VARS)]
    [InlineData("entero",   PatitoLexer.KW_ENTERO)]
    [InlineData("flotante", PatitoLexer.KW_FLOTANTE)]
    [InlineData("nula",     PatitoLexer.KW_NULA)]
    [InlineData("si",       PatitoLexer.KW_SI)]
    [InlineData("sino",     PatitoLexer.KW_SINO)]
    [InlineData("mientras", PatitoLexer.KW_MIENTRAS)]
    [InlineData("haz",      PatitoLexer.KW_HAZ)]
    [InlineData("escribe",  PatitoLexer.KW_ESCRIBE)]
    public void PalabraReservada_SeReconoceComoKeyword(string text, int expected)
    {
        Assert.Equal(new[] { expected }, TokenTypes(text));
    }

    [Theory]
    [InlineData("contador")]      // identificador comun
    [InlineData("x1")]             // mezcla letra/digito
    [InlineData("siempre")]        // empieza con 'si' pero NO debe ser KW_SI (max munch)
    [InlineData("inicios")]        // similar a 'inicio' pero con caracter extra
    public void Identificador_NoColisionaConKeywords(string text)
    {
        Assert.Equal(new[] { PatitoLexer.ID }, TokenTypes(text));
    }

    [Theory]
    [InlineData("Hola")]           // empieza con mayuscula
    [InlineData("sumarHasta")]     // camelCase
    [InlineData("MiVariable1")]    // mayusculas + digito
    public void Identificador_AceptaMayusculasYMinusculas(string text)
    {
        // [a-zA-Z] permite identificadores con mayusculas (camelCase / PascalCase).
        Assert.Equal(new[] { PatitoLexer.ID }, TokenTypes(text));
    }

    // ---- 2. Constantes ---------------------------------------------------------

    [Theory]
    [InlineData("0",        PatitoLexer.CTE_ENT)]
    [InlineData("123",      PatitoLexer.CTE_ENT)]
    [InlineData("1039203",  PatitoLexer.CTE_ENT)]
    public void ConstanteEntera_SeReconoce(string text, int expected)
    {
        Assert.Equal(new[] { expected }, TokenTypes(text));
    }

    [Theory]
    [InlineData("3.14")]
    [InlineData("0.0")]
    [InlineData("100.0")]
    [InlineData("3.14159265")]
    public void ConstanteFlotante_PrefiereCteFlotSobreEntero(string text)
    {
        // Verifica que '3.14' es UN solo token CTE_FLOT, no CTE_ENT '.' CTE_ENT.
        Assert.Equal(new[] { PatitoLexer.CTE_FLOT }, TokenTypes(text));
    }

    // ---- 3. Letrero ------------------------------------------------------------

    [Fact]
    public void Letrero_AcceptaTextoSimple()
    {
        Assert.Equal(new[] { PatitoLexer.LETRERO }, TokenTypes("\"Me llamo Misael\""));
    }

    [Fact]
    public void Letrero_NoAceptaSaltoDeLinea()
    {
        var result = PatitoFrontEnd.Compile("\"linea1\nlinea2\"", "<test>");
        Assert.NotEmpty(result.LexErrors);
    }

    // ---- 4. Operadores: prioridad de longest-match -----------------------------

    [Theory]
    [InlineData("==",  PatitoLexer.OP_EQ)]
    [InlineData("!=",  PatitoLexer.OP_NEQ)]
    [InlineData("=",   PatitoLexer.OP_ASIGNA)]
    [InlineData("<",   PatitoLexer.OP_LT)]
    [InlineData(">",   PatitoLexer.OP_GT)]
    [InlineData("+",   PatitoLexer.OP_MAS)]
    [InlineData("-",   PatitoLexer.OP_MENOS)]
    [InlineData("*",   PatitoLexer.OP_POR)]
    [InlineData("/",   PatitoLexer.OP_DIV)]
    public void Operadores_TokenIndividual(string text, int expected)
    {
        Assert.Equal(new[] { expected }, TokenTypes(text));
    }

    [Fact]
    public void OpEq_TienePrioridadSobreOpAsigna()
    {
        // '==' debe producir UN token, no dos OP_ASIGNA seguidos.
        Assert.Equal(new[] { PatitoLexer.OP_EQ }, TokenTypes("=="));
    }

    // ---- 5. Comentarios y whitespace -> skip -----------------------------------

    [Fact]
    public void ComentarioDeLinea_SeIgnora()
    {
        // El comentario no debe producir tokens; solo el ID 'x'.
        Assert.Equal(new[] { PatitoLexer.ID }, TokenTypes("// hola\nx"));
    }

    [Fact]
    public void ComentarioDeBloque_SeIgnora()
    {
        Assert.Equal(new[] { PatitoLexer.ID }, TokenTypes("/* descripcion */ y"));
    }

    [Fact]
    public void Whitespace_NoGeneraTokens()
    {
        Assert.Equal(
            new[] { PatitoLexer.ID, PatitoLexer.OP_MAS, PatitoLexer.ID },
            TokenTypes("  a   +\t b  "));
    }

    // ---- 6. Caracter invalido --------------------------------------------------

    [Fact]
    public void CaracterInvalido_GeneraErrorLexico()
    {
        var result = PatitoFrontEnd.Compile("a @ b", "<test>");
        Assert.NotEmpty(result.LexErrors);
    }
}
