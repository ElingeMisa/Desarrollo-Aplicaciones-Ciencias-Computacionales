// =============================================================================
//  FunctionInfo.cs - Entrada del Directorio de Funciones.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  FunctionInfo guarda todo lo que el compilador necesita saber de una funcion
//  declarada en Patito. Es la celda del Directorio de Funciones que
//  asocia un identificador con su firma y su tabla de variables locales.
//
//  Campos:
//
//    * Name           : identificador declarado (ej. "sumarHasta").
//    * ReturnType     : tipo declarado en 'typo_fun'. Puede ser Entero,
//                       Flotante o Nula. (Nula indica 'sin retorno').
//    * ParameterTypes : lista ORDENADA con el tipo de cada parametro. El
//                       orden importa porque al llamar f(a,b) cada argumento
//                       tiene que casar posicionalmente con su parametro.
//    * LocalTable     : la VariableTable del alcance de la funcion. Aqui
//                       caen tanto los parametros (con SymbolKind.Parameter)
//                       como las variables locales declaradas en su 'vars'.
//    * Line/Column    : posicion declarada (para diagnostico).
//    * StartQuad      : indice del cuadruplo de inicio. Se llenara en
//                       Entrega 3 (generacion de codigo); por ahora es -1.
//
//  La instancia se construye con tabla local vacia y parametros vacios; el
//  analizador los llena mientras recorre el subarbol de la funcion.
// =============================================================================

using System.Collections.Generic;

namespace Patito.Compiler.Semantic;

public sealed class FunctionInfo
{
    public string Name { get; }
    public SemanticType ReturnType { get; }
    public int Line { get; }
    public int Column { get; }

    /// <summary>Tipos de los parametros, en orden de declaracion.</summary>
    public List<SemanticType> ParameterTypes { get; } = new();

    /// <summary>Tabla de variables local de la funcion (params + locals).</summary>
    public VariableTable LocalTable { get; }

    /// <summary>Indice del primer cuadruplo de la funcion (se llena en Entrega 3).</summary>
    public int StartQuad { get; set; } = -1;

    /// <summary>
    /// Direccion virtual reservada (en el segmento Global) para el valor de
    /// retorno de la funcion, registrada en el AddressBook como "{Name}_ret".
    /// Vale -1 para funciones 'nula' (no producen valor de retorno).
    /// Vivir en Global (y no en el frame local) es necesario porque el valor
    /// debe sobrevivir al EndFunc, que descarta la memoria local de la
    /// activacion antes de que el llamador pueda leerlo.
    /// </summary>
    public int ReturnAddress { get; set; } = -1;

    public FunctionInfo(string name, SemanticType returnType, int line, int column)
    {
        Name = name;
        ReturnType = returnType;
        Line = line;
        Column = column;
        LocalTable = new VariableTable(name);
    }

    public override string ToString() =>
        $"{ReturnType.ToLexeme()} {Name}({string.Join(", ", ParameterTypes)}) @ {Line}:{Column}";
}
