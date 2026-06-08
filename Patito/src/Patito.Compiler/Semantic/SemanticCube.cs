// =============================================================================
//  SemanticCube.cs - Tabla de consideraciones semanticas ("cubo semantico").
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Concepto:
//
//      El cubo semantico responde la pregunta:
//          "Dado el operador <op> aplicado entre un operando de tipo <izq>
//           y un operando de tipo <der>, cual es el tipo del resultado, o
//           es la operacion invalida?"
//
//      Es la tabla unica que centraliza las reglas de compatibilidad de tipos
//      del lenguaje. Cada vez que el analizador semantico evalua una
//      expresion, consulta el cubo en lugar de duplicar logica en multiples
//      puntos del codigo.
//
//  Implementacion:
//
//      Usamos un Dictionary indexado por la tripleta (izq, op, der). Esto es
//      tecnicamente una hash table; conceptualmente sigue siendo un "cubo"
//      tridimensional con dimensiones |tipos| x |operadores| x |tipos|.
//      Optamos por el diccionario sobre un arreglo 3D por dos razones:
//        1. Permite registrar solo las celdas validas; las no registradas
//           devuelven SemanticType.Error sin ocupar memoria.
//        2. Es mas legible al escribir las reglas como una lista de tuplas.
//
//  Cobertura para Patito:
//
//      Tipos operandos: Entero, Flotante (y para asignacion permitimos
//      promocion implicita entero->flotante).
//
//      Resultado de aritmeticos: si ambos son enteros -> entero; si alguno
//      es flotante -> flotante. La division siempre devuelve flotante
//      (regla de diseño documentada).
//
//      Resultado de relacionales: si ambos son numericos (entero/flotante)
//      -> bool. Mezclas con bool son invalidas.
//
//      Resultado de asignacion: el tipo del destino, validando que el valor
//      fuente pueda almacenarse sin perdida (flotante <- entero esta OK;
//      entero <- flotante NO, porque perderia precision).
// =============================================================================

using System.Collections.Generic;

namespace Patito.Compiler.Semantic;

/// <summary>
/// Cubo semantico inmutable de Patito. Para consultar el tipo del resultado
/// de una operacion binaria, use <see cref="Resolve"/>.
/// </summary>
public sealed class SemanticCube
{
    private readonly Dictionary<(SemanticType, SemanticOp, SemanticType), SemanticType> _rules;

    private SemanticCube(Dictionary<(SemanticType, SemanticOp, SemanticType), SemanticType> rules)
    {
        _rules = rules;
    }

    /// <summary>
    /// Resuelve la operacion. Devuelve <see cref="SemanticType.Error"/> si la
    /// combinacion no esta permitida.
    /// </summary>
    public SemanticType Resolve(SemanticType left, SemanticOp op, SemanticType right)
    {
        return _rules.TryGetValue((left, op, right), out var t) ? t : SemanticType.Error;
    }

    /// <summary>True si la combinacion es legal.</summary>
    public bool IsCompatible(SemanticType left, SemanticOp op, SemanticType right)
        => Resolve(left, op, right) != SemanticType.Error;

    /// <summary>Numero total de celdas validas (para tests).</summary>
    public int RuleCount => _rules.Count;

    /// <summary>
    /// Cubo por defecto de Patito. Es la unica instancia que el resto del
    /// compilador necesita; se expone como singleton para evitar reconstruirlo.
    /// </summary>
    public static readonly SemanticCube Default = Build();

    //
    //  Construccion de las reglas. Se hace una sola vez (estatico).
    //
    private static SemanticCube Build()
    {
        var r = new Dictionary<(SemanticType, SemanticOp, SemanticType), SemanticType>();

        // Aritmeticos +, -, * 
        // Promocion: si alguno es flotante el resultado es flotante.
        foreach (var op in new[] { SemanticOp.Plus, SemanticOp.Minus, SemanticOp.Times })
        {
            r[(SemanticType.Entero,   op, SemanticType.Entero)]   = SemanticType.Entero;
            r[(SemanticType.Entero,   op, SemanticType.Flotante)] = SemanticType.Flotante;
            r[(SemanticType.Flotante, op, SemanticType.Entero)]   = SemanticType.Flotante;
            r[(SemanticType.Flotante, op, SemanticType.Flotante)] = SemanticType.Flotante;
        }

        // Division -
        // Regla de diseño documentada: la division SIEMPRE produce flotante,
        // incluso entre dos enteros (para evitar perdida silenciosa de la parte
        // fraccionaria, que es una fuente clasica de bugs).
        r[(SemanticType.Entero,   SemanticOp.Divide, SemanticType.Entero)]   = SemanticType.Flotante;
        r[(SemanticType.Entero,   SemanticOp.Divide, SemanticType.Flotante)] = SemanticType.Flotante;
        r[(SemanticType.Flotante, SemanticOp.Divide, SemanticType.Entero)]   = SemanticType.Flotante;
        r[(SemanticType.Flotante, SemanticOp.Divide, SemanticType.Flotante)] = SemanticType.Flotante;

        // Relacionales <, >, ==, != -
        // Resultado siempre Bool cuando ambos operandos son numericos.
        foreach (var op in new[] { SemanticOp.Lt, SemanticOp.Gt, SemanticOp.Eq, SemanticOp.Neq })
        {
            r[(SemanticType.Entero,   op, SemanticType.Entero)]   = SemanticType.Bool;
            r[(SemanticType.Entero,   op, SemanticType.Flotante)] = SemanticType.Bool;
            r[(SemanticType.Flotante, op, SemanticType.Entero)]   = SemanticType.Bool;
            r[(SemanticType.Flotante, op, SemanticType.Flotante)] = SemanticType.Bool;
        }

        // Asignacion =  (destino, op, fuente) -
        // El "izquierdo" es el TIPO del lvalue (variable destino) y el
        // "derecho" es el tipo de la expresion. Permitimos promocion
        // flotante <- entero, pero NO entero <- flotante.
        r[(SemanticType.Entero,   SemanticOp.Assign, SemanticType.Entero)]   = SemanticType.Entero;
        r[(SemanticType.Flotante, SemanticOp.Assign, SemanticType.Entero)]   = SemanticType.Flotante;
        r[(SemanticType.Flotante, SemanticOp.Assign, SemanticType.Flotante)] = SemanticType.Flotante;
        // entero <- flotante => NO se agrega (cubo devolvera Error).

        return new SemanticCube(r);
    }
}
