// =============================================================================
//  SemanticType.cs - Tipos semanticos del lenguaje Patito.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Patito maneja unicamente dos tipos basicos para sus variables: 'entero' y
//  'flotante'. Aun asi, el analizador semantico necesita representar otros
//  resultados intermedios:
//
//    * Bool : valor que solo aparece como resultado de operadores relacionales
//             (<, >, ==, !=). NO es declarable por el usuario, pero el cubo
//             semantico lo produce y lo consume al validar la expresion de
//             control de un 'si' o un 'mientras'.
//    * Nula : tipo de retorno especial para funciones que no devuelven valor
//             (palabra reservada 'nula' en la gramatica).
//    * Error: comodin para indicar incompatibilidad de tipos. El cubo lo
//             devuelve cuando una operacion no esta definida (p.ej. flotante
//             == entero esta permitido, pero entero = flotante NO lo esta).
//
//  Mantenemos un enum simple porque el cubo y las tablas se indexan por estos
//  valores enteros (mas rapido y mas claro que comparar strings).
// =============================================================================

namespace Patito.Compiler.Semantic;

/// <summary>
/// Tipos semanticos reconocidos por el compilador de Patito.
/// </summary>
public enum SemanticType
{
    /// <summary>Tipo entero (palabra reservada 'entero').</summary>
    Entero = 0,
    /// <summary>Tipo de coma flotante (palabra reservada 'flotante').</summary>
    Flotante = 1,
    /// <summary>Tipo booleano implicito. Resultado de operadores relacionales.</summary>
    Bool = 2,
    /// <summary>Tipo de retorno de funciones que no devuelven valor ('nula').</summary>
    Nula = 3,
    /// <summary>Marca de incompatibilidad reportada por el cubo semantico.</summary>
    Error = 4,
}

/// <summary>
/// Helpers para imprimir/parsear los tipos semanticos. Centralizar estos
/// ayuda a mantener los mensajes de error consistentes con la gramatica.
/// </summary>
public static class SemanticTypes
{
    /// <summary>Devuelve la representacion textual usada en mensajes (igual que la palabra reservada).</summary>
    public static string ToLexeme(this SemanticType t) => t switch
    {
        SemanticType.Entero   => "entero",
        SemanticType.Flotante => "flotante",
        SemanticType.Bool     => "bool",
        SemanticType.Nula     => "nula",
        _                     => "<error>",
    };
}
