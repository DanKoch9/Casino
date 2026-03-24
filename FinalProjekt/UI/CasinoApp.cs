using FinalProjekt.Games;
using Spectre.Console;

namespace FinalProjekt.Core;

public class CasinoApp
{
    private readonly List<IGame> _games;
    private readonly Account _account;

    public CasinoApp()
    {
        _account = new Account();
        _games = new List<IGame>
        {
            new SlotMachine(_account),
            new NumberGuess(_account)
        };
    }

    public async Task InitializeAsync()
    {
        await _account.Initialize();
    }

    public void ShowSplash()
    {
        Console.Clear();
        AnsiConsole.Write(new FigletText("CASINO")
            .Color(Color.White)
        );
        AnsiConsole.MarkupLine($"\n[gold1]You have {_account.Balance} credits[/]\n");
    }

    public void Loop()
    {
        while (true)
        {
            Console.Clear();
            ShowSplash();

            SelectionPrompt<string> menu = new SelectionPrompt<string>()
                .Title("Select a game")
                .AddChoices(_games.Select(g => g.Name))
                .AddChoices("Add Credits", "Exit");

            string choice = AnsiConsole.Prompt(menu);

            if (choice == "Exit")
            {
                return;
            }

            if (choice == "Add Credits")
            {
                _account.Deposit();
                continue;
            }

            IGame? selectedGame = _games.FirstOrDefault(g => g.Name == choice);
            if (selectedGame != null)
            {
                selectedGame.ShowSplash();
                selectedGame.Play();
            }
        }
    }
}