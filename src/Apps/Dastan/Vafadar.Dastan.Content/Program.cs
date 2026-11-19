using System.Text;
using Vafadar.Dastan.Content;

// The command of the writers of content. Everything it does is in ContentCommands, where the tests reach it.
Console.OutputEncoding = Encoding.UTF8;

// What the command writes – a day, a number – is written the same on every machine: not in the calendar and the
// digits of the place it runs in, which would say another year than the files do.
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
return ContentCommands.Run(args, Console.Out, DateOnly.FromDateTime(DateTime.Today), secret: Console.IsInputRedirected ? null : Typed);

// Reads a text from the keyboard without showing it.
static string? Typed(string prompt)
{
    Console.Write(prompt);
    var typed = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return typed.ToString();
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            typed.Length = Math.Max(0, typed.Length - 1);
        }
        else if (!char.IsControl(key.KeyChar))
        {
            typed.Append(key.KeyChar);
        }
    }
}
