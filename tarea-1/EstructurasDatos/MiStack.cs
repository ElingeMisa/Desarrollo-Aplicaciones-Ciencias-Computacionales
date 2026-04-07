namespace EstructurasDatos;

/// <summary>
/// Stack genérico (LIFO) implementado con lista enlazada usando Nodo&lt;T&gt;.
/// </summary>
public class MiStack<T>
{
    private Nodo<T>? _cima;
    private int _tamanio;

    public MiStack()
    {
        _cima = null;
        _tamanio = 0;
    }

    public MiStack(T[] elementos) : this()
    {
        for (int i = 0; i < elementos.Length; i++)
            Push(elementos[i]);
    }

    public int Size => _tamanio;

    public bool IsEmpty => _tamanio == 0;

    public void Push(T elemento)
    {
        _cima = new Nodo<T>(elemento, _cima);
        _tamanio++;
    }

    public T Pop()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Stack vacío.");

        T valor = _cima!.Valor;
        _cima = _cima.Siguiente;
        _tamanio--;
        return valor;
    }

    public T Peek()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Stack vacío.");

        return _cima!.Valor;
    }

    public void Clear()
    {
        _cima = null;
        _tamanio = 0;
    }

    public bool Contains(T elemento)
    {
        Nodo<T>? actual = _cima;
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
        if (IsEmpty) return "MiStack [ cima -> vacío ]";

        var partes = new List<string>();
        Nodo<T>? actual = _cima;
        while (actual is not null)
        {
            partes.Add(actual.Valor?.ToString() ?? "null");
            actual = actual.Siguiente;
        }
        return $"MiStack [ cima -> {string.Join(", ", partes)} ]";
    }
}
