using EstructurasDatos;

class Test
{
    static void Main()
    {
        RunAllTests();
    }

    static void RunAllTests()
    {
        StackDemo();
        QueueDemo();
        DictionaryDemo();
    }

    static void StackDemo()
    {
        MiStack<int> stack = new MiStack<int>(new int[] { 10, 20, 30 });

        Utilities.LogHeader("STACK  (LIFO)");
        Utilities.WriteLine(stack.ToString());
        Utilities.WriteLine($"Peek : {stack.Peek()}");
        Utilities.WriteLine($"Pop  : {stack.Pop()}");
        Utilities.WriteLine($"Size : {stack.Size}");
        Utilities.WriteLine($"Contains 10: {stack.Contains(10)}");
        Utilities.WriteLine(stack.ToString());
    }

    static void QueueDemo()
    {
        MiQueue<string> queue = new MiQueue<string>(new string[] { "alfa", "beta", "gamma" });
        
        Utilities.LogHeader("QUEUE  (FIFO)");
        Utilities.WriteLine(queue.ToString());
        Utilities.WriteLine($"Peek    : {queue.Peek()}");
        Utilities.WriteLine($"Dequeue : {queue.Dequeue()}");
        Utilities.WriteLine($"Size    : {queue.Size}");
        Utilities.WriteLine($"Contains 'gamma': {queue.Contains("gamma")}");
        Utilities.WriteLine(queue.ToString());
    }

    static void DictionaryDemo()
    {
        Utilities.LogHeader("DICTIONARY  (Hash Table)");
        MiDictionary<string, int> dict = new MiDictionary<string, int>(new (string, int)[] { ("manzana", 3), ("pera", 7), ("uva", 12) });
        Utilities.WriteLine(dict.ToString());

        dict["pera"] = 99;
        Utilities.WriteLine($"pera actualizada : {dict["pera"]}");
        Utilities.WriteLine($"ContainsKey 'uva': {dict.ContainsKey("uva")}");

        dict.Remove("manzana");
        Utilities.WriteLine($"Count tras Remove: {dict.Count}");

        if (dict.TryGetValue("uva", out int val))
            Utilities.WriteLine($"TryGet 'uva' -> {val}");

        Utilities.WriteLine("Claves : " + string.Join(", ", dict.Keys));
        Utilities.WriteLine("Valores: " + string.Join(", ", dict.Values));
    }
}
