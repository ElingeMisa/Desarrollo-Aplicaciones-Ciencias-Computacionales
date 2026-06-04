// =============================================================================
//  ActivationRecord.cs - Frame de la pila de llamadas de la VM Patito.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Representa el contexto de ejecucion de una llamada a funcion:
//
//    * FunctionName  : identificador de la funcion que creo este frame.
//    * ReturnQuad    : indice del cuadruplo al que regresar cuando la funcion
//                      termine (el cuadruplo inmediatamente despues del Gosub).
//    * LocalMemory   : memoria local del frame (parametros + variables locales
//                      + temporales). Instancia dedicada de ExecutionMemory;
//                      se descarta al salir de la funcion.
//    * _pendingArgs  : lista de (paramAddr, value) construida por ERA+Param
//                      antes de ejecutar el Gosub. Cada entrada indica la
//                      direccion virtual del parametro formal y el valor del
//                      argumento real.
//
//  Ciclo de vida:
//    1. ERA  → new ActivationRecord(funcName)
//    2. Param* → PushArg(paramAddr, argVal) una vez por argumento
//    3. Gosub → PopArgs() para copiar args a LocalMemory; push en call stack
//    4. EndFunc → pop de call stack; restaurar pc y memoria local activa
// =============================================================================

using System.Collections.Generic;

namespace Patito.Compiler.VM;

/// <summary>
/// Frame de activacion de una llamada a funcion en la VM Patito.
/// </summary>
public sealed class ActivationRecord
{
    private readonly List<(int addr, object val)> _pendingArgs = new();

    /// <summary>Nombre de la funcion a la que pertenece este frame.</summary>
    public string FunctionName { get; }

    /// <summary>
    /// Indice del cuadruplo de retorno (quad inmediatamente posterior al Gosub).
    /// Se establece cuando el Gosub es ejecutado.
    /// </summary>
    public int ReturnQuad { get; set; }

    /// <summary>
    /// Memoria local del frame: contiene los parametros formales, variables
    /// locales y temporales de esta activacion de la funcion.
    /// </summary>
    public ExecutionMemory LocalMemory { get; } = new();

    /// <summary>Construye un frame para la funcion <paramref name="functionName"/>.</summary>
    public ActivationRecord(string functionName) => FunctionName = functionName;

    /// <summary>
    /// Agrega un argumento pendiente al frame. <paramref name="addr"/> es la
    /// direccion virtual del parametro formal en LocalMemory; <paramref name="val"/>
    /// es el valor del argumento real evaluado en el contexto del llamador.
    /// </summary>
    public void PushArg(int addr, object val) => _pendingArgs.Add((addr, val));

    /// <summary>
    /// Devuelve todos los argumentos pendientes (en orden de declaracion) y
    /// limpia la lista interna. Llamado una sola vez, en el Gosub.
    /// </summary>
    public IReadOnlyList<(int addr, object val)> PopArgs()
    {
        var result = new List<(int, object)>(_pendingArgs);
        _pendingArgs.Clear();
        return result;
    }
}
