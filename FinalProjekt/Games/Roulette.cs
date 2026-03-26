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
        AnsiConsole.Write(new FigletText("Roulette")
            .Color(Color.Red)
        );
    }

    public void Play()
    {
        while (true)
        {
            int bet = AnsiConsole.Prompt(   
                new TextPrompt<int>("How much do you want to bet?")
                    .ValidationErrorMessage("[red]That's not a valid number[/]")
                    .Validate(n =>
                        n > 0 && n <= _account.Balance
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[red]Bet must be between 1 and your balance[/]"))
            );
            List<int> betNums = new List<int>();
            var betType = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What type of [green]bet[/] would you like to place?")
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
                    var betNum = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("What type of [green]bet[/] would you like to place?")
                            .AddChoices(new[]
                            {
                                "Single Number",
                                "Red/Black",
                                "Even/Odd",
                                "1st Dozen (1-12)",
                                "2nd Dozen (13-24)",
                                "3rd Dozen (25-36)"
                            }));
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

        }
    }
}