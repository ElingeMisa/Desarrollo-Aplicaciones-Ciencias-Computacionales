// ===
//  SemanticAnalyzer.cs - Listener con los "puntos neuralgicos" semanticos.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// ===
//
//      Listener es candidato a contener un punto neuralgico.
//
//  Puntos neuralgicos implementados en Entrega 2 (declaraciones):
//
//      [PN-1]  EnterPrograma      : registra programa + globales + funciones.
//      [PN-2]  ProcessVars        : declara cada ID en su VariableTable.
//      [PN-3]  ProcessFuncs       : declara cada funcion con params y locales.
//      [PN-4]  EnterAsigna        : valida existencia del ID destino.
//      [PN-5]  EnterFactorSimple  : valida existencia del ID en expresion.
//      [PN-6]  EnterLlamada       : valida existencia de la funcion invocada.
//      [PN-7]  EnterFunc_body / ExitFunc_body : push/pop de alcance activo.
//
//  Puntos neuralgicos implementados en Entrega 3 (generacion de cuadruplos):
//
//      [PN-8]  ExitFactorSimple   : apila operando y tipo en las pilas.
//      [PN-9]  ExitTermino        : aplica * / en las pilas y emite cuadruplos.
//      [PN-10] ExitExp            : aplica + - en las pilas y emite cuadruplos.
//      [PN-11] ExitExpresion      : aplica operador relacional; emite GotoF si
//                                   la expresion es condicion de si/mientras.
//      [PN-12] ExitAsigna         : valida tipos con el cubo y emite Assign.
//      [PN-13] ExitImp            : emite Print para cada elemento de escribe().
//      [PN-14] EnterCiclo         : guarda el indice de inicio del ciclo.
//      [PN-15] ExitCuerpo         : al salir del cuerpo de un si con sino,
//                                   emite Goto y hace Backfill del GotoF.
//      [PN-16] ExitCondicion      : hace Backfill del Goto (con sino) o del
//                                   GotoF (sin sino).
//      [PN-17] ExitCiclo          : emite Goto al inicio y hace Backfill del
//                                   GotoF de la condicion del mientras.
//      [PN-18] ExitCall_stmt      : emite ERA + Param + Gosub para llamadas.
//
//  Puntos neuralgicos implementados en Entrega 4 (funciones completas):
//
//      [PN-7b] EnterFunc_body     : registra StartQuad (indice del primer
//                                   cuadruplo del cuerpo de la funcion).
//      [PN-7c] ExitFunc_body      : emite EndFunc al cerrar el cuerpo.
//      [PN-18] ExitCall_stmt      : actualizado para emitir ERA antes de Param,
//                                   y pasar startQuad como Result del Gosub.
//
//  Puntos neuralgicos implementados en Entrega 5 (direcciones virtuales):
//
//      [PN-2]  ProcessVars (ampliado) : asigna direccion virtual a cada
//                                       variable al declararla (Symbol.Address).
//      [PN-3]  ProcessFuncs (ampliado): asigna direcciones a params y locales
//                                       de funcion (segmento Local).
//      [PN-7b] EnterFunc_body (amp.) : reinicia contadores de temporales para
//                                       que cada funcion reutilice las mismas
//                                       direcciones del segmento Temp.
//      [PN-8]  ExitFactorSimple (amp.): registra la constante en el pool para
//                                       que obtenga (y deduplique) su direccion.
//      [PN-13] ExitImp (ampliado)    : registra literales de cadena (LETRERO)
//                                       en el segmento Const-Cadena.
// ===

using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using Patito.Compiler.CodeGen;
using Patito.Compiler.Generated;

namespace Patito.Compiler.Semantic;

public sealed class SemanticAnalyzer : PatitoBaseListener
{
    private readonly FunctionDirectory _directory = new();
    private readonly List<SemanticError> _errors = new();

    // Mapa Func_bodyContext -> FunctionInfo: nos permite saber en que funcion
    // estamos sin tener que recorrer de nuevo la lista de funcs.
    private readonly Dictionary<PatitoParser.Func_bodyContext, FunctionInfo> _funcByBody = new();

    // Pila de funciones activas (0 o 1 elemento en Patito, sin anidamiento).
    private readonly Stack<FunctionInfo> _scopeStack = new();

    // Indice del Goto inicial que salta sobre los cuerpos de funcion al inicio{}.
    private int _mainGotoIdx = -1;

    // Emitter que agrupa las tres pilas (Operadores, Operandos, Tipos) y la
    // fila de cuadruplos.
    private readonly QuadrupleEmitter _emitter = new();

    // Pilas de saltos pendientes de backfill para si/sino y mientras.
    private readonly Stack<int> _pendingGotoF = new(); // indices de GotoF a resolver
    private readonly Stack<int> _pendingGoto  = new(); // indices de Goto (entre si y sino)
    private readonly Stack<int> _cicloStart   = new(); // indice de inicio de cada mientras

    // - API publica -
    public FunctionDirectory Directory  => _directory;
    public VariableTable GlobalTable    => _directory.GlobalTable;
    public IReadOnlyList<SemanticError> Errors => _errors;
    public bool HasErrors               => _errors.Count > 0;
    public SemanticCube Cube            => SemanticCube.Default;
    public string? ProgramName          => _directory.ProgramName;
    public QuadrupleEmitter Emitter     => _emitter;
    public IReadOnlyList<Quadruple> Quads => _emitter.Fila.Quads;

    // 
    //  [PN-1] EnterPrograma: pasada de declaraciones (PN-2 y PN-3).
    // 
    public override void EnterPrograma(PatitoParser.ProgramaContext ctx)
    {
        var idNode = ctx.ID();
        _directory.ProgramName = idNode?.GetText();

        var varsCtx = ctx.vars();
        if (varsCtx is not null)
            ProcessVars(varsCtx, _directory.GlobalTable, isGlobal: true);

        var funcsCtx = ctx.funcs();
        if (funcsCtx is not null)
            ProcessFuncs(funcsCtx);

        // [PN-0] Si hay funciones, emitir Goto inicial que salta sobre sus
        //        cuerpos y aterriza en el inicio{}. Se hace backfill en
        //        EnterCuerpo cuando el padre es ProgramaContext.
        //        (Sin funciones el goto es innecesario pero inocuo.)
        _mainGotoIdx = _emitter.Fila.Emit(QuadOp.Goto, null, null, "?");
    }

    // 
    //  [PN-2] ProcessVars: registra cada ID en la tabla con validacion.
    //  [Entrega 5] Propaga isGlobal para que DeclareVariable asigne la
    //  direccion virtual correcta (segmento Global o Local segun el alcance).
    // 
    private void ProcessVars(PatitoParser.VarsContext varsCtx, VariableTable table, bool isGlobal)
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
                DeclareVariable(table, idNode, tipo, SymbolKind.Variable, isGlobal);
        }
    }

    // [Entrega 5] DeclareVariable asigna una direccion virtual al simbolo y la
    // registra en el AddressBook del emitter para poder mostrar DIR(NOMBRE).
    private void DeclareVariable(VariableTable table, ITerminalNode idNode,
                                  SemanticType type, SymbolKind kind, bool isGlobal)
    {
        var name = idNode.GetText();
        int line = idNode.Symbol.Line;
        int col  = idNode.Symbol.Column + 1;

        // Asignar direccion virtual antes de crear el simbolo
        int addr = _emitter.AllocateVariable(type, isGlobal);
        _emitter.RegisterAddress(name, addr);

        var sym = new Symbol(name, type, kind, line, col, addr);

        if (!table.TryDeclare(sym))
        {
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

    // 
    //  [PN-3] ProcessFuncs: registra cada funcion con params y locales.
    // 
    private void ProcessFuncs(PatitoParser.FuncsContext funcsCtx)
    {
        var idArray = funcsCtx.ID();
        if (idArray is null) return;
        int n = idArray.Length;

        for (int i = 0; i < n; i++)
        {
            var idNode     = funcsCtx.ID(i);
            var name       = idNode.GetText();
            int line       = idNode.Symbol.Line;
            int col        = idNode.Symbol.Column + 1;
            var returnType = ParseTypoFun(funcsCtx.typo_fun(i));
            var info       = new FunctionInfo(name, returnType, line, col);

            if (_directory.ProgramName is not null &&
                string.Equals(name, _directory.ProgramName, System.StringComparison.Ordinal))
            {
                _errors.Add(new SemanticError(line, col,
                    SemanticErrorCode.NameClashesWithProgram, name,
                    $"La funcion '{name}' usa el mismo identificador que el programa."));
            }

            if (!_directory.TryDeclare(info))
            {
                var prev = _directory.Lookup(name)!;
                _errors.Add(new SemanticError(line, col,
                    SemanticErrorCode.FunctionRedeclared, name,
                    $"Funcion '{name}' ya fue declarada en {prev.Line}:{prev.Column}."));
                continue;
            }

            // [PN-19a] Reservar (en el segmento Global) la direccion del valor
            // de retorno de la funcion, registrada como "{name}_ret". Solo las
            // funciones no-'nula' producen un valor que el llamador consume.
            if (returnType != SemanticType.Nula)
            {
                int retAddr = _emitter.AllocateVariable(returnType, isGlobal: true);
                _emitter.RegisterAddress($"{name}_ret", retAddr);
                info.ReturnAddress = retAddr;
            }

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
                        info.ParameterTypes.Add(pType);
                        DeclareVariable(info.LocalTable, ids[p], pType, SymbolKind.Parameter, isGlobal: false);
                    }
                }
            }

            var bodyCtx = funcsCtx.func_body(i);
            _funcByBody[bodyCtx] = info;

            var localVars = bodyCtx.vars();
            if (localVars is not null)
                ProcessVars(localVars, info.LocalTable, isGlobal: false);
        }
    }

    // 
    //  [PN-0b] EnterCuerpo: si el padre es ProgramaContext, este es el cuerpo
    //          del inicio{}. Hacer backfill del Goto inicial para que la VM
    //          arranque directamente aqui.
    // 
    public override void EnterCuerpo(PatitoParser.CuerpoContext ctx)
    {
        if (ctx.Parent is PatitoParser.ProgramaContext && _mainGotoIdx >= 0)
            _emitter.Fila.Backfill(_mainGotoIdx, _emitter.Fila.Count.ToString());
    }

    // 
    //  [PN-7] Manejo del alcance activo (push/pop) + StartQuad + EndFunc.
    //
    //   EnterFunc_body: registra el indice de inicio del cuerpo de la funcion
    //   en FunctionInfo.StartQuad (PN-7b).
    //   ExitFunc_body:  emite EndFunc para que la maquina virtual sepa donde
    //   termina la funcion y pueda restaurar el contexto (PN-7c).
    // 
    public override void EnterFunc_body(PatitoParser.Func_bodyContext ctx)
    {
        if (_funcByBody.TryGetValue(ctx, out var info))
        {
            // [PN-7b'] Reiniciar contadores de temporales: cada funcion reutiliza
            //          las mismas direcciones del segmento Temp en su activacion.
            _emitter.ResetTemps();

            // [PN-7b] Guardar el indice del primer cuadruplo del cuerpo.
            info.StartQuad = _emitter.Fila.Count;
            _scopeStack.Push(info);
        }
    }

    public override void ExitFunc_body(PatitoParser.Func_bodyContext ctx)
    {
        if (_scopeStack.Count > 0)
        {
            var info = _scopeStack.Pop();
            // [PN-7c] Emitir EndFunc al terminar el cuerpo de la funcion.
            _emitter.Fila.Emit(QuadOp.EndFunc, null, null, info.Name);
        }
    }

    private FunctionInfo? CurrentFunction => _scopeStack.Count > 0 ? _scopeStack.Peek() : null;

    private Symbol? LookupVariable(string name)
    {
        var current = CurrentFunction;
        if (current is not null && current.LocalTable.TryLookup(name, out var local)) return local;
        return _directory.GlobalTable.Lookup(name);
    }

    // 
    //  [PN-4] EnterAsigna : valida que el ID destino exista.
    // 
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

    // 
    //  [PN-5] EnterFactorSimple : valida que un ID en una expresion exista.
    // 
    public override void EnterFactorSimple(PatitoParser.FactorSimpleContext ctx)
    {
        var atom = ctx.simple_atom();
        if (atom is null) return;
        var id = atom.ID();
        if (id is null) return;

        var name = id.GetText();
        if (LookupVariable(name) is null)
        {
            _errors.Add(new SemanticError(
                id.Symbol.Line, id.Symbol.Column + 1,
                SemanticErrorCode.UndeclaredVariable, name,
                $"Variable '{name}' usada sin declaracion previa."));
        }
    }

    // 
    //  [PN-6] EnterLlamada : valida que la funcion invocada exista.
    // 
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

    // 
    //  [PN-8] ExitFactorSimple : apila el operando y su tipo.
    // 
    public override void ExitFactorSimple(PatitoParser.FactorSimpleContext ctx)
    {
        var atom = ctx.simple_atom();
        if (atom is null) return;

        string name;
        SemanticType type;

        if (atom.ID() is not null)
        {
            name = atom.ID().GetText();
            var sym = LookupVariable(name);
            type = sym?.Type ?? SemanticType.Error;
        }
        else
        {
            var cte = atom.cte();
            if (cte?.CTE_FLOT() is not null)
            {
                name = cte.GetText();
                type = SemanticType.Flotante;
            }
            else
            {
                name = cte?.GetText() ?? "0";
                type = SemanticType.Entero;
            }
        }

        // Signo unario negativo
        if (ctx.OP_MENOS() is not null)
        {
            if (atom.ID() is not null)
            {
                // Variable con signo negativo: emitir t = 0 - var
                var temp = _emitter.NewTemp(type);   // [E5] pasa el tipo para asignar dir
                _emitter.Fila.Emit(QuadOp.Neg, null, name, temp);
                name = temp;
            }
            else
            {
                name = "-" + name;
            }
        }

        // [Entrega 5] Registrar constante numerica en el pool de direcciones.
        // Solo aplica a constantes (atom.ID() is null); las variables ya fueron
        // registradas en DeclareVariable, y los temporales de negacion se
        // registraron en NewTemp(type). Tambien cubre el caso negativo ("-42").
        if (atom.ID() is null)
            _emitter.AllocateConstant(name, type);

        _emitter.PushOperand(name, type);
    }

    // 
    //  [PN-9] ExitTermino : aplica * y / (mayor precedencia).
    // 
    public override void ExitTermino(PatitoParser.TerminoContext ctx)
    {
        int n = ctx.factor().Length;
        if (n == 1) return; // Un solo factor, ya esta en las pilas

        // Los n operandos estan en las pilas en orden de evaluacion
        // (el ultimo en el tope). Sacarlos en orden para procesarlos izq->der.
        var names = new string[n];
        var types = new SemanticType[n];
        for (int i = n - 1; i >= 0; i--)
        {
            types[i] = _emitter.Tipos.Pop();
            names[i] = _emitter.Operandos.Pop();
        }

        string leftName = names[0];
        SemanticType leftType = types[0];
        int opIdx = 0;

        // Recorrer hijos para obtener operadores en orden izquierda-derecha
        foreach (var child in ctx.children)
        {
            if (child is not ITerminalNode tn) continue;
            QuadOp? qop = tn.Symbol.Type switch
            {
                PatitoLexer.OP_POR => QuadOp.Times,
                PatitoLexer.OP_DIV => QuadOp.Divide,
                _ => (QuadOp?)null,
            };
            if (qop is null) continue;

            var rightName = names[++opIdx];
            var rightType = types[opIdx];
            var (rn, rt) = _emitter.EmitBinary(qop.Value, leftName, leftType, rightName, rightType);

            if (rt == SemanticType.Error)
            {
                _errors.Add(new SemanticError(
                    ctx.Start.Line, ctx.Start.Column + 1,
                    SemanticErrorCode.TypeMismatch, "",
                    $"Tipos incompatibles en operacion '{qop.Value.ToSymbol()}': " +
                    $"{leftType.ToLexeme()} y {rightType.ToLexeme()}."));
            }
            leftName = rn;
            leftType = rt;
        }

        _emitter.PushOperand(leftName, leftType);
    }

    // 
    //  [PN-10] ExitExp : aplica + y - (menor precedencia que * /).
    // 
    public override void ExitExp(PatitoParser.ExpContext ctx)
    {
        int n = ctx.termino().Length;
        if (n == 1) return;

        var names = new string[n];
        var types = new SemanticType[n];
        for (int i = n - 1; i >= 0; i--)
        {
            types[i] = _emitter.Tipos.Pop();
            names[i] = _emitter.Operandos.Pop();
        }

        string leftName = names[0];
        SemanticType leftType = types[0];
        int opIdx = 0;

        foreach (var child in ctx.children)
        {
            if (child is not ITerminalNode tn) continue;
            QuadOp? qop = tn.Symbol.Type switch
            {
                PatitoLexer.OP_MAS   => QuadOp.Plus,
                PatitoLexer.OP_MENOS => QuadOp.Minus,
                _ => (QuadOp?)null,
            };
            if (qop is null) continue;

            var rightName = names[++opIdx];
            var rightType = types[opIdx];
            var (rn, rt) = _emitter.EmitBinary(qop.Value, leftName, leftType, rightName, rightType);

            if (rt == SemanticType.Error)
            {
                _errors.Add(new SemanticError(
                    ctx.Start.Line, ctx.Start.Column + 1,
                    SemanticErrorCode.TypeMismatch, "",
                    $"Tipos incompatibles en operacion '{qop.Value.ToSymbol()}': " +
                    $"{leftType.ToLexeme()} y {rightType.ToLexeme()}."));
            }
            leftName = rn;
            leftType = rt;
        }

        _emitter.PushOperand(leftName, leftType);
    }

    // 
    //  [PN-11] ExitExpresion : aplica operador relacional si lo hay y, si la
    //          expresion es condicion de un si/mientras, emite GotoF.
    // 
    public override void ExitExpresion(PatitoParser.ExpresionContext ctx)
    {
        if (ctx.rel_op() is not null)
        {
            // Hay operador relacional: sacar los dos operandos
            var rightType = _emitter.Tipos.Pop();
            var rightName = _emitter.Operandos.Pop();
            var leftType  = _emitter.Tipos.Pop();
            var leftName  = _emitter.Operandos.Pop();

            var relCtx = ctx.rel_op();
            QuadOp op;
            if      (relCtx.OP_LT()  is not null) op = QuadOp.Lt;
            else if (relCtx.OP_GT()  is not null) op = QuadOp.Gt;
            else if (relCtx.OP_EQ()  is not null) op = QuadOp.Eq;
            else                                   op = QuadOp.Neq;

            var (rn, rt) = _emitter.EmitBinary(op, leftName, leftType, rightName, rightType);

            if (rt == SemanticType.Error)
            {
                _errors.Add(new SemanticError(
                    ctx.Start.Line, ctx.Start.Column + 1,
                    SemanticErrorCode.TypeMismatch, "",
                    $"Tipos incompatibles en operacion relacional '{op.ToSymbol()}': " +
                    $"{leftType.ToLexeme()} y {rightType.ToLexeme()}."));
            }

            _emitter.PushOperand(rn, rt);
        }

        // Si la expresion es la condicion de un si o un mientras, emitir GotoF
        MaybeEmitGotoF(ctx);
    }

    // Emite un GotoF si esta expresion es condicion de si/mientras.
    private void MaybeEmitGotoF(PatitoParser.ExpresionContext ctx)
    {
        if (ctx.Parent is not PatitoParser.CondicionContext &&
            ctx.Parent is not PatitoParser.CicloContext) return;

        if (_emitter.Tipos.IsEmpty) return;
        _emitter.Tipos.Pop();
        var condName = _emitter.Operandos.Pop();
        int gfIdx = _emitter.Fila.Emit(QuadOp.GotoF, condName, null, "?");
        _pendingGotoF.Push(gfIdx);
    }

    // 
    //  [PN-12] ExitAsigna : valida tipos con el cubo y emite Assign.
    // 
    public override void ExitAsigna(PatitoParser.AsignaContext ctx)
    {
        var idNode = ctx.ID();
        if (idNode is null) return;
        var destName = idNode.GetText();
        var destSym  = LookupVariable(destName);

        // La expresion siempre deja un resultado en las pilas; hay que sacarlo
        // incluso en caso de error para mantener las pilas consistentes.
        if (_emitter.Tipos.IsEmpty) return;
        var exprType = _emitter.Tipos.Pop();
        var exprName = _emitter.Operandos.Pop();

        if (destSym is null) return; // UndeclaredVariable ya reportado en PN-4

        var resultType = SemanticCube.Default.Resolve(destSym.Type, SemanticOp.Assign, exprType);
        if (resultType == SemanticType.Error)
        {
            _errors.Add(new SemanticError(
                idNode.Symbol.Line, idNode.Symbol.Column + 1,
                SemanticErrorCode.TypeMismatch, destName,
                $"No se puede asignar {exprType.ToLexeme()} a '{destName}' de tipo {destSym.Type.ToLexeme()}."));
            return;
        }

        _emitter.Fila.Emit(QuadOp.Assign, exprName, null, destName);
    }

    // 
    //  [PN-13] ExitImp : emite Print para cada elemento de escribe().
    // 
    public override void ExitImp(PatitoParser.ImpContext ctx)
    {
        if (ctx.LETRERO() is not null)
        {
            var literal = ctx.LETRERO().GetText();
            // [Entrega 5] Registrar la cadena literal en el segmento Const-Cadena.
            _emitter.AllocateStringConst(literal);
            _emitter.Fila.Emit(QuadOp.Print, null, null, literal);
        }
        else
        {
            if (_emitter.Tipos.IsEmpty) return;
            _emitter.Tipos.Pop();
            var exprName = _emitter.Operandos.Pop();
            _emitter.Fila.Emit(QuadOp.Print, null, null, exprName);
        }
    }

    // 
    //  [PN-14] EnterCiclo : guarda el indice de inicio del ciclo.
    // 
    public override void EnterCiclo(PatitoParser.CicloContext ctx)
    {
        _cicloStart.Push(_emitter.Fila.Count);
    }

    // 
    //  [PN-15] ExitCuerpo : al salir del cuerpo-si (cuando hay sino) emite
    //          Goto y hace Backfill del GotoF hacia el inicio del sino.
    // 
    public override void ExitCuerpo(PatitoParser.CuerpoContext ctx)
    {
        if (ctx.Parent is not PatitoParser.CondicionContext condCtx) return;
        if (condCtx.KW_SINO() is null) return;
        if (condCtx.cuerpo(0) != ctx) return; // solo aplica al primer cuerpo (si-body)

        // Emitir Goto para saltar el bloque sino una vez ejecutado el si-body
        int gotoIdx = _emitter.Fila.Emit(QuadOp.Goto, null, null, "?");
        _pendingGoto.Push(gotoIdx);

        // Backfill del GotoF: el sino empieza en el siguiente cuadruplo
        if (_pendingGotoF.Count > 0)
        {
            int gfIdx = _pendingGotoF.Pop();
            _emitter.Fila.Backfill(gfIdx, _emitter.Fila.Count.ToString());
        }
    }

    // 
    //  [PN-16] ExitCondicion : hace Backfill del Goto (con sino) o del GotoF
    //          (sin sino) al final del estatuto completo.
    // 
    public override void ExitCondicion(PatitoParser.CondicionContext ctx)
    {
        int current = _emitter.Fila.Count;
        if (ctx.KW_SINO() is not null)
        {
            // Backfill del Goto (emitido en PN-15 antes del bloque sino)
            if (_pendingGoto.Count > 0)
                _emitter.Fila.Backfill(_pendingGoto.Pop(), current.ToString());
        }
        else
        {
            // Backfill del GotoF directamente al final del si
            if (_pendingGotoF.Count > 0)
                _emitter.Fila.Backfill(_pendingGotoF.Pop(), current.ToString());
        }
    }

    // 
    //  [PN-17] ExitCiclo : emite Goto al inicio y resuelve el GotoF.
    // 
    public override void ExitCiclo(PatitoParser.CicloContext ctx)
    {
        if (_cicloStart.Count > 0)
            _emitter.Fila.Emit(QuadOp.Goto, null, null, _cicloStart.Pop().ToString());

        if (_pendingGotoF.Count > 0)
            _emitter.Fila.Backfill(_pendingGotoF.Pop(), _emitter.Fila.Count.ToString());
    }

    // 
    //  [PN-19] ExitRetorno : valida que 'regresa <expr>;' aparezca dentro de
    //          una funcion con tipo de retorno distinto de 'nula' y que el
    //          tipo de la expresion sea compatible (cubo semantico, regla
    //          Assign). Emite (Return, exprName, null, "{func}_ret").
    // 
    public override void ExitRetorno(PatitoParser.RetornoContext ctx)
    {
        // La expresion siempre deja un resultado en las pilas; sacarlo incluso
        // en caso de error para mantener las pilas consistentes.
        if (_emitter.Tipos.IsEmpty) return;
        var exprType = _emitter.Tipos.Pop();
        var exprName = _emitter.Operandos.Pop();

        var current = CurrentFunction;
        if (current is null)
        {
            _errors.Add(new SemanticError(
                ctx.Start.Line, ctx.Start.Column + 1,
                SemanticErrorCode.InvalidReturn, "",
                "'regresa' solo puede usarse dentro del cuerpo de una funcion."));
            return;
        }

        if (current.ReturnType == SemanticType.Nula)
        {
            _errors.Add(new SemanticError(
                ctx.Start.Line, ctx.Start.Column + 1,
                SemanticErrorCode.InvalidReturn, current.Name,
                $"La funcion '{current.Name}' es 'nula' y no puede regresar un valor."));
            return;
        }

        var resultType = SemanticCube.Default.Resolve(current.ReturnType, SemanticOp.Assign, exprType);
        if (resultType == SemanticType.Error)
        {
            _errors.Add(new SemanticError(
                ctx.Start.Line, ctx.Start.Column + 1,
                SemanticErrorCode.TypeMismatch, current.Name,
                $"No se puede regresar {exprType.ToLexeme()} en la funcion '{current.Name}' " +
                $"de tipo {current.ReturnType.ToLexeme()}."));
            return;
        }

        _emitter.Fila.Emit(QuadOp.Return, exprName, null, $"{current.Name}_ret");
    }

    // 
    //  [PN-18] ExitCall_stmt : emite ERA + Param (por cada arg) + Gosub.
    //
    //  Secuencia de cuadruplos para f(a, b):
    //    ERA  _  _  f          <- reserva el Espacio de Registro de Activacion
    //    Param  _  _  a        <- pasa cada argumento en orden
    //    Param  _  _  b
    //    Gosub  f  _  startQ   <- transfiere control; Result = indice de inicio
    // 
    public override void ExitCall_stmt(PatitoParser.Call_stmtContext ctx)
    {
        var llamada = ctx.llamada();
        if (llamada is null) return;
        var funcName = llamada.ID()?.GetText();
        if (funcName is null) return;

        EmitCallSequence(funcName, llamada.args()?.expresion()?.Length ?? 0);
    }

    // 
    //  FactorLlamada: funcion invocada como operando dentro de una expresion.
    //  Emite ERA+Param+Gosub y luego copia "{func}_ret" (la direccion global
    //  reservada para el valor de retorno) a un TEMPORAL fresco antes de
    //  apilarlo. Esto es indispensable: si la misma funcion se invoca mas de
    //  una vez dentro de la misma expresion (p.ej. fib(k-1) + fib(k-2)), las
    //  dos llamadas comparten la direccion "{func}_ret"; copiar de inmediato a
    //  un temporal distinto evita que la segunda llamada sobreescriba el
    //  resultado de la primera antes de combinarlos.
    // 
    public override void ExitFactorLlamada(PatitoParser.FactorLlamadaContext ctx)
    {
        var llamada = ctx.llamada();
        if (llamada is null) return;
        var funcName = llamada.ID()?.GetText();
        if (funcName is null) return;

        EmitCallSequence(funcName, llamada.args()?.expresion()?.Length ?? 0);

        SemanticType returnType = SemanticType.Error;
        if (_directory.TryLookup(funcName, out var info))
            returnType = info.ReturnType;

        if (returnType == SemanticType.Error || returnType == SemanticType.Nula)
        {
            // Funcion inexistente o 'nula' usada como factor: ya se reporto el
            // error correspondiente (UndeclaredFunction / TypeMismatch en la
            // asignacion que la contiene). Apilar un placeholder para no
            // romper el balance de las pilas.
            _emitter.PushOperand("?", SemanticType.Error);
            return;
        }

        var temp = _emitter.NewTemp(returnType);
        _emitter.Fila.Emit(QuadOp.Assign, $"{funcName}_ret", null, temp);
        _emitter.PushOperand(temp, returnType);
    }

    // 
    //  Helper: emite ERA + Param* + Gosub para cualquier llamada a funcion.
    // 
    private void EmitCallSequence(string funcName, int nArgs)
    {
        // Los argumentos ya fueron evaluados; sus nombres estan en las pilas
        // en orden de evaluacion (el ultimo arg en el tope). Sacarlos en LIFO
        // y reordenar para restituir el orden original de los argumentos.
        var argNames = new string[nArgs];
        for (int i = nArgs - 1; i >= 0; i--)
        {
            _emitter.Tipos.Pop();
            argNames[i] = _emitter.Operandos.Pop();
        }

        // ERA: reservar el espacio de activacion ANTES de pasar argumentos
        _emitter.Fila.Emit(QuadOp.Era, null, null, funcName);

        // Param: un cuadruplo por argumento en orden de declaracion
        foreach (var arg in argNames)
            _emitter.Fila.Emit(QuadOp.Param, null, null, arg);

        // Gosub: Left=nombre de funcion, Result=startQuad (o "?" si no disponible)
        string startQuad = "?";
        if (_directory.TryLookup(funcName, out var fi) && fi.StartQuad >= 0)
            startQuad = fi.StartQuad.ToString();
        _emitter.Fila.Emit(QuadOp.Gosub, funcName, null, startQuad);
    }

    // 
    //  Helpers para traducir contextos de tipo en SemanticType.
    // 
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
