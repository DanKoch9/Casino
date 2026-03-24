namespace FinalProjekt.Core;
using Spectre.Console;
public class Account
{
    public double Balance { get; set; }
    private double _deposited;
    private const string BalancePath = "../../../Data/balance.txt";
    private const string DepositedPath = "../../../Data/deposited.txt";
    
    public Account()
    {
        if (File.Exists(BalancePath))
        {
            Balance = double.Parse(File.ReadAllText(BalancePath));
            
        }
        else
        {
            Balance = 1000;
            SaveBalance();
        }
        if (File.Exists(DepositedPath))
        {
            _deposited = double.Parse(File.ReadAllText(DepositedPath));
        }
        else
        {
            _deposited = 0;
            SaveDeposit();
        }
    }
    public void SaveBalance()
    {
        File.WriteAllText(BalancePath, Balance.ToString());
    }

    public void SaveDeposit()
    {
        File.WriteAllText(DepositedPath, _deposited.ToString());
    }
    public void Add(int amount)
    {
        Balance += amount;
        SaveBalance();
    }
    public void Deduct(int amount)
    {
        Balance -= amount;
        SaveBalance();
    }

    public void Deposit()
    {
        int amount = AnsiConsole.Prompt(
            new TextPrompt<int>("How much do you want to add? ")
                .Validate(n => n > 0 ? ValidationResult.Success() : ValidationResult.Error("[red]Invalid input[/]"))
        );
        _deposited += amount;
        Balance += amount;
        SaveBalance();
        SaveDeposit();
        AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
}