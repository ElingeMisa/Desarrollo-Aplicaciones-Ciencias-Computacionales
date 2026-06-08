// =============================================================================
//  QuadrupleEmitter.cs - Orquestador de las estructuras de generacion de codigo.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  QuadrupleEmitter centraliza las tres pilas y la fila de cuadruplos que
//  intervienen en la generacion de codigo intermedio de Patito:
//
//    PilaOperadores  -- operadores binarios pendientes de aplicar.
//    PilaOperandos   -- nombres de operandos/temporales.
//    PilaTipos       -- tipos semanticos paralelos a PilaOperandos.
//    FilaCuadruplos  -- lista ordenada de cuadruplos emitidos.
//
//  Entrega 5: el emitter ahora tambien lleva el libro de direcciones
//  (AddressBook) y el pool de constantes que permiten:
//
//    * Asignar una direccion virtual a cada variable, constante y temporal.
//    * Deduplicar constantes: la misma literal siempre obtiene la misma dir.
//    * Imprimir cuadruplos con el formato  DIR(NOMBRE)  en cada operando.
//
//  El contador de temporales (_tempCounter) sigue generando nombres unicos
//  globales "t0", "t1", ... para facilitar la lectura; las direcciones
//  virtuales se reinician por funcion (via ResetTemps()) porque cada
//  activacion recibe su propio espacio de temporales.
// =============================================================================

using System.Collections.Generic;
using Patito.Compiler.Semantic;

namespace Patito.Compiler.CodeGen;

/// <summary>
/// Orquesta las pilas y la fila de cuadruplos durante la generacion de
/// codigo intermedio. A partir de la Entrega 5 tambien gestiona el
/// <see cref="AddressBook"/> (nombre a  direccion virtual).
/// </summary>
public sealed class QuadrupleEmitter
{
    //  Contador de temporales (global, nunca reinicia) 
    private int _tempCounter;

    //  Mapa de memoria virtual ─
    private readonly VirtualMemoryMap _map = new();

    //  Pool de constantes: literal a  direccion (para deduplicacion) 
    private readonly Dictionary<string, int> _constPool = new();

    //  Libro de direcciones: nombre a  direccion virtual 
    private readonly Dictionary<string, int> _addressBook = new();

    // =========================================================================
    //  Propiedades publicas
    // =========================================================================

    /// <summary>Pila de operadores pendientes.</summary>
    public PilaOperadores Operadores { get; } = new();

    /// <summary>Pila de nombres de operandos.</summary>
    public PilaOperandos Operandos { get; } = new();

    /// <summary>Pila de tipos semanticos (paralela a <see cref="Operandos"/>).</summary>
    public PilaTipos Tipos { get; } = new();

    /// <summary>Fila (lista ordenada) de cuadruplos generados.</summary>
    public FilaCuadruplos Fila { get; } = new();

    /// <summary>
    /// Libro de direcciones: mapea cada nombre de operando (variable, constante
    /// o temporal) a su direccion virtual asignada. Solo los operandos con
    /// direccion conocida aparecen aqui; los demas (nombres de funcion, indices
    /// de salto) se muestran sin prefijo de direccion.
    /// </summary>
    public IReadOnlyDictionary<string, int> AddressBook => _addressBook;

    // =========================================================================
    //  Asignacion de direcciones (Entrega 5)
    // =========================================================================

    /// <summary>
    /// Asigna una direccion virtual para una variable (global o local) y la
    /// registra en el libro de direcciones. Devuelve la direccion asignada.
    /// </summary>
    public int AllocateVariable(SemanticType type, bool isGlobal)
    {
        int addr = _map.AllocateFor(type, isGlobal);
        return addr;
    }

    /// <summary>
    /// Registra explicitamente la asociacion  nombre a  direccion  en el libro.
    /// Se llama despues de <see cref="AllocateVariable"/> para que el nombre
    /// del simbolo quede disponible en el libro.
    /// </summary>
    public void RegisterAddress(string name, int address)
    {
        _addressBook[name] = address;
    }

    /// <summary>
    /// Asigna o recupera (deduplicando) la direccion virtual de una constante
    /// numerica. La misma literal siempre recibe la misma direccion.
    /// </summary>
    public int AllocateConstant(string literal, SemanticType type)
    {
        if (_constPool.TryGetValue(literal, out int existing))
        {
            _addressBook[literal] = existing;   // asegurar que este registrado
            return existing;
        }
        MemorySegment seg = type switch
        {
            SemanticType.Entero   => MemorySegment.ConstInt,
            SemanticType.Flotante => MemorySegment.ConstFloat,
            _                     => MemorySegment.ConstString,
        };
        int addr = _map.Allocate(seg);
        _constPool[literal]   = addr;
        _addressBook[literal] = addr;
        return addr;
    }

    /// <summary>
    /// Asigna o recupera (deduplicando) la direccion virtual de una cadena
    /// literal (LETRERO). Las cadenas van al segmento Const-Cadena.
    /// </summary>
    public int AllocateStringConst(string literal)
    {
        if (_constPool.TryGetValue(literal, out int existing))
        {
            _addressBook[literal] = existing;
            return existing;
        }
        int addr = _map.Allocate(MemorySegment.ConstString);
        _constPool[literal]   = addr;
        _addressBook[literal] = addr;
        return addr;
    }

    /// <summary>
    /// Reinicia los contadores de los segmentos temporales en el mapa de
    /// memoria. Se llama al entrar al cuerpo de cada funcion: cada activacion
    /// obtiene sus propios temporales desde la base del segmento.
    /// El contador de nombres (_tempCounter) NO se reinicia para conservar
    /// nombres unicos a lo largo del programa.
    /// </summary>
    public void ResetTemps() => _map.ResetTemps();

    // =========================================================================
    //  Valores de constantes (Entrega 5 — VM)
    // =========================================================================

    /// <summary>
    /// Construye un diccionario  direccion a  valor real  con todas las
    /// constantes registradas en el pool. La VM lo usa para pre-cargar la
    /// memoria antes de iniciar la ejecucion.
    ///
    /// Conversion por segmento:
    ///   ConstInt    (25000-25999) a  <see cref="int"/> (parse del literal).
    ///   ConstFloat  (26000-26999) a  <see cref="double"/> (parse del literal).
    ///   ConstString (27000-27999) a  <see cref="string"/> sin comillas externas.
    /// </summary>
    public System.Collections.Generic.Dictionary<int, object> BuildConstValues()
    {
        var result = new System.Collections.Generic.Dictionary<int, object>();
        foreach (var kvp in _constPool)
        {
            string literal = kvp.Key;
            int    addr    = kvp.Value;
            object val;

            if (addr >= VirtualMemoryMap.ConstIntBase &&
                addr <  VirtualMemoryMap.ConstIntBase + VirtualMemoryMap.SegmentSize)
            {
                val = int.Parse(literal, System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (addr >= VirtualMemoryMap.ConstFloatBase &&
                     addr <  VirtualMemoryMap.ConstFloatBase + VirtualMemoryMap.SegmentSize)
            {
                val = double.Parse(literal, System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                // ConstString: quitar comillas externas si las tiene
                val = literal.Length >= 2 && literal[0] == '"' && literal[^1] == '"'
                    ? literal[1..^1]
                    : literal;
            }

            result[addr] = val;
        }
        return result;
    }

    // =========================================================================
    //  Generacion de temporales
    // =========================================================================

    /// <summary>
    /// Genera un nombre de temporal unico ("t0", "t1", …), le asigna una
    /// direccion virtual segun su tipo semantico y lo registra en el libro.
    /// </summary>
    public string NewTemp(SemanticType type)
    {
        var name = $"t{_tempCounter++}";
        int addr = _map.AllocateTemp(type);
        _addressBook[name] = addr;
        return name;
    }

    // =========================================================================
    //  API de pilas
    // =========================================================================

    /// <summary>
    /// Apila un par (nombre, tipo) en PilaOperandos y PilaTipos simultaneamente.
    /// Conveniente para ExitFactor*.
    /// </summary>
    public void PushOperand(string name, SemanticType type)
    {
        Operandos.Push(name);
        Tipos.Push(type);
    }

    // =========================================================================
    //  Emision de cuadruplos binarios
    // =========================================================================

    /// <summary>
    /// Extrae el operador del tope de <see cref="Operadores"/> y los dos operandos
    /// del tope de <see cref="Operandos"/> / <see cref="Tipos"/>, consulta el cubo
    /// semantico y emite el cuadruplo. Devuelve el nombre y tipo del resultado
    /// (o ("?", Error) si los tipos son incompatibles).
    /// </summary>
    public (string name, SemanticType type) EmitBinary(
        QuadOp op, string leftName, SemanticType leftType,
        string rightName, SemanticType rightType)
    {
        Operadores.Push(op);
        var quadOp = Operadores.Pop();

        var semOp = ToSemanticOp(quadOp);
        var resultType = SemanticCube.Default.Resolve(leftType, semOp, rightType);
        if (resultType == SemanticType.Error)
            return ("?", SemanticType.Error);

        var temp = NewTemp(resultType);     // asigna nombre Y direccion virtual
        Fila.Emit(quadOp, leftName, rightName, temp);
        return (temp, resultType);
    }

    // -------------------------------------------------------------------------
    //  Conversion QuadOp <-> SemanticOp
    // -------------------------------------------------------------------------
    private static SemanticOp ToSemanticOp(QuadOp op) => op switch
    {
        QuadOp.Plus   => SemanticOp.Plus,
        QuadOp.Minus  => SemanticOp.Minus,
        QuadOp.Times  => SemanticOp.Times,
        QuadOp.Divide => SemanticOp.Divide,
        QuadOp.Lt     => SemanticOp.Lt,
        QuadOp.Gt     => SemanticOp.Gt,
        QuadOp.Eq     => SemanticOp.Eq,
        QuadOp.Neq    => SemanticOp.Neq,
        QuadOp.Assign => SemanticOp.Assign,
        _ => throw new System.InvalidOperationException($"QuadOp {op} no tiene SemanticOp equivalente."),
    };
}
