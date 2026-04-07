using System;

namespace EstructurasDatos;

public class Utilities
{
    private const int LineWidth = 40;
    private static string Spacer = new string('=', LineWidth);
    public static void LogHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine(Spacer);
        Console.WriteLine($"\t\t{title}");
        Console.WriteLine(Spacer);
    }

    public static void LogSubHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"--- {title} ---");
    }

    public static void WriteLine(string message)
    {
        Console.WriteLine("\t" + message);
    }
}
