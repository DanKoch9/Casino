using Stripe;
using Stripe.Checkout;
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
}
