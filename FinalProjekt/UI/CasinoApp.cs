using FinalProjekt.Games;

namespace FinalProjekt.Core;

public class CasinoApp
{
        private readonly Account account = new Account();
        private Game slots = new SlotMachine();
        
        public void Loop()
        {
                slots.ShowSplash();
                slots.Play();
        }
}