// =============================================================================
//  SemanticAnalyzer.cs - Listener con los "puntos neuralgicos" semanticos.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Que es un "punto neuralgico"?
//
//      Un punto neuralgico es un instante especifico durante el recorrido del
//      arbol de derivacion en el que el compilador realiza una accion
//      semantica (registrar un simbolo, validar tipos, emitir un cuadruplo,
//      etc.). En la implementacion via ANTLR4, cada metodo Enter/Exit de un
//      Listener es candidato a contener un punto neuralgico.
//
//  Puntos neuralgicos implementados en esta Entrega (declaraciones):
//
//      [PN-1]  EnterPrograma:
//                * registra el nombre del programa en el directorio
//                * llama a ProcessVars para los globales y a ProcessFuncs
//                  para todas las funciones.
//
//      [PN-2]  ProcessVars (invocado para 'vars' global y para 'vars' local
//              dentro de func_body):
//                * recorre cada grupo 'ids : tipo ;' y agrega cada ID al
//                  VariableTable correspondiente.
//                * VALIDACION: si el ID ya existe en la tabla -> error
//                  VariableRedeclared.
//
//      [PN-3]  ProcessFuncs (recorre 'funcs'):
//                * por cada funcion, construye FunctionInfo (nombre, tipo de
//                  retorno) y registra en el directorio.
//                * VALIDACION 1: si el nombre coincide con el del programa
//                  -> error NameClashesWithProgram.
//                * VALIDACION 2: si la funcion ya existe -> error
//                  FunctionRedeclared.
//                * Procesa parametros (los agrega a LocalTable con
//                  SymbolKind.Parameter); VALIDACION: parametro duplicado
//                  -> error ParameterRedeclared.
//                * Procesa vars locales (los agrega a LocalTable); VALIDACION:
//                  un local que choca con un parametro o con otro local
//                  -> error VariableRedeclared.
//
//  Puntos neuralgicos secundarios (usos):
//
//      [PN-4]  EnterAsigna:
//                * VALIDACION: la variable destino debe existir en el alcance
//                  visible (local primero, luego global). Si no existe
//                  -> error UndeclaredVariable.
//
//      [PN-5]  EnterFactorSimple:
//                * VALIDACION: cada ID usado en una expresion debe existir
//                  en el alcance visible -> error UndeclaredVariable.
//
//      [PN-6]  EnterLlamada:
//                * VALIDACION: la funcion invocada debe estar registrada
//                  en el directorio -> error UndeclaredFunction.
//
//  El soporte completo del cubo semantico (verificar tipos en cada operacion
//  y en cada asignacion) requiere apilar tipos durante el recorrido, lo cual
//  pertenece a la Entrega 3. Por ahora el cubo esta implementado y
//  testeado, listo para conectarse.
//
//  ESTRATEGIA DE ALCANCE:
//
//    * Cuando ProcessFuncs procesa una funcion, registra sus simbolos en
//      FunctionInfo.LocalTable inmediatamente, de modo que cuando el walker
//      visita los estatutos del cuerpo de la funcion, basta con saber "en
//      que funcion estoy" para resolver una variable.
//    * Para saber "en que funcion estoy" durante el walker, mantenemos el
//      mapa _funcByBody (Func_bodyContext -> FunctionInfo). En EnterFunc_body
//      hacemos push de la funcion al stack; en ExitFunc_body, pop. Asi los
//      lookups en EnterAsigna / EnterFactorSimple / EnterLlamada consultan
//      la funcion activa.
// =============================================================================

using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using Patito.Compiler.Generated;

namespace Patito.Compiler.Semantic;

public sealed class SemanticAnalyzer : PatitoBaseListener
{
    private readonly FunctionDirectory _directory = new();
    private readonly List<SemanticError> _errors = new();

    // Mapa Func_bodyContext -> FunctionInfo: nos permite saber, al entrar
    // a un cuerpo de funcion, en que funcion estamos sin tener que recorrer
    // de nuevo la lista de funcs.
    private readonly Dictionary<PatitoParser.Func_bodyContext, FunctionInfo> _funcByBody = new();

    // Pila de funciones activas. Normalmente tiene 0 o 1 elemento (Patito no
    // admite funciones anidadas), pero usamos pila por robustez.
    private readonly Stack<FunctionInfo> _scopeStack = new();

    // ---- API publica ----------------------------------------------------------
    public FunctionDirectory Directory => _directory;
    public VariableTable GlobalTable => _directory.GlobalTable;
    public IReadOnlyList<SemanticError> Errors => _errors;
    public bool HasErrors => _errors.Count > 0;
    public SemanticCube Cube => SemanticCube.Default;
    public string? ProgramName => _directory.ProgramName;

    // ==========================================================================
    //  [PN-1] EnterPrograma: pasada de declaraciones (puntos PN-2 y PN-3).
    // ==========================================================================
    public override void EnterPrograma(PatitoParser.ProgramaContext ctx)
    {
        var idNode = ctx.ID();
        _directory.ProgramName = idNode?.GetText();

        // ----- Vars globales [PN-2] ----------------------------------------
        var varsCtx = ctx.vars();
        if (varsCtx is not null)
        {
            ProcessVars(varsCtx, _directory.GlobalTable);
        }

        // ----- Funciones [PN-3] --------------------------------------------
        var funcsCtx = ctx.funcs();
        if (funcsCtx is not null)
        {
            ProcessFuncs(funcsCtx);
        }
    }

    // ==========================================================================
    //  [PN-2] ProcessVars: registra cada ID en la tabla con validacion de doble
    //  declaracion.
    // ==========================================================================
    private void ProcessVars(PatitoParser.VarsContext varsCtx, VariableTable table)
    {
        var listado = varsCtx.listado_vars();
        if (listado is null) return;

        var grupos = listado.lista_ids();
        var tipos  = listado.tipo();
        if (grupos is null || tipos is null) return;

        for (int g = 0; g < grupos.Length; g++)
        {
            var tipo = ParseTipo(tipos[g]);
            foreach (var idNode in grupos[g].ID())
            {
                DeclareVariable(table, idNode, tipo, SymbolKind.Variable);
            }
        }
    }

    /// <summary>
    /// Punto neuralgico unitario: agregar un simbolo a la tabla con validacion
    /// de doble declaracion. Tambien lo usamos para parametros.
    /// </summary>
    private void DeclareVariable(VariableTable table, ITerminalNode idNode, SemanticType type, SymbolKind kind)
    {
        var name = idNode.GetText();
        int line = idNode.Symbol.Line;
        int col  = idNode.Symbol.Column + 1;
        var sym  = new Symbol(name, type, kind, line, col);

        if (!table.TryDeclare(sym))
        {
            // VALIDACION: variable doblemente declarada
            var prev = table.Lookup(name)!;
            var what = kind == SymbolKind.Parameter ? "Parametro" : "Variable";
            var code = kind == SymbolKind.Parameter
                ? SemanticErrorCode.ParameterRedeclared
                : SemanticErrorCode.VariableRedeclared;
            _errors.Add(new SemanticError(line, col, code, name,
                $"{what} '{name}' ya fue declarado en alcance '{table.ScopeName}' " +
                $"(declaracion previa en {prev.Line}:{prev.Column})."));
        }
    }

    // ==========================================================================
    //  [PN-3] ProcessFuncs: registra cada funcion en el directorio y llena su
    //  tabla local con parametros y locales (con todas las validaciones).
    // ==========================================================================
    private void ProcessFuncs(PatitoParser.FuncsContext funcsCtx)
    {
        // El numero de funciones se infiere por la cantidad de IDs en este nivel.
        // (typo_fun ID LPAREN params RPAREN func_body SEMICOLON)*
        var idArray = funcsCtx.ID();
        if (idArray is null) return;
        int n = idArray.Length;

        for (int i = 0; i < n; i++)
        {
            var idNode = funcsCtx.ID(i);
            var name   = idNode.GetText();
            int line   = idNode.Symbol.Line;
            int col    = idNode.Symbol.Column + 1;

            var returnType = ParseTypoFun(funcsCtx.typo_fun(i));
            var info       = new FunctionInfo(name, returnType, line, col);

            // ----- VALIDACION 1: nombre choca con el del programa ----------
            if (_directory.ProgramName is not null &&
                string.Equals(name, _directory.ProgramName, System.StringComparison.Ordinal))
            {
                _errors.Add(new SemanticError(line, col,
                    SemanticErrorCode.NameClashesWithProgram, name,
                    $"La funcion '{name}' usa el mismo identificador que el programa."));
                // Aun asi seguimos para validar el cuerpo (mejor diagnostico).
            }

            // ----- VALIDACION 2: funcion ya declarada ----------------------
            if (!_directory.TryDeclare(info))
            {
                var prev = _directory.Lookup(name)!;
                _errors.Add(new SemanticError(line, col,
                    SemanticErrorCode.FunctionRedeclared, name,
                    $"Funcion '{name}' ya fue declarada en {prev.Line}:{prev.Column}."));
                // Saltamos a la siguiente funcion: no tiene sentido llenar
                // una tabla local que no podremos referenciar.
                continue;
            }

            // ----- Parametros ----------------------------------------------
            var paramsCtx = funcsCtx.@params(i);
            if (paramsCtx is not null)
            {
                var ids = paramsCtx.ID();
                var ts  = paramsCtx.tipo();
                if (ids is not null && ts is not null)
                {
                    for (int p = 0; p < ids.Length; p++)
                    {
                        var pType = ParseTipo(ts[p]);
                        // Antes de declarar contamos el tipo en la firma; aun
                        // si el nombre se duplica, el aridad y tipos deben
                        // verse igual desde la perspectiva de la llamada.
                        info.ParameterTypes.Add(pType);
                        DeclareVariable(info.LocalTable, ids[p], pType, SymbolKind.Parameter);
                    }
                }
            }

            // ----- Variables locales (del func_body.vars) ------------------
            var bodyCtx = funcsCtx.func_body(i);
            _funcByBody[bodyCtx] = info; // <-- para PN-4..PN-6 saber el scope

            var localVars = bodyCtx.vars();
            if (localVars is not null)
            {
                ProcessVars(localVars, info.LocalTable);
            }
        }
    }

    // ==========================================================================
    //  Manejo del alcance activo (push/pop). Permite que EnterAsigna, etc.
    //  resuelvan variables locales antes que globales.
    // ==========================================================================
    public override void EnterFunc_body(PatitoParser.Func_bodyContext ctx)
    {
        if (_funcByBody.TryGetValue(ctx, out var info))
        {
            _scopeStack.Push(info);
        }
    }

    public override void ExitFunc_body(PatitoParser.Func_bodyContext ctx)
    {
        if (_scopeStack.Count > 0)
        {
            _scopeStack.Pop();
        }
    }

    /// <summary>Funcion actualmente activa en el recorrido, o null si estamos en el cuerpo principal.</summary>
    private FunctionInfo? CurrentFunction => _scopeStack.Count > 0 ? _scopeStack.Peek() : null;

    private Symbol? LookupVariable(string name)
    {
        var current = CurrentFunction;
        if (current is not null && current.LocalTable.TryLookup(name, out var local)) return local;
        return _directory.GlobalTable.Lookup(name);
    }

    // ==========================================================================
    //  [PN-4] EnterAsigna : valida que el ID destino exista.
    // ==========================================================================
    public override void EnterAsigna(PatitoParser.AsignaContext ctx)
    {
        var idNode = ctx.ID();
        if (idNode is null) return;
        var name = idNode.GetText();
        if (LookupVariable(name) is null)
        {
            _errors.Add(new SemanticError(
                idNode.Symbol.Line, idNode.Symbol.Column + 1,
                SemanticErrorCode.UndeclaredVariable, name,
                $"Variable '{name}' usada sin declaracion previa."));
        }
    }

    // ==========================================================================
    //  [PN-5] EnterFactorSimple : valida que un ID en una expresion exista.
    // ==========================================================================
    public override void EnterFactorSimple(PatitoParser.FactorSimpleContext ctx)
    {
        var atom = ctx.simple_atom();
        if (atom is null) return;
        var id = atom.ID();
        if (id is null) return; // es una constante, no un ID

        var name = id.GetText();
        if (LookupVariable(name) is null)
        {
            _errors.Add(new SemanticError(
                id.Symbol.Line, id.Symbol.Column + 1,
                SemanticErrorCode.UndeclaredVariable, name,
                $"Variable '{name}' usada sin declaracion previa."));
        }
    }

    // ==========================================================================
    //  [PN-6] EnterLlamada : valida que la funcion invocada exista.
    // ==========================================================================
    public override void EnterLlamada(PatitoParser.LlamadaContext ctx)
    {
        var id = ctx.ID();
        if (id is null) return;
        var name = id.GetText();
        if (!_directory.Contains(name))
        {
            _errors.Add(new SemanticError(
                id.Symbol.Line, id.Symbol.Column + 1,
                SemanticErrorCode.UndeclaredFunction, name,
                $"Funcion '{name}' invocada sin declaracion previa."));
        }
    }

    // ==========================================================================
    //  Helpers para traducir contextos de tipo en SemanticType.
    // ==========================================================================
    private static SemanticType ParseTipo(PatitoParser.TipoContext ctx)
    {
        if (ctx is null) return SemanticType.Error;
        if (ctx.KW_ENTERO()   is not null) return SemanticType.Entero;
        if (ctx.KW_FLOTANTE() is not null) return SemanticType.Flotante;
        return SemanticType.Error;
    }

    private static SemanticType ParseTypoFun(PatitoParser.Typo_funContext ctx)
    {
        if (ctx is null) return SemanticType.Error;
        if (ctx.KW_NULA() is not null) return SemanticType.Nula;
        return ParseTipo(ctx.tipo());
    }
}
