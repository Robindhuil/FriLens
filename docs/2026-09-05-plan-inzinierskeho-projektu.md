# Plán inžinierskeho projektu — tri semestre

**Verzia:** 0.2.0-alpha · **Dátum:** 2026-09-05 · **Stav:** návrh pre vedúceho projektu
**Rozpočet:** 3 × 125 h = **375 h** · **Predpoklad:** [ADR 008](decisions/008-rozsirenie-rozsahu-na-navigaciu-a-hru.md)

---

## Zhrnutie pre vedúceho projektu

**Téma.** Presné určovanie polohy telefónu vnútri budovy fakulty bez akejkoľvek inštalovanej
infraštruktúry — len z kamery, senzorov telefónu a **existujúceho navigačného modelu budovy**.
Overené aplikáciou, ktorá na dni otvorených dverí naviguje návštevníka k miestnostiam a vedie
ho hrou po stanovištiach.

**Prečo to nie je triviálne.** Bežná AR (ARCore) si mapu stavia za chôdze a jej chyba rastie
s prejdenou vzdialenosťou. Vlastné merania z leta 2026 ukazujú, že **pätnásť sekúnd zakrytej
kamery spôsobí chybu 13 až 36 metrov**, ktorá sa ešte minútu po obnove zväčšuje. V budove plnej
ľudí je to normálna prevádzka, nie okrajový prípad. Práca útočí presne na toto.

**Ako to riešime.** Fakulta má navigačný model všetkých podlaží. Ten sa dá použiť ako mapa,
voči ktorej sa telefón lokalizuje — chyba potom nie je ohraničená dĺžkou chôdze, ale presnosťou
modelu. Postupne sa pridávajú štyri korekcie (kurz podľa smerov chodieb, väzba na výšku podlahy,
map matching časticovým filtrom, prezarovnanie na značkách) a **každá sa meria samostatne**.

**Kde je meranie.** Herné stanovište je zároveň zameraná značka, teda bod so známou pravdou.
Návštevník si myslí, že zbiera body; aplikácia zbiera dvojicu *(nazbieraná chyba, prejdená
vzdialenosť)* s referenciou. Sto návštevníkov × osem stanovíšť je 800 meraní za jedno popoludnie.
**Herná mechanika a meracia infraštruktúra sú tá istá vec.**

**Semestre v jednej vete.**

| | Cieľ | Merateľný výstup |
|---|---|---|
| **1.** | Zistiť, ako presne model sedí na budovu, a postaviť navigáciu | číslo zhody model↔budova; trasa k miestnosti na jednom podlaží |
| **2.** | Potlačiť drift znalosťou modelu a postaviť hru | ablačná tabuľka baseline vs. korekcie na ≥ 12 behoch; hrateľná trasa |
| **3.** | Nasadiť na reálnu udalosť a vyhodnotiť | dataset zo stoviek reálnych behov; porovnanie s kontrolnou skupinou; práca |

---

## Kalendár: kedy je udalosť a kedy semester

Toto je jediné miesto, kde sa plán musel zmeniť oproti prvému návrhu, a je to dôležité.

| | obdobie | udalosť v okne |
|---|---|---|
| **Semester 1** | ZS 2026/27, sep – jan | — |
| | **február 2027** | **DOD 2027** — aplikácia existuje päť mesiacov, na nasadenie priskoro |
| **Semester 2** | LS 2026/27, feb – jún | DOD 2027 padne do prvých týždňov |
| | júl – august 2027 | mimo rozpočtu projektu |
| **Semester 3** | ZS 2027/28, sep – **jan 2028** | **hlavné vyhodnotenie: november 2027** |
| | február 2028 | **DOD 2028** — už po odovzdaní práce |

**Zrážka.** Deň otvorených dverí je vo februári, tretí semester končí v januári. Oba DOD
v okne projektu teda ležia mimo použiteľného času: prvý je príliš skoro, druhý až po odovzdaní.
Nasadenie na deň otvorených dverí sa **nedá použiť ako zdroj dát pre prácu**.

**Riešenie: hlavné vyhodnotenie sa neviaže na DOD.** V novembri 2027 sa pozvú tri až štyri
triedy stredoškolákov, po 20–25 ľuďoch. Vzorka 60–100 návštevníkov je na porovnanie s kontrolnou
skupinou dosť a na drift medzi stanovišťami dá stovky meraní.

Nie je to náhradné riešenie z núdze, je to **lepší plán**:

- **Termín si určuješ sám.** Najväčšie rozvrhové riziko projektu tým mizne úplne. Dátum DOD
  neurčuje ani vedúci projektu, ani ty.
- **Vzorka je čistejšia.** Trieda príde naraz, dá sa rozdeliť na skupinu s aplikáciou a kontrolnú
  s papierovou mapou, a obe idú tou istou trasou v ten istý čas. Na dni otvorených dverí sa to
  nedá — návštevníci prichádzajú priebežne a robia si, čo chcú.
- **Dá sa opakovať.** Ak prvá trieda odhalí chybu, druhá príde o dva týždne. Deň otvorených dverí
  je raz za rok a čo sa naň nestihne, nestihne sa vôbec.

**Čo sa s dňami otvorených dverí urobí namiesto toho:**

| kedy | čo |
|---|---|
| **DOD február 2027** | jeden merací beh v budove plnej ľudí. Nič sa nenasadzuje, nikto z návštevníkov appku nevidí. Je to jediná príležitosť odmerať tracking v dave a stojí 4 h. |
| **DOD február 2028** | ostrá prevádzka aplikácie pre návštevníkov, **po** odovzdaní práce. To, že projekt pokračuje aj po obhajobe, je argument pre fakultu, nie súčasť práce. |

> **Ešte overiť:** či fakulta nemá **jesenný termín dňa otvorených dverí** (november býva bežný).
> Ak áno, nahradí pozvané triedy a vyhodnotenie prebehne na skutočnom DOD vnútri semestra 3.
> Zmenil by sa tým iba názov udalosti v balíku 3.3, nie rozpočet ani poradie práce.

---

## Východiskový stav — čo už existuje

Projekt sa nezačína od nuly. K 0.1.8-alpha je hotové a **overené na zariadení**:

| | |
|---|---|
| Unity projekt | AR Foundation 6.4.3 + ARCore, URP, Android, IL2CPP, `sk.uniza.fri.frilens` |
| Extrakcia podlaží | 9 navigačných plôch z `navmesh.blend` (`ra0`–`ra3`, `rb0`–`rb3`, `rc0`) |
| Zosúladenie na značku | priemerovanie 30 vzoriek, matematika overená proti známym odpovediam |
| Meranie dráhy | overené pásmom, chyba **−2,7 %** na 8 m úseku |
| Kotvenie | `ARAnchor`, prežije stratu trackingu (overené) |
| Telemetria | CSV 4 Hz + udalosti, flush pri prechode na pozadie |
| Diagnostický HUD | UI Toolkit, tracking / značka / rozptyl / prejdená vzdialenosť |
| Režim bez ARCore | `AR Optional` + Preview vetva ([ADR 004](decisions/004-zariadenia-bez-arcore.md)) |
| Dokumentácia | 7 ADR + 5 analytických dokumentov, priebežne písané |

**Čo chýba a blokuje všetko ostatné:** vytlačená a **zameraná** značka (fázy 3a, 3b
[plánu](2026-09-02-implementacny-plan.md)). Dovtedy je overený *prístroj*, nie *zhoda modelu
s budovou*. Toto je prvý pracovný balík prvého semestra a nič sa nesmie začať pred ním.

Posledná poznámka k východisku: **dokumentácia sa píše priebežne od začiatku projektu.** Text
diplomovej práce tým nevzniká v treťom semestri z ničoho, ale z existujúcich analýz a ADR. To je
dôvod, prečo je 45 h na písanie v treťom semestri realistických a nie optimistických.

---

## Ultimátny cieľ a výskumná otázka

> **Výskumná otázka.** O koľko dokáže znalosť existujúceho navigačného modelu budovy potlačiť
> drift vizuálno-inerciálnej odometrie na bežnom telefóne, bez inštalácie čohokoľvek do budovy?

Otázka je zvolená tak, aby mala odpoveď aj vtedy, keď výsledok bude „o málo". Neúspešná korekcia
je publikovateľný výsledok; nefunkčná appka nie je.

**Ultimátny cieľ na jednu vetu:** telefón, ktorý po nasnímaní jednej značky vie počas celej
prehliadky fakulty, kde stojí, s chybou pod pol metra, bez čohokoľvek nainštalovaného do budovy —
a hra na deň otvorených dverí, ktorá to dokáže na stovkách reálnych návštevníkov.

### Tri prínosy, každý obhájiteľný samostatne

1. **Lokalizačný engine** — štyri korekcie z [ADR 007](decisions/007-vyuzitie-modelu-na-lokalizaciu.md)
   ako samostatne vypínateľné režimy, s ablačným meraním. Jadro práce.
2. **Autorský pipeline** — z `navmesh.blend` a `Rooms.json` vygenerovať trasy, stanovištia
   a questy bez ručného umiestňovania v Unity. Bez toho sa dvadsať stanovíšť nedá udržiavať.
3. **Overenie v prevádzke** — dataset z reálnej udalosti, nie z prázdnej chodby s piatimi kolegami.

---

## Rozpočet času a ako sa počítal

125 h na semester, 13 týždňov výučby → **≈ 9,5 h týždenne**. To je strop, ktorý určuje rozsah:
každý pracovný balík nižšie má odhad v hodinách a súčet sa do 125 h zmestí **vrátane rezervy**.

Pravidlá, podľa ktorých sú odhady robené:

- **Terénny beh stojí 3 h**, nie 30 minút — cesta, príprava, beh, stiahnutie logu, prvý pohľad.
- **Réžia je 12 %** — konzultácie, priebežná dokumentácia, semestrálna správa. Je to samostatný
  balík v každom semestri, nie prirážka schovaná v ostatných.
- **Rezerva 8 %** v prvých dvoch semestroch. V treťom je namiesto nej pevný termín udalosti.
- Balík, ktorý sa nezmestí, má v každom semestri napísané, **čo sa z neho vypustí** — nie „bude
  sa pracovať rýchlejšie".

---

## Semester 1 — Zhoda modelu s budovou a navigácia

**Cieľ:** povedať **číslom**, ako presne navigačný model sedí na skutočnú fakultu, a na tom
postaviť navigáciu k miestnosti.

**Prečo práve toto ako prvé:** každá ďalšia funkcia — hra, questy, korekcie polohy — stojí na
predpoklade, že model je 1:1 a že sa naň dá zosúladiť. Ten predpoklad zatiaľ nikto neoveril.
Keby neplatil, mení sa celá práca a je lepšie to vedieť v októbri než o rok.

### Pracovné balíky

| # | Balík | Obsah | h |
|---|---|---|---:|
| 1.1 | **Zameranie značiek** | výber štyroch miest lokalizovateľných v navmeshi; tlač v mierke; meranie rámu pravítkom; odčítanie pózy z hrán polygónov; editorový nástroj na zadanie póz (nie ručné klikanie v scéne) | 22 |
| 1.2 | **Meranie zhody model↔budova** | protokol fázy 6 so značkou: pohľad zblízka pri značke, overenie šírky chodby pásmom, odchýlka po 10 / 25 / 50 / 100 m, návrat a prezarovnanie; **tri behy** | 20 |
| 1.3 | **Vyhodnocovacie skripty** | Python nad CSV: drift medzi značkami, skoky, straty, grafy; raz napísané, používané všetky tri semestre | 10 |
| 1.4 | **Extraktor stien** | hraničné hrany navmeshu → zvislé roviny; overenie proti známemu pôdorysu; odomyká korekcie A, C, D | 15 |
| 1.5 | **Korekcia A — zarovnanie kurzu** | histogram normál stien → dominantné smery chodieb; zrezanie driftu kurzu; **vypínateľný režim** | 10 |
| 1.6 | **Korekcia B — väzba na výšku podlahy** | výška podlahy z modelu; zrezanie zvislého driftu (baseline ukázal až 3 m); **vypínateľný režim** | 8 |
| 1.7 | **Navigácia po navmeshi** | graf susedností trojuholníkov → A*; vyhladenie trasy; vykreslenie šípok na podlahe v AR; prepočet pri odbočení | 24 |
| 1.8 | **Réžia** | konzultácie, ADR, priebežná dokumentácia, semestrálna správa | 15 |
| | **Spolu** | | **124** |

Rezerva je v tom, že balíky 1.5 a 1.6 sú podľa [ADR 007](decisions/007-vyuzitie-modelu-na-lokalizaciu.md)
„takmer zadarmo" a odhad je na ne zámerne veľkorysý.

### Akceptačné kritériá

- [ ] Štyri značky vytlačené, nalepené a **zamerané**; póza každej zapísaná v projekte
- [ ] Zhoda modelu s budovou vyjadrená **číslom** pri značke (cm) a **trendom** so vzdialenosťou
- [ ] Šírka chodby `ra000_corridor_3` overená pásmom proti modelovým 3,20 m
- [ ] Extraktor vygeneruje steny pre `ra0`; dvere v nich zostanú otvorené
- [ ] A a B bežia ako samostatne vypínateľné režimy a zapisujú sa do logu
- [ ] Aplikácia dovedie používateľa k zadanej miestnosti na `ra0` šípkami na podlahe

### Ak sa nestíha, vypúšťa sa v tomto poradí

1. Korekcia A (1.5) → presunúť do semestra 2 k časticovému filtru
2. Vyhladenie trasy v 1.7 → lomená čiara cez stredy trojuholníkov stačí na demo
3. Tretí terénny beh v 1.2 → dva behy sú minimum, pod to nie

**Čo sa nevypúšťa za žiadnych okolností:** 1.1 a 1.2. Bez nich semester nemá výsledok.

### Čo musí byť hotové na február

DOD 2027 padne pár týždňov po konci tohto semestra. **Nenasadzuje sa naň nič** — ide sa tam iba
merať tracking v dave (balík 2.8). Znamená to však, že na konci semestra 1 musí appka **behať na
telefóne a logovať**, nie byť rozostavaná. Akceptačné kritériá vyššie sú napísané tak, aby to
vynútili.

---

## Semester 2 — Potlačenie driftu a hra

**Cieľ:** dokázať tabuľkou, o koľko znalosť modelu potlačí drift, a mať hrateľnú trasu po
stanovištiach.

Semester začína dňom otvorených dverí 2027, ktorý padne do jeho prvých týždňov. Ide sa naň merať,
nie nasadzovať (balík 2.8) — dôvody v [kalendári](#kalendár-kedy-je-udalosť-a-kedy-semester).

**Na konci tohto semestra musí byť aplikácia hotová.** Vyhodnocovacia udalosť je v novembri, teda
v druhom mesiaci semestra 3, a dovtedy zostáva len čas na odolnosť a prípravu. Čo sa do júna
nedostane do aplikácie, do práce sa nedostane vôbec.

### Pracovné balíky

| # | Balík | Obsah | h |
|---|---|---|---:|
| 2.1 | **Offline replay** | prehratie zaznamenaného behu cez lokalizačný reťazec bez telefónu; ladenie filtra na dátach, nie v chodbe. **Násobí produktivitu všetkého ďalšieho** | 12 |
| 2.2 | **Korekcia C — časticový filter** | stovky hypotéz polohy, posun odometriou, zabíjanie tých, čo prešli stenou; jadro práce | 24 |
| 2.3 | **Prezarovnanie na značkách za behu** | viac značiek po trase; automatické prezarovnanie pri uvidení; liek na 15-sekundový útes z baseline | 10 |
| 2.4 | **Autorský pipeline** | definícia stanovišťa a questu ako asset; editorové okno; predvyplnenie z `Rooms.json`; validácia dosiahnuteľnosti po navmeshi | 16 |
| 2.5 | **Herná slučka** | trasa stanovíšť, skenovanie značky, body, postup, obrazovky, obsah pre 4 stanovištia | 21 |
| 2.6 | **Telemetria s odovzdaním** | anonymné ID, súhlasová obrazovka, upload behu, žiadne snímky z kamery na disk | 10 |
| 2.7 | **Ablačná štúdia** | ≥ 12 behov: baseline / A+B / A+B+C / A+B+C+značky; vyhodnotenie skriptami z 1.3 | 14 |
| 2.8 | **DOD 2027 — meranie v dave** | jeden beh v budove plnej ľudí; nič sa nenasadzuje. Jediná príležitosť odmerať tracking v prevádzkových podmienkach, aké sa v prázdnej chodbe nasimulovať nedajú | 4 |
| 2.9 | **Réžia** | konzultácie, ADR, dokumentácia, semestrálna správa | 14 |
| | **Spolu** | | **125** |

Semester je nabitý presne na strop, bez rezervy. Poistkou je zoznam nižšie, nie optimizmus.

### Akceptačné kritériá

- [ ] Replay prehrá uložený beh a dá **rovnaký výsledok** ako beh na telefóne
- [ ] Časticový filter beží v reálnom čase na cieľovom zariadení (≥ 25 fps)
- [ ] **Ablačná tabuľka** s chybou v metroch a v % prejdenej dráhy pre štyri konfigurácie, ≥ 3 behy na konfiguráciu
- [ ] Po 15 s zakrytej kamery sa poloha obnoví na najbližšej značke do 5 s od jej uvidenia
- [ ] Nové stanovište sa pridá **v editore za pár minút**, bez zásahu do kódu
- [ ] Štyri stanovištia prejde cudzí človek bez inštruktáže

### Ak sa nestíha

Vypúšťa sa **2.2 časticový filter** — presunie sa do semestra 3 ako kapitola „experiment", nie
ako nasadený režim. Práca tým nestráca výsledok: ablačná tabuľka baseline / A+B / značky je aj
tak platná a odpovedá na výskumnú otázku, len s menším rozsahom korekcií.

Nikdy sa nevypúšťa 2.1 a 2.7. Replay je nástroj, bez ktorého sa ladí v teréne za trojnásobok
času, a 2.7 je jediný výstup, ktorý odpovedá na výskumnú otázku.

---

## Semester 3 — Nasadenie a práca

**Cieľ:** nasadiť aplikáciu na reálnu udalosť, vyhodnotiť dáta z reálnych používateľov a napísať
prácu.

**Udalosť je v novembri 2027** — tri až štyri pozvané triedy stredoškolákov po 20–25 ľuďoch, alebo
jesenný DOD, ak ho fakulta má. September a október sú na odolnosť a prípravu (36 h, čo je pri
9,5 h týždenne pohodlné), november na udalosť, december a január na vyhodnotenie a písanie.

### Pracovné balíky

| # | Balík | Obsah | h |
|---|---|---|---:|
| 3.1 | **Odolnosť a záložná vetva** | 2D pôdorys ako plnohodnotná náhrada pri strate trackingu aj na zariadeniach bez ARCore; obnova session; hlášky, ktoré nelžú | 18 |
| 3.2 | **Príprava nasadenia** | obsah od katedier, zariadenia, dohoda so školami, prevádzkový postup pre obsluhu, skúšobný beh so študentmi týždeň vopred | 18 |
| 3.3 | **Udalosť** | tri až štyri návštevy tried; obsluha, zber telemetrie a dotazníkov, **kontrolná skupina s papierovou mapou** — polovica triedy s aplikáciou, polovica s mapou, tá istá trasa v ten istý čas | 12 |
| 3.4 | **Vyhodnotenie** | drift medzi stanovišťami naprieč všetkými behmi; čas do miestnosti appka vs. mapa; dotazník; pokrytie zariadení; miera dokončenia | 18 |
| 3.5 | **Písanie práce** | text, obrázky, tabuľky — z priebežných analýz a ADR, nie z ničoho | 45 |
| 3.6 | **Réžia a obhajoba** | konzultácie, oponentúra, príprava prezentácie | 12 |
| | **Spolu** | | **123** |

### Akceptačné kritériá

- [ ] Aplikácia beží celý deň udalosti bez zásahu vývojára
- [ ] **≥ 50 dokončených behov** návštevníkov v telemetrii (3 triedy × ~20 ľudí)
- [ ] Porovnanie času do miestnosti proti kontrolnej skupine, so štatistickou významnosťou alebo s priznaním, že vzorka na ňu nestačí
- [ ] Dataset driftu s pravdou zo stanovíšť, zverejniteľný ako príloha práce
- [ ] Práca odovzdaná v termíne

### Ak sa nestíha

Vypúšťajú sa **návštevy tried, nie prvá z nich**. Jedna trieda (20–25 ľudí, 2 h) je minimum, pod
ktoré sa ísť nedá — bez nej nie je s čím porovnávať kontrolnú skupinu. Každá ďalšia je zväčšenie
vzorky a dá sa obetovať termínu.

Druhá poistka je, že **termín si určuješ sám**. Ak október utečie, udalosť sa posunie na december
a stále je pred odovzdaním. Toto je celý dôvod, prečo sa vyhodnotenie neviaže na deň otvorených
dverí.

Text práce sa nevypúšťa a nekráti. Ak sa niečo nestihne, nestihne sa funkcia, nie kapitola.

---

## Míľniky a kritická cesta

```
S1  značky zamerané ──► zhoda model↔budova zmeraná ──► steny ──► A, B
         │                                                        │
         └──────────────► navigácia A*  ─────────────────────────┐│
                                                                 ▼▼
S2  replay ──► časticový filter ──► ablačná tabuľka
       │                                  ▲
       └──► autorský pipeline ──► hra ────┘

S3  odolnosť ──► príprava ──► TRIEDY (nov 2027) ──► vyhodnotenie ──► práca
```

**Na kritickej ceste sú dve veci:** zameranie značiek (S1) a offline replay (S2). Tretia — dátum
udalosti — na nej bola, kým sa vyhodnotenie viazalo na deň otvorených dverí. Presunutím na pozvané
triedy sa z pevného termínu stal posunuteľný a **kritická cesta sa skrátila**.

---

## Riziká

| Riziko | Dopad | Čo s tým |
|---|---|---|
| **Model nie je 1:1 alebo polygóny nesedia na stenách** | mení tému práce | meria sa ako prvé, v S1; ak neplatí, prácou sa stáva *kvantifikácia nepresnosti modelu*, čo je stále platná téma |
| ~~Dátum udalosti padne za termín odovzdania~~ | ~~nie je čo vyhodnotiť~~ | **vyriešené** — oba DOD v okne projektu ležia mimo použiteľného času, preto sa vyhodnotenie presunulo na pozvané triedy v novembri 2027, ktorých termín je pod kontrolou ([kalendár](#kalendár-kedy-je-udalosť-a-kedy-semester)) |
| **Školy neprídu alebo zrušia termín** | menšia vzorka | osloviť viac škôl, než treba, a rozložiť na tri termíny; jedna trieda je minimum a to sa dá naplniť aj študentmi prvého ročníka |
| **Aplikácia nie je hotová do júna 2027** | udalosť v novembri sa nestihne pripraviť | semester 2 má na strope nulovú rezervu a **zoznam, čo sa vypúšťa**; prvým na rade je časticový filter, nie hra ani meranie |
| **Časticový filter sa nerozbehne v reálnom čase** | chýba najsilnejšia korekcia | replay (2.1) to odhalí v laboratóriu, nie na udalosti; fallback je A+B+značky |
| **Obsah od katedier nepríde** | prázdne stanovištia | autorský pipeline (2.4) umožní generický obsah; termín pre katedry o mesiac skôr než treba |
| **Návštevníci si appku nenainštalujú** | malé N | rozhodnúť distribúciu v S1; požičané telefóny sú istota, obchod je bonus |
| **Batéria a prehrievanie** | beh sa neukončí | dĺžka hry cielene 20–30 min; meranie spotreby už v S2 |
| **iPhony návštevníkov** | polovica vzorky mimo | rozhodnúť v S1 — buď sa prijme obmedzenie na Android, alebo sa vráti ARKit a rozpočet S3 sa kráti |
| **Ochrana osobných údajov** | zastavenie nasadenia | anonymné ID, žiadne snímky z kamery na disk, súhlas pri prvom spustení; rieši sa v 2.6, nie deň pred udalosťou |

---

## Predpoklady, ktoré treba potvrdiť pred schválením zadania

Tri veci menia plán a treba na ne odpoveď skôr, než sa začne pracovať:

1. **Či fakulta nemá jesenný termín dňa otvorených dverí** (november býva bežný). Ak áno,
   nahradí pozvané triedy a vyhodnotenie prebehne na skutočnom DOD vnútri semestra 3 — mení sa
   tým názov udalosti v balíku 3.3, nie rozpočet ani poradie práce.

   Druhá, širšia otázka na to isté: **dá sa poradie semestrov posunúť** tak, aby tretí bol letný
   a končil v júni 2028? Vtedy by DOD vo februári 2028 padol do jeho stredu a vyhodnotenie by
   prebehlo na skutočnom dni otvorených dverí s najväčšou možnou vzorkou. Ak to študijný plán
   dovolí, je to lepšia varianta než pozvané triedy a plán sa na ňu prepíše bez zmeny obsahu práce.
2. **Android, alebo aj iOS.** Plán počíta s Androidom (ARKit bol z projektu odstránený).
   Návrat iOS je technicky priechodný cez AR Foundation, ale znamená Mac, vývojársky účet
   a približne 25 h navyše, ktoré v rozpočte nie sú.
3. **Ako sa aplikácia dostane k návštevníkom** — požičané zariadenia fakulty (lepšie dáta,
   menšia vzorka) alebo inštalácia z obchodu (väčšia vzorka, špinavšie dáta). Určuje to veľkosť
   N v semestri 3 a spôsob vyhodnotenia.

---

## Čo práca zámerne nerieši

Aby bol rozsah obhájiteľný, mimo zostáva: okluzia virtuálnych objektov reálnymi, rekonštrukcia
geometrie budovy z kamery, viacpoužívateľské zdieľané AR, exteriérová navigácia a prepojenie
areálu, integrácia s rozvrhom, a trvalá prevádzka aplikácie po skončení projektu.

Každá z nich je samostatná téma a niektoré sú prirodzeným pokračovaním.
