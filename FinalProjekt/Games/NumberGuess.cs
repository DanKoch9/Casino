using FinalProjekt.Core;
using Spectre.Console;

namespace FinalProjekt.Games;

public class NumberGuess : IGame
{
    private readonly Account _account;
    public string Name => "Number Guess";
    private readonly RigEngine _rigEngine = new RigEngine();
    public void ShowSplash()
    {
        Console.Clear();

        AnsiConsole.Write(new FigletText("Number Guess")
            .Color(Color.Yellow)
        );
        AnsiConsole.MarkupLine($"\n[gold1]You have {_account.Balance} credits[/]\n");
    }

    public NumberGuess(Account account)
    {
        _account = account;
    }
    public void Play()
    {
        while (true)
        {
            string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select an option")
                .AddChoices("Play", "Main Menu")
            );

            switch (choice)
            {
                case "Play":
                    AnsiConsole.MarkupLine("\nGoon\n");
                    Console.ReadKey(true);
                    break;
                case "Main Menu":
                    return;
            }
        }
    }
}