namespace FinalProjekt.Core;

public interface IGame
{
    string Name { get; }
    void Play();
    void ShowSplash();
}