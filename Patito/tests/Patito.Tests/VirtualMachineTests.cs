// =============================================================================
//  VirtualMachineTests.cs - Test Cases de la Maquina Virtual Patito.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Estos tests compilan un programa Patito y verifican el output de la VM.
//  Cada test cubre un aspecto distinto del motor de ejecucion:
//
//    TC-VM-01  escribe de constante entera
//    TC-VM-02  asignacion de variable y escribe
//    TC-VM-03  condicional si/sino
//    TC-VM-04  ciclo mientras/haz
//    TC-VM-05  llamada a funcion void con parametros
//    TC-VM-06  funcion con "retorno" via variable global
//    TC-VM-07  expresiones aritmeticas mixtas (entero y flotante)
//
//  Como correr:
//      dotnet test --filter "FullyQualifiedName~VirtualMachineTests" -v normal
// =============================================================================

using System.Collections.Generic;
using System.IO;
using Patito.Compiler;
using Patito.Compiler.VM;
using Xunit;
using Xunit.Abstractions;

namespace Patito.Tests;

public class VirtualMachineTests
{
    private readonly ITestOutputHelper _out;

    public VirtualMachineTests(ITestOutputHelper output) => _out = output;

    // =========================================================================
    //  Helper central: compila + ejecuta, imprime diagnostico en caso de fallo
    // =========================================================================

    private VmResult Run(string source, string label = "test")
    {
        var cr = PatitoFrontEnd.Compile(source, label);

        if (!cr.Success)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"  [COMPILACION FALLIDA: {label}]");
            foreach (var e in cr.LexErrors)     sb.AppendLine($"    [LEX]   {e}");
            foreach (var e in cr.ParseErrors)   sb.AppendLine($"    [PARSE] {e}");
            foreach (var e in cr.SemanticErrors) sb.AppendLine($"    [SEM]   {e}");
            Assert.Fail(sb.ToString());
        }

        var constValues = cr.ConstValues
            ?? new Dictionary<int, object>();

        var debugWriter = new StringWriter();
        var vm = new VirtualMachine(
            cr.Quads!,
            cr.AddressBook!,
            constValues,
            cr.FunctionDirectory!,
            debugWriter);

        var result = vm.Execute();

        _out.WriteLine($"  === {label} ===");
        _out.WriteLine($"  Output: {result.Output.TrimEnd()}");
        if (!result.Success)
            _out.WriteLine($"  Error: {result.Error?.Message}");

        return result;
    }

    // =========================================================================
    //  TC-VM-01: escribe de constante entera
    // =========================================================================
    [Fact]
    public void TC_VM_01_PrintIntConstant()
    {
        const string source = """
            programa tc01;
            inicio {
                escribe(42);
            } fin
            """;

        var result = Run(source, "TC-VM-01");

        Assert.True(result.Success, result.Error?.Message);
        Assert.Contains("42", result.Output);
    }

    // =========================================================================
    //  TC-VM-02: asignacion y escribe de variable
    // =========================================================================
    [Fact]
    public void TC_VM_02_AssignAndPrint()
    {
        const string source = """
            programa tc02;
            vars x: entero;
            inicio {
                x = 10;
                escribe(x);
            } fin
            """;

        var result = Run(source, "TC-VM-02");

        Assert.True(result.Success, result.Error?.Message);
        Assert.Contains("10", result.Output);
    }

    // =========================================================================
    //  TC-VM-03: condicional si/sino
    // =========================================================================
    [Fact]
    public void TC_VM_03_IfElse()
    {
        const string source = """
            programa tc03;
            vars x: entero;
            inicio {
                x = 7;
                si (x > 5) {
                    escribe("mayor");
                } sino {
                    escribe("menor o igual");
                };
                x = 3;
                si (x > 5) {
                    escribe("mayor");
                } sino {
                    escribe("menor o igual");
                };
            } fin
            """;

        var result = Run(source, "TC-VM-03");

        Assert.True(result.Success, result.Error?.Message);
        var lines = result.Output.Split('\n',
            System.StringSplitOptions.RemoveEmptyEntries |
            System.StringSplitOptions.TrimEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("mayor",          lines[0]);
        Assert.Equal("menor o igual",  lines[1]);
    }

    // =========================================================================
    //  TC-VM-04: ciclo mientras/haz
    // =========================================================================
    [Fact]
    public void TC_VM_04_WhileLoop()
    {
        const string source = """
            programa tc04;
            vars i: entero;
            inicio {
                i = 0;
                mientras (i < 4) haz {
                    escribe(i);
                    i = i + 1;
                };
            } fin
            """;

        var result = Run(source, "TC-VM-04");

        Assert.True(result.Success, result.Error?.Message);
        var lines = result.Output.Split('\n',
            System.StringSplitOptions.RemoveEmptyEntries |
            System.StringSplitOptions.TrimEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("0", lines[0]);
        Assert.Equal("1", lines[1]);
        Assert.Equal("2", lines[2]);
        Assert.Equal("3", lines[3]);
    }

    // =========================================================================
    //  TC-VM-05: llamada a funcion void con parametros
    // =========================================================================
    [Fact]
    public void TC_VM_05_FunctionCallWithParams()
    {
        const string source = """
            programa tc05;
            vars a: entero;

            nula imprimir (n: entero) {
                escribe("valor:");
                escribe(n);
            };

            inicio {
                a = 99;
                imprimir(a);
                imprimir(7);
            } fin
            """;

        var result = Run(source, "TC-VM-05");

        Assert.True(result.Success, result.Error?.Message);
        var lines = result.Output.Split('\n',
            System.StringSplitOptions.RemoveEmptyEntries |
            System.StringSplitOptions.TrimEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("valor:", lines[0]);
        Assert.Equal("99",     lines[1]);
        Assert.Equal("valor:", lines[2]);
        Assert.Equal("7",      lines[3]);
    }

    // =========================================================================
    //  TC-VM-06: funcion con "retorno" via variable global
    // =========================================================================
    [Fact]
    public void TC_VM_06_FunctionReturnViaGlobal()
    {
        const string source = """
            programa tc06;
            vars retval, x: entero;

            entero cuadrado (base: entero) {
                retval = base * base;
            };

            inicio {
                x = 6;
                cuadrado(x);
                escribe(retval);
                cuadrado(3);
                escribe(retval);
            } fin
            """;

        var result = Run(source, "TC-VM-06");

        Assert.True(result.Success, result.Error?.Message);
        var lines = result.Output.Split('\n',
            System.StringSplitOptions.RemoveEmptyEntries |
            System.StringSplitOptions.TrimEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("36", lines[0]);
        Assert.Equal("9",  lines[1]);
    }

    // =========================================================================
    //  TC-VM-07: expresiones aritmeticas mixtas (entero y flotante)
    // =========================================================================
    [Fact]
    public void TC_VM_07_MixedArithmetic()
    {
        const string source = """
            programa tc07;
            vars
                a, b: entero;
                f: flotante;
            inicio {
                a = 10;
                b = 3;
                f = 2.5;
                escribe(a + b);
                escribe(a * b);
                escribe(f + 1.5);
            } fin
            """;

        var result = Run(source, "TC-VM-07");

        Assert.True(result.Success, result.Error?.Message);
        var lines = result.Output.Split('\n',
            System.StringSplitOptions.RemoveEmptyEntries |
            System.StringSplitOptions.TrimEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("13",  lines[0]);
        Assert.Equal("30",  lines[1]);
        Assert.Equal("4",   lines[2]);
    }
}
