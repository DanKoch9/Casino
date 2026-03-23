using FinalProjekt.Games;

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
        
        public void Loop()
        {
                slots.ShowSplash();
                slots.Play();
        }
}