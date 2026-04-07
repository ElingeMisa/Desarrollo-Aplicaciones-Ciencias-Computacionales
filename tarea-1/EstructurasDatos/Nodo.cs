namespace EstructurasDatos;

/// <summary>
/// Nodo genérico para estructuras enlazadas (Stack, Queue).
/// </summary>
public class Nodo<T>
{
    public T Valor;
    public Nodo<T>? Siguiente;

    public Nodo(T valor, Nodo<T>? siguiente = null)
    {
        Valor = valor;
        Siguiente = siguiente;
    }
}

/// <summary>
/// Nodo para pares clave-valor usado en MiDictionary.
/// </summary>
public class NodoDict<TKey, TValue> where TKey : notnull
{
    public TKey Clave;
    public TValue Valor;
    public NodoDict<TKey, TValue>? Siguiente;

    public NodoDict(TKey clave, TValue valor, NodoDict<TKey, TValue>? siguiente = null)
    {
        Clave = clave;
        Valor = valor;
        Siguiente = siguiente;
    }
}
