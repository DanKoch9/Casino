# Konzolové kasino – Dokumentace

## Přehled

Konzolová kasino aplikace napsaná v **C# (.NET 10)**. Uživatel se přihlásí přes Google účet, dostane startovní kredit a může hrát čtyři kasino hry. Vše běží v terminálu s animacemi a barevným UI přes knihovnu **Spectre.Console**. Platby jsou zpracovávány přes **Stripe** a data uživatele jsou uložena na vlastním **PocketBase** serveru.

---

## Technický stack

| Vrstva | Technologie |
|---|---|
| Jazyk | C# 13, .NET 10 |
| TUI | Spectre.Console 0.54 |
| Backend / Auth | PocketBase (self-hosted) |
| Platby | Stripe .NET SDK, testovací mód |
| Konfigurace | DotNetEnv (.env soubor) |

---

## Spuštění

```bash
dotnet run
```

Vyžaduje soubor `.env` v kořeni projektu:

```
PB_BASE_URL=https://casino.danykoch.cz
STRIPE_SECRET_KEY=sk_test_...
STRIPE_PUBLISHABLE_KEY=pk_test_...
```

---

## Autentizace a účet

### Google OAuth 2.0

Při prvním spuštění aplikace:

1. Načte dostupné OAuth poskytovatele z PocketBase API (`/api/collections/users/auth-methods`)
2. Otevře prohlížeč s Google přihlašovací URL
3. Lokálně spustí HTTP listener na `http://localhost:8123/` a čeká na OAuth callback s autorizačním kódem
4. Kód odešle zpět do PocketBase (`/api/collections/users/auth-with-oauth2`)
5. Přijatý JWT token a userId uloží do souboru `token.dat`

Při každém dalším spuštění se token načte z `token.dat` — přihlášení přes prohlížeč se přeskočí. Odhlášením se soubor smaže.

### Účet v databázi

V PocketBase kolekci `casino` je pro každého uživatele jeden záznam se dvěma hodnotami:

- **`balance`** — aktuální stav kreditů
- **`deposited`** — celková částka vložená přes Stripe (slouží výhradně pro RigEngine)

Nový uživatel automaticky dostane **1 000 startovních kreditů** (záznam se vytvoří při prvním přihlášení).

Po každé změně zůstatku (`Add`, `Deduct`) se stav asynchronně uloží do databáze přes PATCH request.

### Dobíjení kreditů (Stripe)

1. Uživatel zadá částku v CZK (minimum 15, maximum 999 999)
2. Vytvoří se Stripe Checkout Session
3. Prohlížeč se otevře na platební stránce Stripe
4. Aplikace každé 3 sekundy polling-uje stav session dokud `paymentStatus` není `paid` nebo session nevyprší
5. Po potvrzení se kredit přičte k `balance` i `deposited`

---

## Herní mechaniky

### RigEngine — "the house always wins"

`RigEngine` je sdílená třída rozhodující zda hráč danou hru vyhraje nebo prohraje. Kasino nikdy nestojí na náhodě — výsledek je předem určen a vizuální animace je pak zmanipulována aby výsledek odpovídal.

**Výpočet pravděpodobnosti výhry:**

```
balanceRatio  = balance / max(1, deposited)
winChance     = 0.30 / max(0.1, balanceRatio)
winChance    += consecutiveLosses * 0.02
winChance     = clamp(winChance, 0.005, 0.60)
```

Klíčové chování:
- **Základní šance výhry je 30 %**
- Čím více kreditů má hráč oproti vložené částce (`balanceRatio > 1`), tím nižší je šance výhry — kasino brání hráči odejít s výhrou
- Každá po sobě jdoucí prohra přidá **+2 %** k šanci výhry — mechanismus udržování hráče u hry (po 10 prohrách je bonus +20 %)
- Výsledná šance je vždy v rozmezí **0,5 % – 60 %**

Stav RigEngine (počítadlo proher) se resetuje při výhře. Každá hra má vlastní instanci RigEngine — série proher v ruletě neovlivní automaty.

---

### PayoutEngine — škálování odměn

Výpočet výplaty za výhru u automatů a hádání čísel:

```
výplata = bet × (baseMultiplier + log₁₀(bet) × 1.2)
```

Logaritmický bonus způsobuje, že vyšší sázky mají proporcionálně vyšší odměnu, ale ne lineárně — je to kompromis mezi atraktivitou vysokých sázek a zachováním výhody kasina.

Příklady (base 2.0):

| Sázka | Multiplikátor | Výplata |
|---|---|---|
| 10 | 2.0 + 1.2 = 3.2 | 32 |
| 100 | 2.0 + 2.4 = 4.4 | 440 |
| 1 000 | 2.0 + 3.6 = 5.6 | 5 600 |

---

## Hry

### Automaty (Slot Machine)

**Princip:** Tři válce, každý ukazuje číslo 0–9.

**Výherní kombinace:**
- Všechna tři čísla stejná → **Jackpot** (PayoutEngine, base 5.0)
- Dvě ze tří čísel stejná → **Výhra** (PayoutEngine, base 2.0)

**Rigging:**
- Pokud RigEngine povolí výhru, jeden ze dvou způsobů: buď `num1 = num2` nebo `num2 = num3`
- Pokud RigEngine zakáže výhru, smyčka zajišťuje že všechna tři čísla jsou navzájem různá

**Animace (SlotRenderer):**
- 30 snímků celkem
- Každý snímek zobrazuje náhodná čísla; zpomalení se počítá jako `40 + frame^1.5 / 2` ms — čím déle animace běží, tím je pomalejší
- Válce se postupně "zamykají" na finální hodnotu: první po snímku 10, druhý po 20, třetí na snímku 30

---

### Hádání čísel (Number Guess)

**Princip:** Hráč si zvolí rozsah (minimum 4), tipuje číslo. Výplata roste s rozsahem.

**Výpočet výhry:** PayoutEngine s `baseMultiplier = rozsah / 4`

Příklad: rozsah 20 → base = 5.0 → stejný základ jako jackpot v automatech.

**Poznámka:** RigEngine je použit ale výhru neřídí — pokud hráč uhodne a RigEngine zakazuje výhru, hráč přesto dostane výhru (hra je čistě náhodná). RigEngine slouží jen pro záznam výsledku (streak tracking).

---

### Ruleta (Roulette)

**Princip:** Evropská ruleta, čísla 0–36.

**Typy sázek a multiplikátory:**

| Typ | Pokrytí čísel | Výplata |
|---|---|---|
| Konkrétní číslo | 1 | 35× sázku |
| Červená / Černá | 18 | 1× sázku |
| Sudá / Lichá | 18 | 1× sázku |
| 1. tucet (1–12) | 12 | 2× sázku |
| 2. tucet (13–24) | 12 | 2× sázku |
| 3. tucet (25–36) | 12 | 2× sázku |

**Rigging:**
- Pokud RigEngine povolí výhru: `target = betNums[náhodný index]` — kulička padne na jedno ze vsazených čísel
- Pokud RigEngine zakáže výhru: smyčka generuje nová čísla dokud `target` není mimo `betNums`

**Animace (RouletteRenderer):**
- Ruleta je zobrazena jako 10×10 mřížka, čísla jsou uspořádána ve skutečném pořadí evropského kola: `0, 32, 15, 19, 4, 21, 2, 25, 17...`
- Kulička (bílý highlight) obíhá kolo ve dvou fázích:
  - **Rychlá fáze:** 74+ kroků po 50 ms
  - **Zpomalovací fáze:** 30 kroků s prodlevou `50 + (progress^2.5) × 800` ms — kvadratické zpomalení až na ~850 ms na poslední pozici
- Barvy: 0 = zelená, 1 3 5 7 9 12 14 16 18 19 21 23 25 27 30 32 34 36 = červená, ostatní = šedá

---

### NHL Sázení (Sports Betting)

**Princip:** Jsou náhodně vybrány dva ze 32 skutečných NHL týmů. Hráč sází na výsledek zápasu.

**Typy sázek:**

| Typ | Výplata |
|---|---|
| Vítěz – favorit | 1.4 – 1.8× sázku (náhodně) |
| Vítěz – outsider | 2.0 – 2.8× sázku (náhodně) |
| Přesný výsledek | 18× sázku |

Favorit a outsider jsou přiřazeni náhodně před každým zápasem.

**Simulace zápasu:**
- Góly pro každý tým se generují váženou pravděpodobností:

| Góly | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |
|---|---|---|---|---|---|---|---|---|---|---|
| Váha | 2 | 8 | 16 | 20 | 18 | 14 | 10 | 6 | 4 | 2 |

- Nejpravděpodobnější výsledek je 3 góly na tým
- Góly jsou náhodně rozděleny do 3 třetin; výsledek každé třetiny je zobrazen postupně s prodlevami

**Rigging:**
- Sázka na vítěze: pokud RigEngine povolí výhru a vybraný tým prohrával, přidají se soupeři 1–3 góly navíc
- Přesný výsledek: pokud RigEngine povolí výhru, finální skóre je nastaveno přesně na hráčův tip; pokud zakáže výhru, smyčka generuje skóre dokud se neshoduje s tipem

---

## Historie transakcí

Každá operace se zůstatkem je zaznamenána v paměti jako `Transaction(čas, popis, částka)`:

| Operace | Popis záznamu | Znaménko |
|---|---|---|
| Sázka | `[Hra] - Bet` | záporné |
| Výhra | `[Hra] - Win / Jackpot` | kladné |
| Vklad | `Deposit` | kladné |

Historie je **pouze session** — po restartu aplikace se resetuje. Zobrazuje se z hlavního menu jako tabulka s časem, popisem a barevně odlišenou částkou (zelená = příjem, červená = výdaj).

---

## Architektura

```
Core/
  Program.cs         — vstupní bod, načte .env, spustí CasinoApp
  CasinoApp.cs       — hlavní smyčka, menu
  Account.cs         — stav hráče, Add/Deduct/Deposit, historie
  IGame.cs           — rozhraní pro hry (Name, Play, ShowSplash)
  IRenderer.cs       — rozhraní pro animace (PlayAnim)
  RigEngine.cs       — dynamická pravděpodobnost výhry
  PayoutEngine.cs    — logaritmická výplata

Games/
  SlotMachine.cs     — automaty
  NumberGuess.cs     — hádání čísel
  Roulette.cs        — ruleta
  SportsBetting.cs   — NHL sázení

UI/
  CasinoApp.cs       — menu a zobrazení historie
  SlotRenderer.cs    — animace automatů
  RouletteRenderer.cs— animace rulety

Data/
  DBConnector.cs     — PocketBase HTTP klient, Google OAuth, token cache
  StripeService.cs   — Stripe checkout session, polling platby
```

Každá hra dostane instanci `Account` přes konstruktor. `RigEngine` a `PayoutEngine` jsou nezávislé — hry si je vytvářejí samy. `DBConnector` komunikuje se serverem asynchronně; `Account.Add` a `Account.Deduct` spouštějí save fire-and-forget (`_ = Save()`), aby UI neblokovalo na každé sázce.
