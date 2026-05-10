using FinalProjekt.Core;
using Spectre.Console;

namespace FinalProjekt.Games;

public class SportsBetting : IGame
{
    private readonly Account _account;
    public string Name => "Sports Betting";
    private readonly RigEngine _rigEngine = new RigEngine();

    private readonly string[] _teams =
    {
        "Boston Bruins", "Buffalo Sabres", "Detroit Red Wings", "Florida Panthers",
        "Montreal Canadiens", "Ottawa Senators", "Tampa Bay Lightning", "Toronto Maple Leafs",
        "Carolina Hurricanes", "Columbus Blue Jackets", "New Jersey Devils", "New York Islanders",
        "New York Rangers", "Philadelphia Flyers", "Pittsburgh Penguins", "Washington Capitals",
        "Chicago Blackhawks", "Colorado Avalanche", "Dallas Stars", "Minnesota Wild",
        "Nashville Predators", "St. Louis Blues", "Utah Hockey Club", "Winnipeg Jets",
        "Anaheim Ducks", "Calgary Flames", "Edmonton Oilers", "Los Angeles Kings",
        "San Jose Sharks", "Seattle Kraken", "Vancouver Canucks", "Vegas Golden Knights"
    };

    public SportsBetting(Account account)
    {
        _account = account;
    }

    public void ShowSplash()
    {
        Console.Clear();
        AnsiConsole.Write(new FigletText("Sports Betting")
            .Color(Color.Blue)
        );
        AnsiConsole.MarkupLine($"\n[gold1]You have {_account.Balance} credits[/]\n");
    }

    private int SimGoals()
    {
        int[] weights = { 2, 8, 16, 20, 18, 14, 10, 6, 4, 2 };
        int roll = Random.Shared.Next(100);
        int cum = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            cum += weights[i];
            if (roll < cum) return i;
        }
        return 3;
    }

    private void SimulateMatch(string home, string away, int homeGoals, int awayGoals)
    {
        int[] homePeriods = new int[3];
        int[] awayPeriods = new int[3];
        for (int i = 0; i < homeGoals; i++) homePeriods[Random.Shared.Next(3)]++;
        for (int i = 0; i < awayGoals; i++) awayPeriods[Random.Shared.Next(3)]++;

        AnsiConsole.MarkupLine($"\n[bold white]--- GAME START ---[/]");
        AnsiConsole.MarkupLine($"  [cyan]{away}[/] @ [yellow]{home}[/]\n");
        Thread.Sleep(900);

        string[] periods = { "1st Period", "2nd Period", "3rd Period" };
        int hr = 0, ar = 0;
        for (int p = 0; p < 3; p++)
        {
            AnsiConsole.MarkupLine($"[grey]{periods[p]}...[/]");
            Thread.Sleep(1400);
            hr += homePeriods[p];
            ar += awayPeriods[p];
            AnsiConsole.MarkupLine($"  [yellow]{home}[/] [bold white]{hr}[/] - [bold white]{ar}[/] [cyan]{away}[/]");
            Thread.Sleep(700);
        }

        AnsiConsole.MarkupLine($"\n[bold white]--- FINAL ---[/]");
        AnsiConsole.MarkupLine($"  [yellow]{home}[/] [bold gold1]{homeGoals}[/] - [bold gold1]{awayGoals}[/] [cyan]{away}[/]");
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

                    var shuffled = _teams.OrderBy(_ => Random.Shared.Next()).ToArray();
                    string home = shuffled[0];
                    string away = shuffled[1];

                    bool homeIsFav = Random.Shared.Next(2) == 0;
                    string fav = homeIsFav ? home : away;
                    string dog = homeIsFav ? away : home;
                    double favOdds = Math.Round(1.4 + Random.Shared.NextDouble() * 0.4, 1);
                    double dogOdds = Math.Round(2.0 + Random.Shared.NextDouble() * 0.8, 1);

                    AnsiConsole.MarkupLine($"\n[bold]Tonight's matchup:[/]");
                    AnsiConsole.MarkupLine($"  [yellow]{home}[/] (home) vs [cyan]{away}[/] (away)");
                    AnsiConsole.MarkupLine($"\n  Favorite: [green]{fav}[/], pays [green]{favOdds}x[/]");
                    AnsiConsole.MarkupLine($"  Underdog: [red]{dog}[/], pays [red]{dogOdds}x[/]\n");

                    int bet = AnsiConsole.Prompt(
                        new TextPrompt<int>("How much do you want to bet?")
                            .ValidationErrorMessage("[red]That's not a valid number[/]")
                            .Validate(n => n > 0 && n <= _account.Balance
                                ? ValidationResult.Success()
                                : ValidationResult.Error("[red]Bet must be between 1 and your balance[/]"))
                    );

                    var betType = AnsiConsole.Prompt(new SelectionPrompt<string>()
                        .Title("What do you want to bet on?")
                        .AddChoices("Winner", "Exact Score (18x)")
                    );

                    bool willWin = _rigEngine.IsWinAllowed(_account);
                    _account.Deduct(bet, "Sports Betting - Bet");

                    int homeGoals, awayGoals;
                    string pickedTeam = "";
                    int guessHome = 0, guessAway = 0;
                    double multiplier = 1;

                    if (betType == "Winner")
                    {
                        pickedTeam = AnsiConsole.Prompt(new SelectionPrompt<string>()
                            .Title("Pick the winner")
                            .AddChoices(home, away)
                        );
                        multiplier = pickedTeam == fav ? favOdds : dogOdds;

                        homeGoals = SimGoals();
                        awayGoals = SimGoals();
                        if (homeGoals == awayGoals) awayGoals++;

                        if (willWin)
                        {
                            if (pickedTeam == home && homeGoals <= awayGoals)
                                homeGoals = awayGoals + Random.Shared.Next(1, 4);
                            else if (pickedTeam == away && awayGoals <= homeGoals)
                                awayGoals = homeGoals + Random.Shared.Next(1, 4);
                        }
                        else
                        {
                            if (pickedTeam == home && homeGoals > awayGoals)
                                awayGoals = homeGoals + Random.Shared.Next(1, 4);
                            else if (pickedTeam == away && awayGoals > homeGoals)
                                homeGoals = awayGoals + Random.Shared.Next(1, 4);
                        }
                    }
                    else
                    {
                        multiplier = 18;
                        guessHome = AnsiConsole.Prompt(
                            new TextPrompt<int>($"Goals for [yellow]{home}[/]?")
                                .ValidationErrorMessage("[red]Invalid[/]")
                                .Validate(n => n >= 0 && n <= 15 ? ValidationResult.Success() : ValidationResult.Error("[red]0-15[/]"))
                        );
                        guessAway = AnsiConsole.Prompt(
                            new TextPrompt<int>($"Goals for [cyan]{away}[/]?")
                                .ValidationErrorMessage("[red]Invalid[/]")
                                .Validate(n => n >= 0 && n <= 15 ? ValidationResult.Success() : ValidationResult.Error("[red]0-15[/]"))
                        );

                        if (willWin)
                        {
                            homeGoals = guessHome;
                            awayGoals = guessAway;
                        }
                        else
                        {
                            homeGoals = SimGoals();
                            awayGoals = SimGoals();
                            while (homeGoals == guessHome && awayGoals == guessAway)
                            {
                                homeGoals = SimGoals();
                                awayGoals = SimGoals();
                            }
                            if (homeGoals == awayGoals) awayGoals++;
                        }
                    }

                    ShowSplash();
                    SimulateMatch(home, away, homeGoals, awayGoals);

                    bool won = betType == "Winner"
                        ? (pickedTeam == home ? homeGoals > awayGoals : awayGoals > homeGoals)
                        : (homeGoals == guessHome && awayGoals == guessAway);

                    if (won)
                    {
                        int winAmount = (int)(bet * multiplier);
                        AnsiConsole.MarkupLine($"\n[gold1]YOU WIN! +{winAmount} credits[/]");
                        _account.Add(winAmount + bet, "Sports Betting - Win");
                        _rigEngine.RecordResult(true);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"\n[red]You lost {bet} credits[/]");
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
