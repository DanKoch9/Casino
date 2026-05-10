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
            string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Select an option")
                .AddChoices("Play", "Main Menu")
            );
            switch (choice)
            {
                case "Play":
                    bool willWin = _rigEngine.IsWinAllowed(_account);

                    int bet = AnsiConsole.Prompt(
                        new TextPrompt<int>("How much do you want to bet?")
                            .ValidationErrorMessage("[red]That's not a valid number[/]")
                            .Validate(n =>
                                n > 0 && n <= _account.Balance
                                    ? ValidationResult.Success()
                                    : ValidationResult.Error("[red]Bet must be between 1 and your balance[/]"))
                    );
                    _account.Deduct(bet, "Roulette - Bet");
                    ShowSplash();
                    List<int> betNums = new List<int>();
                    string betType = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("What type of bet would you like to place?")
                            .AddChoices(new[]
                            {
                                "Single Number",
                                "Red/Black",
                                "Even/Odd",
                                "1st Dozen (1-12)",
                                "2nd Dozen (13-24)",
                                "3rd Dozen (25-36)"
                            }));

                    int target = Random.Shared.Next(0, 37);
                    double multiplier = 0;
                    switch (betType)
                    {
                        case "Single Number":
                            int num = AnsiConsole.Prompt(
                                new TextPrompt<int>("What number do you want to bet on?")
                                    .ValidationErrorMessage("[red]That's not a valid number[/]")
                                    .Validate(n => n >= 0 && n <= 36
                                        ? ValidationResult.Success()
                                        : ValidationResult.Error("[red]Valid numbers are 0-36[/]"))
                            );
                            betNums.Add(num);
                            multiplier = 35;
                            break;
                        case "Red/Black":
                            string color = AnsiConsole.Prompt(
                                new SelectionPrompt<string>()
                                    .Title("Select color")
                                    .AddChoices("Red", "Black")
                            );
                            int[] redNums = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
                            int[] blackNums = { 2, 4, 6, 8, 10, 11, 13, 15, 17, 20, 22, 24, 26, 28, 29, 31, 33, 35 };
                            betNums.AddRange(color == "Red" ? redNums : blackNums);
                            multiplier = 1;
                            break;

                        case "Even/Odd":
                            string eo = AnsiConsole.Prompt(
                                new SelectionPrompt<string>()
                                    .Title("Select even or odd")
                                    .AddChoices("Even", "Odd")
                            );
                            for (int i = 1; i <= 36; i++)
                            {
                                if (eo == "Even" && i % 2 == 0)
                                {
                                    betNums.Add(i);
                                }
                                else if (eo == "Odd" && i % 2 != 0)
                                {
                                    betNums.Add(i);
                                }
                            }
                            multiplier = 1;
                            break;

                        case "1st Dozen (1-12)":
                            for (int i = 1; i <= 12; i++)
                            {
                                betNums.Add(i);
                            }
                            multiplier = 2;
                            break;

                        case "2nd Dozen (13-24)":
                            for (int i = 13; i <= 24; i++)
                            {
                                betNums.Add(i);
                            }
                            multiplier = 2;
                            break;

                        case "3rd Dozen (25-36)":
                            for (int i = 25; i <= 36; i++)
                            {
                                betNums.Add(i);
                            }
                            multiplier = 2;
                            break;
                    }

                    if (willWin)
                    {
                        target = betNums[Random.Shared.Next(betNums.Count)];
                    }
                    else
                    {
                        while (betNums.Contains(target))
                        {
                            target = Random.Shared.Next(0, 37);
                        }
                    }

                    _renderer.PlayAnim(target);
                    if (betNums.Contains(target))
                    {
                        int winAmount = (int)(bet * multiplier);
                        AnsiConsole.MarkupLine($"[gold1] YOU WIN {winAmount} credits!!![/]");
                        _account.Add(winAmount + bet, "Roulette - Win");
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
                    break;
                case "Main Menu":
                    return;

            }
        }
    }
}