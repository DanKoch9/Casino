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
        _renderer.PlayAnim(Random.Shared.Next(0, 37));
    }
}