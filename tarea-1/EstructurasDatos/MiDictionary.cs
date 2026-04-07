namespace EstructurasDatos;

/// <summary>
/// Hash table genérica (clave -> valor) con encadenamiento usando NodoDict&lt;TKey, TValue&gt;.
/// </summary>
public class MiDictionary<TKey, TValue> where TKey : notnull
{
    private NodoDict<TKey, TValue>?[] _cubetas;
    private int _count;
    private const double FactorCarga = 0.75;
    private const int CapacidadInicial = 8;

    public MiDictionary()
    {
        _cubetas = new NodoDict<TKey, TValue>[CapacidadInicial];
        _count = 0;
    }

    public MiDictionary(int capacidadInicial)
    {
        _cubetas = new NodoDict<TKey, TValue>[capacidadInicial];
        _count = 0;
    }

    public MiDictionary(IEnumerable<(TKey clave, TValue valor)>? pares = null) : this()
    {
        if (pares is not null)
        {
            foreach (var (clave, valor) in pares)
                Add(clave, valor);
        }
    }

    public int Count => _count;

    public bool IsEmpty => _count == 0;

    private int Indice(TKey clave) =>
        Math.Abs(clave.GetHashCode()) % _cubetas.Length;

    public void Add(TKey clave, TValue valor)
    {
        if (ContainsKey(clave))
            throw new ArgumentException($"La clave '{clave}' ya existe.");

        Insertar(clave, valor);
        _count++;

        if ((double)_count / _cubetas.Length >= FactorCarga)
            Rehash();
    }

    public TValue Get(TKey clave)
    {
        NodoDict<TKey, TValue>? nodo = Buscar(clave);
        if (nodo is null)
            throw new KeyNotFoundException($"Clave '{clave}' no encontrada.");
        return nodo.Valor;
    }

    public TValue this[TKey clave]
    {
        get => Get(clave);
        set
        {
            NodoDict<TKey, TValue>? nodo = Buscar(clave);
            if (nodo is not null)
            {
                nodo.Valor = value;
            }
            else
            {
                Insertar(clave, value);
                _count++;
                if ((double)_count / _cubetas.Length >= FactorCarga)
                    Rehash();
            }
        }
    }

    public bool Remove(TKey clave)
    {
        int idx = Indice(clave);
        NodoDict<TKey, TValue>? anterior = null;
        NodoDict<TKey, TValue>? actual = _cubetas[idx];

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

    public bool ContainsKey(TKey clave) => Buscar(clave) is not null;

    public bool TryGetValue(TKey clave, out TValue valor)
    {
        NodoDict<TKey, TValue>? nodo = Buscar(clave);
        if (nodo is not null)
        {
            valor = nodo.Valor;
            return true;
        }
        valor = default!;
        return false;
    }

    public void Clear()
    {
        _cubetas = new NodoDict<TKey, TValue>[_cubetas.Length];
        _count = 0;
    }

    public IEnumerable<TKey> Keys
    {
        get
        {
            for (int i = 0; i < _cubetas.Length; i++)
            {
                NodoDict<TKey, TValue>? actual = _cubetas[i];
                while (actual is not null)
                {
                    yield return actual.Clave;
                    actual = actual.Siguiente;
                }
            }
        }
    }

    public IEnumerable<TValue> Values
    {
        get
        {
            for (int i = 0; i < _cubetas.Length; i++)
            {
                NodoDict<TKey, TValue>? actual = _cubetas[i];
                while (actual is not null)
                {
                    yield return actual.Valor;
                    actual = actual.Siguiente;
                }
            }
        }
    }

    private void Insertar(TKey clave, TValue valor)
    {
        int idx = Indice(clave);
        NodoDict<TKey, TValue> nuevo = new NodoDict<TKey, TValue>(clave, valor, _cubetas[idx]);
        _cubetas[idx] = nuevo;
    }

    private NodoDict<TKey, TValue>? Buscar(TKey clave)
    {
        int idx = Indice(clave);
        NodoDict<TKey, TValue>? actual = _cubetas[idx];
        while (actual is not null)
        {
            if (actual.Clave.Equals(clave))
                return actual;
            actual = actual.Siguiente;
        }
        return null;
    }

    private void Rehash()
    {
        NodoDict<TKey, TValue>?[] antiguas = _cubetas;
        _cubetas = new NodoDict<TKey, TValue>[antiguas.Length * 2];
        _count = 0;

        for (int i = 0; i < antiguas.Length; i++)
        {
            NodoDict<TKey, TValue>? actual = antiguas[i];
            while (actual is not null)
            {
                Insertar(actual.Clave, actual.Valor);
                _count++;
                actual = actual.Siguiente;
            }
        }
    }

    public override string ToString()
    {
        var pares = new List<string>();
        foreach (TKey k in Keys)
            pares.Add($"{k}: {Get(k)}");
        return $"MiDictionary {{ {string.Join(", ", pares)} }}";
    }
}
