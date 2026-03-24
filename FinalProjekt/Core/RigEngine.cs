namespace FinalProjekt.Core;

public class RigEngine
{
    private const string DepositedPath = "../../../Data/deposited.txt";
    private int _consecutiveLosses = 0;

    public void RecordResult(bool won)
    {
        if (won) _consecutiveLosses = 0;
        else _consecutiveLosses++;
    }

    public bool IsWinAllowed(Account account)
    {
        double luckRoll = Random.Shared.NextDouble();
        double deposited = 1.0;
        

        deposited = double.Parse(File.ReadAllText(DepositedPath));

        double balanceRatio = account.Balance / Math.Max(1.0, deposited);
        double winChance = 0.1 / Math.Max(0.1, balanceRatio);
        winChance += (_consecutiveLosses * 0.01);
        winChance = Math.Clamp(winChance, 0.005, 0.15);

        return luckRoll <= winChance;
    }
}