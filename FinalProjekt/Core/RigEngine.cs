namespace FinalProjekt.Core;

public class RigEngine
{
    private const string DepositedPath = "../../../Data/deposited.txt";
    private int _consecutiveLosses = 0;

    //0.01 je extreme malo, 0.1 je malo, 0.5 je pohoda, 0.9 skoro zarucenej win.
    private double _winProbability = 0.3;

    public void RecordResult(bool won)
    {
        if (won) _consecutiveLosses = 0;
        else _consecutiveLosses++;
    }

    public bool IsWinAllowed(Account account)
    {
        double luckRoll = Random.Shared.NextDouble();

        double balanceRatio = account.Balance / Math.Max(1.0, account.Deposited);
        
        double winChance = _winProbability / Math.Max(0.1, balanceRatio);
        
        winChance += (_consecutiveLosses * 0.01);
        
        winChance = Math.Clamp(winChance, 0.005, _winProbability * 2);

        return luckRoll <= winChance;
    }
}