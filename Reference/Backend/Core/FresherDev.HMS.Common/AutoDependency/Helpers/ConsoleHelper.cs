namespace FresherDev.HMS.Common.AutoDependency;

public class ConsoleHelper
{
    public static void Write(string title, IEnumerable<string> items)
    {        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($">> {title}:");
        Console.ResetColor();
        foreach (var item in items)
        {
            Console.WriteLine($"- {item}");
        }

        Console.WriteLine("Count: " + items.Count());
        Console.WriteLine("=============================================");
    }
}
