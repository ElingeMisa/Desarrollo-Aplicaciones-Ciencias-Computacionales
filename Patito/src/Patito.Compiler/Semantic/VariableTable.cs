// =============================================================================
//  VariableTable.cs - Tabla de Variables de un alcance.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Una VariableTable representa el contenido de un alcance (scope) en Patito.
//  Hay una tabla para los globales del programa y una tabla por cada funcion
//  declarada. Conceptualmente:
//
//      identificador  -->  Symbol (tipo, kind, ubicacion, etc.)
//
//  Eleccion de estructura: Dictionary<string, Symbol>.
//
//    * Permite la operacion mas critica (busqueda por nombre) en O(1)
//      promedio. Esa busqueda ocurre en CADA referencia a una variable, asi
//      que es el cuello de botella del analisis semantico.
//    * Mantiene unicidad implicita: TryDeclare devuelve false si el nombre
//      ya esta presente, lo que mapea directamente a la validacion de
//      "variable doblemente declarada".
//    * No necesitamos preservar el orden de declaracion ahora; si en Entrega
//      3 lo necesitamos para asignar direcciones consecutivas, podremos
//      iterar la coleccion (el Dictionary de .NET preserva el orden de
//      insercion al enumerar).
//
//  Operaciones expuestas:
//
//    * TryDeclare(symbol)  -> bool : intenta agregar el simbolo. Devuelve false si ya existia (sin sobrescribir).
//    * TryLookup(name, out sym) -> bool : busca por nombre.
//    * Contains(name)      -> bool : version corta de TryLookup.
//    * Symbols             -> enumeracion de todos los simbolos (debug/test).
// =============================================================================

using System.Collections.Generic;

namespace Patito.Compiler.Semantic;

/// <summary>
/// Tabla de variables para un alcance (global o de funcion).
/// La estructura subyacente es <c>Dictionary&lt;string, Symbol&gt;</c>.
/// </summary>
public sealed class VariableTable
{
    private readonly Dictionary<string, Symbol> _symbols;

    /// <summary>Nombre del alcance (p.ej. "&lt;global&gt;" o "sumarHasta"). Solo para diagnostico.</summary>
    public string ScopeName { get; }

    public VariableTable(string scopeName)
    {
        ScopeName = scopeName;
        _symbols = new Dictionary<string, Symbol>();
    }

    /// <summary>Numero de simbolos registrados.</summary>
    public int Count => _symbols.Count;

    /// <summary>Enumera todos los simbolos (orden de insercion).</summary>
    public IEnumerable<Symbol> Symbols => _symbols.Values;

    /// <summary>True si el nombre ya esta declarado en esta tabla.</summary>
    public bool Contains(string name) => _symbols.ContainsKey(name);

    /// <summary>
    /// Intenta registrar un simbolo. Devuelve <c>false</c> sin modificar la
    /// tabla si ya existia un simbolo con el mismo nombre.
    /// </summary>
    public bool TryDeclare(Symbol symbol)
    {
        if (_symbols.ContainsKey(symbol.Name)) return false;
        _symbols[symbol.Name] = symbol;
        return true;
    }

    /// <summary>Busca un simbolo por nombre.</summary>
    public bool TryLookup(string name, out Symbol symbol)
        => _symbols.TryGetValue(name, out symbol!);

    /// <summary>
    /// Devuelve el simbolo o null si no existe. Conveniente para encadenar
    /// con el operador de propagacion nula.
    /// </summary>
    public Symbol? Lookup(string name)
        => _symbols.TryGetValue(name, out var s) ? s : null;
}
