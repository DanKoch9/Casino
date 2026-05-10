using Stripe;
using Stripe.Checkout;
using Spectre.Console;
using System.Diagnostics;
using FinalProjekt.Core;

namespace FinalProjekt.Data;

public class StripeService
{
    private readonly string? _secretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");

    public StripeService()
    {
        StripeConfiguration.ApiKey = _secretKey;
    }

    public Session CreateCheckoutSession(long amount, string userId)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = amount * 100,
                        Currency = "czk",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{amount} Casino Credits",
                        },
                    },
                    Quantity = 1,
                },
            },
            Mode = "payment",
            SuccessUrl = "https://casino.danykoch.cz/success/",
            CancelUrl = "https://casino.danykoch.cz/cancel/",
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId }
            }
        };

        SessionService service = new SessionService();
        return service.Create(options);
    }

    public Session GetSession(string sessionId)
    {
        SessionService service = new SessionService();
        return service.Get(sessionId);
    }

    public void ProcessDeposit(Core.Account account)
    {
        int amount = AnsiConsole.Prompt(
            new TextPrompt<int>("How much do you want to add? 1 CZK = 1 credit, minimum 15 CZK: ")
                .Validate(n => n >= 15 && n < 1000000
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]Minimum deposit is 15 CZK, maximum is 999,999 CZK[/]"))
        );

        Session session = CreateCheckoutSession(amount, account.UserId ?? "unknown");

        AnsiConsole.MarkupLine($"\n[gold1]Opening Stripe Checkout for {amount} credits...[/]");
        Process.Start(new ProcessStartInfo(session.Url) { UseShellExecute = true });

        AnsiConsole.Status()
            .Start("[yellow]Waiting for payment confirmation from Stripe...[/]", ctx =>
            {
                while (true)
                {
                    Session updated = GetSession(session.Id);
                    if (updated.PaymentStatus == "paid")
                    {
                        ctx.Status("[green]Payment Confirmed! Adding credits...[/]");
                        account.ConfirmDeposit(amount);
                        break;
                    }
                    if (updated.Status == "expired" || updated.Status == "canceled")
                    {
                        AnsiConsole.MarkupLine("[red]Payment session failed or was canceled.[/]");
                        break;
                    }
                    Thread.Sleep(3000);
                }
            });

        AnsiConsole.MarkupLine($"\n[green]Successfully added {amount} credits! New Balance: {account.Balance}[/]");
        AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
}
