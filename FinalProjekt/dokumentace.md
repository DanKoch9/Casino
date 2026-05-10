# FinalProjekt — Úplná dokumentace kódu

Konzolová kasinová aplikace napsaná v C# (.NET 10). Používá Spectre.Console pro terminálové UI, PocketBase jako backendovou databázi s autentizací přes Google OAuth a Stripe pro zpracování skutečných plateb. Jazyk UI je čeština.

---

## Obsah

1. [Program](#1-program)
2. [Transaction (záznam)](#2-transaction-záznam)
3. [Account](#3-account)
4. [PayoutEngine](#4-payoutengine)
5. [RigEngine](#5-rigengine)
6. [ShopItem (záznam)](#6-shopitem-záznam)
7. [ShopCatalog](#7-shopcatalog)
8. [IGame (rozhraní)](#8-igame-rozhraní)
9. [IRenderer (rozhraní)](#9-irenderer-rozhraní)
10. [CasinoApp](#10-casinoapp)
11. [StatsMenu](#11-statsmenu)
12. [HistoryMenu](#12-historymenu)
13. [ShopMenu](#13-shopmenu)
14. [SlotRenderer](#14-slotrenderer)
15. [RouletteRenderer](#15-rouletterenderer)
16. [SlotMachine](#16-slotmachine)
17. [NumberGuess](#17-numberguess)
18. [Roulette](#18-roulette)
19. [SportsBetting](#19-sportsbetting)
20. [DBConnector](#20-dbconnector)
21. [StripeService](#21-stripeservice)

---

## 1. `Program`

**Soubor:** `Core/Program.cs`  
**Jmenný prostor:** `FinalProjekt.Core`

Vstupní bod aplikace. Třída obsahuje pouze metodu `Main` a zajišťuje spuštění celé aplikace.

### `static async Task Main()`

Asynchronní vstupní bod, který zavolá runtime .NET při spuštění programu.

**Co dělá krok po kroku:**

1. **Načte soubor `.env`** — zavolá `DotNetEnv.Env.TraversePath().Load()`. Tato metoda prochází adresářovou strukturou nahoru, hledá soubor `.env` a načte jeho hodnoty jako proměnné prostředí. Aplikace vyžaduje `PB_BASE_URL`, `STRIPE_SECRET_KEY` a `STRIPE_PUBLISHABLE_KEY`.

2. **Vytvoří `CasinoApp`** — vytvoří hlavní objekt aplikace, který interně vytvoří `Account`, všechny čtyři herní instance a tři objekty menu (`ShopMenu`, `StatsMenu`, `HistoryMenu`).

3. **Čeká na `app.Initialize()`** — toto asynchronní volání načte hráčův zůstatek a vlastněné předměty z PocketBase backendu. Při prvním spuštění spustí Google OAuth, pokud neexistuje žádný uložený token.

4. **Zavolá `app.Loop()`** — vstoupí do synchronní hlavní smyčky, která řídí navigaci v menu, dokud uživatel nezvolí Konec.

---

## 2. `Transaction` (záznam)

**Soubor:** `Core/Account.cs`  
**Jmenný prostor:** `FinalProjekt.Core`

```csharp
public record Transaction(string Date, string Time, string Description, int Amount);
```

Lehký neměnný datový typ reprezentující jednu finanční událost v paměti sezení. Záznamy (`record`) v C# automaticky generují hodnotovou rovnost, `ToString` a destruktor.

### Vlastnosti

| Vlastnost | Typ | Popis |
|---|---|---|
| `Date` | `string` | Datum ve formátu `"dd-MM-yyyy"` v okamžiku vytvoření transakce |
| `Time` | `string` | Časové razítko ve formátu `"HH:mm:ss"` v okamžiku vytvoření transakce |
| `Description` | `string` | Lidsky čitelný popis (např. `"Slot Machine - Bet"`, `"Deposit"`, `"Shop - Rolex Submariner"`) |
| `Amount` | `int` | Kladné číslo pro příjem kreditů (výhry, vklady), záporné pro výdaje (sázky, nákupy v obchodě) |

Tento záznam je uchováván pouze v paměti v `Account.History` po dobu sezení. Trvalý záznam v databázi PocketBase spravuje `DBConnector.LogTransaction`.

---

## 3. `Account`

**Soubor:** `Core/Account.cs`  
**Jmenný prostor:** `FinalProjekt.Core`

Ústřední modelový objekt, který sleduje finanční stav hráče po dobu sezení. Všechny herní třídy a třídy menu sdílejí jeden odkaz na instanci `Account`, takže čtou a zapisují do stejného zůstatku. Každá metoda, která mění stav, aktualizuje data v paměti a zároveň asynchronně spustí uložení do databáze.

### Pole a vlastnosti

| Člen | Typ | Popis |
|---|---|---|
| `_db` | `DBConnector` | Soukromá instance konektoru databáze. Veškeré I/O s backendem prochází přes ni. |
| `Balance` | `double` | Aktuální zůstatek kreditů v paměti. Modifikují ho `Add`, `Deduct`, `BuyItem` a `ConfirmDeposit`. |
| `Deposited` | `double` | Celková suma skutečných vkladů provedených přes Stripe. Nikdy neklesá. `RigEngine` ji používá k výpočtu poměru hráčova zůstatku vůči vloženým penězům. |
| `History` | `List<Transaction>` | Záznam v paměti o všech pohybech kreditů v tomto sezení. Zobrazuje `HistoryMenu`. |
| `OwnedItems` | `List<ShopItem>` | Všechny předměty z obchodu, které hráč vlastní. Naplní se z databáze při `Initialize` a aktualizuje se přes `BuyItem`. |

### `async Task Initialize()`

Načte hráčova data z databáze. Volá se jednou při startu přes `CasinoApp.Initialize`.

1. Zavolá `_db.Load()`, které nejprve provede autentizaci (OAuth nebo uložený token) a poté načte záznam z kolekce `casino` v PocketBase pro tohoto uživatele. Vrátí trojici `(balance, deposited, properties)`. Pokud záznam ještě neexistuje, PocketBase ho vytvoří se startovním zůstatkem 1000 a vrátí výchozí hodnoty.
2. Nastaví `Balance` a `Deposited` z vrácených hodnot.
3. Vymaže `OwnedItems` a znovu ho naplní tak, že projde každý předmět v `ShopCatalog.Items` a zkontroluje, zda ho slovník `properties` (klíčovaný názvem předmětu) označuje jako vlastněný.

**Proč mazat a znovu naplňovat:** To umožňuje bezpečně volat metodu vícekrát — například když se uživatel odhlásí a přihlásí se nový uživatel během stejného procesu.

### `string? UserId`

Vlastnost jen pro čtení, která deleguje na `_db.UserId`. Vrátí ID záznamu uživatele v PocketBase nebo `null`, pokud není přihlášen. Používá ho `StripeService.CreateCheckoutSession`, aby Stripe session označil identitou hráče.

### `bool IsLoggedIn()`

Deleguje na `_db.IsLoggedIn()`, které zkontroluje, zda existuje platný token (v paměti nebo v souboru `token.dat`). `CasinoApp.Loop` tuto metodu volá při každé iteraci, aby rozhodlo, zda vynutit opětovnou autentizaci.

### `void Logout()`

Vymaže veškerý stav v paměti a smaže soubor s uloženým tokenem.

1. Zavolá `_db.Logout()` — nastaví tokeny konektoru na null a smaže `token.dat`.
2. Resetuje `Balance` a `Deposited` na `0`.
3. Vymaže `History` a `OwnedItems`.

Po tomto volání `IsLoggedIn()` vrátí false a další iterace hlavní smyčky spustí opětovnou autentizaci.

### `private async Task Persist(double value, string type)`

Interní pomocník, který provede dva zápisy do databáze (z pohledu volajícího fire-and-forget):

1. **Zaznamená transakci** — zavolá `_db.LogTransaction(value, type)`, čímž zapíše řádek do kolekce `transactions` v PocketBase.
2. **Uloží celý stav** — sestaví slovník, kde klíčem je název každého předmětu a hodnota říká, zda ho hráč vlastní; poté zavolá `_db.Save(Balance, Deposited, props)` a PATCHne hlavní záznam `casino`.

Tato metoda je volána každou mutující metodou jako `_ = Persist(...)`, tedy volající na ni nečeká (`await`). Výsledek je záměrně zahozen — herní smyčka se nikdy nezastaví kvůli zápisu do databáze.

### `async Task Save()`

Veřejná varianta logiky persistence, která uloží pouze hlavní záznam `casino` bez zaznamenání transakce. Určena pro případy, kdy chcete synchronizovat stav bez vytváření záznamu transakce.

### `void BuyItem(ShopItem item)`

Zpracuje nákup v obchodě.

1. Odečte `item.Price` od `Balance`.
2. Přidá předmět do `OwnedItems`.
3. Přidá `Transaction` do `History` se zápornou částkou.
4. Spustí `Persist(-item.Price, $"Shop - {item.Name}")` jako fire-and-forget.

Poznámka: tato metoda **nekontroluje**, zda má hráč dostatek kreditů — toto ověření provádí `ShopMenu.Show()` před voláním této metody.

### `void Add(int amount, string desc = "Win")`

Přičte `amount` k zůstatku.

1. Přidá `amount` k `Balance`.
2. Přidá kladnou `Transaction` do `History`.
3. Spustí `Persist(amount, desc)` jako fire-and-forget.

Používají hry při výhře. Původní sázka je přidána zpět nad rámec `winAmount` volajícím hrou (např. `_account.Add(winAmount + bet, "Slot Machine - Jackpot")`), takže odečtená sázka je implicitně vrácena jako součást výhry.

### `void Deduct(int amount, string desc = "Bet")`

Odečte `amount` od zůstatku.

1. Odečte `amount` od `Balance`.
2. Přidá zápornou `Transaction` do `History` (uloženo jako `-amount`).
3. Spustí `Persist(-amount, desc)` jako fire-and-forget.

Volají všechny hry na začátku každého kola před určením výsledku.

### `void ConfirmDeposit(int amount)`

Volá `StripeService.ProcessDeposit` po potvrzení platby ze strany Stripe.

1. Přidá `amount` do `Balance` i `Deposited`.
2. Přidá kladnou `Transaction` označenou `"Deposit"`.
3. Spustí `Persist(amount, "Deposit")` jako fire-and-forget.

`Deposited` je záměrně sledováno odděleně od `Balance`, aby `RigEngine` mohl vypočítat poměr hráčovy aktuální hodnoty k tomu, co původně vložil, a umožnit tak dynamické nastavení pravděpodobnosti výhry.

---

## 4. `PayoutEngine`

**Soubor:** `Core/PayoutEngine.cs`  
**Jmenný prostor:** `FinalProjekt.Core`

Statická pomocná třída s jedinou metodou. Poskytuje vzorec pro výplatu používaný hrami. Záměrem je, aby vyšší sázky škálovaly lépe než lineárně — sázka 1000 kreditů vyplatí více než 10× sázku 100 kreditů.

### `static int GetLogPayout(int bet, double baseMultiplier)`

Vypočítá množství kreditů, které hráč dostane při výhře.

**Vzorec:**

```
logBonus        = log10(max(1, bet)) * 1.2
finalMultiplier = baseMultiplier + logBonus
výplata         = zaokrouhlit(bet * finalMultiplier)
```

**Parametry:**
- `bet` — vsazená částka.
- `baseMultiplier` — základní faktor návratnosti pro tento typ výhry. Příklady: `5.0` pro jackpot na automatech (všechny tři stejné), `2.0` pro částečnou výhru na automatech (dvě stejné), `35` pro přímý tip v ruletě.

**Proč logaritmické škálování:** Plochý multiplikátor by triviálně zhodnocoval vysoké sázky. Logaritmický faktor odměňuje riskování, ale zmenšuje se při extrémních hodnotách — např. při sázce 1000 přidá `log10(1000)*1.2 = 3.6` k multiplikátoru; při sázce 10000 jen `4.8`.

**Důležité:** Vrácená hodnota je **pouze čistá výhra** — původní sázka není zahrnuta. Volající ji přidají zpět voláním `account.Add(winAmount + bet, ...)`.

---

## 5. `RigEngine`

**Soubor:** `Core/RigEngine.cs`  
**Jmenný prostor:** `FinalProjekt.Core`

Stavová třída, která řídí, zda dané kolo skončí výhrou nebo prohrou. Klíčový princip je, že kasino si udržuje výhodu, ale engine zmírňuje prohry při sérii proher a zpřísňuje, když je hráč v plusu. Jedna sdílená instance `RigEngine` je vytvořena v `CasinoApp` a předána všem hrám, takže série proher přechází mezi hrami.

### Pole

| Pole | Typ | Popis |
|---|---|---|
| `_losses` | `int` | Čítač inkrementovaný při každé prohře, resetovaný na 0 při výhře. Řídí mechaniku kompenzace série. |
| `_prob` | `double` | Základní pravděpodobnost výhry nastavená na `0.3` (30 %). Toto je strop pro hráče, který je přesně na nule. |

### `void RecordResult(bool won)`

Aktualizuje čítač série po každém kole.

- Pokud `won` je `true`: resetuje `_consecutiveLosses` na 0.
- Pokud `won` je `false`: inkrementuje `_consecutiveLosses`.

Hry musí tuto metodu volat po výsledku každého kola, aby stav enginu zůstal přesný.

### `bool IsWinAllowed(Account account)`

Hlavní rozhodovací funkce. Vrátí `true`, pokud by toto kolo mělo být výhra, `false` pokud prohra. Hry tuto metodu volají **před** generováním vizuálů, aby mohl být výsledek zmanipulován dříve, než ho hráč uvidí.

**Výpočet krok po kroku:**

1. **Hod** — vygeneruje náhodné `double` v `[0, 1)`.
2. **Výpočet čisté hodnoty** — `account.Balance + součet cen všech vlastněných předmětů`. Měří celkovou hodnotu hráče včetně majetku.
3. **Výpočet poměru zůstatku** — `čistáHodnota / clamp(account.Deposited, 1, 100000)`. Kolikrát více má hráč oproti tomu, co vložil. Poměr 1.0 znamená vyrovnaný stav; 2.0 znamená zdvojnásobení investice.
4. **Výpočet šance výhry** — `_prob / max(0.1, balanceRatio)`. Šance výhry je nepřímo úměrná poměru zůstatku: čím je hráč ziskovější, tím nižší je šance. Při poměru 2.0 klesne šance výhry na 15 %; při 0.5 stoupne na 60 %.
5. **Přidání bonusu za sérii** — `winChance += _losses * 0.02`. Každá po sobě jdoucí prohra přidá 2 procentní body k šanci výhry jako mechanismus soucitu.
6. **Ořezání** — finální šance výhry je ořezána na `[0.005, _prob * 2]` = `[0.5 %, 60 %]`. Spodní mez zabraňuje zániku výhody kasina; horní mez zabraňuje bonusu za sérii triviálně zjednodušit hru.
7. **Rozhodnutí** — vrátí `luckRoll <= winChance`. Pokud náhodný hod spadne do vypočítaného okna, kolo je výhra.

---

## 6. `ShopItem` (záznam)

**Soubor:** `Core/Shop.cs`  
**Jmenný prostor:** `FinalProjekt.Core`

```csharp
public record ShopItem(string Name, string Category, int Price);
```

Neměnný hodnotový typ reprezentující jeden koupitelný předmět. Používá se jako klíč při kontrole `OwnedItems` (rovnost záznamu porovnává všechna tři pole hodnotou).

| Vlastnost | Popis |
|---|---|
| `Name` | Jedinečný zobrazovaný název. Používá se také jako klíč slovníku v poli `properties` v PocketBase. |
| `Category` | Jedna z hodnot: `"Watches"`, `"Clothes"`, `"Real Estate"`, `"Yachts"`, `"Private Jets"`. Používá `ShopMenu` k seskupování předmětů. |
| `Price` | Cena v kreditech nutná ke koupi. Používá také `RigEngine` při výpočtu čisté hodnoty. |

---

## 7. `ShopCatalog`

**Soubor:** `Core/Shop.cs`  
**Jmenný prostor:** `FinalProjekt.Core`

Statická třída uchovávající celý katalog předmětů jako pevné pole. Nic v kódu toto pole za běhu nemodifikuje.

### `static readonly ShopItem[] Items`

Dvacet předmětů v pěti kategoriích, seřazeno od nejlevnějšího po nejdražší v každé kategorii:

| Kategorie | Předměty | Cenové rozpětí |
|---|---|---|
| Watches (Hodinky) | Casio F-91W → Richard Mille RM 11-03 | 500 – 600 000 kreditů |
| Clothes (Oblečení) | Ralph Lauren Polo → Hermès Birkin Bag | 800 – 90 000 kreditů |
| Real Estate (Nemovitosti) | Prague City Penthouse → Little Saint James Island | 500 000 – 1 000 000 000 kreditů |
| Yachts (Jachty) | Sunseeker 50ft → Feadship 80m Mega Yacht | 800 000 – 20 000 000 kreditů |
| Private Jets (Soukromá letadla) | Cessna Citation M2 → Boeing BBJ | 2 000 000 – 35 000 000 kreditů |

Předmět Little Saint James Island za 1 miliardu kreditů je v podstatě prestižní položka — existuje jako aspirační strop, ne jako praktický nákup.

---

## 8. `IGame` (rozhraní)

**Soubor:** `Core/IGame.cs`  
**Jmenný prostor:** `FinalProjekt.Core`

Kontrakt, který musí implementovat všechny herní třídy. `CasinoApp` ukládá hry jako `List<IGame>` a volá je polymorfně.

### Členy

| Člen | Popis |
|---|---|
| `string Name { get; }` | Zobrazovaný název zobrazený v menu výběru her (např. `"Slot Machine"`, `"Roulette"`). |
| `void Play()` | Spustí hlavní smyčku hry. Volá `CasinoApp.GamesMenu` po `ShowSplash`. Obsahuje celý cyklus hraj/sázej/výsledek a vrátí se volajícímu, když hráč zvolí „Hlavní menu". |
| `void ShowSplash()` | Vymaže terminál a zobrazí záhlaví hry (Figlet název + aktuální zůstatek). Volá se před `Play()` a také mezi koly pro obnovení zobrazení. |

---

## 9. `IRenderer` (rozhraní)

**Soubor:** `Core/IRenderer.cs`  
**Jmenný prostor:** `FinalProjekt.Core`

Kontrakt pro animační třídy, které zobrazují vizuální výsledek kola hry pomocí živého renderu Spectre.Console (`Live`).

### Členy

| Člen | Popis |
|---|---|
| `void PlayAnim(params object[] args)` | Spustí blokující terminálovou animaci. Podpis `params object[]` umožňuje každému rendereru přijímat různé sady argumentů, aniž by rozhraní znalo detaily. `SlotRenderer` přijímá tři inty (hodnoty tří válců); `RouletteRenderer` přijímá jeden int (číslo výherní kapsy). |

---

## 10. `CasinoApp`

**Soubor:** `UI/CasinoApp.cs`  
**Jmenný prostor:** `FinalProjekt.UI`

Hlavní orchestrátor. Vlastní všechny subsystémy (účet, hry, menu) a řídí hlavní navigační smyčku. Toto je jediná třída, která zná strukturu celé aplikace na nejvyšší úrovni.

### Pole

| Pole | Typ | Popis |
|---|---|---|
| `_account` | `Account` | Jediná sdílená instance účtu předávaná všem hrám a menu. |
| `_games` | `List<IGame>` | Seřazený seznam všech čtyř herních instancí. Pořadí určuje pořadí zobrazení v menu her. |
| `_shop` | `ShopMenu` | Obsluha UI obchodu. |
| `_stats` | `StatsMenu` | Obsluha UI statistik. |
| `_history` | `HistoryMenu` | Obsluha UI historie transakcí. |

### `CasinoApp()` (konstruktor)

Vytvoří a propojí všechny závislosti:
- Vytvoří instanci `Account`.
- Vytvoří jednu sdílenou instanci `RigEngine`.
- Vytvoří jednu instanci `SlotMachine`, `NumberGuess`, `Roulette` a `SportsBetting`, každé dostane sdílený `Account` i sdílený `RigEngine`.
- Vytvoří `ShopMenu`, `StatsMenu` a `HistoryMenu`, každý dostane tentýž `Account`.

### `async Task Initialize()`

Čeká na `_account.Initialize()`, které provede autentizaci a načte hráčova data z databáze. Volá se jednou v `Program.Main` před vstupem do synchronní smyčky.

### `void ShowSplash()`

Vymaže terminál, zobrazí bílý Figlet banner „CASINO" a aktuální zůstatek hráče zlatou barvou. Volá se na začátku každé iterace hlavního menu pro obnovení zobrazení.

### `void Loop()`

Nekonečná hlavní smyčka. Běží, dokud uživatel nezvolí „Konec".

**Každá iterace:**

1. **Kontrola přihlášení** — pokud `_account.IsLoggedIn()` vrátí false (např. po odhlášení nebo při prvním spuštění bez uloženého tokenu), vymaže obrazovku, zobrazí banner, blokujícím způsobem zavolá `_account.Initialize().Wait()` a pokud stále není přihlášen, opakuje iteraci.
2. **Zobrazí splash** — zobrazí hlavní záhlaví s aktuálním zůstatkem.
3. **Menu nejvyšší úrovně** — zobrazí čtyři volby: `"Games"`, `"Finances"`, `"Logout"`, `"Exit"`.
   - `"Exit"` — vrátí se z `Loop()`, ukončí proces.
   - `"Logout"` — zavolá `_account.Logout()` a pokračuje smyčkou, která při další iteraci spustí opětovnou autentizaci.
   - `"Games"` — zavolá `GamesMenu()`.
   - `"Finances"` — zavolá `FinancesMenu()`.

### `private void GamesMenu()`

Dílčí smyčka pro sekci her.

**Každá iterace:**
1. Zobrazí splash.
2. Zobrazí výběrový prompt se jménem každé hry plus `"Back"`.
3. Pokud `"Back"`, vrátí se.
4. Najde odpovídající `IGame` podle jména, zavolá `game.ShowSplash()` a pak `game.Play()`. Řízení se vrátí sem, když hráč hru opustí.

### `private void FinancesMenu()`

Dílčí smyčka pro sekci financí.

**Každá iterace:**
1. Zobrazí splash.
2. Zobrazí: `"Add Credits"`, `"Shop"`, `"Stats"`, `"Transaction History"`, `"Back"`.
   - `"Back"` — vrátí se.
   - `"Add Credits"` — vytvoří nový `StripeService` a zavolá `ProcessDeposit(_account)`.
   - `"Shop"` — zavolá `_shop.Show()`.
   - `"Stats"` — zavolá `_stats.Show()`.
   - `"Transaction History"` — zavolá `_history.Show()`.

---

## 11. `StatsMenu`

**Soubor:** `UI/StatsMenu.cs`  
**Jmenný prostor:** `FinalProjekt.UI`

Zobrazí statistický přehled hráčova sezení a kolekci vlastněných předmětů.

### `StatsMenu(Account account)` (konstruktor)

Uloží sdílený odkaz na `Account`.

### `void Show()`

Vymaže terminál, zobrazí tyrkysový Figlet banner „Stats" a sestaví a zobrazí dvě sekce.

**Tabulka statistik** — tabulka Spectre.Console se zaoblenými okraji s dvěma sloupci („Stat" / „Amount") obsahující šest řádků:

| Řádek | Výpočet |
|---|---|
| Vložené kredity | `_account.Deposited` — celková částka vložená přes Stripe. |
| Utracené kredity | Součet všech záznamů `History`, kde `Amount < 0`, absolutní hodnota. Zahrnuje sázky a nákupy v obchodě. |
| Vyhrané kredity | Součet všech záznamů `History`, kde `Description` obsahuje `"Win"` nebo `"Jackpot"`. Filtruje pouze výherní transakce. |
| Aktuální zůstatek | `_account.Balance` jako takový. |
| Hodnota majetku | Součet `Price` přes všechny `OwnedItems`. |
| Čistá hodnota | `Zůstatek + Hodnota majetku`. Kombinovaná hodnota hotovosti v ruce a koupených předmětů. |

**Seznam kolekce** — pokud hráč vlastní nějaké předměty, vypíše každý z nich s kategorií a cenou pod tabulkou.

Na konci čeká na stisk libovolné klávesy před vrácením.

---

## 12. `HistoryMenu`

**Soubor:** `UI/HistoryMenu.cs`  
**Jmenný prostor:** `FinalProjekt.UI`

Zobrazí záznam transakcí v paměti pro aktuální sezení.

### `HistoryMenu(Account account)` (konstruktor)

Uloží sdílený odkaz na `Account`.

### `void Show()`

Vymaže terminál, zobrazí šedý Figlet banner „History".

- Pokud je `History` prázdná, zobrazí `"No transactions yet."` a čeká na stisk klávesy.
- Jinak sestaví tabulku Spectre.Console se zaoblenými okraji se sloupci pro čas, popis a částku. Každá částka je zobrazena zeleně s předponou `+`, pokud je kladná (výhra/vklad), nebo červeně, pokud je záporná (sázka/nákup). Všechny záznamy v `History` jsou zobrazeny v pořadí vložení (chronologicky).

Čeká na stisk libovolné klávesy před vrácením.

---

## 13. `ShopMenu`

**Soubor:** `UI/ShopMenu.cs`  
**Jmenný prostor:** `FinalProjekt.UI`

Dvouúrovňové rozhraní pro procházení obchodu. Vnější smyčka vybírá kategorii; vnitřní smyčka vybírá konkrétní předmět v rámci kategorie.

### `ShopMenu(Account account)` (konstruktor)

Uloží sdílený odkaz na `Account`.

### `void Show()`

Vnější smyčka — běží, dokud uživatel nezvolí „Back" na úrovni kategorie.

**Každá vnější iterace:**
1. Vymaže obrazovku, zobrazí zlatý Figlet banner „Shop" s aktuálním zůstatkem.
2. Sestaví seznam unikátních kategorií z `ShopCatalog.Items` plus `"Back"`.
3. Zobrazí výběrový prompt.
4. Pokud `"Back"`, vrátí se.
5. Vstoupí do vnitřní smyčky pro vybranou kategorii.

**Vnitřní smyčka** — běží, dokud uživatel nezvolí „Back" na úrovni předmětu.

**Každá vnitřní iterace:**
1. Vymaže obrazovku, znovu zobrazí záhlaví obchodu.
2. Sestaví mapu popisků: pro předměty, které hráč již vlastní, je popisek šedý s příponou `✓ Owned`. Pro nevlastněné předměty zobrazí název a cenu normálně.
3. Zobrazí výběrový prompt. Pokud `"Back"`, přejde do vnější smyčky.
4. Načte vybraný `ShopItem` z mapy popisků.
5. Pokud již vlastní: zobrazí `"You already own this."`, čeká na klávesu, pokračuje.
6. Pokud nedostatečný zůstatek: zobrazí chybu s požadovanou částkou, čeká na klávesu, pokračuje.
7. Pokud lze koupit: zobrazí potvrzovací prompt Ano/Ne. Po `"Yes"` zavolá `_account.BuyItem(selected)`, zobrazí zprávu o úspěchu a čeká na klávesu.

---

## 14. `SlotRenderer`

**Soubor:** `UI/SlotRenderer.cs`  
**Jmenný prostor:** `FinalProjekt.UI`  
**Implementuje:** `IRenderer`

Přehraje 30snímkovou animaci výherního automatu v terminálu pomocí živého renderu Spectre.Console, simulující rotující válce, které se postupně zastaví.

### `void PlayAnim(params object[] args)`

**Argumenty:** `args[0]`, `args[1]`, `args[2]` — tři finální hodnoty válců jako `int`.

**Příprava:**
- Vytvoří 3sloupcovou, 1řádkovou `Table` se zaoblenými okraji, vycentrovanou, se skrytými záhlavími.
- Inicializuje jediný řádek zástupnými symboly `"?"`.

**Animační smyčka (31 snímků, 0..30):**

Každý snímek:
1. Vygeneruje tři náhodné jednociferné číslice (`r1`, `r2`, `r3`).
2. Určí, které válce se „zamkly" podle průběhu snímku:
   - Válec 1 se zamkne na snímku 10 (1/3 z 30).
   - Válec 2 se zamkne na snímku 20 (2/3 z 30).
   - Válec 3 se zamkne na posledním snímku (30).
3. Zamknuté válce zobrazí cílovou hodnotu zeleně; točící se válce zobrazí náhodné číslice červeně.
4. Aktualizuje buňky tabulky přes `UpdateCell` a zavolá `ctx.Refresh()`.
5. Vypočítá prodlevu, která exponenciálně roste: `baseDelayMs + (int)(snímek^1.5 / 2)`. Na snímku 0 je prodleva 40 ms; na snímku 30 vyroste na ~450 ms, čímž vzniká efekt zpomalení.

Výsledkem je realisticky vypadající animace postupného zamykání, kde každý válec zastaví samostatně.

---

## 15. `RouletteRenderer`

**Soubor:** `UI/RouletteRenderer.cs`  
**Jmenný prostor:** `FinalProjekt.UI`  
**Implementuje:** `IRenderer`

Přehraje animaci ruletového kola. 37 kapes kola (0–36) je rozmístěno v mřížce 10×10 ve spirálovém vzoru. Token „kuličky" se pohybuje po kole a před přistáním na cílovém čísle postupně zpomaluje.

### `void PlayAnim(params object[] args)`

**Argument:** `args[0]` — číslo výherní kapsy jako `int`.

**Rozmístění kola:**

```csharp
int[] wheel = { 0, 32, 15, 19, 4, 21, 2, 25, 17, 34,
                6, 27, 13, 36, 11, 30, 8, 23, 10, 5,
                24, 16, 33, 1, 20, 14, 31, 9, 22, 18,
                29, 7, 28, 12, 35, 3, 26 };
```

Toto je standardní pořadí kapes evropské rulety.

**Plánování animace:**
- Najde `target` v poli kola a získá `targetIndex`.
- `fastSteps` — 2 celé oběhy plus náhodný offset (`74 + rand(0, 37)`), aby každé roztočení vypadalo jinak.
- `slowdownSteps` — 30 dalších kroků, během nichž kulička zpomaluje.
- `startPos` — vypočítán tak, aby po přesně `totalSteps` krocích kulička přistála na `targetIndex`.

**Animační smyčka:**

Každý krok:
1. Vypočítá `currentWheelIndex = (startPos + i) % 37`.
2. Znovu sestaví celou tabulku 10×10 od začátku v každém snímku (Spectre.Console to vyžaduje pro `UpdateTarget`).
3. Pro každou z 37 pozic kola umístí barevné číslo na její pozici v mřížce pomocí `GetGridPos`. Pozice kuličky je zobrazena tučně černě na bílém pozadí; ostatní čísla použijí `GetColor`.
4. Během rychlé fáze: konstantní prodleva 50 ms.
5. Během zpomalovací fáze: prodleva roste přes `baseDelayMs + (int)(progress^2.5 * 800)`. V posledním pomalém kroku může extra prodleva dosáhnout až 800 ms, čímž vzniká jasné vizuální zastavení.

### `private (int row, int col) GetGridPos(int index)`

Mapuje index kola (0–36) na pozici v mřížce 10×10 pomocí spirály po směru hodinových ručiček:

- Indexy 0–9: horní řádek, zleva doprava.
- Indexy 10–18: pravý sloupec, shora dolů.
- Indexy 19–27: dolní řádek, zprava doleva.
- Indexy 28–36: levý sloupec, zdola nahoru.

Tím se všech 37 čísel rozmístí po obvodu mřížky a střed zůstane prázdný.

### `private Color GetColor(int num)`

Vrátí Spectre.Console `Color` pro číslo rulety:
- `0` → `Color.Green` (číslo kasina).
- Čísla ze standardní červené sady `{1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36}` → `Color.Red`.
- Ostatní → `Color.Grey` (černé kapsy, zobrazeny šedě pro čitelnost v terminálu).

---

## 16. `SlotMachine`

**Soubor:** `Games/SlotMachine.cs`  
**Jmenný prostor:** `FinalProjekt.Games`  
**Implementuje:** `IGame`

Výherní automat se třemi válci. `RigEngine` předem určí, zda je kolo výherní, ještě před generováním hodnot válců, a pak jsou hodnoty zkonstruovány tak, aby odpovídaly předem stanovenému výsledku.

### Pole

| Pole | Popis |
|---|---|
| `_account` | Sdílený odkaz na účet. |
| `_renderer` | Instance `SlotRenderer` pro animaci válců. |
| `_rigEngine` | Instance `RigEngine` per-instanci sledující po sobě jdoucí prohry pouze pro tuto hru. |

### `string Name => "Slot Machine"`

Zobrazovaný název zobrazený v `CasinoApp.GamesMenu`.

### `void ShowSplash()`

Vymaže terminál, zobrazí zelený Figlet název hry a aktuální zůstatek.

### `void Play()`

Hlavní herní smyčka. Běží, dokud hráč nezvolí „Main Menu".

**Každé kolo:**

1. Zobrazí prompt `"Play"` / `"Main Menu"`.
2. Pokud `"Play"`:
   - Zkontroluje zůstatek > 0; pokud ne, zobrazí chybu.
   - Vyzve k zadání sázky (ověřeno: 1 až aktuální zůstatek).
   - Zavolá `_account.Deduct(bet, "Slot Machine - Bet")`.
   - Zavolá `_rigEngine.IsWinAllowed(_account)` pro předem stanovení výsledku (po odečtení sázky, takže engine vidí přesný stav zůstatku).
   - Vygeneruje tři náhodné číslice (`num1`, `num2`, `num3`) v rozsahu 0–9.
   - **Pokud je výhra povolena:** náhodně zvolí buď nastavit `num1 = num2` (cesta ke jackpotu) nebo `num2 = num3` (cesta k částečné výhře). Jackpot (všechny tři stejné) nastane pouze v případě, že obě dvojice skončí stejně, což se stane v 50 % případů, kdy je výhra povolena.
   - **Pokud výhra není povolena:** vstoupí do smyčky, která zamíchá `num2` a `num3`, dokud nejsou všechny tři hodnoty různé, čímž zaručí prohru.
   - Zavolá `_renderer.PlayAnim(num1, num2, num3)` pro zobrazení animace.
   - **Vyhodnocení výsledku:**
     - Všechny tři stejné → Jackpot. Výplata: `PayoutEngine.GetLogPayout(bet, 5.0)`. Zaznamená výhru.
     - Jakékoli dvě stejné → Výhra. Výplata: `PayoutEngine.GetLogPayout(bet, 2.0)`. Zaznamená výhru.
     - Všechny různé → Prohra. Zaznamená prohru.
   - Přidá výhry zpět přes `_account.Add(winAmount + bet, ...)`.
3. Čeká na stisk klávesy, poté zavolá `ShowSplash()` pro obnovení.

---

## 17. `NumberGuess`

**Soubor:** `Games/NumberGuess.cs`  
**Jmenný prostor:** `FinalProjekt.Games`  
**Implementuje:** `IGame`

Hra na hádání čísel, kde hráč zvolí rozsah a pokusí se uhodnout náhodně vylosované číslo. Větší rozsahy dávají vyšší multiplikátory, ale `RigEngine` tuto hru **nemanipuluje** — výsledek je čistě náhodný. `_rigEngine` je sice vytvořen a `RecordResult` je voláno, ale `IsWinAllowed` se nikdy nekontroluje; cílové číslo je vylosováno až poté, co hráč hádá.

### `string Name => "Number Guess"`

### `void ShowSplash()`

Vymaže terminál, zobrazí žlutý Figlet název, ukáže zůstatek.

### `void Play()`

**Každé kolo:**

1. Zobrazí prompt `"Play"` / `"Main Menu"`.
2. Pokud `"Play"`:
   - Zkontroluje zůstatek > 0.
   - Vyzve k zadání sázky (1 až zůstatek).
   - Zavolá `_account.Deduct(bet, "Number Guess - Bet")`.
   - Vyzve k zadání maximálního čísla (minimum 4). Toto řídí obtížnost i odměnu.
   - Vyzve k zadání hráčova tipu (libovolné celé číslo, bez ověření rozsahu).
   - Vylosuje `target = Random.Shared.Next(1, maxNum+1)` — náhodné číslo od 1 do `maxNum` včetně.
   - Počká 1 sekundu, poté odhalí cíl.
   - Počká dalších 670 ms pro dramatický efekt.
   - **Pokud správně:** výplata je `PayoutEngine.GetLogPayout(bet, maxNum / 4)`. Maximum 4 dá základní multiplikátor 1.0; maximum 20 dá 5.0. Zaznamená výhru.
   - **Pokud špatně:** zaznamená prohru.
   - Čeká na stisk klávesy, vymaže obrazovku, zavolá `ShowSplash`.

**Poznámka k zdvojenému podmínění:** V kódu je redundantní `if (guess == target)` vnořen uvnitř vnějšího `if (guess == target)` — toto je artefakt kódu bez funkčního dopadu.

---

## 18. `Roulette`

**Soubor:** `Games/Roulette.cs`  
**Jmenný prostor:** `FinalProjekt.Games`  
**Implementuje:** `IGame`

Standardní evropská ruleta se šesti typy sázek. `RigEngine` předem stanoví výhru/prohru a poté buď vynutí výsledek kola do hráčovy sázky, nebo ho vynutí mimo.

### Pole

| Pole | Popis |
|---|---|
| `_account` | Sdílený odkaz na účet. |
| `_rigEngine` | Instance `RigEngine` per-instanci. |
| `_renderer` | Instance `RouletteRenderer` pro animaci kola. |

### `string Name => "Roulette"`

### `void ShowSplash()`

Vymaže terminál, zobrazí červený Figlet název „Roulette", ukáže zůstatek.

### `void Play()`

**Každé kolo:**

1. Zobrazí prompt `"Play"` / `"Main Menu"`.
2. Pokud `"Play"`:
   - Zkontroluje zůstatek > 0; pokud ne, zobrazí chybu.
   - Vyzve k zadání sázky.
   - Zavolá `_account.Deduct(bet, "Roulette - Bet")`.
   - Zavolá `_rigEngine.IsWinAllowed(_account)` pro předem stanovení výsledku (po odečtení sázky).
   - Zavolá `ShowSplash()` pro obnovení zobrazení po odečtení.
   - Vylosuje předběžný náhodný `target` v `[0, 36]`.
   - Vyzve k výběru typu sázky a sestaví `betNums` (seznam výherních čísel kapsy) a `multiplier`:

| Typ sázky | Výherní kapsy | Multiplikátor |
|---|---|---|
| Konkrétní číslo | Zvolené číslo | 35 |
| Červená | 18 červených čísel | 1 |
| Černá | 18 černých čísel | 1 |
| Sudá | 18 sudých čísel (1–36, bez 0) | 1 |
| Lichá | 18 lichých čísel | 1 |
| 1. tucet (1–12) | 12 čísel | 2 |
| 2. tucet (13–24) | 12 čísel | 2 |
| 3. tucet (25–36) | 12 čísel | 2 |

   - **Manipulace výhry:** pokud `willWin`, nahradí `target` náhodným prvkem z `betNums`.
   - **Manipulace prohry:** pokud `!willWin`, přegeneruje `target` ve smyčce, dokud nespadne mimo `betNums`.
   - Zavolá `_renderer.PlayAnim(target)` pro zobrazení animace kola.
   - **Výsledek:** pokud `betNums.Contains(target)`, vyhraje `bet * multiplier` a zavolá `_account.Add(winAmount + bet, ...)`. Jinak zaznamená prohru.
   - Čeká na stisk klávesy, zavolá `ShowSplash`.

**Poznámka k výplatě:** Výherní částka je vypočítána jako `(int)(bet * multiplier)` — jednoduchým násobením, ne přes `PayoutEngine.GetLogPayout`. Logaritmickou výplatu používají pouze automaty a hádání čísel. Ruleta používá standardní kasinové kurzy.

---

## 19. `SportsBetting`

**Soubor:** `Games/SportsBetting.cs`  
**Jmenný prostor:** `FinalProjekt.Games`  
**Implementuje:** `IGame`

Simulátor hokejového zápasu NHL. Náhodně se vyberou dva týmy, vygenerují se kurzy a hráč sází buď na vítěze zápasu nebo na přesný výsledek. `RigEngine` manipuluje simulované góly tak, aby odpovídaly nebo odporovaly výběru hráče.

### Pole

| Pole | Popis |
|---|---|
| `_account` | Sdílený odkaz na účet. |
| `_rigEngine` | Instance `RigEngine` per-instanci. |
| `_teams` | Pole 32 názvů skutečných týmů NHL používané jako pool zápasů. |

### `string Name => "Sports Betting"`

### `void ShowSplash()`

Vymaže terminál, zobrazí modrý Figlet název „Sports Betting", ukáže zůstatek.

### `private int SimGoals()`

Simuluje realistický počet gólů pro jeden tým pomocí váženého náhodného rozdělení:

```
pole vah:  [2, 8, 16, 20, 18, 14, 10, 6, 4, 2]
indexy:     0   1   2   3   4   5   6  7  8  9
```

Vylosuje číslo 0–99 a sčítá váhy zleva doprava, aby zjistilo, do jakého koše hod spadne. Rozdělení kulminuje na 3 gólech (váha 20), což dělá zápasy s 3 góly nejčastějším výsledkem. Zápasy s 0 nebo 9 góly jsou vzácné (váha 2).

### `private void SimulateMatch(string home, string away, int homeGoals, int awayGoals)`

Vytiskne průběh zápasu kolo po kole s prodlevami pro efekt živého skóre.

1. Náhodně rozdělí celkové góly každého týmu do tří třetin.
2. Vytiskne `"--- GAME START ---"` s názvy týmů, počká 900 ms.
3. Pro každou ze tří třetin: vytiskne název třetiny, počká 1400 ms, akumuluje průběžné součty, vytiskne aktuální skóre, počká 700 ms.
4. Vytiskne konečné skóre.

Prodlevy jsou všechny blokující volání `Thread.Sleep`, takže simulace zápasu trvá vždy přibližně 7 sekund bez ohledu na výsledek.

### `void Play()`

**Každé kolo:**

1. Zobrazí prompt `"Play"` / `"Main Menu"`.
2. Pokud `"Play"`:
   - Zkontroluje zůstatek > 0.
   - Zamíchá `_teams` a vezme první dva jako `home` a `away`.
   - Náhodně určí favorita a vygeneruje kurzy:
     - Kurz favorita: `1.4 + rand(0, 0.4)` zaokrouhleno na 1 desetinné místo (rozsah ~1.4–1.8×).
     - Kurz outsidera: `2.0 + rand(0, 0.8)` zaokrouhleno na 1 desetinné místo (rozsah ~2.0–2.8×).
   - Zobrazí zápas a kurzy.
   - Vyzve k zadání sázky.
   - Vyzve k výběru typu sázky: `"Winner"` nebo `"Exact Score (18x)"`.
   - Zavolá `_account.Deduct(bet, "Sports Betting - Bet")`.
   - Zavolá `_rigEngine.IsWinAllowed(_account)` pro předem stanovení výsledku (po odečtení sázky).

   **Sázka na vítěze:**
   - Hráč vybere tým. Multiplikátor se nastaví na kurz tohoto týmu.
   - Góly obou týmů jsou nezávisle vylosovány přes `SimGoals()`. Remízy jsou řešeny přidáním gólu hostům.
   - **Pokud manipulovaná výhra:** pokud vybraný tým prohrává nebo remizuje, jeho góly se navýší na `soupeř + rand(1, 4)` pro zaručení výhry.
   - **Pokud manipulovaná prohra:** pokud vybraný tým vede, góly soupeře se navýší pro zaručení prohry.

   **Sázka na přesný výsledek:**
   - Hráč hádá přesný počet gólů obou týmů (0–15). Multiplikátor je pevně nastaven na 18×.
   - **Pokud manipulovaná výhra:** finální skóre se nastaví přesně na hráčův tip.
   - **Pokud manipulovaná prohra:** góly se přegenerují přes `SimGoals()` ve smyčce, dokud se výsledek neshoduje s hráčovým tipem.

   - Zavolá `ShowSplash()` a poté `SimulateMatch(home, away, homeGoals, awayGoals)`.
   - Vyhodnotí výsledek:
     - Sázka na vítěze: zvolený tým hráče má více gólů.
     - Přesný výsledek: oba počty gólů se shodují.
   - Při výhře přičte `(int)(bet * multiplier)`, zaznamená výsledek přes `RecordResult`.
   - Čeká na stisk klávesy, zavolá `ShowSplash`.

---

## 20. `DBConnector`

**Soubor:** `Data/DBConnector.cs`  
**Jmenný prostor:** `FinalProjekt.Data`

Veškerá komunikace s PocketBase backendem. Zpracovává autentizaci přes Google OAuth 2.0, uchování tokenu, načítání/ukládání záznamů a zaznamenávání transakcí. Používá `System.Net.Http.HttpClient` se serializací JSON.

### Pole

| Pole | Popis |
|---|---|
| `_client` | Sdílená instance `HttpClient`. Po přihlášení je nastaven hlavičkou `Authorization`. |
| `_token` | Bearer token vrácený PocketBase OAuth. `null` pokud není přihlášen. |
| `_userId` | ID záznamu uživatele v PocketBase. Používá se k omezení všech dotazů na tohoto uživatele. |
| `_recordId` | ID záznamu uživatele v kolekci `casino`. Cachováno pro zamezení opakovaných vyhledávání. |
| `TokenFile` | Konstanta `"token.dat"` — cesta k souboru s lokální cache tokenu. |

### `string? UserId`

Vlastnost jen pro čtení zpřístupňující `_userId`. Používá `Account.UserId`, který pak používá `StripeService`.

### `private string GetUrl()`

Vrátí základní URL PocketBase z proměnné prostředí `PB_BASE_URL`, nebo použije záložní hodnotu `"https://casino.danykoch.cz"`, pokud proměnná není nastavena.

### `bool IsLoggedIn()`

Vrátí `LoadLocalToken()` — true, pokud lze najít token v paměti nebo na disku.

### `void Logout()`

Vymaže veškerý stav autentizace:
- Nastaví `_token`, `_userId`, `_recordId` na `null`.
- Smaže `token.dat`, pokud existuje.
- Odstraní hlavičku `Authorization` z `_client`.

### `async Task<bool> Authenticate()`

Zajistí, že konektor má platný token. Vrátí `true` při úspěchu, `false` při selhání.

1. Pokud `LoadLocalToken()` uspěje, okamžitě vrátí `true` (bez síťového volání).
2. Načte konfiguraci auth metod z PocketBase: `GET /api/collections/users/auth-methods`.
3. Najde záznam poskytovatele Google; pokud není nakonfigurován, vrátí false.
4. Spustí lokální HTTP listener na `http://localhost:8123/`.
5. Sestaví Google OAuth URL pomocí `authUrl` a `codeVerifier` poskytovatele a přidá redirect URI.
6. Otevře URL v systémovém prohlížeči přes `Process.Start`.
7. Čeká na OAuth callback: `await listener.GetContextAsync()`. Toto blokuje, dokud Google nepřesměruje zpět s autorizačním kódem.
8. Odešle úspěšnou HTML odpověď zpět do záložky prohlížeče.
9. Zastaví listener.
10. POSTne kód, code verifier a redirect URI do endpointu `auth-with-oauth2` PocketBase.
11. Při úspěchu: uloží token a ID uživatele, nastaví hlavičku `Authorization`, zavolá `SaveLocalToken`, vrátí true.

### `private void SaveLocalToken()`

Zapíše `_token` a `_userId` jako dva řádky do `token.dat`. Voláno ihned po úspěšném OAuth, aby uživatel nebyl vyzýván znovu při příštím spuštění.

### `private bool LoadLocalToken()`

Pokusí se obnovit sezení z `token.dat`:
1. Pokud je `_token` již v paměti, vrátí true.
2. Pokud `token.dat` neexistuje, vrátí false.
3. Přečte oba řádky; pokud je soubor poškozený (méně než 2 řádky), vrátí false.
4. Obnoví `_token` a `_userId`, nastaví hlavičku `Authorization`, vrátí true.

Poznámka: tato metoda **neověřuje**, zda token stále server přijímá. Expirovaný token bude odhalen až při prvním neúspěšném API volání.

### `async Task<(double balance, double deposited, Dictionary<string, bool>? properties)> Load()`

Načte (nebo vytvoří) hráčův kasinový záznam z PocketBase.

1. Zavolá `Authenticate()`; při selhání vrátí výchozí hodnoty `(1000, 0, null)`.
2. `GET /api/collections/casino/records?filter=(user='{_userId}')&limit=1` pro nalezení záznamu tohoto uživatele.
3. **Pokud nalezen:** uloží `_recordId`, vrátí `(balance, deposited, properties)`.
4. **Pokud nenalezen (neúspěšná odpověď):** POSTne nový záznam s `balance=1000`, `deposited=0`. Uloží nové `_recordId` a vrátí `(1000, 0, null)`.

Noví hráči začínají s 1000 kredity.

### `async Task LogTransaction(double value, string type)`

Zapíše jeden řádek do kolekce `transactions` v PocketBase.

- Nejprve zavolá `Authenticate()`.
- POSTne `{ value, type, user }` do `/api/collections/transactions/records`.
- Zaznamená jakékoli selhání do konzole, ale nevyvolá výjimku.

### `async Task Save(double balance, double deposited, Dictionary<string, bool>? properties)`

Uloží celý stav hráče do PocketBase.

1. Zavolá `Authenticate()`.
2. Pokud je `_recordId` null, zavolá `Load()` pro jeho zjištění nebo vytvoření.
3. Pokud je `_recordId` nyní nastaven: PATCHne existující záznam novými hodnotami `balance`, `deposited` a `properties`.
4. Pokud je stále null: POSTne nový záznam a uloží vrácené ID.

### Privátní DTO třídy

Toto jsou cíle deserializace JSON používané interně v `DBConnector`. Všechna pole používají atributy `[JsonPropertyName]` pro shodu s camelCase JSON PocketBase.

| Třída | Účel |
|---|---|
| `AuthMethods` | Kořenový objekt z `/auth-methods`. Obsahuje seznam `authProviders`. |
| `AuthProvider` | Jeden záznam poskytovatele s `name`, `authUrl` a `codeVerifier`. |
| `AuthResponse` | Kořenový objekt z výměny OAuth. Obsahuje `token` a `record`. |
| `UserRecord` | Podobjekt uživatele uvnitř `AuthResponse`. Používá se pouze pole `id`. |
| `RecordList` | Obálka pro stránkované odpovědi seznamu PocketBase. Obsahuje pole `items`. |
| `GameRecord` | Řádek v kolekci `casino`: `id`, `balance`, `deposited`, `properties`. |

---

## 21. `StripeService`

**Soubor:** `Data/StripeService.cs`  
**Jmenný prostor:** `FinalProjekt.Data`

Zpracovává vklady skutečných peněz pomocí Stripe SDK. Vytváří Checkout Sessions pro platby v CZK a dotazuje se na jejich stav, dokud není platba potvrzena, a teprve poté připíše kredity na účet.

### Pole

| Pole | Popis |
|---|---|
| `_secretKey` | Stripe tajný klíč načtený z proměnné prostředí `STRIPE_SECRET_KEY`. |

### `StripeService()` (konstruktor)

Načte tajný klíč z prostředí a přiřadí ho do `StripeConfiguration.ApiKey`, čímž nakonfiguruje globálního Stripe SDK klienta pro všechna následující volání.

### `Session CreateCheckoutSession(long amount, string userId)`

Vytvoří Stripe Checkout Session pro nákup kreditů.

**Konfigurace session:**
- Platební metoda: pouze karta.
- Jedna položka: `amount * 100` (Stripe pracuje v nejmenší měnové jednotce — haléřích pro CZK), měna `"czk"`, název produktu `"{amount} Casino Credits"`.
- Režim: `"payment"` (jednorázový poplatek, ne předplatné).
- URL při úspěchu: `https://casino.danykoch.cz/success/`
- URL při zrušení: `https://casino.danykoch.cz/cancel/`
- Metadata: `{ "userId": userId }` — označí session ID uživatele v PocketBase pro případné zpracování webhookem.

Vrátí vytvořený objekt `Session`, který obsahuje URL hostované stránky Checkout.

### `Session GetSession(string sessionId)`

Načte aktuální stav existující Stripe session podle ID. Používá se v dotazovací smyčce pro kontrolu stavu platby.

### `void ProcessDeposit(Account account)`

Celý tok vkladu — jediná metoda volaná zvenčí této třídy.

1. **Výzva k zadání částky** — zeptá se na výši vkladu (15–999 999 CZK). 1 CZK = 1 kredit.
2. **Vytvoří session** — zavolá `CreateCheckoutSession(amount, account.UserId)`.
3. **Otevře prohlížeč** — otevře Stripe-hostovanou platební stránku přes `Process.Start`.
4. **Dotazovací smyčka** — spustí spinner `AnsiConsole.Status` a dotazuje se na `GetSession` každé 3 sekundy:
   - Pokud `PaymentStatus == "paid"`: zavolá `account.ConfirmDeposit(amount)` pro připsání kreditů, přeruší smyčku.
   - Pokud `Status == "expired"` nebo `"canceled"`: zobrazí chybu, přeruší smyčku.
5. Zobrazí aktualizovaný zůstatek a čeká na stisk klávesy.

Interval dotazování je 3 sekundy a neexistuje žádný timeout — smyčka běží donekonečna, dokud Stripe nehlásí terminální stav. Pokud uživatel zavře prohlížeč bez zaplacení nebo čekání, aplikace se zablokuje, dokud session nevyprší (na Stripe obvykle po 30 minutách).
