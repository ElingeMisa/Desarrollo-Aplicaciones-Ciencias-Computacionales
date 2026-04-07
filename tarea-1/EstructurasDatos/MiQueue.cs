namespace EstructurasDatos;

/// <summary>
/// Queue genérica (FIFO) implementada con lista enlazada simple.
/// Operaciones principales: Enqueue, Dequeue, Peek, IsEmpty, Size, Clear.
/// </summary>
public class MiQueue<T>
{
    private class Nodo(T valor)
    {
        public T Valor = valor;
        public Nodo? Siguiente = null;
    }

    private Nodo? _frente;
    private Nodo? _final;
    private int _tamanio;

    /// <summary>Número de elementos en la queue.</summary>
    public int Size => _tamanio;

    /// <summary>True si la queue no tiene elementos.</summary>
    public bool IsEmpty => _tamanio == 0;

    /// <summary>Agrega un elemento al final de la queue.</summary>
    public void Enqueue(T elemento)
    {
        var nuevo = new Nodo(elemento);
        if (_final is null)
            _frente = _final = nuevo;
        else
        {
            _final.Siguiente = nuevo;
            _final = nuevo;
        }
        _tamanio++;
    }

    /// <summary>Elimina y regresa el elemento del frente.</summary>
    public T Dequeue()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Queue vacía.");

        T valor = _frente!.Valor;
        _frente = _frente.Siguiente;
        if (_frente is null) _final = null;
        _tamanio--;
        return valor;
    }

    /// <summary>Regresa el elemento del frente sin eliminarlo.</summary>
    public T Peek()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Queue vacía.");

        return _frente!.Valor;
    }

    /// <summary>Elimina todos los elementos.</summary>
    public void Clear()
    {
        _frente = _final = null;
        _tamanio = 0;
    }

    /// <summary>True si el elemento existe en la queue.</summary>
    public bool Contains(T elemento)
    {
        var actual = _frente;
        while (actual is not null)
        {
            if (EqualityComparer<T>.Default.Equals(actual.Valor, elemento))
                return true;
            actual = actual.Siguiente;
        }
        return false;
    }

    /// <summary>
    /// Constructor a partir de una colección de elementos (opcional).
    /// </summary>
    /// <returns></returns>
    /// <param name="elementos"></param>
    public MiQueue(IEnumerable<T>? elementos = null)
    {
        _frente = _final = null;
        _tamanio = 0;
        if (elementos is not null)
        {
            foreach (T elemento in elementos)
                Enqueue(elemento);
        }
    }
    public override string ToString()
    {
        if (IsEmpty) return "MiQueue [ frente -> vacía ]";
        var nodos = new List<string>();
        var actual = _frente;
        while (actual is not null)
        {
            nodos.Add(actual.Valor?.ToString() ?? "null");
            actual = actual.Siguiente;
        }
        return $"MiQueue [ frente -> {string.Join(" -> ", nodos)} ← final ]";
    }
}
