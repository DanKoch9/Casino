using FinalProjekt.Data;

namespace FinalProjekt.Core;

public record Transaction(string Date, string Time, string Description, long Amount);

public class Account
{
    private readonly DBConnector _db = new();
    public double Balance { get; set; }
    public double Deposited { get; set; }
    public List<Transaction> History { get; } = new();
    public List<ShopItem> OwnedItems { get; } = new();
    
    public async Task Initialize()
    {
        (double bal, double dep, Dictionary<string, bool>? props) = await _db.Load();
        Balance = bal;
        Deposited = dep;
        OwnedItems.Clear();
        if (props != null)
        {
            foreach (ShopItem item in ShopCatalog.Items)
            {
                if (props.TryGetValue(item.Name, out bool owned) && owned)
                {
                    OwnedItems.Add(item);
                }
            }
        }

        List<(string time, string type, double val)> dbHistory = await _db.GetTransactions();
        History.Clear();
        foreach ((string time, string type, double val) in dbHistory)
        {
            string t = time;
            string d = "";
            if (DateTime.TryParse(time, out DateTime dt))
            {
                DateTime local = dt.ToLocalTime();
                t = local.ToString("HH:mm:ss");
                d = local.ToString("dd-MM-yyyy");
            }

            History.Add(new Transaction(d, t, type, (int)val));
        }
    }

    public string? UserId => _db.UserId;
    
    public bool IsLoggedIn()
    {
        return _db.IsLoggedIn();
    }

    public void Logout()
    {
        _db.Logout();
        Balance = 0;
        Deposited = 0;
        History.Clear();
        OwnedItems.Clear();
    }

    public async Task Save()
    {
        Dictionary<string, bool> props = ShopCatalog.Items.ToDictionary(i => i.Name, i => OwnedItems.Contains(i));
        await _db.Save(Balance, Deposited, props);
    }

    private async Task Save(double val, string type)
    {
        await _db.LogTransaction(val, type);
        await Save();
    }

    public void BuyItem(ShopItem item)
    {
        Balance -= item.Price;
        OwnedItems.Add(item);
        DateTime now = DateTime.Now;
        History.Add(new Transaction(now.ToString("yyyy-MM-dd"), now.ToString("HH:mm:ss"), $"Shop - {item.Name}", -item.Price));
        _ = Save(-item.Price, $"Shop - {item.Name}");
    }

    public async Task Add(long amt, string desc = "Win")
    {
        Balance += amt;
        DateTime now = DateTime.Now;
        History.Add(new Transaction(now.ToString("yyyy-MM-dd"), now.ToString("HH:mm:ss"), desc, amt));
        await Save(amt, desc);
    }

    public async Task Deduct(long amount, string description = "Bet")
    {
        Balance -= amount;
        DateTime now = DateTime.Now;
        History.Add(new Transaction(now.ToString("yyyy-MM-dd"), now.ToString("HH:mm:ss"), description, -amount));
        await Save(-amount, description);
    }

    public async Task ConfirmDeposit(long amt)
    {
        Balance += amt;
        Deposited += amt;
        DateTime now = DateTime.Now;
        History.Add(new Transaction(now.ToString("yyyy-MM-dd"), now.ToString("HH:mm:ss"), "Deposit", amt));
        await Save(amt, "Deposit");
    }
}