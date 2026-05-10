using FinalProjekt.Core;
using Spectre.Console;

namespace FinalProjekt.Games;

public class NumberGuess : IGame
{
    private readonly Account _account;
    public string Name => "Number Guess";
    private readonly RigEngine _rigEngine;
    public void ShowSplash()
    {
        Console.Clear();

        AnsiConsole.Write(new FigletText(Name)
            .Color(Color.Yellow)
        );
        AnsiConsole.MarkupLine($"\n[gold1]You have {_account.Balance:N0} credits[/]\n");
    }

    public NumberGuess(Account account, RigEngine rigEngine)
    {
        _account = account;
        _rigEngine = rigEngine;
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
                    if (_account.Balance <= 0)
                    {
                        AnsiConsole.MarkupLine("[red]You have no credits left! Go back to the main menu to add more.[/]");
                        break;
                    }
                    long bet = AnsiConsole.Prompt(
                        new TextPrompt<long>("How much do you want to bet?")
                            .ValidationErrorMessage("[red]That's not a valid number[/]")
                            .Validate(n => n > 0 && n <= _account.Balance ? ValidationResult.Success() : ValidationResult.Error("[red]Bet must be between 1 and your balance[/]"))
                    );
                    _account.Deduct(bet, "Number Guess - Bet");

                    long maxNum = AnsiConsole.Prompt(
                        new TextPrompt<long>("What is the highest number you can guess? (Minimum is 4)")
                            .ValidationErrorMessage("[red]That's not a valid number[/]")
                            .Validate(n => n >= 4 ? ValidationResult.Success() : ValidationResult.Error("[red]That's too low[/]"))
                    );
                    
                    long guess = AnsiConsole.Prompt(
                        new TextPrompt<long>("What is your guess?")
                            .ValidationErrorMessage("[red]That's not a valid number[/]")
                    );
                    long target = Random.Shared.Next(1, (int)maxNum+1);
                    Thread.Sleep(1000);
                    AnsiConsole.MarkupLine($"[green]The number was {target}[/]");
                    Thread.Sleep(670);
                    if (guess == target)
                    {
                        if (guess == target)
                        {
                            long winAmount = PayoutEngine.GetLogPayout(bet, maxNum/4.0);
                            _rigEngine.RecordResult(true);
                            AnsiConsole.MarkupLine($"[gold1]YOU GUESSED IT!! You won {winAmount:N0} credits[/]");
                            _account.Add(winAmount + bet, "Number Guess - Win");
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]You lost {bet} credits [/]");
                        _rigEngine.RecordResult(false);
                    }
                    AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]\n");
                    Console.ReadKey(true);
                        
                    Console.Clear();
                    ShowSplash();
                    break;
                case "Main Menu":
                    return;
            }
        }
    }
}