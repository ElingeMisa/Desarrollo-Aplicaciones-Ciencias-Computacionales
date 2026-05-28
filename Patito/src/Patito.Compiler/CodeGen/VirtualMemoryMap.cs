// =============================================================================
//  VirtualMemoryMap.cs - Distribucion de Direcciones Virtuales de Patito.
//  Autor: Victor Misael Escalante Alvarado, A01741176
// =============================================================================
//
//  Define los segmentos de memoria virtual del compilador Patito.
//  Cada segmento tiene un rango fijo de 1 000 direcciones, lo que garantiza
//  un techo maximo claro de 1 000 simbolos por categoria y permite detectar
//  desbordamiento con una comparacion simple.
//
//  Nota de diseno (Entrega 4):
//    La asignacion de direcciones a simbolos concretos (variables, constantes
//    y temporales) es trabajo de la Entrega 5. Por ahora este archivo define
//    la ESTRUCTURA del mapa y expone los metodos de consulta y de asignacion
//    por tipo/alcance para que los algoritmos futuros los usen sin conocer
//    los rangos concretos.
//
//  Distribucion (base 18 000):
//  ┌──────────────────────────┬─────────┬─────────┬────────┐
//  │ Segmento                 │  Inicio │   Fin   │  Tam.  │
//  ├──────────────────────────┼─────────┼─────────┼────────┤
//  │ Global   – Entero        │ 18 000  │ 18 999  │ 1 000  │
//  │ Global   – Flotante      │ 19 000  │ 19 999  │ 1 000  │
//  │ Local    – Entero        │ 20 000  │ 20 999  │ 1 000  │
//  │ Local    – Flotante      │ 21 000  │ 21 999  │ 1 000  │
//  │ Temporal – Entero        │ 22 000  │ 22 999  │ 1 000  │
//  │ Temporal – Flotante      │ 23 000  │ 23 999  │ 1 000  │
//  │ Temporal – Bool          │ 24 000  │ 24 999  │ 1 000  │
//  │ Constante – Entero       │ 25 000  │ 25 999  │ 1 000  │
//  │ Constante – Flotante     │ 26 000  │ 26 999  │ 1 000  │
//  │ Constante – Cadena       │ 27 000  │ 27 999  │ 1 000  │
//  └──────────────────────────┴─────────┴─────────┴────────┘
// =============================================================================

using Patito.Compiler.Semantic;

namespace Patito.Compiler.CodeGen;

/// <summary>
/// Categorias de memoria virtual disponibles en Patito.
/// </summary>
public enum MemorySegment
{
    GlobalInt,
    GlobalFloat,
    LocalInt,
    LocalFloat,
    TempInt,
    TempFloat,
    TempBool,
    ConstInt,
    ConstFloat,
    ConstString,
}

/// <summary>
/// Distribucion de Direcciones Virtuales del compilador Patito.
/// Expone los rangos de cada segmento y lleva un contador de asignacion
/// por segmento para la fase futura de traduccion a direcciones virtuales.
/// </summary>
public sealed class VirtualMemoryMap
{
    // ── Capacidad maxima por segmento ─────────────────────────────────────
    public const int SegmentSize = 1_000;

    // ── Bases de cada segmento ────────────────────────────────────────────
    public const int GlobalIntBase     = 18_000;
    public const int GlobalFloatBase   = 19_000;
    public const int LocalIntBase      = 20_000;
    public const int LocalFloatBase    = 21_000;
    public const int TempIntBase       = 22_000;
    public const int TempFloatBase     = 23_000;
    public const int TempBoolBase      = 24_000;
    public const int ConstIntBase      = 25_000;
    public const int ConstFloatBase    = 26_000;
    public const int ConstStringBase   = 27_000;

    // ── Contadores de asignacion (uno por segmento) ───────────────────────
    private readonly int[] _counters = new int[10];

    // ── Instancia compartida (singleton sin estado para solo leer rangos) ─
    public static VirtualMemoryMap Default { get; } = new();

    // =========================================================================
    //  Consulta de rangos
    // =========================================================================

    /// <summary>
    /// Devuelve la direccion base del segmento indicado.
    /// </summary>
    public static int BaseOf(MemorySegment seg) => seg switch
    {
        MemorySegment.GlobalInt   => GlobalIntBase,
        MemorySegment.GlobalFloat => GlobalFloatBase,
        MemorySegment.LocalInt    => LocalIntBase,
        MemorySegment.LocalFloat  => LocalFloatBase,
        MemorySegment.TempInt     => TempIntBase,
        MemorySegment.TempFloat   => TempFloatBase,
        MemorySegment.TempBool    => TempBoolBase,
        MemorySegment.ConstInt    => ConstIntBase,
        MemorySegment.ConstFloat  => ConstFloatBase,
        MemorySegment.ConstString => ConstStringBase,
        _ => throw new System.ArgumentOutOfRangeException(nameof(seg)),
    };

    /// <summary>
    /// Devuelve la ultima direccion valida (inclusive) del segmento.
    /// </summary>
    public static int EndOf(MemorySegment seg) => BaseOf(seg) + SegmentSize - 1;

    /// <summary>
    /// Determina si una direccion virtual pertenece al segmento indicado.
    /// </summary>
    public static bool InSegment(int address, MemorySegment seg)
        => address >= BaseOf(seg) && address <= EndOf(seg);

    /// <summary>
    /// Determina el segmento al que pertenece una direccion virtual,
    /// o null si no corresponde a ningun segmento conocido.
    /// </summary>
    public static MemorySegment? SegmentOf(int address)
    {
        foreach (MemorySegment seg in System.Enum.GetValues<MemorySegment>())
            if (InSegment(address, seg)) return seg;
        return null;
    }

    // =========================================================================
    //  Asignacion de direcciones (preparacion para Entrega 5)
    // =========================================================================

    /// <summary>
    /// Asigna la siguiente direccion disponible en el segmento indicado.
    /// Lanza <see cref="System.OverflowException"/> si el segmento esta lleno.
    /// </summary>
    public int Allocate(MemorySegment seg)
    {
        int idx = (int)seg;
        if (_counters[idx] >= SegmentSize)
            throw new System.OverflowException(
                $"El segmento {seg} esta lleno (maximo {SegmentSize} direcciones).");
        return BaseOf(seg) + _counters[idx]++;
    }

    /// <summary>
    /// Determina el segmento correcto segun el alcance (global/local) y el tipo
    /// semantico, y asigna la siguiente direccion disponible.
    /// </summary>
    public int AllocateFor(SemanticType type, bool isGlobal)
    {
        MemorySegment seg = (type, isGlobal) switch
        {
            (SemanticType.Entero,   true)  => MemorySegment.GlobalInt,
            (SemanticType.Flotante, true)  => MemorySegment.GlobalFloat,
            (SemanticType.Entero,   false) => MemorySegment.LocalInt,
            (SemanticType.Flotante, false) => MemorySegment.LocalFloat,
            _ => throw new System.ArgumentException(
                     $"No hay segmento para tipo={type} isGlobal={isGlobal}."),
        };
        return Allocate(seg);
    }

    /// <summary>
    /// Asigna una direccion para un temporal segun su tipo semantico.
    /// </summary>
    public int AllocateTemp(SemanticType type)
    {
        MemorySegment seg = type switch
        {
            SemanticType.Entero   => MemorySegment.TempInt,
            SemanticType.Flotante => MemorySegment.TempFloat,
            _                     => MemorySegment.TempBool,   // bool / resultado relacional
        };
        return Allocate(seg);
    }

    /// <summary>
    /// Restablece los contadores del mapa (util al compilar multiples programas
    /// en la misma sesion, p.ej. durante las pruebas unitarias).
    /// </summary>
    public void Reset()
    {
        for (int i = 0; i < _counters.Length; i++)
            _counters[i] = 0;
    }

    // =========================================================================
    //  Representacion imprimible (para debug y documentacion)
    // =========================================================================

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("  Segmento                Inicio    Fin      Usadas");
        sb.AppendLine("  ──────────────────────────────────────────────────");
        foreach (MemorySegment seg in System.Enum.GetValues<MemorySegment>())
        {
            int idx  = (int)seg;
            int used = _counters[idx];
            sb.AppendLine($"  {seg,-24}{BaseOf(seg),6}   {EndOf(seg),6}    {used,4}");
        }
        return sb.ToString();
    }
}
