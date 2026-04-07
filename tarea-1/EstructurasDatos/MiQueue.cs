namespace EstructurasDatos;

/// <summary>
/// Queue genérica (FIFO) implementada con lista enlazada usando Nodo&lt;T&gt;.
/// </summary>
public class MiQueue<T>
{
    private Nodo<T>? _frente;
    private Nodo<T>? _final;
    private int _tamanio;

    public MiQueue()
    {
        _frente = null;
        _final = null;
        _tamanio = 0;
    }

    public MiQueue(IEnumerable<T>? elementos = null) : this()
    {
        if (elementos is not null)
        {
            foreach (T elemento in elementos)
                Enqueue(elemento);
        }
    }

    public int Size => _tamanio;

    public bool IsEmpty => _tamanio == 0;

    public void Enqueue(T elemento)
    {
        Nodo<T> nuevo = new Nodo<T>(elemento);

        if (_final is null)
        {
            _frente = nuevo;
            _final = nuevo;
        }
        else
        {
            _final.Siguiente = nuevo;
            _final = nuevo;
        }
        _tamanio++;
    }

    public T Dequeue()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Queue vacía.");

        T valor = _frente!.Valor;
        _frente = _frente.Siguiente;

        if (_frente is null)
            _final = null;

        _tamanio--;
        return valor;
    }

    public T Peek()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Queue vacía.");

        return _frente!.Valor;
    }

    public void Clear()
    {
        _frente = null;
        _final = null;
        _tamanio = 0;
    }

    public bool Contains(T elemento)
    {
        Nodo<T>? actual = _frente;
        while (actual is not null)
        {
            if (EqualityComparer<T>.Default.Equals(actual.Valor, elemento))
                return true;
            actual = actual.Siguiente;
        }
        return false;
    }

    public override string ToString()
    {
        if (IsEmpty) return "MiQueue [ frente -> vacía ]";

        var partes = new List<string>();
        Nodo<T>? actual = _frente;
        while (actual is not null)
        {
            partes.Add(actual.Valor?.ToString() ?? "null");
            actual = actual.Siguiente;
        }
        return $"MiQueue [ frente -> {string.Join(" -> ", partes)} <- final ]";
    }
}
