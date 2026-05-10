using FinalProjekt.Core;
using Spectre.Console;

namespace FinalProjekt.UI;

public class StatsMenu
{
    private readonly Account _account;

    public StatsMenu(Account account)
    {
        _account = account;
    }

    public void Show()
    {
        Console.Clear();
        AnsiConsole.Write(new FigletText("Stats").Color(Color.Aqua));

        double totalSpent  = _account.History.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));
        double totalWon    = _account.History.Where(t => t.Description.Contains("Win") || t.Description.Contains("Jackpot")).Sum(t => t.Amount);
        double assetsValue = _account.OwnedItems.Sum(i => i.Price);
        double netWorth    = _account.Balance + assetsValue;

        Table table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Stat[/]")
            .AddColumn("[bold]Amount[/]");

        table.AddRow("Credits Deposited",   $"[green]{_account.Deposited:N0} cr[/]");
        table.AddRow("Credits Spent",       $"[red]{totalSpent:N0} cr[/]");
        table.AddRow("Credits Won",         $"[gold1]{totalWon:N0} cr[/]");
        table.AddRow("Current Balance",     $"[white]{_account.Balance:N0} cr[/]");
        table.AddRow("Assets Value (shop)", $"[cyan]{assetsValue:N0} cr[/]");
        table.AddRow("[bold]Net Worth[/]",  $"[bold gold1]{netWorth:N0} cr[/]");

        AnsiConsole.Write(table);

        if (_account.OwnedItems.Count > 0)
        {
            AnsiConsole.MarkupLine("\n[bold]Your Collection:[/]");
            foreach (ShopItem item in _account.OwnedItems)
            {
                AnsiConsole.MarkupLine($"  [gold1]•[/] {item.Name}  [grey]({item.Category} — {item.Price:N0} cr)[/]");
            }
        }

        AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
}
