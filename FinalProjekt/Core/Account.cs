using FinalProjekt.Data;

namespace FinalProjekt.Core;

public record Transaction(string Time, string Description, int Amount);

public class Account
{
    private readonly DBConnector _db = new();
    public double Balance { get; set; }
    public double Deposited { get; set; }
    public List<Transaction> History { get; } = new();
    public List<ShopItem> OwnedItems { get; } = new();
    
    public async Task Initialize()
    {
        var (balance, deposited, properties) = await _db.Load();
        Balance = balance;
        Deposited = deposited;
        OwnedItems.Clear();
        if (properties != null)
        {
            foreach (var item in ShopCatalog.Items)
            {
                if (properties.TryGetValue(item.Name, out bool owned) && owned)
                    OwnedItems.Add(item);
            }
        }
    }

    public string? UserId => _db.UserId;
    public bool IsLoggedIn() => _db.IsLoggedIn();

    public void Logout()
    {
        _db.Logout();
        Balance = 0;
        Deposited = 0;
        History.Clear();
        OwnedItems.Clear();
    }

    private async Task Persist(double value, string type)
    {
        await _db.LogTransaction(value, type);
        var props = ShopCatalog.Items.ToDictionary(i => i.Name, i => OwnedItems.Contains(i));
        await _db.Save(Balance, Deposited, props);
    }

    public async Task Save()
    {
        var props = ShopCatalog.Items.ToDictionary(i => i.Name, i => OwnedItems.Contains(i));
        await _db.Save(Balance, Deposited, props);
    }

    public void BuyItem(ShopItem item)
    {
        Balance -= item.Price;
        OwnedItems.Add(item);
        History.Add(new Transaction(DateTime.Now.ToString("HH:mm:ss"), $"Shop - {item.Name}", -item.Price));
        _ = Persist(-item.Price, $"Shop - {item.Name}");
    }

    public void Add(int amount, string desc = "Win")
    {
        Balance += amount;
        History.Add(new Transaction(DateTime.Now.ToString("HH:mm:ss"), desc, amount));
        _ = Persist(amount, desc);
    }

    public void Deduct(int amount, string desc = "Bet")
    {
        Balance -= amount;
        History.Add(new Transaction(DateTime.Now.ToString("HH:mm:ss"), desc, -amount));
        _ = Persist(-amount, desc);
    }

    public void ConfirmDeposit(int amount)
    {
        Balance += amount;
        Deposited += amount;
        History.Add(new Transaction(DateTime.Now.ToString("HH:mm:ss"), "Deposit", amount));
        _ = Persist(amount, "Deposit");
    }
}