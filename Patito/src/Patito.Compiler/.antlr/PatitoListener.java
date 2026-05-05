// Generated from /Users/vicm/Library/CloudStorage/OneDrive-InstitutoTecnologicoydeEstudiosSuperioresdeMonterrey/Sem 8/Desarrollo de Aplicaciones Avanzadas/Desarrollo-de-Aplicaciones-Avanzadas/Desarrollo-Aplicaciones-Ciencias-Computacionales/Patito/src/Patito.Compiler/Patito.g4 by ANTLR 4.13.1
import org.antlr.v4.runtime.tree.ParseTreeListener;

/**
 * This interface defines a complete listener for a parse tree produced by
 * {@link PatitoParser}.
 */
public interface PatitoListener extends ParseTreeListener {
	/**
	 * Enter a parse tree produced by {@link PatitoParser#programa}.
	 * @param ctx the parse tree
	 */
	void enterPrograma(PatitoParser.ProgramaContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#programa}.
	 * @param ctx the parse tree
	 */
	void exitPrograma(PatitoParser.ProgramaContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#vars}.
	 * @param ctx the parse tree
	 */
	void enterVars(PatitoParser.VarsContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#vars}.
	 * @param ctx the parse tree
	 */
	void exitVars(PatitoParser.VarsContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#listado_vars}.
	 * @param ctx the parse tree
	 */
	void enterListado_vars(PatitoParser.Listado_varsContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#listado_vars}.
	 * @param ctx the parse tree
	 */
	void exitListado_vars(PatitoParser.Listado_varsContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#lista_ids}.
	 * @param ctx the parse tree
	 */
	void enterLista_ids(PatitoParser.Lista_idsContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#lista_ids}.
	 * @param ctx the parse tree
	 */
	void exitLista_ids(PatitoParser.Lista_idsContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#tipo}.
	 * @param ctx the parse tree
	 */
	void enterTipo(PatitoParser.TipoContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#tipo}.
	 * @param ctx the parse tree
	 */
	void exitTipo(PatitoParser.TipoContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#funcs}.
	 * @param ctx the parse tree
	 */
	void enterFuncs(PatitoParser.FuncsContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#funcs}.
	 * @param ctx the parse tree
	 */
	void exitFuncs(PatitoParser.FuncsContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#typo_fun}.
	 * @param ctx the parse tree
	 */
	void enterTypo_fun(PatitoParser.Typo_funContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#typo_fun}.
	 * @param ctx the parse tree
	 */
	void exitTypo_fun(PatitoParser.Typo_funContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#params}.
	 * @param ctx the parse tree
	 */
	void enterParams(PatitoParser.ParamsContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#params}.
	 * @param ctx the parse tree
	 */
	void exitParams(PatitoParser.ParamsContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#func_body}.
	 * @param ctx the parse tree
	 */
	void enterFunc_body(PatitoParser.Func_bodyContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#func_body}.
	 * @param ctx the parse tree
	 */
	void exitFunc_body(PatitoParser.Func_bodyContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#cuerpo}.
	 * @param ctx the parse tree
	 */
	void enterCuerpo(PatitoParser.CuerpoContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#cuerpo}.
	 * @param ctx the parse tree
	 */
	void exitCuerpo(PatitoParser.CuerpoContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#estatuto}.
	 * @param ctx the parse tree
	 */
	void enterEstatuto(PatitoParser.EstatutoContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#estatuto}.
	 * @param ctx the parse tree
	 */
	void exitEstatuto(PatitoParser.EstatutoContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#asigna}.
	 * @param ctx the parse tree
	 */
	void enterAsigna(PatitoParser.AsignaContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#asigna}.
	 * @param ctx the parse tree
	 */
	void exitAsigna(PatitoParser.AsignaContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#condicion}.
	 * @param ctx the parse tree
	 */
	void enterCondicion(PatitoParser.CondicionContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#condicion}.
	 * @param ctx the parse tree
	 */
	void exitCondicion(PatitoParser.CondicionContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#ciclo}.
	 * @param ctx the parse tree
	 */
	void enterCiclo(PatitoParser.CicloContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#ciclo}.
	 * @param ctx the parse tree
	 */
	void exitCiclo(PatitoParser.CicloContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#imprime}.
	 * @param ctx the parse tree
	 */
	void enterImprime(PatitoParser.ImprimeContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#imprime}.
	 * @param ctx the parse tree
	 */
	void exitImprime(PatitoParser.ImprimeContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#imp}.
	 * @param ctx the parse tree
	 */
	void enterImp(PatitoParser.ImpContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#imp}.
	 * @param ctx the parse tree
	 */
	void exitImp(PatitoParser.ImpContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#call_stmt}.
	 * @param ctx the parse tree
	 */
	void enterCall_stmt(PatitoParser.Call_stmtContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#call_stmt}.
	 * @param ctx the parse tree
	 */
	void exitCall_stmt(PatitoParser.Call_stmtContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#llamada}.
	 * @param ctx the parse tree
	 */
	void enterLlamada(PatitoParser.LlamadaContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#llamada}.
	 * @param ctx the parse tree
	 */
	void exitLlamada(PatitoParser.LlamadaContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#args}.
	 * @param ctx the parse tree
	 */
	void enterArgs(PatitoParser.ArgsContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#args}.
	 * @param ctx the parse tree
	 */
	void exitArgs(PatitoParser.ArgsContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#expresion}.
	 * @param ctx the parse tree
	 */
	void enterExpresion(PatitoParser.ExpresionContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#expresion}.
	 * @param ctx the parse tree
	 */
	void exitExpresion(PatitoParser.ExpresionContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#rel_op}.
	 * @param ctx the parse tree
	 */
	void enterRel_op(PatitoParser.Rel_opContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#rel_op}.
	 * @param ctx the parse tree
	 */
	void exitRel_op(PatitoParser.Rel_opContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#exp}.
	 * @param ctx the parse tree
	 */
	void enterExp(PatitoParser.ExpContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#exp}.
	 * @param ctx the parse tree
	 */
	void exitExp(PatitoParser.ExpContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#termino}.
	 * @param ctx the parse tree
	 */
	void enterTermino(PatitoParser.TerminoContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#termino}.
	 * @param ctx the parse tree
	 */
	void exitTermino(PatitoParser.TerminoContext ctx);
	/**
	 * Enter a parse tree produced by the {@code FactorParen}
	 * labeled alternative in {@link PatitoParser#factor}.
	 * @param ctx the parse tree
	 */
	void enterFactorParen(PatitoParser.FactorParenContext ctx);
	/**
	 * Exit a parse tree produced by the {@code FactorParen}
	 * labeled alternative in {@link PatitoParser#factor}.
	 * @param ctx the parse tree
	 */
	void exitFactorParen(PatitoParser.FactorParenContext ctx);
	/**
	 * Enter a parse tree produced by the {@code FactorLlamada}
	 * labeled alternative in {@link PatitoParser#factor}.
	 * @param ctx the parse tree
	 */
	void enterFactorLlamada(PatitoParser.FactorLlamadaContext ctx);
	/**
	 * Exit a parse tree produced by the {@code FactorLlamada}
	 * labeled alternative in {@link PatitoParser#factor}.
	 * @param ctx the parse tree
	 */
	void exitFactorLlamada(PatitoParser.FactorLlamadaContext ctx);
	/**
	 * Enter a parse tree produced by the {@code FactorSimple}
	 * labeled alternative in {@link PatitoParser#factor}.
	 * @param ctx the parse tree
	 */
	void enterFactorSimple(PatitoParser.FactorSimpleContext ctx);
	/**
	 * Exit a parse tree produced by the {@code FactorSimple}
	 * labeled alternative in {@link PatitoParser#factor}.
	 * @param ctx the parse tree
	 */
	void exitFactorSimple(PatitoParser.FactorSimpleContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#simple_atom}.
	 * @param ctx the parse tree
	 */
	void enterSimple_atom(PatitoParser.Simple_atomContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#simple_atom}.
	 * @param ctx the parse tree
	 */
	void exitSimple_atom(PatitoParser.Simple_atomContext ctx);
	/**
	 * Enter a parse tree produced by {@link PatitoParser#cte}.
	 * @param ctx the parse tree
	 */
	void enterCte(PatitoParser.CteContext ctx);
	/**
	 * Exit a parse tree produced by {@link PatitoParser#cte}.
	 * @param ctx the parse tree
	 */
	void exitCte(PatitoParser.CteContext ctx);
}