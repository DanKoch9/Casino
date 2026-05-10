using FinalProjekt.Core;
using FinalProjekt.Data;
using FinalProjekt.Games;
using Spectre.Console;

namespace FinalProjekt.UI;

public class CasinoApp
{
    private readonly Account _account;
    private readonly RigEngine _rigEngine;
    private readonly List<IGame> _games;
    private readonly ShopMenu _shop;
    private readonly StatsMenu _stats;
    private readonly HistoryMenu _history;

    public CasinoApp()
    {
        _account = new Account();
        _rigEngine = new RigEngine();
        _games = new List<IGame>
        {
            new SlotMachine(_account, _rigEngine),
            new NumberGuess(_account, _rigEngine),
            new Roulette(_account, _rigEngine),
            new SportsBetting(_account, _rigEngine)
        };
        _shop    = new ShopMenu(_account);
        _stats   = new StatsMenu(_account);
        _history = new HistoryMenu(_account);
    }

    public async Task Initialize()
    {
        await _account.Initialize();
    }

    public void ShowSplash()
    {
        Console.Clear();
        AnsiConsole.Write(new FigletText("CASINO").Color(Color.White));
        AnsiConsole.MarkupLine($"\n[gold1]You have {_account.Balance:N0} credits[/]\n");
    }

    public void Loop()
    {
        while (true)
        {
            if (!_account.IsLoggedIn())
            {
                Console.Clear();
                AnsiConsole.Write(new FigletText("CASINO").Color(Color.White));
                AnsiConsole.MarkupLine("[yellow]Please log in to play...[/]");
                _account.Initialize().Wait();
                if (!_account.IsLoggedIn())
                {
                    continue;
                }
            }

            ShowSplash();

            string section = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Main menu")
                .AddChoices("Games", "Finances", "Logout", "Exit")
            );

            switch (section)
            {
                case "Exit":
                    return;
                case "Logout":
                    _account.Logout();
                    break;
                case "Games":
                    GamesMenu();
                    break;
                case "Finances":
                    FinancesMenu();
                    break;
            }
        }
    }

    private void GamesMenu()
    {
        while (true)
        {
            ShowSplash();

            string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select a game")
                .AddChoices(_games.Select(g => g.Name))
                .AddChoices("Back")
            );

            if (choice == "Back")
            {
                return;
            }

            IGame? game = _games.FirstOrDefault(g => g.Name == choice);
            if (game != null)
            {
                game.ShowSplash();
                game.Play();
            }
        }
    }

    private void FinancesMenu()
    {
        while (true)
        {
            ShowSplash();

            string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Finances")
                .AddChoices("Add Credits", "Shop", "Stats", "Transaction History", "Back")
            );

            switch (choice)
            {
                case "Back":
                    return;
                case "Add Credits":
                    new StripeService().ProcessDeposit(_account);
                    break;
                case "Shop":
                    _shop.Show();
                    break;
                case "Stats":
                    _stats.Show();
                    break;
                case "Transaction History":
                    _history.Show();
                    break;
            }
        }
    }
}
