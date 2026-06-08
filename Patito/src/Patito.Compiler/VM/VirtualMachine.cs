// =============================================================================
//  VirtualMachine.cs - Maquina Virtual del compilador Patito.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  La VM recibe la lista de cuadruplos generada por el compilador y los
//  ejecuta secuencialmente, modificando el contador de programa (pc) cuando
//  encuentra saltos.
//
//  Modelo de memoria:
//  ┌─────────────────────────────────────────────────────────────────────┐
//  │ Segmento            │ Rango          │ Almacen               │      │
//  ├─────────────────────┼────────────────┼───────────────────────┤      │
//  │ GlobalInt/Float     │ 18000-19999    │ _globalMemory         │ R/W  │
//  │ LocalInt/Float      │ 20000-21999    │ _activeLocal (frame)  │ R/W  │
//  │ TempInt/Float/Bool  │ 22000-24999    │ _activeLocal (frame)  │ R/W  │
//  │ ConstInt/Float/Str  │ 25000-27999    │ _globalMemory         │ R    │
//  └─────────────────────┴────────────────┴───────────────────────┴──────┘
//
//  Al iniciar:
//    * Las constantes (25000-27999) se cargan en _globalMemory desde
//      el diccionario constValues  (address -> valor real).
//    * _activeLocal apunta al frame del programa principal (un
//      ExecutionMemory vacio que acumula los temporales del inicio{}).
//
//  Al llamar a una funcion (Gosub):
//    * Se empuja (returnPc, currentActiveLocal) al call stack.
//    * _activeLocal se reemplaza por LocalMemory del nuevo frame.
//    * Los argumentos (pre-cargados via Param) se copian al nuevo frame.
//    * pc salta al StartQuad de la funcion.
//
//  Al regresar de una funcion (EndFunc):
//    * Se restaura (returnPc, savedActiveLocal) desde el call stack.
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Patito.Compiler.CodeGen;
using Patito.Compiler.Semantic;

namespace Patito.Compiler.VM;

/// <summary>
/// Interprete de cuadruplos Patito. Recibe los artefactos del compilador
/// y los ejecuta en memoria virtual.
/// </summary>
public sealed class VirtualMachine
{
    private readonly IReadOnlyList<Quadruple> _quads;
    private readonly IReadOnlyDictionary<string, int> _addressBook;
    private readonly IReadOnlyDictionary<int, object> _constValues;
    private readonly FunctionDirectory _funcDir;
    private readonly TextWriter? _output;

    // ── Memoria ───────────────────────────────────────────────────────────
    // Global: variables globales (18000-19999) y constantes (25000-27999).
    private readonly ExecutionMemory _globalMemory = new();

    // Activo: el frame del programa principal o del tope de la call stack.
    private ExecutionMemory _activeLocal = new();

    // Pila de llamadas: (indice de retorno, memoria local guardada).
    private readonly Stack<(int returnPc, ExecutionMemory savedLocal)> _callStack = new();

    // ── Estado de llamada pendiente (ERA -> Param* -> Gosub) ──────────────
    private ActivationRecord? _pendingRecord;
    private int _argCounter;

    /// <summary>
    /// Crea una nueva maquina virtual lista para ejecutar.
    /// </summary>
    /// <param name="quads">Lista de cuadruplos generada por el compilador.</param>
    /// <param name="addressBook">Mapa nombre -> direccion virtual.</param>
    /// <param name="constValues">Mapa direccion -> valor real de cada constante.</param>
    /// <param name="funcDir">Directorio de funciones (para obtener dir. de parametros).</param>
    /// <param name="output">
    /// TextWriter al que se escriben los <c>escribe</c> en tiempo real.
    /// Puede ser <c>null</c> (solo se captura en <see cref="VmResult.Output"/>).
    /// </param>
    public VirtualMachine(
        IReadOnlyList<Quadruple> quads,
        IReadOnlyDictionary<string, int> addressBook,
        IReadOnlyDictionary<int, object> constValues,
        FunctionDirectory funcDir,
        TextWriter? output = null)
    {
        _quads       = quads;
        _addressBook = addressBook;
        _constValues = constValues;
        _funcDir     = funcDir;
        _output      = output;
    }

    // =========================================================================
    //  Ejecucion principal
    // =========================================================================

    /// <summary>
    /// Ejecuta el programa completo desde el cuadruplo 0.
    /// Devuelve un <see cref="VmResult"/> con todo lo impreso y el estado final.
    /// </summary>
    public VmResult Execute()
    {
        var capture = new StringBuilder();
        try
        {
            // Cargar constantes en memoria global
            foreach (var kvp in _constValues)
                _globalMemory.Set(kvp.Key, kvp.Value);

            int pc = 0;
            while (pc < _quads.Count)
            {
                var q = _quads[pc];
                pc = Step(q, pc, capture);
            }
            return new VmResult(capture.ToString(), null, true);
        }
        catch (Exception ex)
        {
            return new VmResult(capture.ToString(), ex, false);
        }
    }

    /// <summary>
    /// Expone el valor almacenado en la direccion virtual <paramref name="addr"/>.
    /// Busca primero en la memoria local activa y luego en la global.
    /// Util para tests de caja blanca.
    /// </summary>
    public object? GetMemory(int addr)
    {
        if (_activeLocal.TryGet(addr, out var lval)) return lval;
        if (_globalMemory.TryGet(addr, out var gval)) return gval;
        return null;
    }

    // =========================================================================
    //  Dispatch por operacion
    // =========================================================================

    private int Step(Quadruple q, int pc, StringBuilder capture)
    {
        switch (q.Op)
        {
            // ── Aritmeticos ──────────────────────────────────────────────────
            case QuadOp.Plus:
                SetValue(q.Result, ArithAdd(GetValue(q.Left!), GetValue(q.Right!)));
                return pc + 1;

            case QuadOp.Minus:
                SetValue(q.Result, ArithSub(GetValue(q.Left!), GetValue(q.Right!)));
                return pc + 1;

            case QuadOp.Times:
                SetValue(q.Result, ArithMul(GetValue(q.Left!), GetValue(q.Right!)));
                return pc + 1;

            case QuadOp.Divide:
                SetValue(q.Result, ArithDiv(GetValue(q.Left!), GetValue(q.Right!)));
                return pc + 1;

            // ── Relacionales ─────────────────────────────────────────────────
            case QuadOp.Lt:
                SetValue(q.Result, RelCompare(GetValue(q.Left!), GetValue(q.Right!)) < 0);
                return pc + 1;

            case QuadOp.Gt:
                SetValue(q.Result, RelCompare(GetValue(q.Left!), GetValue(q.Right!)) > 0);
                return pc + 1;

            case QuadOp.Eq:
                SetValue(q.Result, RelCompare(GetValue(q.Left!), GetValue(q.Right!)) == 0);
                return pc + 1;

            case QuadOp.Neq:
                SetValue(q.Result, RelCompare(GetValue(q.Left!), GetValue(q.Right!)) != 0);
                return pc + 1;

            // ── Asignacion y negacion ─────────────────────────────────────────
            case QuadOp.Assign:
                SetValue(q.Result, GetValue(q.Left!));
                return pc + 1;

            case QuadOp.Neg:
                // (Neg, null, operando, resultado)
                SetValue(q.Result, Negate(GetValue(q.Right!)));
                return pc + 1;

            // ── Saltos ───────────────────────────────────────────────────────
            case QuadOp.GotoF:
            {
                var cond = GetValue(q.Left!);
                bool isFalse = cond is false || (cond is int ci && ci == 0);
                return isFalse ? int.Parse(q.Result) : pc + 1;
            }

            case QuadOp.Goto:
                return int.Parse(q.Result);

            // ── Salida ───────────────────────────────────────────────────────
            case QuadOp.Print:
            {
                string text = FormatForPrint(q.Result);
                capture.AppendLine(text);
                _output?.WriteLine(text);
                return pc + 1;
            }

            // ── Funciones ────────────────────────────────────────────────────
            case QuadOp.Era:
                _pendingRecord = new ActivationRecord(q.Result);
                _argCounter    = 0;
                return pc + 1;

            case QuadOp.Param:
                ProcessParam(q.Result);
                return pc + 1;

            case QuadOp.Gosub:
                return ExecuteGosub(q, pc);

            case QuadOp.EndFunc:
                return ExecuteEndFunc();

            // ── Retorno de funciones ─────────────────────────────────────────
            case QuadOp.Return:
                // (Return, exprName, null, "{func}_ret"): copia el valor de
                // 'exprName' (en el frame activo de la funcion) a la direccion
                // global reservada para el retorno; el llamador la copiara a
                // su vez a un temporal inmediatamente despues del Gosub.
                SetValue(q.Result, GetValue(q.Left!));
                return pc + 1;

            default:
                throw new InvalidOperationException($"Operacion de cuadruplo no soportada: {q.Op}");
        }
    }

    // =========================================================================
    //  Implementacion de llamadas a funcion
    // =========================================================================

    private void ProcessParam(string argName)
    {
        if (_pendingRecord is null)
            throw new InvalidOperationException(
                $"Cuadruplo Param sin ERA previo (arg '{argName}').");

        var funcInfo = _funcDir.Lookup(_pendingRecord.FunctionName)
            ?? throw new InvalidOperationException(
                $"Funcion '{_pendingRecord.FunctionName}' no encontrada en el directorio.");

        // Los parametros estan al principio de LocalTable (orden de insercion),
        // y ParameterTypes.Count indica cuantos hay.
        var paramSymbols = funcInfo.LocalTable.Symbols
            .Where(s => s.Kind == SymbolKind.Parameter)
            .ToList();

        if (_argCounter >= paramSymbols.Count)
            throw new InvalidOperationException(
                $"La funcion '{_pendingRecord.FunctionName}' espera {paramSymbols.Count} " +
                $"argumento(s), pero se paso el argumento #{_argCounter + 1}.");

        int paramAddr = paramSymbols[_argCounter].Address;
        object argVal = GetValue(argName);
        _pendingRecord.PushArg(paramAddr, argVal);
        _argCounter++;
    }

    private int ExecuteGosub(Quadruple q, int pc)
    {
        if (_pendingRecord is null)
            throw new InvalidOperationException("Gosub encontrado sin ERA previo.");

        // Copiar argumentos al LocalMemory del frame pendiente
        foreach (var (addr, val) in _pendingRecord.PopArgs())
            _pendingRecord.LocalMemory.Set(addr, val);

        _pendingRecord.ReturnQuad = pc + 1;

        // Guardar contexto actual en la pila de llamadas
        _callStack.Push((pc + 1, _activeLocal));

        // Activar el nuevo frame
        _activeLocal   = _pendingRecord.LocalMemory;
        _pendingRecord = null;

        // Saltar al primer cuadruplo de la funcion
        if (!int.TryParse(q.Result, out int startQuad))
            throw new InvalidOperationException(
                $"Gosub con startQuad invalido: '{q.Result}'.");
        return startQuad;
    }

    private int ExecuteEndFunc()
    {
        if (_callStack.Count == 0)
            throw new InvalidOperationException(
                "EndFunc encontrado pero la call stack esta vacia.");

        var (returnPc, savedLocal) = _callStack.Pop();
        _activeLocal = savedLocal;
        return returnPc;
    }

    // =========================================================================
    //  Acceso a memoria por nombre
    // =========================================================================

    /// <summary>
    /// Resuelve el nombre de un operando a su valor actual en memoria.
    /// Ruta el acceso segun el segmento de la direccion virtual.
    /// </summary>
    private object GetValue(string name)
    {
        if (!_addressBook.TryGetValue(name, out int addr))
            throw new InvalidOperationException(
                $"El nombre '{name}' no tiene direccion virtual asignada.");

        if (IsLocalOrTemp(addr))
            return _activeLocal.Get(addr);
        return _globalMemory.Get(addr);
    }

    /// <summary>
    /// Escribe <paramref name="value"/> en la direccion virtual del nombre dado.
    /// </summary>
    private void SetValue(string name, object value)
    {
        if (!_addressBook.TryGetValue(name, out int addr))
            throw new InvalidOperationException(
                $"El nombre '{name}' no tiene direccion virtual asignada.");

        if (IsLocalOrTemp(addr))
            _activeLocal.Set(addr, value);
        else
            _globalMemory.Set(addr, value);
    }

    // =========================================================================
    //  Formato de impresion
    // =========================================================================

    private string FormatForPrint(string operandName)
    {
        if (!_addressBook.TryGetValue(operandName, out int addr))
            return operandName; // no deberia ocurrir con un programa bien compilado

        object val;
        if (IsLocalOrTemp(addr))
        {
            if (!_activeLocal.TryGet(addr, out val))
                return "<sin-inicializar>";
        }
        else
        {
            if (!_globalMemory.TryGet(addr, out val))
                return "<sin-inicializar>";
        }
        return FormatValue(val);
    }

    private static string FormatValue(object val) => val switch
    {
        double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
        int    i => i.ToString(),
        bool   b => b ? "true" : "false",
        string s => s,
        _        => val.ToString() ?? string.Empty,
    };

    // =========================================================================
    //  Helpers de segmento
    // =========================================================================

    // Las direcciones [LocalIntBase, ConstIntBase) pertenecen al frame activo.
    private static bool IsLocalOrTemp(int addr)
        => addr >= VirtualMemoryMap.LocalIntBase &&
           addr <  VirtualMemoryMap.ConstIntBase;

    // =========================================================================
    //  Operaciones aritmeticas y relacionales
    // =========================================================================

    private static object ArithAdd(object l, object r)
    {
        if (l is double || r is double) return ToDouble(l) + ToDouble(r);
        return (int)l + (int)r;
    }

    private static object ArithSub(object l, object r)
    {
        if (l is double || r is double) return ToDouble(l) - ToDouble(r);
        return (int)l - (int)r;
    }

    private static object ArithMul(object l, object r)
    {
        if (l is double || r is double) return ToDouble(l) * ToDouble(r);
        return (int)l * (int)r;
    }

    private static object ArithDiv(object l, object r)
    {
        if (l is double || r is double) return ToDouble(l) / ToDouble(r);
        int ri = (int)r;
        if (ri == 0) throw new DivideByZeroException("Division por cero en tiempo de ejecucion.");
        return (int)l / ri;
    }

    private static int RelCompare(object l, object r)
    {
        if (l is double || r is double) return ToDouble(l).CompareTo(ToDouble(r));
        return ((int)l).CompareTo((int)r);
    }

    private static object Negate(object val) => val switch
    {
        double d => (object)(-d),
        int    i => (object)(-i),
        _        => throw new InvalidOperationException(
                        $"No se puede negar un valor de tipo {val.GetType().Name}."),
    };

    private static double ToDouble(object val) => val switch
    {
        double d => d,
        int    i => (double)i,
        _        => throw new InvalidOperationException(
                        $"No se puede convertir {val.GetType().Name} a flotante."),
    };
}
