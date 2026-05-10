namespace FinalProjekt.Core;

public record ShopItem(string Name, string Category, int Price);

public static class ShopCatalog
{
    public static readonly ShopItem[] Items =
    {
        new("Casio F-91W",              "Watches",       500),
        new("Tag Heuer Carrera",        "Watches",       12_000),
        new("Rolex Submariner",         "Watches",       60_000),
        new("Patek Philippe Nautilus",  "Watches",       250_000),
        new("Richard Mille RM 11-03",   "Watches",       600_000),

        new("Ralph Lauren Polo",        "Clothes",       800),
        new("Gucci Leather Belt",       "Clothes",       3_000),
        new("Louis Vuitton Jacket",     "Clothes",       18_000),
        new("Brioni Bespoke Suit",      "Clothes",       45_000),
        new("Hermès Birkin Bag",        "Clothes",       90_000),

        new("Prague City Penthouse",    "Real Estate",   500_000),
        new("Malibu Beach House",       "Real Estate",   3_000_000),
        new("Monaco Villa",             "Real Estate",   12_000_000),
        new("Private Island (Bahamas)", "Real Estate",   50_000_000),
        new("Little Saint James Island", "Real Estate",   1_000_000_000),

        new("Sunseeker 50ft",           "Yachts",        800_000),
        new("Feadship 80m Mega Yacht",  "Yachts",        20_000_000),

        new("Cessna Citation M2",       "Private Jets",  2_000_000),
        new("Gulfstream G700",          "Private Jets",  15_000_000),
        new("Boeing BBJ",               "Private Jets",  35_000_000),
    };
}
