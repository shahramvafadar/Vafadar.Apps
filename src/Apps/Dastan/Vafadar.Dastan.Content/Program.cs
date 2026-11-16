using System.Text;
using Vafadar.Dastan.Content;

// The command of the writers of content. Everything it does is in ContentCommands, where the tests reach it.
Console.OutputEncoding = Encoding.UTF8;
return ContentCommands.Run(args, Console.Out, DateOnly.FromDateTime(DateTime.Today));
