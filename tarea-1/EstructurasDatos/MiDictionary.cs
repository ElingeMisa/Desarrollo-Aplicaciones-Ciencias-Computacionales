namespace EstructurasDatos;

/// <summary>
/// Hash table generica (clave -> valor) implementada con encadenamiento (chaining).
/// Operaciones principales: Add, Get, Remove, ContainsKey, Update, Keys, Values, Clear.
/// </summary>
public class MiDictionary<TKey, TValue> where TKey : notnull
{
    private class Entrada(TKey clave, TValue valor)
    {
        public TKey Clave = clave;
        public TValue Valor = valor;
        public Entrada? Siguiente = null;
    }

    private Entrada?[] _cubetas;
    private int _count;
    private const double FactorCarga = 0.75;

    public MiDictionary(int capacidadInicial = 8)
    {
        _cubetas = new Entrada[capacidadInicial];
    }

    /// <summary>Numero de pares clave-valor almacenados.</summary>
    public int Count => _count;

    /// <summary>True si no hay entradas.</summary>
    public bool IsEmpty => _count == 0;

    // indice de cubeta para una clave dada
    private int Indice(TKey clave) =>
        Math.Abs(clave.GetHashCode()) % _cubetas.Length;

    /// <summary>Agrega un nuevo par clave-valor. Lanza excepcion si la clave ya existe.</summary>
    public void Add(TKey clave, TValue valor)
    {
        if (ContainsKey(clave))
            throw new ArgumentException($"La clave '{clave}' ya existe.");

        InsertarInterno(clave, valor);
        _count++;

        if ((double)_count / _cubetas.Length >= FactorCarga)
            Rehash();
    }

    /// <summary>Regresa el valor asociado a la clave.</summary>
    public TValue Get(TKey clave)
    {
        var entrada = BuscarEntrada(clave);
        if (entrada is null)
            throw new KeyNotFoundException($"Clave '{clave}' no encontrada.");
        return entrada.Valor;
    }

    /// <summary>Indexador: permite usar dict[clave] para leer y escribir.</summary>
    public TValue this[TKey clave]
    {
        get => Get(clave);
        set
        {
            var entrada = BuscarEntrada(clave);
            if (entrada is not null)
                entrada.Valor = value;
            else
            {
                InsertarInterno(clave, value);
                _count++;
                if ((double)_count / _cubetas.Length >= FactorCarga)
                    Rehash();
            }
        }
    }

    /// <summary>Elimina el par con la clave dada. Regresa true si existia.</summary>
    public bool Remove(TKey clave)
    {
        int idx = Indice(clave);
        Entrada? anterior = null;
        Entrada? actual = _cubetas[idx];

        while (actual is not null)
        {
            if (actual.Clave.Equals(clave))
            {
                if (anterior is null)
                    _cubetas[idx] = actual.Siguiente;
                else
                    anterior.Siguiente = actual.Siguiente;
                _count--;
                return true;
            }
            anterior = actual;
            actual = actual.Siguiente;
        }
        return false;
    }

    /// <summary>True si la clave existe en el diccionario.</summary>
    public bool ContainsKey(TKey clave) => BuscarEntrada(clave) is not null;

    /// <summary>Intenta obtener el valor sin lanzar excepcion.</summary>
    public bool TryGetValue(TKey clave, out TValue valor)
    {
        var entrada = BuscarEntrada(clave);
        if (entrada is not null)
        {
            valor = entrada.Valor;
            return true;
        }
        valor = default!;
        return false;
    }

    /// <summary>Elimina todos los pares.</summary>
    public void Clear()
    {
        _cubetas = new Entrada[_cubetas.Length];
        _count = 0;
    }

    /// <summary>Coleccion de todas las claves.</summary>
    public IEnumerable<TKey> Keys
    {
        get
        {
            foreach (var cubeta in _cubetas)
            {
                var actual = cubeta;
                while (actual is not null)
                {
                    yield return actual.Clave;
                    actual = actual.Siguiente;
                }
            }
        }
    }

    /// <summary>Coleccion de todos los valores.</summary>
    public IEnumerable<TValue> Values
    {
        get
        {
            foreach (var cubeta in _cubetas)
            {
                var actual = cubeta;
                while (actual is not null)
                {
                    yield return actual.Valor;
                    actual = actual.Siguiente;
                }
            }
        }
    }

    // Helpers privados

    private void InsertarInterno(TKey clave, TValue valor)
    {
        int idx = Indice(clave);
        var nueva = new Entrada(clave, valor) { Siguiente = _cubetas[idx] };
        _cubetas[idx] = nueva;
    }

    private Entrada? BuscarEntrada(TKey clave)
    {
        int idx = Indice(clave);
        var actual = _cubetas[idx];
        while (actual is not null)
        {
            if (actual.Clave.Equals(clave)) return actual;
            actual = actual.Siguiente;
        }
        return null;
    }

    private void Rehash()
    {
        var antiguas = _cubetas;
        _cubetas = new Entrada[antiguas.Length * 2];
        _count = 0;
        foreach (var cubeta in antiguas)
        {
            var actual = cubeta;
            while (actual is not null)
            {
                InsertarInterno(actual.Clave, actual.Valor);
                _count++;
                actual = actual.Siguiente;
            }
        }
    }

    public override string ToString()
    {
        var pares = Keys.Select(k => $"{k}: {Get(k)}");
        return $"MiDictionary {{ {string.Join(", ", pares)} }}";
    }
}
