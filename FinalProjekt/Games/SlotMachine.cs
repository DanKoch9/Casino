using FinalProjekt.Core;
using FinalProjekt.UI;
using Spectre.Console;

namespace FinalProjekt.Games;

public class SlotMachine : IGame
{
    
    public string Name => "Slot Machine";
    private readonly Account _account;    
    private readonly SlotRenderer _renderer = new SlotRenderer();
    private readonly RigEngine _rigEngine = new RigEngine();
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
                    if (_account.Balance <= 0)
                    {
                        AnsiConsole.MarkupLine("[red]You have no credits left! Go back to the main menu to add more.[/]");
                        break;
                    }
                    int bet = AnsiConsole.Prompt(
                        new TextPrompt<int>("How much do you want to bet?")
                            .ValidationErrorMessage("[red]That's not a valid number[/]")
                            .Validate(n => n > 0 && n <= _account.Balance ? ValidationResult.Success() : ValidationResult.Error("[red]Bet must be between 1 and your balance[/]"))
                    );
                    _account.Deduct(bet);
                    int num1 = Random.Shared.Next(0, 10);
                    int num2 = Random.Shared.Next(0, 10);
                    int num3 = Random.Shared.Next(0, 10);
                    
                    bool willWin = _rigEngine.IsWinAllowed(_account);

                    if (willWin)
                    {
                        if (Random.Shared.Next(1, 3) == 1)
                        {
                            num1 = num2;
                        }
                        else
                        {
                            num2 = num3;
                        }
                    }
                    else
                    {
                        while (num1 == num2 || num1 == num3 || num2 == num3)
                        {
                            num2 = (num1 + Random.Shared.Next(1, 10)) % 10;
                            num3 = (num2 + Random.Shared.Next(1, 10)) % 10;
                            if (num3 == num1) num3 = (num3 + 1) % 10;
                        }
                    }
                    
                    _renderer.PlayAnim(num1, num2, num3);
                    
                    if (num1 == num2 && num2 == num3)
                    {
                        int winAmount = PayoutEngine.GetLogPayout(bet, 5.0);
                        AnsiConsole.MarkupLine($"[gold1]JACKPOT!! You won {winAmount} credits[/]");
                        _account.Add(winAmount + bet);
                        _rigEngine.RecordResult(true);
                    }
                    else if (num1 == num2 || num2 == num3 || num1 == num3)
                    {
                        int winAmount = PayoutEngine.GetLogPayout(bet, 2.0);
                        AnsiConsole.MarkupLine($"[green]BIG WIN! You won {winAmount} credits[/]");
                        _account.Add(winAmount + bet);
                        _rigEngine.RecordResult(true);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]You lost {bet} credits [/]");
                        _rigEngine.RecordResult(false);
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

        AnsiConsole.Write(new FigletText(Name)
            .Color(Color.Green)
        );
        AnsiConsole.MarkupLine($"\n[gold1]You have {_account.Balance} credits[/]\n");
    }
    
}