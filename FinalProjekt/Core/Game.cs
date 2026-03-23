namespace FinalProjekt.Core;

public interface Game
{
    public string Name { get; }
    
    void Play();
    void ShowSplash();
}