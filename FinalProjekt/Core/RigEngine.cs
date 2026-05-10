namespace FinalProjekt.Core;

public class RigEngine
{
    private int _losses = 0;
    private double _prob = 0.3;

    public void RecordResult(bool won)
    {
        if (won)
        {
            _losses = 0;
        }
        else
        {
            _losses++;
        }
    }

    public bool IsWinAllowed(Account account)
    {
        double roll = Random.Shared.NextDouble();
        double worth = account.Balance + account.OwnedItems.Sum(i => i.Price);
        double ratio = worth / Math.Clamp(account.Deposited, 1.0, 100000);
        double chance = _prob / Math.Max(0.1, ratio);
        
        chance = chance + (_losses * 0.02);
        chance = Math.Clamp(chance, 0.005, _prob * 2);
        
        if (roll <= chance)
        {
            return true;
        }
        return false;
    }
}