using FinalProjekt.Games;

namespace FinalProjekt.Core;

public class CasinoApp
{
        private Game slots = new SlotMachine();
        
        public void Loop()
        {
                slots.ShowSplash();
                slots.Play();
        }
}