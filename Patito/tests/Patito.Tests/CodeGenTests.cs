// =============================================================================
//  CodeGenTests.cs - Pruebas de generacion de codigo intermedio (cuadruplos).
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Estas pruebas verifican los cuadruplos emitidos por los puntos neuralgicos
//  PN-8 a PN-18 del SemanticAnalyzer.  El formato de cada cuadruplo es:
//
//      (Index, Op, Left?, Right?, Result)
//
//  Convencion de temporales: "t0", "t1", "t2", ... en orden de emision.
//  Los indices de cuadruplos son base 0.
//
//  Puntos neuralgicos cubiertos:
//      [PN-8]  ExitFactorSimple   -> push operando/tipo; Neg para unario
//      [PN-9]  ExitTermino        -> Times / Divide
//      [PN-10] ExitExp            -> Plus / Minus
//      [PN-11] ExitExpresion      -> Lt / Gt / Eq / Neq + GotoF condicional
//      [PN-12] ExitAsigna         -> Assign con validacion de cubo
//      [PN-13] ExitImp            -> Print
//      [PN-14] EnterCiclo         -> guarda inicio del while
//      [PN-15] ExitCuerpo         -> Goto + Backfill(GotoF) en si-sino
//      [PN-16] ExitCondicion      -> Backfill final del si o si-sino
//      [PN-17] ExitCiclo          -> Goto al inicio + Backfill(GotoF) del while
//      [PN-18] ExitCall_stmt      -> Param + Gosub
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using Patito.Compiler;
using Patito.Compiler.CodeGen;
using Xunit;

namespace Patito.Tests;

public class CodeGenTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static IReadOnlyList<Quadruple> Quads(string src)
    {
        var r = PatitoFrontEnd.Compile(src, "<codegen-test>");
        Assert.True(r.Success,
            $"La compilacion fallo antes de generar cuadruplos:\n" +
            $"  LEX:   {string.Join(", ", r.LexErrors)}\n" +
            $"  PARSE: {string.Join(", ", r.ParseErrors)}\n" +
            $"  SEM:   {string.Join(", ", r.SemanticErrors)}");
        Assert.NotNull(r.Quads);
        return r.Quads!;
    }

    private static void AssertQuad(
        IReadOnlyList<Quadruple> qs, int idx,
        QuadOp op, string? left, string? right, string result)
    {
        Assert.True(idx < qs.Count,
            $"Se esperaba cuadruplo [{idx}] pero solo hay {qs.Count} cuadruplos.");
        var q = qs[idx];
        Assert.Equal(op,     q.Op);
        Assert.Equal(left,   q.Left);
        Assert.Equal(right,  q.Right);
        Assert.Equal(result, q.Result);
    }

    // -------------------------------------------------------------------------
    //  [PN-12] Asignacion simple
    // -------------------------------------------------------------------------

    [Fact]
    public void AsignaConstanteEntera_EmiteUnSoloAssign()
    {
        // x = 42;  ->  (Assign, "42", null, "x")
        const string src = """
            programa t;
            vars
                x: entero;
            inicio {
                x = 42;
            } fin
            """;
        var qs = Quads(src);
        Assert.Single(qs);
        AssertQuad(qs, 0, QuadOp.Assign, "42", null, "x");
    }

    [Fact]
    public void AsignaConstanteFlotante_EmiteAssign()
    {
        // pi = 3.14;  ->  (Assign, "3.14", null, "pi")
        const string src = """
            programa t;
            vars
                pi: flotante;
            inicio {
                pi = 3.14;
            } fin
            """;
        var qs = Quads(src);
        Assert.Single(qs);
        AssertQuad(qs, 0, QuadOp.Assign, "3.14", null, "pi");
    }

    [Fact]
    public void AsignaVariable_EmiteAssign()
    {
        // b = a;  ->  (Assign, "a", null, "b")
        const string src = """
            programa t;
            vars
                a, b: entero;
            inicio {
                a = 1;
                b = a;
            } fin
            """;
        var qs = Quads(src);
        Assert.Equal(2, qs.Count);
        AssertQuad(qs, 0, QuadOp.Assign, "1",  null, "a");
        AssertQuad(qs, 1, QuadOp.Assign, "a",  null, "b");
    }

    // -------------------------------------------------------------------------
    //  [PN-10] Suma y resta
    // -------------------------------------------------------------------------

    [Fact]
    public void Suma_DosVariables_EmitePlusYAssign()
    {
        // x = a + b;  ->  (Plus, "a", "b", "t0"), (Assign, "t0", null, "x")
        const string src = """
            programa t;
            vars
                x, a, b: entero;
            inicio {
                a = 2;
                b = 3;
                x = a + b;
            } fin
            """;
        var qs = Quads(src);
        Assert.Equal(4, qs.Count);
        AssertQuad(qs, 0, QuadOp.Assign, "2",  null, "a");
        AssertQuad(qs, 1, QuadOp.Assign, "3",  null, "b");
        AssertQuad(qs, 2, QuadOp.Plus,   "a",  "b",  "t0");
        AssertQuad(qs, 3, QuadOp.Assign, "t0", null, "x");
    }

    [Fact]
    public void Resta_DosVariables_EmiteMinusYAssign()
    {
        // x = a - b;  ->  (Minus, "a", "b", "t0"), (Assign, "t0", null, "x")
        const string src = """
            programa t;
            vars
                x, a, b: entero;
            inicio {
                x = a - b;
            } fin
            """;
        var qs = Quads(src);
        Assert.Equal(2, qs.Count);
        AssertQuad(qs, 0, QuadOp.Minus,  "a",  "b",  "t0");
        AssertQuad(qs, 1, QuadOp.Assign, "t0", null, "x");
    }

    // -------------------------------------------------------------------------
    //  [PN-9] Multiplicacion y division
    // -------------------------------------------------------------------------

    [Fact]
    public void Multiplicacion_EmiteTimesYAssign()
    {
        // x = a * b;
        const string src = """
            programa t;
            vars
                x, a, b: entero;
            inicio {
                x = a * b;
            } fin
            """;
        var qs = Quads(src);
        Assert.Equal(2, qs.Count);
        AssertQuad(qs, 0, QuadOp.Times,  "a",  "b",  "t0");
        AssertQuad(qs, 1, QuadOp.Assign, "t0", null, "x");
    }

    [Fact]
    public void Division_EmiteDivideYAssign()
    {
        // r = a / b;  -- division siempre produce flotante
        const string src = """
            programa t;
            vars
                a, b: entero;
                r: flotante;
            inicio {
                r = a / b;
            } fin
            """;
        var qs = Quads(src);
        Assert.Equal(2, qs.Count);
        AssertQuad(qs, 0, QuadOp.Divide, "a",  "b",  "t0");
        AssertQuad(qs, 1, QuadOp.Assign, "t0", null, "r");
    }

    // -------------------------------------------------------------------------
    //  Precedencia: * antes que +
    // -------------------------------------------------------------------------

    [Fact]
    public void Precedencia_MulAntesQueSuma_EmiteTimesLuegoPlusLuegoAssign()
    {
        // x = a + b * c;
        // Parse tree: exp( termino(a) + termino(b * c) )
        // ->  [0](Times,"b","c","t0")
        //     [1](Plus, "a","t0","t1")
        //     [2](Assign,"t1",null,"x")
        const string src = """
            programa t;
            vars
                x, a, b, c: entero;
            inicio {
                x = a + b * c;
            } fin
            """;
        var qs = Quads(src);
        Assert.Equal(3, qs.Count);
        AssertQuad(qs, 0, QuadOp.Times,  "b",  "c",  "t0");
        AssertQuad(qs, 1, QuadOp.Plus,   "a",  "t0", "t1");
        AssertQuad(qs, 2, QuadOp.Assign, "t1", null, "x");
    }

    [Fact]
    public void Precedencia_ParentesisAnulanMultiplicacion()
    {
        // x = (a + b) * c;
        // ->  [0](Plus,"a","b","t0")
        //     [1](Times,"t0","c","t1")
        //     [2](Assign,"t1",null,"x")
        const string src = """
            programa t;
            vars
                x, a, b, c: entero;
            inicio {
                x = (a + b) * c;
            } fin
            """;
        var qs = Quads(src);
        Assert.Equal(3, qs.Count);
        AssertQuad(qs, 0, QuadOp.Plus,   "a",  "b",  "t0");
        AssertQuad(qs, 1, QuadOp.Times,  "t0", "c",  "t1");
        AssertQuad(qs, 2, QuadOp.Assign, "t1", null, "x");
    }

    // -------------------------------------------------------------------------
    //  [PN-8] Negacion unaria
    // -------------------------------------------------------------------------

    [Fact]
    public void NegacionUnaria_Variable_EmiteNegYAssign()
    {
        // y = -x;
        // FactorSimple detecta OP_MENOS + ID -> emite (Neg, null, "x", "t0")
        const string src = """
            programa t;
            vars
                x, y: entero;
            inicio {
                x = 5;
                y = -x;
            } fin
            """;
        var qs = Quads(src);
        Assert.Equal(3, qs.Count);
        AssertQuad(qs, 0, QuadOp.Assign, "5",  null, "x");
        AssertQuad(qs, 1, QuadOp.Neg,    null, "x",  "t0");
        AssertQuad(qs, 2, QuadOp.Assign, "t0", null, "y");
    }

    // -------------------------------------------------------------------------
    //  [PN-11] Operadores relacionales
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("<",  QuadOp.Lt)]
    [InlineData(">",  QuadOp.Gt)]
    [InlineData("==", QuadOp.Eq)]
    [InlineData("!=", QuadOp.Neq)]
    public void OperadorRelacional_EmiteQuadCorrespondiente(string op, QuadOp expectedOp)
    {
        var src = $$"""
            programa t;
            vars
                a, b: entero;
            inicio {
                si (a {{op}} b) {
                    escribe("si");
                };
            } fin
            """;
        var qs = Quads(src);
        // [0] = quad relacional, [1] = GotoF, [2] = Print
        Assert.True(qs.Count >= 3, $"Se esperaban >= 3 cuadruplos, hay {qs.Count}");
        AssertQuad(qs, 0, expectedOp, "a", "b", "t0");
        Assert.Equal(QuadOp.GotoF, qs[1].Op);
    }

    // -------------------------------------------------------------------------
    //  [PN-13] Print (escribe)
    // -------------------------------------------------------------------------

    [Fact]
    public void Escribe_Letrero_EmitePrint()
    {
        const string src = """
            programa t;
            inicio {
                escribe("hola mundo");
            } fin
            """;
        var qs = Quads(src);
        Assert.Single(qs);
        AssertQuad(qs, 0, QuadOp.Print, null, null, "\"hola mundo\"");
    }

    [Fact]
    public void Escribe_Variable_EmitePrintConNombre()
    {
        const string src = """
            programa t;
            vars
                x: entero;
            inicio {
                x = 7;
                escribe(x);
            } fin
            """;
        var qs = Quads(src);
        Assert.Equal(2, qs.Count);
        AssertQuad(qs, 0, QuadOp.Assign, "7", null, "x");
        AssertQuad(qs, 1, QuadOp.Print,  null, null, "x");
    }

    [Fact]
    public void Escribe_MixtoLetreroyVariable_EmiteDosQuadsPrint()
    {
        const string src = """
            programa t;
            vars
                n: entero;
            inicio {
                n = 42;
                escribe("respuesta:", n);
            } fin
            """;
        var qs = Quads(src);
        Assert.Equal(3, qs.Count);
        AssertQuad(qs, 0, QuadOp.Assign, "42",         null, "n");
        AssertQuad(qs, 1, QuadOp.Print,  null,          null, "\"respuesta:\"");
        AssertQuad(qs, 2, QuadOp.Print,  null,          null, "n");
    }

    // -------------------------------------------------------------------------
    //  [PN-11 + PN-16] Condicional sin sino
    // -------------------------------------------------------------------------

    [Fact]
    public void CondicionSinSino_EmiteGotoFBackfilled()
    {
        // si (x < 5) { y = 1; };
        // ->  [0](Lt,  "x","5","t0")
        //     [1](GotoF,"t0",null,"3")   <- backfill en ExitCondicion: count=3
        //     [2](Assign,"1",null,"y")
        const string src = """
            programa t;
            vars
                x, y: entero;
            inicio {
                si (x < 5) {
                    y = 1;
                };
            } fin
            """;
        var qs = Quads(src);
        Assert.Equal(3, qs.Count);
        AssertQuad(qs, 0, QuadOp.Lt,    "x",  "5",  "t0");
        AssertQuad(qs, 1, QuadOp.GotoF, "t0", null, "3");
        AssertQuad(qs, 2, QuadOp.Assign,"1",  null, "y");
    }

    // -------------------------------------------------------------------------
    //  [PN-11 + PN-15 + PN-16] Condicional con sino
    // -------------------------------------------------------------------------

    [Fact]
    public void CondicionConSino_EmiteGotoFYGotoAmbosBackfilled()
    {
        // si (x < 5) { y = 1; } sino { y = 2; };
        // ->  [0](Lt,  "x","5","t0")
        //     [1](GotoF,"t0",null,"4")   <- PN-15 backfill con count=4 antes de sino
        //     [2](Assign,"1",null,"y")
        //     [3](Goto, null,null,"5")   <- PN-15 emite Goto; PN-16 backfill con count=5
        //     [4](Assign,"2",null,"y")
        const string src = """
            programa t;
            vars
                x, y: entero;
            inicio {
                si (x < 5) {
                    y = 1;
                } sino {
                    y = 2;
                };
            } fin
            """;
        var qs = Quads(src);
        Assert.Equal(5, qs.Count);
        AssertQuad(qs, 0, QuadOp.Lt,    "x",  "5",  "t0");
        AssertQuad(qs, 1, QuadOp.GotoF, "t0", null, "4");
        AssertQuad(qs, 2, QuadOp.Assign,"1",  null, "y");
        AssertQuad(qs, 3, QuadOp.Goto,  null, null, "5");
        AssertQuad(qs, 4, QuadOp.Assign,"2",  null, "y");
    }

    // -------------------------------------------------------------------------
    //  [PN-14 + PN-17] Ciclo mientras
    // -------------------------------------------------------------------------

    [Fact]
    public void CicloMientras_EmiteGotoFYGotoAlInicio()
    {
        // i = 0;
        // mientras (i < 5) haz { i = i + 1; };
        //
        // EnterCiclo guarda inicio = count = 1 (despues de [0] Assign)
        //
        // ->  [0](Assign,"0",null,"i")
        //     [1](Lt,   "i","5","t0")
        //     [2](GotoF,"t0",null,"6")   <- backfill en ExitCiclo: count=6
        //     [3](Plus, "i","1","t1")
        //     [4](Assign,"t1",null,"i")
        //     [5](Goto, null,null,"1")   <- ExitCiclo emite Goto al inicio=1
        const string src = """
            programa t;
            vars
                i: entero;
            inicio {
                i = 0;
                mientras (i < 5) haz {
                    i = i + 1;
                };
            } fin
            """;
        var qs = Quads(src);
        Assert.Equal(6, qs.Count);
        AssertQuad(qs, 0, QuadOp.Assign, "0",  null, "i");
        AssertQuad(qs, 1, QuadOp.Lt,     "i",  "5",  "t0");
        AssertQuad(qs, 2, QuadOp.GotoF,  "t0", null, "6");
        AssertQuad(qs, 3, QuadOp.Plus,   "i",  "1",  "t1");
        AssertQuad(qs, 4, QuadOp.Assign, "t1", null, "i");
        AssertQuad(qs, 5, QuadOp.Goto,   null, null, "1");
    }

    // -------------------------------------------------------------------------
    //  [PN-18] Llamadas a funciones
    // -------------------------------------------------------------------------

    [Fact]
    public void LlamadaSinArgs_EmiteEraYGosub()
    {
        // Entrega 4: la secuencia es ERA(f) + Gosub(f, startQ).
        // f tiene solo EndFunc, asi que su cuerpo es el cuadruplo 0.
        const string src = """
            programa t;
            nula f () { };
            inicio {
                f();
            } fin
            """;
        var qs = Quads(src);
        // f_body: EndFunc[0]
        // main:   ERA[1], Gosub[2]
        AssertQuad(qs, 1, QuadOp.Era,   null, null, "f");
        AssertQuad(qs, 2, QuadOp.Gosub, "f",  null, "0");
    }

    [Fact]
    public void LlamadaConUnArg_EmiteEraParamYGosub()
    {
        // Secuencia esperada: ERA(f) + Param(n) + Gosub(f, startQ).
        const string src = """
            programa t;
            vars
                n: entero;
            nula f (x: entero) { };
            inicio {
                n = 5;
                f(n);
            } fin
            """;
        var qs = Quads(src);
        // f_body: EndFunc[0]
        // main:   Assign[1], ERA[2], Param[3], Gosub[4]
        var era   = qs[^3];
        var param = qs[^2];
        var gosub = qs[^1];
        Assert.Equal(QuadOp.Era,   era.Op);
        Assert.Equal("f",          era.Result);
        Assert.Equal(QuadOp.Param, param.Op);
        Assert.Equal("n",          param.Result);
        Assert.Equal(QuadOp.Gosub, gosub.Op);
        Assert.Equal("f",          gosub.Left);
    }

    [Fact]
    public void LlamadaConDosArgs_EmiteEraParamsEnOrdenYGosub()
    {
        // f(a, 7) debe emitir ERA(f) + Param(a) + Param(7) + Gosub(f) en ese orden.
        const string src = """
            programa t;
            vars
                a: entero;
            nula f (x: entero, y: entero) { };
            inicio {
                a = 3;
                f(a, 7);
            } fin
            """;
        var qs = Quads(src);
        // Los cuatro ultimos cuadruplos del main son: ERA(f), Param(a), Param(7), Gosub(f)
        int n = qs.Count;
        Assert.True(n >= 4);
        AssertQuad(qs, n - 4, QuadOp.Era,   null, null, "f");
        AssertQuad(qs, n - 3, QuadOp.Param, null, null, "a");
        AssertQuad(qs, n - 2, QuadOp.Param, null, null, "7");
        AssertQuad(qs, n - 1, QuadOp.Gosub, "f",  null, "0");
    }

    // -------------------------------------------------------------------------
    //  Pruebas de integracion (multiples features combinadas)
    // -------------------------------------------------------------------------

    [Fact]
    public void Integracion_AsignacionMultipleYEscribe_OrdenCorrecto()
    {
        const string src = """
            programa t;
            vars
                a, b, c: entero;
            inicio {
                a = 1;
                b = 2;
                c = a + b;
                escribe("suma:", c);
            } fin
            """;
        var qs = Quads(src);
        Assert.Equal(6, qs.Count);
        AssertQuad(qs, 0, QuadOp.Assign, "1",     null, "a");
        AssertQuad(qs, 1, QuadOp.Assign, "2",     null, "b");
        AssertQuad(qs, 2, QuadOp.Plus,   "a",     "b",  "t0");
        AssertQuad(qs, 3, QuadOp.Assign, "t0",    null, "c");
        AssertQuad(qs, 4, QuadOp.Print,  null,    null, "\"suma:\"");
        AssertQuad(qs, 5, QuadOp.Print,  null,    null, "c");
    }

    [Fact]
    public void Integracion_CicloConPrint_GeneraQuadsCorrectos()
    {
        // Verifica que el ciclo genera exactamente los quads esperados
        // y que Print queda dentro del cuerpo (entre GotoF y Goto-al-inicio).
        const string src = """
            programa t;
            vars
                i: entero;
            inicio {
                i = 0;
                mientras (i < 3) haz {
                    escribe(i);
                    i = i + 1;
                };
            } fin
            """;
        var qs = Quads(src);
        // [0](Assign,"0",null,"i")
        // [1](Lt,"i","3","t0")
        // [2](GotoF,"t0",null,"7")
        // [3](Print,null,null,"i")
        // [4](Plus,"i","1","t1")
        // [5](Assign,"t1",null,"i")
        // [6](Goto,null,null,"1")
        Assert.Equal(7, qs.Count);
        AssertQuad(qs, 0, QuadOp.Assign, "0",  null, "i");
        AssertQuad(qs, 1, QuadOp.Lt,     "i",  "3",  "t0");
        AssertQuad(qs, 2, QuadOp.GotoF,  "t0", null, "7");
        AssertQuad(qs, 3, QuadOp.Print,  null, null, "i");
        AssertQuad(qs, 4, QuadOp.Plus,   "i",  "1",  "t1");
        AssertQuad(qs, 5, QuadOp.Assign, "t1", null, "i");
        AssertQuad(qs, 6, QuadOp.Goto,   null, null, "1");
    }

    [Fact]
    public void Integracion_CondicionAnidada_SinErrores()
    {
        // Verifica que si/sino anidados no corrompan las pilas de backfill.
        const string src = """
            programa t;
            vars
                x, y: entero;
            inicio {
                x = 10;
                y = 5;
                si (x > y) {
                    si (y > 0) {
                        escribe("ambos");
                    } sino {
                        escribe("solo x");
                    };
                } sino {
                    escribe("ninguno");
                };
            } fin
            """;
        var qs = Quads(src);
        // Solo verificamos que se generaron quads y que el programa compilo.
        Assert.True(qs.Count > 0);
        // Los GotoF deben apuntar a indices validos (no "?").
        foreach (var q in qs)
        {
            if (q.Op == QuadOp.GotoF || q.Op == QuadOp.Goto)
                Assert.NotEqual("?", q.Result);
        }
    }

    [Fact]
    public void Integracion_EjemploFuncion_GeneraCuadruplos()
    {
        // Smoke test sobre el ejemplo de funciones del disco.
        // Solo verifica que se generan cuadruplos y no hay "?" sin backfill.
        const string src = """
            programa smoke;
            vars
                a: entero;

            nula saludar (n: entero) {
                vars
                    i: entero;
                i = 0;
                mientras (i < n) haz {
                    escribe("hola");
                    i = i + 1;
                };
            };

            inicio {
                a = 3;
                saludar(a);
            } fin
            """;
        var qs = Quads(src);
        Assert.True(qs.Count > 0);
        foreach (var q in qs)
        {
            if (q.Op is QuadOp.GotoF or QuadOp.Goto)
                Assert.NotEqual("?", q.Result);
        }
    }

    // =========================================================================
    //  Entrega 4 — ERA, EndFunc, StartQuad
    // =========================================================================

    // -------------------------------------------------------------------------
    //  [PN-7b] StartQuad se registra al entrar al func_body
    // -------------------------------------------------------------------------

    [Fact]
    public void FuncionSinCuerpo_StartQuad_EsElPrimerCuadruplo()
    {
        // La funcion 'noop' no tiene estatutos; su cuerpo genera solo EndFunc.
        // StartQuad debe apuntar a ese cuadruplo.
        const string src = """
            programa t;

            nula noop () {
            };

            inicio {
            } fin
            """;
        var r = PatitoFrontEnd.Compile(src, "<test>");
        Assert.True(r.Success);
        var fi = r.FunctionDirectory!.Lookup("noop");
        Assert.NotNull(fi);
        // El primer cuadruplo del programa es el EndFunc de noop (indice 0).
        Assert.Equal(0, fi!.StartQuad);
    }

    [Fact]
    public void FuncionConCuerpo_StartQuad_EsAntesDePrimerEstatuto()
    {
        // La funcion 'duplicar' tiene un Assign. StartQuad debe valer 0.
        const string src = """
            programa t;
            vars
                g: entero;

            nula duplicar (x: entero) {
                vars
                    r: entero;
                r = x + x;
                escribe(r);
            };

            inicio {
                g = 3;
                duplicar(g);
            } fin
            """;
        var r = PatitoFrontEnd.Compile(src, "<test>");
        Assert.True(r.Success);
        var fi = r.FunctionDirectory!.Lookup("duplicar");
        Assert.NotNull(fi);
        // El cuerpo de duplicar es el primer codigo que se genera (indice 0).
        Assert.Equal(0, fi!.StartQuad);
    }

    // -------------------------------------------------------------------------
    //  [PN-7c] EndFunc se emite al salir del func_body
    // -------------------------------------------------------------------------

    [Fact]
    public void FuncionNula_UltimoQuadDeFunc_EsEndFunc()
    {
        const string src = """
            programa t;

            nula saluda () {
                escribe("hola");
            };

            inicio {
            } fin
            """;
        var qs = Quads(src);
        // Los cuadruplos del cuerpo de 'saluda' terminan con EndFunc.
        // El EndFunc es el segundo cuadruplo (0=Print, 1=EndFunc).
        AssertQuad(qs, 1, QuadOp.EndFunc, null, null, "saluda");
    }

    [Fact]
    public void DosFunciones_CadaUnaTerminaConSuEndFunc()
    {
        const string src = """
            programa t;

            nula f1 () {
                escribe("f1");
            };

            nula f2 () {
                escribe("f2");
            };

            inicio {
            } fin
            """;
        var qs = Quads(src);
        // f1: Print[0], EndFunc[1]
        // f2: Print[2], EndFunc[3]
        AssertQuad(qs, 1, QuadOp.EndFunc, null, null, "f1");
        AssertQuad(qs, 3, QuadOp.EndFunc, null, null, "f2");
    }

    // -------------------------------------------------------------------------
    //  [PN-18] ERA se emite ANTES de los Param en cada llamada a funcion
    // -------------------------------------------------------------------------

    [Fact]
    public void LlamadaSinArgs_EmiteEra_LuegoGosub()
    {
        // Para f(): ERA(f) + Gosub(f, startQ) — sin Param
        const string src = """
            programa t;

            nula f () {
                escribe("ok");
            };

            inicio {
                f();
            } fin
            """;
        var qs = Quads(src);
        // Cuadruplos del cuerpo principal:
        //   f_body: Print[0], EndFunc[1]
        //   main:   ERA[2], Gosub[3]
        AssertQuad(qs, 2, QuadOp.Era,   null, null, "f");
        AssertQuad(qs, 3, QuadOp.Gosub, "f",  null, "0");  // startQuad=0
    }

    [Fact]
    public void LlamadaConUnArg_EmiteEra_Param_Gosub()
    {
        const string src = """
            programa t;
            vars
                x: entero;

            nula doble (n: entero) {
                vars
                    r: entero;
                r = n + n;
                escribe(r);
            };

            inicio {
                x = 5;
                doble(x);
            } fin
            """;
        var qs = Quads(src);
        // Cuerpo de doble: Plus[0], Assign[1], Print[2], EndFunc[3]
        // Cuerpo principal: Assign[4], ERA[5], Param[6], Gosub[7]
        AssertQuad(qs, 5, QuadOp.Era,   null, null, "doble");
        AssertQuad(qs, 6, QuadOp.Param, null, null, "x");
        AssertQuad(qs, 7, QuadOp.Gosub, "doble", null, "0");
    }

    [Fact]
    public void LlamadaConDosArgs_EmiteEra_DosParam_Gosub()
    {
        const string src = """
            programa t;
            vars
                a, b: entero;

            nula suma (x: entero, y: entero) {
                vars
                    r: entero;
                r = x + y;
                escribe(r);
            };

            inicio {
                a = 3;
                b = 4;
                suma(a, b);
            } fin
            """;
        var qs = Quads(src);
        // Cuerpo de suma: Plus[0], Assign[1], Print[2], EndFunc[3]
        // Cuerpo principal: Assign[4](a=3), Assign[5](b=4)
        //   ERA[6], Param[7](a), Param[8](b), Gosub[9]
        AssertQuad(qs, 6, QuadOp.Era,   null, null, "suma");
        AssertQuad(qs, 7, QuadOp.Param, null, null, "a");
        AssertQuad(qs, 8, QuadOp.Param, null, null, "b");
        AssertQuad(qs, 9, QuadOp.Gosub, "suma", null, "0");
    }

    [Fact]
    public void DobleInvocacion_CadaLlamadaTieneEraYGosub()
    {
        const string src = """
            programa t;
            vars
                v: entero;

            nula ping () {
                escribe("ping");
            };

            inicio {
                v = 1;
                ping();
                ping();
            } fin
            """;
        var qs = Quads(src);
        // ping body: Print[0], EndFunc[1]
        // main:      Assign[2], ERA[3], Gosub[4], ERA[5], Gosub[6]
        AssertQuad(qs, 3, QuadOp.Era,   null, null, "ping");
        AssertQuad(qs, 4, QuadOp.Gosub, "ping", null, "0");
        AssertQuad(qs, 5, QuadOp.Era,   null, null, "ping");
        AssertQuad(qs, 6, QuadOp.Gosub, "ping", null, "0");
    }

    // -------------------------------------------------------------------------
    //  [PN-18] Gosub lleva el startQuad correcto en Result
    // -------------------------------------------------------------------------

    [Fact]
    public void Gosub_Result_EsStartQuadDeLaFuncion()
    {
        // El StartQuad de 'calcula' debe ser el indice del primer cuadruplo
        // de su cuerpo, y ese mismo numero debe aparecer en el Gosub.Result.
        const string src = """
            programa t;
            vars
                x: entero;

            nula calcula (n: entero) {
                vars
                    tmp: entero;
                tmp = n + 1;
                escribe(tmp);
            };

            inicio {
                x = 7;
                calcula(x);
            } fin
            """;
        var r = PatitoFrontEnd.Compile(src, "<test>");
        Assert.True(r.Success);
        var fi = r.FunctionDirectory!.Lookup("calcula");
        Assert.NotNull(fi);

        var qs = r.Quads!;
        var gosub = qs.FirstOrDefault(q => q.Op == QuadOp.Gosub);
        Assert.NotNull(gosub);
        Assert.Equal(fi!.StartQuad.ToString(), gosub!.Result);
    }

    // -------------------------------------------------------------------------
    //  Condicional con GotoF + Backfill — verification end-to-end
    // -------------------------------------------------------------------------

    [Fact]
    public void Condicional_SiSino_GotoFYGotoBackfillCorrectos()
    {
        const string src = """
            programa t;
            vars
                x: entero;
            inicio {
                x = 10;
                si (x > 5) {
                    escribe("mayor");
                } sino {
                    escribe("menor o igual");
                };
            } fin
            """;
        var qs = Quads(src);
        // Quad 0: Assign x=10
        // Quad 1: Gt x 5 t0
        // Quad 2: GotoF t0 _ <dest>
        // Quad 3: Print "mayor"
        // Quad 4: Goto _ _ <dest>
        // Quad 5: Print "menor o igual"
        AssertQuad(qs, 2, QuadOp.GotoF, "t0", null, "5");
        AssertQuad(qs, 4, QuadOp.Goto,  null, null, "6");
    }

    [Fact]
    public void Condicional_SiSinSino_GotoFApuntaAlFinal()
    {
        const string src = """
            programa t;
            vars
                n: entero;
            inicio {
                n = 0;
                si (n == 0) {
                    escribe("cero");
                };
            } fin
            """;
        var qs = Quads(src);
        // 0: Assign n=0
        // 1: Eq n 0 t0
        // 2: GotoF t0 _ 4  (despues del si)
        // 3: Print "cero"
        AssertQuad(qs, 2, QuadOp.GotoF, "t0", null, "4");
    }

    // -------------------------------------------------------------------------
    //  Ciclo mientras — GotoF + Goto + Backfill
    // -------------------------------------------------------------------------

    [Fact]
    public void Ciclo_GotoApuntaAlInicio_GotoFApuntaAlFinal()
    {
        const string src = """
            programa t;
            vars
                i: entero;
            inicio {
                i = 0;
                mientras (i < 3) haz {
                    i = i + 1;
                };
            } fin
            """;
        var qs = Quads(src);
        // 0: Assign i=0
        // 1: Lt i 3 t0       <- inicio ciclo (index 1)
        // 2: GotoF t0 _ 6    <- sale del ciclo
        // 3: Plus i 1 t1
        // 4: Assign t1 _ i
        // 5: Goto _ _ 1      <- regresa al inicio
        AssertQuad(qs, 1, QuadOp.Lt,    "i",  "3",  "t0");
        AssertQuad(qs, 2, QuadOp.GotoF, "t0", null, "6");
        AssertQuad(qs, 5, QuadOp.Goto,  null, null, "1");
    }

    // -------------------------------------------------------------------------
    //  VirtualMemoryMap — rangos y asignacion basica
    // -------------------------------------------------------------------------

    [Fact]
    public void VirtualMemoryMap_BasesDentroDeRango()
    {
        Assert.Equal(18_000, Patito.Compiler.CodeGen.VirtualMemoryMap.GlobalIntBase);
        Assert.Equal(19_000, Patito.Compiler.CodeGen.VirtualMemoryMap.GlobalFloatBase);
        Assert.Equal(20_000, Patito.Compiler.CodeGen.VirtualMemoryMap.LocalIntBase);
        Assert.Equal(21_000, Patito.Compiler.CodeGen.VirtualMemoryMap.LocalFloatBase);
        Assert.Equal(22_000, Patito.Compiler.CodeGen.VirtualMemoryMap.TempIntBase);
        Assert.Equal(23_000, Patito.Compiler.CodeGen.VirtualMemoryMap.TempFloatBase);
        Assert.Equal(24_000, Patito.Compiler.CodeGen.VirtualMemoryMap.TempBoolBase);
        Assert.Equal(25_000, Patito.Compiler.CodeGen.VirtualMemoryMap.ConstIntBase);
        Assert.Equal(26_000, Patito.Compiler.CodeGen.VirtualMemoryMap.ConstFloatBase);
        Assert.Equal(27_000, Patito.Compiler.CodeGen.VirtualMemoryMap.ConstStringBase);
    }

    [Fact]
    public void VirtualMemoryMap_AllocatePrimeros_DanBaseDelSegmento()
    {
        var map = new Patito.Compiler.CodeGen.VirtualMemoryMap();
        Assert.Equal(18_000, map.Allocate(Patito.Compiler.CodeGen.MemorySegment.GlobalInt));
        Assert.Equal(18_001, map.Allocate(Patito.Compiler.CodeGen.MemorySegment.GlobalInt));
        Assert.Equal(19_000, map.Allocate(Patito.Compiler.CodeGen.MemorySegment.GlobalFloat));
    }

    [Fact]
    public void VirtualMemoryMap_SegmentOf_IdentificaSegmentoCorrecto()
    {
        Assert.Equal(Patito.Compiler.CodeGen.MemorySegment.GlobalInt,
            Patito.Compiler.CodeGen.VirtualMemoryMap.SegmentOf(18_500));
        Assert.Equal(Patito.Compiler.CodeGen.MemorySegment.TempBool,
            Patito.Compiler.CodeGen.VirtualMemoryMap.SegmentOf(24_000));
        Assert.Null(Patito.Compiler.CodeGen.VirtualMemoryMap.SegmentOf(1_000));
    }
}
