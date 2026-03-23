using FinalProjekt.Core;
using FinalProjekt.UI;
using Spectre.Console;

namespace FinalProjekt.Games;

public class SlotMachine : Game
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
                .Title("Choose a color")
                .AddChoices("Play", "Main Menu")
            );
            switch (choice)
            {
                case "Play":
                    int num1 = Random.Shared.Next(1, 10);
                    int num2 = Random.Shared.Next(1, 10);
                    int num3 = Random.Shared.Next(1, 10);
                    
                    _renderer.AnimateSpin(num1, num2, num3);
                    
                    if (num1 == num2 && num2 == num3)
                    {
                        Console.WriteLine($"JACKPOT!!!");
                        _account.Deposit(1000);
                    }
                    else if (num1 == num2 || num2 == num3 || num1 == num3)
                    {
                        Console.WriteLine($"Big WIN!");
                    }
                    else
                    {
                        Console.WriteLine($"U lost!");
                    }
                    break;
                case "Main Menu":
                    return;
            }
            Console.WriteLine(choice);
            Thread.Sleep(1000);
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