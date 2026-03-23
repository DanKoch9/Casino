using FinalProjekt.Core;
using Spectre.Console;

namespace FinalProjekt.Games;

public class SlotMachine : Game
{
    public string Name => "Slot Machine";
    Account account = new Account();
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
                    int number = Random.Shared.Next(1, 7);
                    if (number == 6)
                    {
                        account.Deposit(1000);
                        Console.WriteLine("You won 1000");
                    }
                    else
                    {
                        account.Withdraw(1000);
                        Console.WriteLine("You lost 1000");
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