using FinalProjekt.Core;
using FinalProjekt.UI;
using Spectre.Console;

namespace FinalProjekt.Games;

public class Roulette : IGame
{
    private readonly Account _account;
    public string Name => "Roulette";
    private readonly RigEngine _rigEngine = new RigEngine();
    private readonly RouletteRenderer _renderer = new RouletteRenderer();
    
    public Roulette(Account account)
    {
        _account = account;
    }
    public void ShowSplash()
    {
        Console.Clear();
        AnsiConsole.Write(new FigletText("Roulette")
            .Color(Color.Red)
        );
        AnsiConsole.MarkupLine($"\n[gold1]You have {_account.Balance} credits[/]\n");
    }

    public void Play()
    {
        while (true)
        {
            bool willWin = _rigEngine.IsWinAllowed(_account);
            
            int bet = AnsiConsole.Prompt(   
                new TextPrompt<int>("How much do you want to bet?")
                    .ValidationErrorMessage("[red]That's not a valid number[/]")
                    .Validate(n =>
                        n > 0 && n <= _account.Balance
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[red]Bet must be between 1 and your balance[/]"))
            );
            _account.Deduct(bet);
            ShowSplash();
            List<int> betNums = new List<int>();
            var betType = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What type of bet would you like to place?")
                    .AddChoices(new[] {
                        "Single Number",
                        "Red/Black",
                        "Even/Odd",
                        "1st Dozen (1-12)",
                        "2nd Dozen (13-24)",
                        "3rd Dozen (25-36)"
                    }));

            switch (betType)
            {
                case "Single Number": ;
                    int num = AnsiConsole.Prompt(   
                        new TextPrompt<int>("What number do you want to bet on?")
                            .ValidationErrorMessage("[red]That's not a valid number[/]")
                            .Validate(n => n >= 0 && n <= 36
                                    ? ValidationResult.Success()
                                    : ValidationResult.Error("[red]Valid numbers are 0-36[/]"))
                    );
                    betNums.Add(num);
                    break;
                case "Red/Black":
                    break;

                case "Even/Odd":
                    break;

                case "1st Dozen (1-12)":
                    break;

                case "2nd Dozen (13-24)":
                    break;

                case "3rd Dozen (25-36)":
                    break;
            }
            int target = Random.Shared.Next(0, 36);
            Thread.Sleep(500);
            Console.WriteLine(willWin);
            if (!willWin)
            {
                Thread.Sleep(500);
                Console.WriteLine(willWin);
                while (betNums.Contains(target))
                {
                    target = Random.Shared.Next(0, 36);
                }
            }
            
            _renderer.PlayAnim(target);
            if (betNums.Contains(target))
            {
                int winAmount = PayoutEngine.GetLogPayout(bet, 2.0);
                AnsiConsole.MarkupLine($"[gold1] YOU WIN {winAmount} credits!!![/]");
                _account.Add(winAmount + bet);
                _rigEngine.RecordResult(true);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]You lost {bet} credits[/]");
                _rigEngine.RecordResult(false);
            }
            AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
            Console.ReadKey(true);
            ShowSplash();

        }
    }
}