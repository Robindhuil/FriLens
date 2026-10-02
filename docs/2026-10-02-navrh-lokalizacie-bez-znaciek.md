# Návrh: lokalizácia bez značiek a mozaiky proti driftu

**Verzia:** 0.3.1-alpha · **Dátum:** 2026-10-02 · **Stav:** nápad na rozbor, nič rozhodnuté

Zapísané z brainstormingu. Rozoberie sa po terénnom teste `0.3.1-alpha`
([protokol](2026-09-17-protokol-testu-viacerych-znaciek.md)) — kým nie je odmerané, či fit
z dvoch značiek drží, nemá zmysel stavať ďalšie vrstvy nad ním.

Cieľ do budúcnosti: **používateľ sa podľa značiek zosúlaďovať nebude.** Zapne appku,
rozhliadne sa a appka sama zistí, kde na fakulte stojí. Drift počas chôdze potláčajú
nenápadné mozaiky, ktoré appka zachytí aj bez toho, aby na ne niekto mieril.

## 1. Určenie polohy z obrazu (VPS)

### Pôvodná predstava a čo na nej treba upraviť

Body v Unity modeli, z nich fotky miestností pod rôznymi uhlami, a appka porovná, čo vidí, s
týmito fotkami.

Porovnanie s fotkou samo o sebe povie len **„si blízko fotky č. 17"** — úroveň miestnosti,
nie centimetre. Na pózu so šiestimi stupňami voľnosti treba vedieť, **kde v 3D ležia body,
ktoré na fotkách vidno.** Fotky teda nie sú mapa; mapa je mračno bodov, ktoré sa z nich
spočíta.

### Ako sa to robí (hierarchická lokalizácia, nástroj HLoc)

1. **Mapovanie, raz.** Prechod fakultou s nahrávaním snímok. COLMAP z nich spočíta mračno 3D
   bodov (structure from motion).
2. **Mapa do súradníc modelu.** Posun, kurz a mierka cez kontrolné body.
3. **Lokalizácia.** Používateľ sa rozhliadne, appka pošle niekoľko snímok. Najprv sa nájdu
   podobné fotky z mapy (globálny deskriptor, napr. NetVLAD), potom sa body na snímke spárujú
   s bodmi mapy (SuperPoint + LightGlue) a z párov sa cez PnP s RANSACom spočíta póza.
   V interiéri bežne **10–50 cm a 1–3°**.
4. **ARCore drží zvyšok.** Spočítaná póza sa použije ako jedno zosúladenie, presne ako dnes
   výsledok fitu zo značiek. Medzi lokalizáciami sleduje pohyb VIO.

### Čo sa dá zobrať z toho, čo už existuje

Body v Unity a ručné fotenie netreba. Stačí **mapovací režim**: zosúladiť sa značkami
a chodiť; každá snímka sa uloží spolu s pózou z ARCore, ktorá je v tej chvíli už v súradniciach
modelu. COLMAP potom nemusí pózy hádať od nuly a mapa vyjde rovno zarovnaná.

**Značky by tak ostali len pri mapovaní ako referencia — používateľ ich nepotrebuje.** To je
čisté zdôvodnenie, prečo sa do nich investovalo.

### Riziká na FRI

| riziko | prečo | čím sa tlmí |
|---|---|---|
| opakujúce sa chodby | rovnaké dvere, rovnaké poschodia — hlavný zdroj zámen | hlasovanie cez viac snímok z rozhliadnutia, geometrická kontrola, barometer na poschodie ([ADR 007](decisions/007-vyuzitie-modelu-na-lokalizaciu.md)) |
| svetlo a zmeny | deň/noc, plagáty, presunutý nábytok | mapovať viackrát v rôznych podmienkach |
| ľudia | na dni otvorených dverí dav zakryje veľkú časť obrazu | viac snímok, vyšší uhol kamery, poistka v mozaikách |
| kde to beží | neurónové párovanie je na telefón ťažké | najprv server v Pythone: telefón pošle snímku, dostane pózu; latencia nevadí, lebo medzitým sleduje ARCore |
| kruhovosť | lokalizácia voči mape napasovanej na model meria model modelom ([ADR 007](decisions/007-vyuzitie-modelu-na-lokalizaciu.md)) | samostatný režim, meraný zvlášť |

### Odhad

- serverový prototyp: **40–60 h**, zhruba polovica semestra,
- beh priamo na telefóne (ORB cez OpenCV, alebo neurónky cez Unity Sentis): samostatný semester.

Hotové riešenia (Immersal, Niantic Lightship VPS) robia presne toto. Ako jadro práce slabé
z rovnakého dôvodu ako Vuforia v ADR 007 — merali by cudzí systém — ako porovnávacia
referencia užitočné.

### Prvý krok: spike offline

Kým sa napíše riadok Unity kódu:

1. ~200 fotiek break roomu a priľahlej chodby, rôzne uhly a vzdialenosti.
2. Offline COLMAP + HLoc.
3. Lokalizovať ~20 testovacích fotiek, ktorých pózu poznáme zo zarovnania značkami.
4. Chyba v centimetroch a stupňoch rozhodne, či má veľká kapitola zmysel.

## 2. Mozaiky proti driftu

Mozaika je pre ARCore len ďalší obrázok v knižnici. **Solver z `0.3.x` už berie N značiek
a režim `Continuous` robí presne „prezarovnaj sa, keď značku zbadáš".** Je to rozšírenie
existujúceho, nie nová vec. Knižnica unesie stovky obrázkov, ARCore ich sleduje až 20 naraz.

### Na čo si dať pozor

- **Dizajn.** ARCore potrebuje veľa kontrastných, neopakujúcich sa rysov. Pravidelná mozaika
  z rovnakých dlaždíc **nefunguje**, nepravidelná umelecká áno. Každú pred tlačou overiť skóre
  kvality cez `arcoreimg eval-db`.
- **Veľkosť.** Značka 18 cm funguje na pár metrov. 60–100 cm sa zachytí z chodby aj za chôdze.
- **Nevedomé zachytenie je slabé miesto.** Pri chôdzi rozmazanie a šikmý uhol detekciu
  zhadzujú. Umiestnenie: výška očí, kolmo na smer chôdze, konce chodieb, križovatky, dvere.
- **Jedna mozaika dá dobre polohu, zle kurz** — to je presne nameraných 11° z `0.2.2`. Buď
  dvojice, alebo jemnejšia oprava: každé zachytenie ako polohová väzba do malého filtra alebo
  grafu póz namiesto tvrdého prezarovnania.
- **Povolenie fakulty** na lepenie a **zameranie každej** — postup už existuje
  ([plán zamerania](2026-09-09-plan-zamerania-znacky.md)).

### Synergia s hrou

Mozaiky ako súčasť hry na deň otvorených dverí — „nájdi všetky". Hráč na ne potom mieri sám,
zachytenie prestane byť náhoda a zároveň je jasné, prečo na stenách visia.

### Alternatíva bez lepenia

Existujúce nástenky a tabule ako obrázkové ciele. Tabuľky s číslami miestností sú si však
navzájom príliš podobné a plagáty sa menia.

### Odhad

Zhruba **20–25 h** vrátane dizajnu, tlače a zamerania.

## Navrhované poradie

1. **Mozaiky** — inkrementálne, stavia na existujúcom kóde, merateľné hneď.
2. **Spike VPS offline** — rozhodne, či ísť do veľkej kapitoly.
3. **VPS cez server** ako hlavná kapitola ďalšieho semestra; mozaiky ostanú ako poistka
   a nezávislá referencia na meranie.

Doplnkom je možnosť D/E z [ADR 007](decisions/007-vyuzitie-modelu-na-lokalizaciu.md) — roviny
a hĺbka z ARCore proti stenám modelu. Tiež lokalizuje kdekoľvek bez lepenia a je menej citlivá
na svetlo a plagáty než obraz, takže sa hodí do chodieb s málo textúrou.

## Otvorené otázky na rozbor

- Je VPS cieľ tejto práce, alebo nadstavba nad meraním? Mení to, koľko hodín mu patrí v
  [pláne](2026-09-05-plan-inzinierskeho-projektu.md).
- Server počas dňa otvorených dverí: kde pobeží a čo keď na chodbe nebude sieť.
- Smie sa na fakulte lepiť a kto to schváli.
