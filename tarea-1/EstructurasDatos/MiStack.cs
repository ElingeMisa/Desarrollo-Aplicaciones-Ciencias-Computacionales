namespace EstructurasDatos;

/// <summary>
/// Stack (LIFO) implementado con array.
/// Operaciones principales: Push, Pop, Peek, IsEmpty, Size, Clear.
/// </summary>
public class MiStack<T>
{
    private T[] _datos;
    private int _tope;
    private const int CapacidadInicial = 4;

    public MiStack()
    {
        _datos = new T[CapacidadInicial];
        _tope = -1;
    }

    /// <summary>Número de elementos en el stack.</summary>
    public int Size => _tope + 1;

    /// <summary>True si el stack no tiene elementos.</summary>
    public bool IsEmpty => _tope == -1;

    /// <summary>Agrega un elemento en la cima.</summary>
    public void Push(T elemento)
    {
        if (_tope == _datos.Length - 1)
            Redimensionar();

        _datos[++_tope] = elemento;
    }

    /// <summary>Elimina y regresa el elemento de la cima.</summary>
    public T Pop()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Stack vacío.");

        T valor = _datos[_tope];
        _datos[_tope--] = default!;
        return valor;
    }

    /// <summary>Regresa el elemento de la cima sin eliminarlo.</summary>
    public T Peek()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Stack vacío.");

        return _datos[_tope];
    }

    /// <summary>Elimina todos los elementos.</summary>
    public void Clear()
    {
        Array.Clear(_datos, 0, _tope + 1);
        _tope = -1;
    }

    /// <summary>True si el elemento existe en el stack.</summary>
    public bool Contains(T elemento) =>
        Array.IndexOf(_datos, elemento, 0, _tope + 1) >= 0;

    private void Redimensionar()
    {
        T[] nuevo = new T[_datos.Length * 2];
        Array.Copy(_datos, nuevo, _datos.Length);
        _datos = nuevo;
    }

    public override string ToString() =>
        $"MiStack [ cima -> {(IsEmpty ? "vacío" : string.Join(", ", _datos[..(_tope + 1)].Reverse()))} ]";
}
