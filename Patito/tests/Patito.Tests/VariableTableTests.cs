// =============================================================================
//  VariableTableTests.cs - Pruebas unitarias de la tabla de variables.
// =============================================================================
using Patito.Compiler.Semantic;
using Xunit;

namespace Patito.Tests;

public class VariableTableTests
{
    private static Symbol MakeSym(string name, SemanticType t = SemanticType.Entero) =>
        new(name, t, SymbolKind.Variable, 1, 1);

    [Fact]
    public void TryDeclare_PrimeraVez_RegresaTrue()
    {
        var t = new VariableTable("test");
        Assert.True(t.TryDeclare(MakeSym("x")));
        Assert.Equal(1, t.Count);
        Assert.True(t.Contains("x"));
    }

    [Fact]
    public void TryDeclare_NombreDuplicado_RegresaFalseYNoSobreescribe()
    {
        var t = new VariableTable("test");
        Assert.True(t.TryDeclare(MakeSym("x", SemanticType.Entero)));
        Assert.False(t.TryDeclare(MakeSym("x", SemanticType.Flotante)));

        // El simbolo conservado debe ser el original (entero), no el segundo.
        Assert.True(t.TryLookup("x", out var keptSym));
        Assert.Equal(SemanticType.Entero, keptSym.Type);
        Assert.Equal(1, t.Count);
    }

    [Fact]
    public void TryLookup_NoExistente_RegresaFalse()
    {
        var t = new VariableTable("test");
        Assert.False(t.TryLookup("noexiste", out _));
        Assert.Null(t.Lookup("noexiste"));
    }

    [Fact]
    public void Symbols_PreservaOrdenDeInsercion()
    {
        var t = new VariableTable("test");
        t.TryDeclare(MakeSym("a"));
        t.TryDeclare(MakeSym("b"));
        t.TryDeclare(MakeSym("c"));

        var names = new System.Collections.Generic.List<string>();
        foreach (var s in t.Symbols) names.Add(s.Name);
        Assert.Equal(new[] { "a", "b", "c" }, names);
    }
}
