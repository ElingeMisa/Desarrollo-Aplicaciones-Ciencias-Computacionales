// =============================================================================
//  SemanticCubeTests.cs - Verifica todas las celdas del cubo semantico.
// =============================================================================
using Patito.Compiler.Semantic;
using Xunit;

namespace Patito.Tests;

/// <summary>
/// El cubo semantico es la fuente de verdad de la compatibilidad de tipos.
/// Estas pruebas afirman cada celda relevante y -mas importante- que las
/// combinaciones invalidas devuelven SemanticType.Error.
/// </summary>
public class SemanticCubeTests
{
    private static readonly SemanticCube Cube = SemanticCube.Default;

    // ------ Aritmeticos +, -, * --------------------------------------------
    [Theory]
    [InlineData(SemanticType.Entero,   SemanticType.Entero,   SemanticType.Entero)]
    [InlineData(SemanticType.Entero,   SemanticType.Flotante, SemanticType.Flotante)]
    [InlineData(SemanticType.Flotante, SemanticType.Entero,   SemanticType.Flotante)]
    [InlineData(SemanticType.Flotante, SemanticType.Flotante, SemanticType.Flotante)]
    public void Suma_PropagaAFlotanteSiAlgunoEsFlotante(SemanticType l, SemanticType r, SemanticType expected)
    {
        Assert.Equal(expected, Cube.Resolve(l, SemanticOp.Plus,  r));
        Assert.Equal(expected, Cube.Resolve(l, SemanticOp.Minus, r));
        Assert.Equal(expected, Cube.Resolve(l, SemanticOp.Times, r));
    }

    // ------ Division: siempre flotante --------------------------------------
    [Theory]
    [InlineData(SemanticType.Entero,   SemanticType.Entero)]
    [InlineData(SemanticType.Entero,   SemanticType.Flotante)]
    [InlineData(SemanticType.Flotante, SemanticType.Entero)]
    [InlineData(SemanticType.Flotante, SemanticType.Flotante)]
    public void Division_SiempreFlotante(SemanticType l, SemanticType r)
    {
        Assert.Equal(SemanticType.Flotante, Cube.Resolve(l, SemanticOp.Divide, r));
    }

    // ------ Relacionales ----------------------------------------------------
    [Theory]
    [InlineData(SemanticOp.Lt)]
    [InlineData(SemanticOp.Gt)]
    [InlineData(SemanticOp.Eq)]
    [InlineData(SemanticOp.Neq)]
    public void Relacionales_DevuelvenBoolEntreNumericos(SemanticOp op)
    {
        foreach (var l in new[] { SemanticType.Entero, SemanticType.Flotante })
            foreach (var r in new[] { SemanticType.Entero, SemanticType.Flotante })
                Assert.Equal(SemanticType.Bool, Cube.Resolve(l, op, r));
    }

    // ------ Asignacion ------------------------------------------------------
    [Fact]
    public void Asignacion_EnteroAEntero_Ok()
    {
        Assert.Equal(SemanticType.Entero, Cube.Resolve(SemanticType.Entero, SemanticOp.Assign, SemanticType.Entero));
    }

    [Fact]
    public void Asignacion_FlotanteAEntero_PermiteWidening()
    {
        // flotante <- entero es OK (promocion implicita).
        Assert.Equal(SemanticType.Flotante, Cube.Resolve(SemanticType.Flotante, SemanticOp.Assign, SemanticType.Entero));
    }

    [Fact]
    public void Asignacion_FlotanteAFlotante_Ok()
    {
        Assert.Equal(SemanticType.Flotante, Cube.Resolve(SemanticType.Flotante, SemanticOp.Assign, SemanticType.Flotante));
    }

    [Fact]
    public void Asignacion_EnteroAFlotante_EsError()
    {
        // entero <- flotante NO esta permitido (perdida de precision).
        Assert.Equal(SemanticType.Error, Cube.Resolve(SemanticType.Entero, SemanticOp.Assign, SemanticType.Flotante));
    }

    // ------ Combinaciones invalidas ----------------------------------------
    [Fact]
    public void Operacion_ConBool_EsError()
    {
        // Patito no tiene operadores logicos en esta version, asi que Bool no debe
        // poder combinarse con nada (excepto como resultado de un relacional).
        Assert.Equal(SemanticType.Error, Cube.Resolve(SemanticType.Bool, SemanticOp.Plus, SemanticType.Entero));
        Assert.Equal(SemanticType.Error, Cube.Resolve(SemanticType.Entero, SemanticOp.Plus, SemanticType.Bool));
        Assert.Equal(SemanticType.Error, Cube.Resolve(SemanticType.Bool, SemanticOp.Lt, SemanticType.Bool));
    }

    [Fact]
    public void IsCompatible_ReflectaResolve()
    {
        Assert.True (Cube.IsCompatible(SemanticType.Entero, SemanticOp.Plus, SemanticType.Entero));
        Assert.False(Cube.IsCompatible(SemanticType.Entero, SemanticOp.Assign, SemanticType.Flotante));
    }
}
