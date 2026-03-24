namespace FinalProjekt.Core;
using FinalProjekt.Data;
using Spectre.Console;

public class Account
{
    private readonly DBConnector _db = new();
    public double Balance { get; set; }
    public double Deposited { get; set; }
    
    public async Task InitializeAsync()
    {
        var (balance, deposited) = await _db.Load();
        Balance = balance;
        Deposited = deposited;
    }

    public async Task SaveAsync()
    {
        await _db.Save(Balance, Deposited);
    }

    public void Add(int amount)
    {
        Balance += amount;
        _ = SaveAsync(); // Fire and forget save
    }

    public void Deduct(int amount)
    {
        Balance -= amount;
        _ = SaveAsync(); // Fire and forget save
    }

    public void Deposit()
    {
        int amount = AnsiConsole.Prompt(
            new TextPrompt<int>("How much do you want to add? ")
                .Validate(n => n > 0 ? ValidationResult.Success() : ValidationResult.Error("[red]Invalid input[/]"))
        );
        Deposited += amount;
        Balance += amount;
        _ = SaveAsync();
        
        AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
}