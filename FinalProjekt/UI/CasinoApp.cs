using FinalProjekt.Games;
using Spectre.Console;

namespace FinalProjekt.Core;

public class CasinoApp
{
        private readonly SlotMachine slots;
        private readonly Account account;
        public CasinoApp()
        {
                account = new Account();
                slots = new SlotMachine(account);
        }

        public void ShowSplash()
        {
                Console.Clear();
                AnsiConsole.Write(new FigletText("CASINO")
                        .Color(Color.White)
                );
        }
        
        public void Loop()
        {
                while (true)
                {
                        Console.Clear();
                        ShowSplash();
                        string choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select a game")
                                .AddChoices("Slot Machine", "Exit"));
                        switch (choice)
                        {
                                case "Slot Machine":
                                        slots.ShowSplash();
                                        slots.Play();
                                        break;
                                case "Exit":
                                        return;
                        }
                }
        }
}