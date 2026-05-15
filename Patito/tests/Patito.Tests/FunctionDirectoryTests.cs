// =============================================================================
//  FunctionDirectoryTests.cs - Pruebas unitarias del directorio de funciones.
// =============================================================================
using Patito.Compiler.Semantic;
using Xunit;

namespace Patito.Tests;

public class FunctionDirectoryTests
{
    private static FunctionInfo MakeFunc(string name, SemanticType ret = SemanticType.Nula) =>
        new(name, ret, 1, 1);

    [Fact]
    public void TryDeclare_PrimeraVez_RegresaTrue()
    {
        var d = new FunctionDirectory();
        Assert.True(d.TryDeclare(MakeFunc("foo")));
        Assert.Equal(1, d.Count);
        Assert.True(d.Contains("foo"));
    }

    [Fact]
    public void TryDeclare_NombreDuplicado_RegresaFalseYNoSobreescribe()
    {
        var d = new FunctionDirectory();
        Assert.True(d.TryDeclare(MakeFunc("foo", SemanticType.Entero)));
        Assert.False(d.TryDeclare(MakeFunc("foo", SemanticType.Flotante)));

        Assert.True(d.TryLookup("foo", out var kept));
        Assert.Equal(SemanticType.Entero, kept.ReturnType);
        Assert.Equal(1, d.Count);
    }

    [Fact]
    public void FunctionInfo_TablaLocal_ArrancaVacia()
    {
        var f = MakeFunc("bar");
        Assert.Equal(0, f.LocalTable.Count);
        Assert.Empty(f.ParameterTypes);
    }

    [Fact]
    public void GlobalTable_EsCompartidaPorElPrograma()
    {
        var d = new FunctionDirectory();
        d.ProgramName = "demo";
        Assert.True(d.GlobalTable.TryDeclare(
            new Symbol("x", SemanticType.Entero, SymbolKind.Variable, 1, 1)));
        Assert.True(d.GlobalTable.Contains("x"));
    }
}
