using FinalProjekt.Core;
using Spectre.Console;

namespace FinalProjekt.Games;

public class SlotMachine : Game
{
    public string Name => "Slot Machine";

    public void Play()
    {
        while (true)
        {
            string choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Choose a color")
                .AddChoices("Play", "Add Credits", "Main Menu"));
            switch (choice)
            {
                case "Play":
                    break;
                case "Add Credits":
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

        AnsiConsole.Write(new FigletText("Slot Machine").Color(Color.Green));
    }
}