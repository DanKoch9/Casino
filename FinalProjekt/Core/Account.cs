namespace FinalProjekt.Core;

public class Account
{
    public double Balance { get; set; }
    private const string FilePath = "../../../Data/balance.txt";
    
    
    public Account()
    {
        if (File.Exists(FilePath))
        {
            Balance = double.Parse(File.ReadAllText(FilePath));
            
        }
        else
        {
            Balance = 1000;
            Save();
        }
    }
    public void Save()
    {
        File.WriteAllText(FilePath, Balance.ToString());
    }
    
    public void Deposit(int amount)
    {
        Balance += amount;
        Save();
    }
    public void Withdraw(int amount)
    {
        Balance -= amount;
        Save();
    }
}