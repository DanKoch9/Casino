using FinalProjekt.Core;
using Spectre.Console;

namespace FinalProjekt.UI;

public class HistoryMenu
{
    private readonly Account _account;

    public HistoryMenu(Account account)
    {
        _account = account;
    }

    public void Show()
    {
        Console.Clear();
        AnsiConsole.Write(new FigletText("History").Color(Color.Grey));

        if (_account.History.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No transactions yet.[/]");
            AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
            Console.ReadKey(true);
            return;
        }

        Table table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[grey]Date[/]")
            .AddColumn("[grey]Time[/]")
            .AddColumn("Description")
            .AddColumn("[grey]Amount[/]");

        foreach (Transaction t in _account.History)
        {
            string amt = t.Amount >= 0 ? $"[green]+{t.Amount}[/]" : $"[red]{t.Amount}[/]";
            table.AddRow($"[grey]{t.Date}[/]", $"[grey]{t.Time}[/]", t.Description, amt);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
}
