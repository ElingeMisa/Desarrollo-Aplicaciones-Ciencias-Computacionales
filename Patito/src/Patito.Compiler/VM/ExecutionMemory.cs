// =============================================================================
//  ExecutionMemory.cs - Memoria de ejecucion en tiempo real de la VM Patito.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Abstraccion sobre un diccionario  int address → object value  que sirve
//  de celda de memoria durante la ejecucion del programa Patito.
//
//  Usos:
//    * Memoria global (variables globales + constantes): una instancia
//      compartida por toda la ejecucion.
//    * Memoria de activacion (LocalInt, LocalFloat, Temp*): una instancia
//      por ActivationRecord; se descarta al regresar de la funcion.
//
//  Tipos de valor almacenados:
//    int    → variables/constantes enteras (SemanticType.Entero)
//    double → variables/constantes flotantes (SemanticType.Flotante)
//    bool   → resultados de operadores relacionales (SemanticType.Bool)
//    string → literales de cadena (ConstString)
// =============================================================================

using System.Collections.Generic;

namespace Patito.Compiler.VM;

/// <summary>
/// Almacen de memoria indexado por direccion virtual.
/// Una instancia representa un segmento de memoria (global o de activacion).
/// </summary>
public sealed class ExecutionMemory
{
    private readonly Dictionary<int, object> _cells = new();

    /// <summary>Escribe <paramref name="value"/> en la direccion <paramref name="address"/>.</summary>
    public void Set(int address, object value) => _cells[address] = value;

    /// <summary>
    /// Lee el valor almacenado en <paramref name="address"/>.
    /// Lanza <see cref="System.InvalidOperationException"/> si la direccion
    /// no ha sido inicializada.
    /// </summary>
    public object Get(int address)
    {
        if (_cells.TryGetValue(address, out var val)) return val;
        throw new System.InvalidOperationException(
            $"La direccion virtual {address} no fue inicializada antes de su primer uso.");
    }

    /// <summary>
    /// Intenta leer el valor en <paramref name="address"/> sin lanzar excepcion.
    /// Devuelve <c>true</c> si la direccion existe.
    /// </summary>
    public bool TryGet(int address, out object value)
        => _cells.TryGetValue(address, out value!);

    /// <summary>Borra todos los valores almacenados.</summary>
    public void Reset() => _cells.Clear();
}
