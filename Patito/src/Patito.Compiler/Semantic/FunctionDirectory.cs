// =============================================================================
//  FunctionDirectory.cs - Directorio de Funciones global del programa.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  El Directorio de Funciones es UNA sola tabla por programa que mapea cada
//  identificador de funcion a su FunctionInfo. Tambien guardamos un slot
//  especial para el "programa" mismo, porque en Patito el nombre del programa
//  es un identificador global que NO puede ser reusado para una funcion.
//
//  Estructura: Dictionary<string, FunctionInfo>.
//
//    * Misma justificacion que VariableTable: O(1) lookup, deteccion de
//      duplicados gratis.
//    * Mantenemos ademas una referencia explicita al nombre del programa y
//      a la tabla de variables globales; conceptualmente "el programa" es
//      un FunctionInfo especial sin parametros y de tipo Nula, pero lo
//      tratamos aparte para que el directorio liste SOLO funciones de
//      usuario (mas legible al imprimir).
// =============================================================================

using System.Collections.Generic;

namespace Patito.Compiler.Semantic;

public sealed class FunctionDirectory
{
    private readonly Dictionary<string, FunctionInfo> _functions = new();

    /// <summary>Nombre del programa (ID despues de la palabra 'programa').</summary>
    public string? ProgramName { get; set; }

    /// <summary>Tabla de variables globales del programa.</summary>
    public VariableTable GlobalTable { get; } = new("<global>");

    /// <summary>Numero de funciones declaradas (excluye el programa).</summary>
    public int Count => _functions.Count;

    /// <summary>Enumera todas las funciones declaradas.</summary>
    public IEnumerable<FunctionInfo> Functions => _functions.Values;

    /// <summary>True si existe una funcion con ese nombre.</summary>
    public bool Contains(string name) => _functions.ContainsKey(name);

    /// <summary>
    /// Intenta registrar una funcion. Devuelve <c>false</c> sin modificar el
    /// directorio si ya existia una funcion con el mismo nombre.
    /// </summary>
    public bool TryDeclare(FunctionInfo info)
    {
        if (_functions.ContainsKey(info.Name)) return false;
        _functions[info.Name] = info;
        return true;
    }

    public bool TryLookup(string name, out FunctionInfo info)
        => _functions.TryGetValue(name, out info!);

    public FunctionInfo? Lookup(string name)
        => _functions.TryGetValue(name, out var f) ? f : null;
}
