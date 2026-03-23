using EstructurasDatos;

// ─────────────────────────────────────────────
//  DEMO: MiStack<T>  (LIFO)
// ─────────────────────────────────────────────
Console.WriteLine("══════════════════════════════");
Console.WriteLine("  STACK  (LIFO)");
Console.WriteLine("══════════════════════════════");

var stack = new MiStack<int>();
stack.Push(10);
stack.Push(20);
stack.Push(30);
Console.WriteLine(stack);                                       // cima → 30, 20, 10
Console.WriteLine($"Peek : {stack.Peek()}");                   // 30
Console.WriteLine($"Pop  : {stack.Pop()}");                    // 30
Console.WriteLine($"Size : {stack.Size}");                     // 2
Console.WriteLine($"Contains 10: {stack.Contains(10)}");       // True
Console.WriteLine(stack);                                       // cima → 20, 10

// ─────────────────────────────────────────────
//  DEMO: MiQueue<T>  (FIFO)
// ─────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("══════════════════════════════");
Console.WriteLine("  QUEUE  (FIFO)");
Console.WriteLine("══════════════════════════════");

var queue = new MiQueue<string>();
queue.Enqueue("alfa");
queue.Enqueue("beta");
queue.Enqueue("gamma");
Console.WriteLine(queue);                                       // frente → alfa → beta → gamma ← final
Console.WriteLine($"Peek    : {queue.Peek()}");                // alfa
Console.WriteLine($"Dequeue : {queue.Dequeue()}");             // alfa
Console.WriteLine($"Size    : {queue.Size}");                  // 2
Console.WriteLine($"Contains 'gamma': {queue.Contains("gamma")}"); // True
Console.WriteLine(queue);                                       // frente → beta → gamma ← final

// ─────────────────────────────────────────────
//  DEMO: MiDictionary<TKey,TValue>  (Hash Table)
// ─────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("══════════════════════════════");
Console.WriteLine("  DICTIONARY  (Hash Table)");
Console.WriteLine("══════════════════════════════");

var dict = new MiDictionary<string, int>();
dict.Add("manzana", 3);
dict.Add("pera", 7);
dict.Add("uva", 12);
Console.WriteLine(dict);

dict["pera"] = 99;                                              // actualizar con indexador
Console.WriteLine($"pera actualizada : {dict["pera"]}");       // 99
Console.WriteLine($"ContainsKey 'uva': {dict.ContainsKey("uva")}"); // True

dict.Remove("manzana");
Console.WriteLine($"Count tras Remove: {dict.Count}");         // 2

if (dict.TryGetValue("uva", out int val))
    Console.WriteLine($"TryGet 'uva' → {val}");                // 12

Console.WriteLine("Claves : " + string.Join(", ", dict.Keys));
Console.WriteLine("Valores: " + string.Join(", ", dict.Values));
