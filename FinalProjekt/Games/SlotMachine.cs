using FinalProjekt.Core;
using Spectre.Console;

namespace FinalProjekt.Games;

public class SlotMachine : Game
{
    public string Name => "Slot Machine";

    public void Play()
    {
        Console.WriteLine("Slot Machine");
    }

    public void ShowSplash()
    {
        Console.Clear();

        AnsiConsole.Write(new FigletText("Slot Machine").Centered().Color(Color.Green));
    }
}