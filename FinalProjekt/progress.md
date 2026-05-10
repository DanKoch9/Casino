# Projektové cíle

Na začátku jsem si zadal následující cíle:

## Základ
- Úvodní menu, kde uživatel uvidí stav účtu, možnost dobít kredity, a výběr her. - **hotovo**
- **4 kasíno hry** - Automaty, hádání čísel, ruleta a sázení na sporty. - **chybí část rulety a sázky**
- **Animace u her**, barevné TUI, rigging (the house always wins) - **hotovo**
- **Pokusit se uživatele u hry co nejdéle udržet** - win/loss streak tracking. - **hotovo**
- **U automatů** scaling odměn podle sázky - **hotovo**
- **U hádání čísel** možnost nastavení rozsahu, větší rozsah = větší odměna - **hotovo**
- **U rulety** animace, jak se “kulička” pohybuje, možnosti sázení jako u reálné rulety - **hotovo**
- **Sázení na jeden sport**, náhodné ale uvěřitelné výsledky - **chybí**
- **Historie transakcí**, výher, a proher - **chybí**
- **Simulovaný účet uživatele** mimo hlavní kód, kam půjdou vybrat peníze a taky z něj dobít. - **hotovo**

## Možná rozšíření
- **Sázení na vícero sportů**, třeba hokej, fotbal, dostihy, nebo curling. - **chybí**
- **Napojení na databázi** s informacemi o hráči - **hotovo**
- **Napojení platební brány Stripe** v test módu - **hotovo**
- **Obchod** (hodinky, oblečení, lodě, auta, domy…) - **chybí**
- **Simulace burzy** - **chybí**
- **Napojení na reálnou burzu** - **chybí**
- **Komplexnější systém sportů** - turnaje, staty týmů. - **chybí**

---

Větší část základu je již hotová s tím, že schází pouze dodělat ruletu, sázky a také historie. Nejnáročnější částí bylo napojení na stripe a vytvoření funkční databáze. OAuth 2.0 byl přepkapivě jednoduchý na nastavení.
