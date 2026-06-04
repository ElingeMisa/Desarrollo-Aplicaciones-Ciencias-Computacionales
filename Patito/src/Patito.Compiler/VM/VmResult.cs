// =============================================================================
//  VmResult.cs - Resultado de la ejecucion de la maquina virtual Patito.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================

using System;

namespace Patito.Compiler.VM;

/// <summary>
/// Encapsula el resultado de una ejecucion de la <see cref="VirtualMachine"/>.
/// </summary>
/// <param name="Output">Todo lo que el programa imprimio via <c>escribe</c>.</param>
/// <param name="Error">Excepcion ocurrida durante la ejecucion; null si no hubo error.</param>
/// <param name="Success">True si la ejecucion termino sin excepciones.</param>
public sealed record VmResult(string Output, Exception? Error, bool Success);
