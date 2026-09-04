# Protokol baseline testu

**Verzia:** 0.1.5-alpha · **Dátum:** 2026-09-04

Toto je meranie **bez akejkoľvek pomoci modelu**. Všetko, čo sa tu nameria, je referencia, voči
ktorej sa neskôr porovná, o koľko znalosť budovy drift potlačí. Bez tohto čísla nemá zmysel
stavať korekcie — nebolo by s čím porovnávať.

Vytlačená značka zatiaľ nie je, takže sa nemeria drift prekryvu voči budove. Meria sa **samotný
tracking**: koľko toho ARCore vie a kde sa láme.

## Pred odchodom

- Zbuildiť `FriLens > Build Android 0.1.5-alpha` a nainštalovať.
- Zobrať **meracie pásmo alebo krokomer s overenou dĺžkou kroku**. Bez referencie sa
  prejdená vzdialenosť nedá overiť a celý test A padá.
- Ísť **cez deň**. Predošlý beh mal 30 riadkov `InsufficientLight` a desať relokalizácií;
  v tme sa meria kvalita osvetlenia, nie kvalita trackingu.
- Papier a pero na poznámky, alebo hlasové poznámky. Čísla z displeja treba zapísať hneď,
  z CSV sa dodatočne nedá zistiť, čo si v tej chvíli robil.

## Test A — sedí prejdená vzdialenosť?

Odmerať pásmom rovný úsek chodby, ideálne 20 m. Označiť začiatok a koniec.

1. Stlačiť `Mark` na začiatku.
2. Prejsť úsek **normálnou chôdzou, telefón držať pokojne pred sebou**.
3. Stlačiť `Mark` na konci a odpísať z displeja veľké číslo aj `raw` pod ním.
4. Zopakovať trikrát.

Potom to isté ešte raz, ale **zámerne s telefónom rozhádzaným** — mávať, otáčať sa, mieriť po
stranách. Rovnaká trasa, rovnaká dĺžka.

| | čaká sa |
|---|---|
| veľké číslo, pokojná chôdza | blízko 20 m, mierne pod |
| `raw`, pokojná chôdza | výrazne nad 20 m |
| veľké číslo, rozhádzaný telefón | stále blízko 20 m |
| `raw`, rozhádzaný telefón | výrazne viac než predtým |

Ak veľké číslo v štvrtom riadku ujde, filter je slabý a treba zdvihnúť prah alebo časovú
konštantu.

## Test B — zakrytie kamery

Toto je ten test, kvôli ktorému vznikol [ADR 006](decisions/006-kotvenie-a-strata-trackingu.md).
Hypotéza je, že chyba **nezávisí od prejdenej vzdialenosti, ale od toho, či je cieľ v mape**.

Pre každý pokus:

1. Postaviť sa tak, aby bol prekryv viditeľný, a zapamätať si, kde presne leží hrana prekryvu
   voči nejakému bodu na stene alebo podlahe. Odfotiť.
2. **Zakryť kameru dlaňou.**
3. Prejsť *d* metrov.
4. **Odkryť** a chvíľu stáť.
5. Odfotiť a odčítať, o koľko sa prekryv posunul voči tomu istému bodu.
6. Zapísať aj riadok `Alignment` — či hlási `unverified` a koľko strát.

Postupnosť pokusov:

| # | *d* | kam sa pozerať po odkrytí |
|---|---|---|
| 1 | 0 m | na to isté miesto |
| 2 | 1 m | na tú istú stenu |
| 3 | 2 m | na tú istú stenu |
| 4 | 5 m | na tú istú stenu |
| 5 | 2 m | **za roh**, do chodby, kde kamera ešte nebola |
| 6 | 5 m | **za roh** |

Pokusy 5 a 6 sú tie zaujímavé. Ak hypotéza platí, chyba v 1–4 bude malá aj pri piatich
metroch, a v 5–6 veľká aj pri dvoch.

**Pri každom pokuse si všimni, či nastal skok.** Skok znamená, že sa ARCore relokalizoval —
to je dobrý prípad. Keď prekryv sedí zle a **žiaden skok nebol**, to je tá tichá porucha.

## Test D — disky na podlahe

Od 0.1.6-alpha. **Nepotrebuje vytlačenú značku**, takže sa dá spraviť hneď a kdekoľvek.

Tlačidlo `Drop` položí disk na podlahu presne pod telefón, do hĺbky, ktorá je nastavená ako
výška očí (`FloorProbe.m_EyeHeightMeters`, štandardne 1,70 m). Výška pochádza od testujúceho,
nie z detekcie rovín — tá by znamenala merať odpoveď ARCore pomocou ARCore.

**Najprv kalibrácia.** Stoj rovno, telefón v úrovni očí, stlač `Drop`. Ak disk nesedí na
podlahe, uprav výšku v inspectore a zopakuj.

Dobrá správa je, že na tom až tak nezáleží: zlá výška posunie **všetky** disky o to isté, takže
rozptyl medzi nimi (D1) ani posun jedného disku voči podlahe (D2) sa tým nezmenia. Kalibrácia
je preto na to, aby sa disky dali pohodlne posudzovať okom, nie aby čísla platili.

Potom dve merania:

**D1 — zvislá zhoda.** Prejdi miestnosťou a polož štyri až šesť diskov na rovnú podlahu.
Na rovnej podlahe majú všetky ležať v rovnakej výške, takže `± N cm` na obrazovke je zvislá
chyba nazbieraná medzi nimi. Je v nej započítané aj to, ako presne držíš telefón — preto sa
oplatí sledovať skôr **tvar v čase** než jedno číslo: rast s prejdenou vzdialenosťou je drift,
náhodné kolísanie o pár centimetrov je ruka.

**D2 — drift bez značky.** Toto je to podstatné.

1. Polož disk a **odfoť ho** aj s okolím, nech je zrejmé, kde presne leží.
2. Prejdi sa — 20, 50, 100 m, aj cez iné miestnosti.
3. Vráť sa na to isté miesto a odfoť ten istý disk.
4. Odčítaj, o koľko sa posunul oproti bodu na podlahe.

Disky sú kotvené na `ARAnchor`, takže sa **nehýbu spolu s driftom** — presne preto ide o pevný
bod, voči ktorému sa drift dá vidieť. Čo sa posúva, je svet okolo nich.

Zapíš prejdenú vzdialenosť z obrazovky ku každému odčítaniu. Drift ako **percento z prejdenej
dráhy** je tvar, v akom sa to v literatúre uvádza.

## Test C — dlhý beh

Jeden súvislý prechod aspoň dvomi podlažiami, 5–10 minút, telefón držať pokojne. Značiť `Mark`
na každom poschodí a pri každom výraznom otočení.

Cieľom je zistiť, **ako často sa tracking láme pri normálnom používaní** a koľko z toho sú
relokalizácie oproti tichým poruchám.

## Čo poslať

CSV z `Android/data/sk.uniza.fri.frilens/files/`, poznámky a fotky. Nové stĺpce:

| stĺpec | čo znamená |
|---|---|
| `walked_m` | prevzorkovaná dráha — toto je to číslo |
| `path_raw_m` | surový súčet, na porovnanie |
| `jumps`, `jumped_m` | relokalizácie, ktoré appka videla |
| `blind_s`, `losses` | ako dlho a koľkokrát bol tracking preč |
| `verified` | 0 = od poslednej straty nebolo prezarovnanie, čísla sú neoverené |
| `origin_anchored` | 1 = origin drží `ARAnchor`, čo je zmena v tejto verzii |

## Čo tento test nezistí

**Ako presne model sedí na budove.** Na to treba zameranú značku (fáza 3a a 3b). Tu sa meria
len tracking — teda horná hranica toho, čo sa vôbec dá dosiahnuť, nech je model akokoľvek dobrý.
