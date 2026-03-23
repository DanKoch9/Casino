using FinalProjekt.Core;
using FinalProjekt.UI;
using Spectre.Console;

namespace FinalProjekt.Games;

public class SlotMachine : IGame
{
    
    public string Name => "Slot Machine";
    private readonly Account _account;    
    private readonly SlotRenderer _renderer = new SlotRenderer();
    public SlotMachine(Account account)
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
                    int bet = AnsiConsole.Prompt(
                        new TextPrompt<int>("How much do you want to bet?")
                            .ValidationErrorMessage("[red]That's not a valid number[/]")
                            .Validate(n => n > 0 && n <= _account.Balance ? ValidationResult.Success() : ValidationResult.Error("[red]Bet must be between 1 and your balance[/]"))
                    );
                    _account.Withdraw(bet);
                    int num1 = Random.Shared.Next(1, 10);
                    int num2 = Random.Shared.Next(1, 10);
                    int num3 = Random.Shared.Next(1, 10);
                    
                    _renderer.AnimateSpin(num1, num2, num3);
                    
                    if (num1 == num2 && num2 == num3)
                    {
                        int winAmount = PayoutEngine.GetLogPayout(bet, 5.0);
                        AnsiConsole.MarkupLine($"[gold1]JACKPOT!! You won {winAmount}[/]");
                        _account.Deposit(winAmount);
                    }
                    else if (num1 == num2 || num2 == num3 || num1 == num3)
                    {
                        int winAmount = PayoutEngine.GetLogPayout(bet, 2.0);
                        AnsiConsole.MarkupLine($"[green]You won {winAmount}[/]");
                        _account.Deposit(winAmount);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]You lost {bet} credits [/]");
                    }
                    break;
                case "Main Menu":
                    return;
            }
            AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
            Console.ReadKey(true);
            ShowSplash();
        }
    }

    public void ShowSplash()
    {
        Console.Clear();

        AnsiConsole.Write(new FigletText("Slot Machine")
            .Color(Color.Green)
        );
    }
}