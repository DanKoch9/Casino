using FinalProjekt.Data;
using Spectre.Console;
using System.Diagnostics;
using Stripe;
using Stripe.Checkout;

namespace FinalProjekt.Core;

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
        _ = SaveAsync();
    }

    public void Deduct(int amount)
    {
        Balance -= amount;
        _ = SaveAsync();
    }

    public void Deposit()
    {
        int amount = AnsiConsole.Prompt(
            new TextPrompt<int>("How much do you want to add? (USD)")
                .Validate(n => n > 0 ? ValidationResult.Success() : ValidationResult.Error("[red]Invalid input[/]"))
        );

        StripeService stripe = new StripeService();
        Session session = stripe.CreateCheckoutSession(amount, "user_123");

        amount *= 100;
        
        AnsiConsole.MarkupLine($"\n[gold1]Opening Stripe Checkout for {amount} credits...[/]");
        Process.Start(new ProcessStartInfo(session.Url) { UseShellExecute = true });

        AnsiConsole.Status()
            .Start("[yellow]Waiting for payment confirmation from Stripe...[/]", ctx => 
            {
                while (true)
                {
                    Session updatedSession = stripe.GetSession(session.Id);
                    if (updatedSession.PaymentStatus == "paid")
                    {
                        ctx.Status("[green]Payment Confirmed! Adding credits...[/]");
                        Balance += amount;
                        Deposited += amount;
                        _ = SaveAsync();
                        break;
                    }
                    else if (updatedSession.Status == "expired" || updatedSession.Status == "canceled")
                    {
                        AnsiConsole.MarkupLine("[red]Payment session failed or was canceled.[/]");
                        break;
                    }
                    Thread.Sleep(3000);
                }
            });

        AnsiConsole.MarkupLine($"\n[green]Successfully added {amount} credits! New Balance: {Balance}[/]");
        AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
}