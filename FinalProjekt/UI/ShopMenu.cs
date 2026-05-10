using FinalProjekt.Core;
using Spectre.Console;

namespace FinalProjekt.UI;

public class ShopMenu
{
    private readonly Account _account;

    public ShopMenu(Account account)
    {
        _account = account;
    }

    public void Show()
    {
        while (true)
        {
            Console.Clear();
            AnsiConsole.Write(new FigletText("Shop").Color(Color.Gold1));
            AnsiConsole.MarkupLine($"[gold1]You have {_account.Balance:N0} credits[/]\n");

            List<string> categories = ShopCatalog.Items.Select(i => i.Category).Distinct().ToList();
            categories.Add("Back");

            string cat = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select a category")
                .AddChoices(categories)
            );

            if (cat == "Back")
            {
                return;
            }

            while (true)
            {
                Console.Clear();
                AnsiConsole.Write(new FigletText("Shop").Color(Color.Gold1));
                AnsiConsole.MarkupLine($"[gold1]You have {_account.Balance:N0} credits[/]\n");

                ShopItem[] items = ShopCatalog.Items.Where(i => i.Category == cat).ToArray();
                Dictionary<string, ShopItem?> labelMap = new Dictionary<string, ShopItem?>();

                foreach (ShopItem item in items)
                {
                    bool owned = _account.OwnedItems.Contains(item);
                    string label = owned
                        ? $"[grey]{item.Name}  ({item.Price:N0} cr)  ✓ Owned[/]"
                        : $"{item.Name}  ({item.Price:N0} cr)";
                    labelMap[label] = item;
                }
                labelMap["Back"] = null;

                string picked = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title($"[bold]{cat}[/]")
                    .AddChoices(labelMap.Keys)
                );

                if (picked == "Back")
                {
                    break;
                }

                ShopItem? selected = labelMap[picked];
                if (selected == null)
                {
                    break;
                }

                if (_account.OwnedItems.Contains(selected))
                {
                    AnsiConsole.MarkupLine("[grey]You already own this.[/]");
                    AnsiConsole.MarkupLine("\n[grey]Press any key...[/]");
                    Console.ReadKey(true);
                    continue;
                }

                if (_account.Balance < selected.Price)
                {
                    AnsiConsole.MarkupLine($"[red]Not enough credits. Need {selected.Price:N0}, have {_account.Balance:N0}.[/]");
                    AnsiConsole.MarkupLine("\n[grey]Press any key...[/]");
                    Console.ReadKey(true);
                    continue;
                }

                string confirm = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title($"Buy [gold1]{selected.Name}[/] for [gold1]{selected.Price:N0}[/] credits?")
                    .AddChoices("Yes", "No")
                );

                if (confirm == "Yes")
                {
                    _account.BuyItem(selected);
                    AnsiConsole.MarkupLine($"\n[gold1]You now own: {selected.Name}[/]");
                    AnsiConsole.MarkupLine("[grey]Press any key...[/]");
                    Console.ReadKey(true);
                }
            }
        }
    }
}
