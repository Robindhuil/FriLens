"""Plán inžinierskeho projektu FriLens ako dáta pre `gh_plan.py`.

Zdroj obsahu: docs/2026-09-05-plan-inzinierskeho-projektu.md. Keď sa zmení plán,
zmení sa najprv dokument a až potom tento súbor — nie naopak.

Nový projekt = kópia tohto súboru s inými `REPO`, `ISSUES` a `MILESTONES`.
`BOARD` zostáva rovnaký, lebo board je jeden pre všetky projekty.
"""

DOCS = "https://github.com/Robindhuil/FriLens/blob/main/docs"
PLAN_DOC = f"{DOCS}/2026-09-05-plan-inzinierskeho-projektu.md"

REPO = {"owner": "Robindhuil", "name": "FriLens"}

# Board je user-level, spoločný pre všetky projekty. Repozitár rozlišuje vstavané
# pole "Repository", preto tu žiadne pole "Projekt" nie je.
BOARD = {
    "owner": "@me",
    "title": "Robin — plán projektov",
    "description": "Jeden board naprieč repozitármi. Filtruj poľom Repository.",
    "fields": [
        {
            "name": "Semester",
            "type": "SINGLE_SELECT",
            "options": ["S1 ZS 2026/27", "S2 LS 2026/27", "S3 ZS 2027/28", "Priebežné"],
        },
        {"name": "Balík", "type": "TEXT"},
        {"name": "Odhad h", "type": "NUMBER"},
        {"name": "Skutočnosť h", "type": "NUMBER"},
        {"name": "Termín", "type": "DATE"},
    ],
}

LABELS = [
    ("semester-1", "1d76db", "Zimný semester 2026/27"),
    ("semester-2", "5319e7", "Letný semester 2026/27"),
    ("semester-3", "0e8a16", "Zimný semester 2027/28"),
    ("epic", "3e4b9e", "Zastrešujúci issue semestra"),
    ("kriticka-cesta", "d93f0b", "Sklz tu posunie celý projekt"),
    ("nevypusta-sa", "b60205", "Bez tohto semester nemá výsledok"),
    ("vypusta-sa-prve", "fbca04", "Prvé na rade, keď sa nestíha"),
    ("teren", "006b75", "Beh v budove — jeden stojí 3 h"),
    ("rozhodnutie", "d4c5f9", "Čaká na odpoveď, nie na prácu"),
    ("engine", "c2e0c6", "Lokalizácia a korekcie polohy"),
    ("hra", "f9d0c4", "Herná slučka, stanovištia, questy"),
    ("meranie", "bfd4f2", "Zber a vyhodnotenie dát"),
    ("nastroj", "e99695", "Editor, skripty, pipeline"),
    ("praca", "fef2c0", "Text práce a obhajoba"),
    ("rezia", "ededed", "Konzultácie, dokumentácia, správa"),
]

MILESTONES = [
    {
        "title": "Semester 1 — ZS 2026/27",
        "due_on": "2027-01-31T23:59:59Z",
        "description": "Zhoda modelu s budovou vyjadrená číslom a navigácia na ra0. 124 h.",
    },
    {
        "title": "Semester 2 — LS 2026/27",
        "due_on": "2027-06-30T23:59:59Z",
        "description": "Ablačná tabuľka a hrateľná trasa. Na konci musí byť aplikácia hotová. 125 h.",
    },
    {
        "title": "Semester 3 — ZS 2027/28",
        "due_on": "2028-01-31T23:59:59Z",
        "description": "Nasadenie na pozvané triedy (nov 2027), vyhodnotenie a text práce. 123 h.",
    },
]

# `key` je stabilný identifikátor balíka z plánu. `number` je predpokladané číslo
# issue pri založení do prázdneho repozitára — skript ho pri behu overuje podľa `key`
# v nadpise a nespolieha sa naň.
ISSUES = [
    # ---------------------------------------------------------------- epicy ---
    {
        "key": "S1",
        "number": 1,
        "title": "Semester 1 — Zhoda modelu s budovou a navigácia",
        "labels": ["epic", "semester-1"],
        "milestone": "Semester 1 — ZS 2026/27",
        "semester": "S1 ZS 2026/27",
        "hours": 124,
        "due": "2027-01-31",
        "body": f"""**Rozpočet 124 h** · ZS 2026/27 · ≈ 9,5 h týždenne

## Cieľ

Povedať **číslom**, ako presne navigačný model sedí na skutočnú fakultu, a na tom
postaviť navigáciu k miestnosti.

Každá ďalšia funkcia — hra, questy, korekcie polohy — stojí na predpoklade, že model je
1:1 a že sa naň dá zosúladiť. Ten predpoklad zatiaľ nikto neoveril. Keby neplatil, mení sa
celá práca a je lepšie to vedieť v októbri než o rok.

## Akceptačné kritériá semestra

- [ ] Štyri značky vytlačené, nalepené a **zamerané**; póza každej zapísaná v projekte
- [ ] Zhoda modelu s budovou vyjadrená **číslom** pri značke (cm) a **trendom** so vzdialenosťou
- [ ] Šírka chodby `ra000_corridor_3` overená pásmom proti modelovým 3,20 m
- [ ] Extraktor vygeneruje steny pre `ra0`; dvere v nich zostanú otvorené
- [ ] Korekcie A a B bežia ako samostatne vypínateľné režimy a zapisujú sa do logu
- [ ] Aplikácia dovedie používateľa k zadanej miestnosti na `ra0` šípkami na podlahe

## Čo musí platiť na konci

DOD 2027 padne pár týždňov po konci semestra. Nenasadzuje sa naň nič, ale appka musí
**behať na telefóne a logovať**, nie byť rozostavaná.

## Ak sa nestíha, vypúšťa sa v tomto poradí

1. Korekcia A (1.5) → presunúť do semestra 2 k časticovému filtru
2. Vyhladenie trasy v 1.7 → lomená čiara cez stredy trojuholníkov stačí na demo
3. Tretí terénny beh v 1.2 → dva behy sú minimum, pod to nie

**Nevypúšťa sa za žiadnych okolností:** 1.1 a 1.2.

[Plán inžinierskeho projektu]({PLAN_DOC})""",
    },
    {
        "key": "S2",
        "number": 2,
        "title": "Semester 2 — Potlačenie driftu a hra",
        "labels": ["epic", "semester-2"],
        "milestone": "Semester 2 — LS 2026/27",
        "semester": "S2 LS 2026/27",
        "hours": 125,
        "due": "2027-06-30",
        "body": f"""**Rozpočet 125 h** · LS 2026/27 · **nulová rezerva**

## Cieľ

Dokázať tabuľkou, o koľko znalosť modelu potlačí drift, a mať hrateľnú trasu po
stanovištiach.

**Na konci tohto semestra musí byť aplikácia hotová.** Vyhodnocovacia udalosť je
v novembri, teda v druhom mesiaci semestra 3. Čo sa do júna nedostane do aplikácie, do
práce sa nedostane vôbec.

## Akceptačné kritériá semestra

- [ ] Replay prehrá uložený beh a dá **rovnaký výsledok** ako beh na telefóne
- [ ] Časticový filter beží v reálnom čase na cieľovom zariadení (≥ 25 fps)
- [ ] **Ablačná tabuľka** s chybou v metroch a v % prejdenej dráhy pre štyri konfigurácie, ≥ 3 behy na konfiguráciu
- [ ] Po 15 s zakrytej kamery sa poloha obnoví na najbližšej značke do 5 s od jej uvidenia
- [ ] Nové stanovište sa pridá **v editore za pár minút**, bez zásahu do kódu
- [ ] Štyri stanovištia prejde cudzí človek bez inštruktáže

## Ak sa nestíha

Vypúšťa sa **2.2 časticový filter** — presunie sa do semestra 3 ako kapitola „experiment",
nie ako nasadený režim. Ablačná tabuľka baseline / A+B / značky je aj tak platná
a odpovedá na výskumnú otázku.

**Nikdy sa nevypúšťa 2.1 a 2.7.**

[Plán inžinierskeho projektu]({PLAN_DOC})""",
    },
    {
        "key": "S3",
        "number": 3,
        "title": "Semester 3 — Nasadenie a práca",
        "labels": ["epic", "semester-3"],
        "milestone": "Semester 3 — ZS 2027/28",
        "semester": "S3 ZS 2027/28",
        "hours": 123,
        "due": "2028-01-31",
        "body": f"""**Rozpočet 123 h** · ZS 2027/28 · udalosť **november 2027**

## Cieľ

Nasadiť aplikáciu na reálnu udalosť, vyhodnotiť dáta z reálnych používateľov a napísať
prácu.

September a október sú na odolnosť a prípravu, november na udalosť, december a január na
vyhodnotenie a písanie.

## Akceptačné kritériá semestra

- [ ] Aplikácia beží celý deň udalosti bez zásahu vývojára
- [ ] **≥ 50 dokončených behov** návštevníkov v telemetrii
- [ ] Porovnanie času do miestnosti proti kontrolnej skupine, so štatistickou významnosťou alebo s priznaním, že vzorka na ňu nestačí
- [ ] Dataset driftu s pravdou zo stanovíšť, zverejniteľný ako príloha práce
- [ ] Práca odovzdaná v termíne

## Ak sa nestíha

Vypúšťajú sa návštevy tried, **nie prvá z nich**. Jedna trieda (20–25 ľudí, 2 h) je
minimum — bez nej nie je s čím porovnávať kontrolnú skupinu.

Druhá poistka je, že **termín si určuješ sám**. Ak október utečie, udalosť sa posunie na
december a stále je pred odovzdaním.

Text práce sa nevypúšťa a nekráti. Ak sa niečo nestihne, nestihne sa funkcia, nie kapitola.

[Plán inžinierskeho projektu]({PLAN_DOC})""",
    },
]

ISSUES += [
    # ------------------------------------------------------------ semester 1 ---
    {
        "key": "1.1",
        "number": 4,
        "title": "1.1 Zameranie značiek",
        "parent": "S1",
        "labels": ["semester-1", "kriticka-cesta", "nevypusta-sa", "teren", "nastroj"],
        "milestone": "Semester 1 — ZS 2026/27",
        "semester": "S1 ZS 2026/27",
        "hours": 22,
        "body": f"""**Balík 1.1** · semester 1 · odhad **22 h** · kritická cesta · nevypúšťa sa

## Čo to je

Výber štyroch miest lokalizovateľných v navmeshi, tlač značky v mierke, meranie rámu
pravítkom, odčítanie pózy z hrán polygónov a **editorový nástroj na zadanie póz** — nie
ručné klikanie v scéne.

## Prečo je to prvé a nič sa nesmie začať pred ním

K 0.1.8-alpha je overený *prístroj*: dráha sedí na −2,7 %, kotvy prežijú stratu trackingu,
disky sadajú na navmesh. Overená **nie je zhoda modelu s budovou** — dovtedy sa meria
tracker, nie model. Značiek je viac než jedna zámerne: značka je jediná vec nezávislá od
mapy ARCore, takže je zároveň liekom na stratu trackingu ([ADR 006]({DOCS}/decisions/006-kotvenie-a-strata-trackingu.md)).

## Hotovo, keď

- [ ] Vybrané štyri miesta, každé jednoznačne lokalizovateľné v navmeshi
- [ ] Značky vytlačené v overenej mierke a nalepené
- [ ] Rám každej značky odmeraný pravítkom
- [ ] Póza každej značky odčítaná z hrán nav polygónov a zapísaná v projekte
- [ ] Póza sa zadáva editorovým nástrojom, nie klikaním v scéne

## Závisí od

Nič. Týmto sa semester začína.

## Blokuje

#5 (1.2 Meranie zhody), #10 (1.7 Navigácia), #17 (2.3 Prezarovnanie za behu)

## Odkazy

- [ADR 003 — Póza značky z nav polygónov]({DOCS}/decisions/003-poza-znacky-z-nav-polygonov.md)
- [Implementačný plán — fázy 3a, 3b]({DOCS}/2026-09-02-implementacny-plan.md)""",
    },
    {
        "key": "1.2",
        "number": 5,
        "title": "1.2 Meranie zhody model ↔ budova",
        "parent": "S1",
        "labels": ["semester-1", "kriticka-cesta", "nevypusta-sa", "teren", "meranie"],
        "milestone": "Semester 1 — ZS 2026/27",
        "semester": "S1 ZS 2026/27",
        "hours": 20,
        "body": f"""**Balík 1.2** · semester 1 · odhad **20 h** · kritická cesta · nevypúšťa sa

## Čo to je

Protokol fázy 6 zopakovaný **so zameranou značkou**: pohľad zblízka pri značke, overenie
šírky chodby pásmom, odchýlka po 10 / 25 / 50 / 100 m, návrat a prezarovnanie. **Tri behy**
(dva sú minimum).

Jeden terénny beh je v rozpočte za **3 h**, nie za 30 minút — cesta, príprava, beh,
stiahnutie logu, prvý pohľad.

## Prečo

Toto je jediný balík, ktorý odpovedá na otázku „sedí model na budovu?". Ak nesedí, mení sa
téma práce na *kvantifikáciu nepresnosti modelu* — čo je stále platná téma, ale treba to
vedieť v októbri.

## Hotovo, keď

- [ ] Zhoda modelu s budovou vyjadrená **číslom** pri značke (cm)
- [ ] Zhoda vyjadrená **trendom** so vzdialenosťou (10 / 25 / 50 / 100 m)
- [ ] Šírka chodby `ra000_corridor_3` overená pásmom proti modelovým 3,20 m
- [ ] Tri behy odbehnuté a vyhodnotené `tools/frilens_eval.py`
- [ ] Výsledky zapísané ako dokument v `docs/`

## Závisí od

#4 (1.1 Zameranie značiek) — bez zameranej značky nie je voči čomu merať.

## Blokuje

#8 (1.5 Korekcia A), #9 (1.6 Korekcia B) — bez baseline nie je proti čomu ukázať zlepšenie.

## Odkazy

- [Protokol baseline testu]({DOCS}/2026-09-04-protokol-baseline-testu.md)
- [Výsledky baseline testu]({DOCS}/2026-09-04-vysledky-baseline.md)
- [ADR 005 — Ako merať prejdenú vzdialenosť]({DOCS}/decisions/005-ako-merat-prejdenu-vzdialenost.md)""",
    },
    {
        "key": "1.3",
        "number": 6,
        "title": "1.3 Vyhodnocovacie skripty",
        "parent": "S1",
        "labels": ["semester-1", "nastroj", "meranie"],
        "milestone": "Semester 1 — ZS 2026/27",
        "semester": "S1 ZS 2026/27",
        "hours": 10,
        "state": "closed",
        "body": f"""**Balík 1.3** · semester 1 · odhad **10 h** · ✅ **hotové**

## Čo to je

Python nad CSV z behov: drift medzi značkami, skoky, straty trackingu, grafy. Raz napísané,
používané všetky tri semestre.

## Stav

Hotové pred začiatkom semestra — `tools/frilens_eval.py`, 23 kontrol v `--selftest`.
Issue je založený a rovno zatvorený, aby 10 h sedelo v rozpočte semestra a balík
nechýbal v poradí.

## Hotovo, keď

- [x] Metriky bežia nad `frilens-*.csv` iba so štandardnou knižnicou
- [x] Úseky medzi značkami, skoky so zvislou zložkou, straty trackingu
- [x] `--table` nad viacerými behmi, `--plot` s matplotlib
- [x] `--selftest` proti známym odpovediam

## Odkazy

- [`tools/README.md`](https://github.com/Robindhuil/FriLens/blob/main/tools/README.md)""",
    },
    {
        "key": "1.4",
        "number": 7,
        "title": "1.4 Extraktor stien",
        "parent": "S1",
        "labels": ["semester-1", "nastroj", "engine"],
        "milestone": "Semester 1 — ZS 2026/27",
        "semester": "S1 ZS 2026/27",
        "hours": 15,
        "body": f"""**Balík 1.4** · semester 1 · odhad **15 h**

## Čo to je

Hraničné hrany navmeshu → zvislé roviny; overenie proti známemu pôdorysu.

## Prečo

Odomyká korekcie **A** (zarovnanie kurzu), **C** (časticový filter) a **D**. Model budovy
na to netreba — steny sa dajú odvodiť z hraníc navmeshu, čo je celé zistenie
[analýzy geometrie]({DOCS}/2026-09-04-analyza-geometrie-a-stien.md).

## Hotovo, keď

- [ ] Extraktor vygeneruje steny pre `ra0`
- [ ] **Dvere v nich zostanú otvorené** (nezamurujú sa hranicou polygónu)
- [ ] Výsledok overený proti známemu pôdorysu

## Závisí od

Nič — dá sa robiť paralelne s 1.1 a 1.2, pracuje sa iba nad `navmesh.blend`.

## Blokuje

#8 (1.5 Korekcia A), #16 (2.2 Korekcia C — časticový filter)

## Odkazy

- [Analýza geometrie a stien]({DOCS}/2026-09-04-analyza-geometrie-a-stien.md)
- [ADR 001 — Zdroj navigačnej geometrie]({DOCS}/decisions/001-zdroj-navmesh-geometrie.md)""",
    },
    {
        "key": "1.5",
        "number": 8,
        "title": "1.5 Korekcia A — zarovnanie kurzu",
        "parent": "S1",
        "labels": ["semester-1", "engine", "vypusta-sa-prve"],
        "milestone": "Semester 1 — ZS 2026/27",
        "semester": "S1 ZS 2026/27",
        "hours": 10,
        "body": f"""**Balík 1.5** · semester 1 · odhad **10 h** · prvý na vypustenie pri sklze

## Čo to je

Histogram normál stien → dominantné smery chodieb → zrezanie driftu kurzu.
**Samostatne vypínateľný režim**, ktorý sa zapisuje do logu.

## Hotovo, keď

- [ ] Dominantné smery chodieb sa odvodia z normál stien
- [ ] Korekcia beží ako **vypínateľný režim**
- [ ] Stav režimu je v CSV logu, takže sa dá vyhodnotiť ablačne

## Závisí od

#7 (1.4 Extraktor stien), #5 (1.2 Meranie zhody — baseline)

## Blokuje

#21 (2.7 Ablačná štúdia)

## Ak sa nestíha

Presúva sa do semestra 2 k časticovému filtru. Odhad 10 h je zámerne veľkorysý — podľa
[ADR 007]({DOCS}/decisions/007-vyuzitie-modelu-na-lokalizaciu.md) je korekcia „takmer zadarmo".

## Odkazy

- [ADR 007 — Využitie modelu na lokalizáciu]({DOCS}/decisions/007-vyuzitie-modelu-na-lokalizaciu.md)""",
    },
    {
        "key": "1.6",
        "number": 9,
        "title": "1.6 Korekcia B — väzba na výšku podlahy",
        "parent": "S1",
        "labels": ["semester-1", "engine"],
        "milestone": "Semester 1 — ZS 2026/27",
        "semester": "S1 ZS 2026/27",
        "hours": 8,
        "body": f"""**Balík 1.6** · semester 1 · odhad **8 h**

## Čo to je

Výška podlahy z modelu → zrezanie zvislého driftu. **Samostatne vypínateľný režim.**

## Prečo

Baseline ukázal zvislý drift **až 3 m** a relokalizácie, ktoré majú zvislú zložku. Model
pritom výšku podlahy pozná presne — je to najlacnejšia korekcia v projekte.

## Hotovo, keď

- [ ] Výška podlahy sa číta z modelu pre aktuálne podlažie
- [ ] Korekcia beží ako **vypínateľný režim**
- [ ] Stav režimu je v CSV logu

## Závisí od

#5 (1.2 Meranie zhody — baseline)

## Blokuje

#21 (2.7 Ablačná štúdia)

## Odkazy

- [Výsledky baseline testu]({DOCS}/2026-09-04-vysledky-baseline.md)
- [ADR 007 — Využitie modelu na lokalizáciu]({DOCS}/decisions/007-vyuzitie-modelu-na-lokalizaciu.md)""",
    },
    {
        "key": "1.7",
        "number": 10,
        "title": "1.7 Navigácia po navmeshi",
        "parent": "S1",
        "labels": ["semester-1", "engine"],
        "milestone": "Semester 1 — ZS 2026/27",
        "semester": "S1 ZS 2026/27",
        "hours": 24,
        "body": f"""**Balík 1.7** · semester 1 · odhad **24 h** · najväčší balík semestra

## Čo to je

Graf susedností trojuholníkov → A\\*; vyhladenie trasy; vykreslenie šípok na podlahe v AR;
prepočet pri odbočení.

## Hotovo, keď

- [ ] Aplikácia dovedie používateľa k **zadanej miestnosti na `ra0`** šípkami na podlahe
- [ ] Trasa sa prepočíta, keď používateľ odbočí
- [ ] Šípky sedia na podlahe, nie vo vzduchu

## Závisí od

#4 (1.1 Zameranie značiek) — bez zosúladenia sa šípky nedajú položiť na správne miesto.

## Blokuje

#19 (2.5 Herná slučka), #24 (3.1 Odolnosť a záložná vetva)

## Ak sa nestíha

Vypúšťa sa **vyhladenie trasy** — lomená čiara cez stredy trojuholníkov na demo stačí.
Samotná navigácia sa nevypúšťa.""",
    },
    {
        "key": "1.8",
        "number": 11,
        "title": "1.8 Réžia semestra 1",
        "parent": "S1",
        "labels": ["semester-1", "rezia"],
        "milestone": "Semester 1 — ZS 2026/27",
        "semester": "S1 ZS 2026/27",
        "hours": 15,
        "body": f"""**Balík 1.8** · semester 1 · odhad **15 h** (12 % rozpočtu)

## Čo to je

Konzultácie s vedúcim, ADR, priebežná dokumentácia, semestrálna správa.

Réžia je **samostatný balík**, nie prirážka schovaná v ostatných. Dôvod: dokumentácia sa
píše priebežne od začiatku, takže text práce v treťom semestri nevzniká z ničoho, ale
z existujúcich analýz a ADR. To je celý dôvod, prečo je 45 h na písanie v S3 realistických.

## Hotovo, keď

- [ ] Semestrálna správa odovzdaná
- [ ] Každé rozhodnutie „prečo takto?" zapísané ako ADR v `docs/decisions/`
- [ ] `docs/README.md` sedí so skutočným stavom projektu

## Odkazy

- [Ako udržiavať dokumentáciu]({DOCS}/README.md)""",
    },
]

ISSUES += [
    # ------------------------- predpoklady, ktoré treba potvrdiť pred prácou ---
    # Nie sú to pracovné balíky a nemajú hodiny — rozpočet 375 h sa nimi nemení.
    # Sú v boarde preto, lebo tri veci menia plán a treba na ne odpoveď skôr,
    # než sa začne pracovať.
    {
        "key": "D1",
        "number": 12,
        "title": "Rozhodnutie: termín a forma hlavného vyhodnotenia",
        "parent": "S1",
        "labels": ["rozhodnutie", "semester-1"],
        "milestone": "Semester 1 — ZS 2026/27",
        "semester": "S1 ZS 2026/27",
        "hours": 0,
        "body": f"""**Predpoklad plánu** · bez hodinového odhadu · odpoveď treba **pred začiatkom práce**

## Otázka

1. **Má fakulta jesenný termín dňa otvorených dverí?** (november býva bežný) Ak áno,
   nahradí pozvané triedy a vyhodnotenie prebehne na skutočnom DOD vnútri semestra 3.
2. **Dá sa posunúť poradie semestrov** tak, aby tretí bol letný a končil v júni 2028?
   Vtedy by DOD vo februári 2028 padol do jeho stredu a vyhodnotenie by prebehlo na
   skutočnom dni otvorených dverí s najväčšou možnou vzorkou.

## Prečo to treba vedieť

Oba DOD v okne projektu ležia mimo použiteľného času: **február 2027** je päť mesiacov po
vzniku aplikácie, teda priskoro, a **február 2028** je až po odovzdaní práce. Preto sa
hlavné vyhodnotenie presunulo na pozvané triedy v novembri 2027, ktorých termín je pod
kontrolou.

## Dopad odpovede

Kladná odpoveď mení **názov udalosti v balíku 3.3**, nie rozpočet ani poradie práce.
Možnosť 2 je lepšia varianta než pozvané triedy a plán sa na ňu prepíše bez zmeny obsahu
práce.

## Hotovo, keď

- [ ] Otázka položená vedúcemu projektu / študijnému oddeleniu
- [ ] Odpoveď zapísaná; ak mení plán, plán prepísaný a #26 (3.3 Udalosť) upravený

## Odkazy

- [ADR 009 — Vyhodnotenie sa neviaže na DOD]({DOCS}/decisions/009-vyhodnotenie-sa-neviaze-na-den-otvorenych-dveri.md)
- [Plán — kalendár]({PLAN_DOC})""",
    },
    {
        "key": "D2",
        "number": 13,
        "title": "Rozhodnutie: Android, alebo aj iOS",
        "parent": "S1",
        "labels": ["rozhodnutie", "semester-1"],
        "milestone": "Semester 1 — ZS 2026/27",
        "semester": "S1 ZS 2026/27",
        "hours": 0,
        "body": f"""**Predpoklad plánu** · bez hodinového odhadu · rozhodnúť **v semestri 1**

## Otázka

Zostane projekt na Androide, alebo sa vráti aj iOS?

## Stav

Plán počíta s Androidom. `com.unity.xr.arkit` bol z projektu odstránený vo fáze 0 vrátane
osirených `AR Kit Loader.asset` a `AR Kit Settings.asset`.

## Dopad odpovede

Návrat iOS je technicky priechodný cez AR Foundation, ale znamená **Mac, vývojársky účet
a približne 25 h navyše, ktoré v rozpočte nie sú**. Buď sa prijme obmedzenie na Android,
alebo sa vráti ARKit a **rozpočet semestra 3 sa kráti**.

Riziko pri odpovedi „len Android": iPhony návštevníkov znamenajú, že polovica vzorky je
mimo — dôležité pre #26 (3.3 Udalosť) a veľkosť N.

## Hotovo, keď

- [ ] Rozhodnuté a zapísané ako ADR
- [ ] Ak áno iOS: rozpočet S3 prepočítaný v pláne

## Odkazy

- [ADR 004 — Zariadenia bez ARCore]({DOCS}/decisions/004-zariadenia-bez-arcore.md)""",
    },
    {
        "key": "D3",
        "number": 14,
        "title": "Rozhodnutie: ako sa aplikácia dostane k návštevníkom",
        "parent": "S1",
        "labels": ["rozhodnutie", "semester-1"],
        "milestone": "Semester 1 — ZS 2026/27",
        "semester": "S1 ZS 2026/27",
        "hours": 0,
        "body": f"""**Predpoklad plánu** · bez hodinového odhadu · rozhodnúť **v semestri 1**

## Otázka

Požičané zariadenia fakulty, alebo inštalácia z obchodu?

| | dáta | vzorka |
|---|---|---|
| **požičané zariadenia** | lepšie, známy hardvér | menšia |
| **inštalácia z obchodu** | špinavšie, neznámy hardvér | väčšia |

## Prečo to treba vedieť skoro

Určuje **veľkosť N v semestri 3** a spôsob vyhodnotenia. Požičané telefóny sú istota,
obchod je bonus. Ak sa ide cez obchod, treba na to čas v #25 (3.2 Príprava nasadenia)
a treba to vedieť skôr než mesiac pred udalosťou.

## Hotovo, keď

- [ ] Rozhodnuté a zapísané ako ADR
- [ ] Ak požičané: zistený počet dostupných zariadení fakulty
- [ ] Dopad na akceptačné kritérium „≥ 50 dokončených behov" overený

## Odkazy

- [Plán — riziká]({PLAN_DOC})""",
    },
]

ISSUES += [
    # ------------------------------------------------------------ semester 2 ---
    {
        "key": "2.1",
        "number": 15,
        "title": "2.1 Offline replay",
        "parent": "S2",
        "labels": ["semester-2", "kriticka-cesta", "nevypusta-sa", "nastroj"],
        "milestone": "Semester 2 — LS 2026/27",
        "semester": "S2 LS 2026/27",
        "hours": 12,
        "body": f"""**Balík 2.1** · semester 2 · odhad **12 h** · kritická cesta · nevypúšťa sa

## Čo to je

Prehratie zaznamenaného behu cez lokalizačný reťazec **bez telefónu**. Ladenie filtra na
dátach, nie v chodbe.

## Prečo je to prvé v semestri

**Násobí produktivitu všetkého ďalšieho.** Bez neho sa časticový filter ladí v teréne za
trojnásobok času a každá zmena parametra stojí 3 h terénneho behu. Zároveň je to jediné
miesto, kde sa v laboratóriu odhalí, že filter nebeží v reálnom čase — a nie až na udalosti.

## Hotovo, keď

- [ ] Replay prehrá uložený beh a dá **rovnaký výsledok** ako beh na telefóne
- [ ] Beží bez Unity editora alebo aspoň bez zariadenia
- [ ] Dá sa ním prehnať dávka behov naraz

## Závisí od

Nič — telemetria z 0.1.x už existuje.

## Blokuje

#16 (2.2 Časticový filter), #21 (2.7 Ablačná štúdia)""",
    },
    {
        "key": "2.2",
        "number": 16,
        "title": "2.2 Korekcia C — časticový filter",
        "parent": "S2",
        "labels": ["semester-2", "engine", "vypusta-sa-prve"],
        "milestone": "Semester 2 — LS 2026/27",
        "semester": "S2 LS 2026/27",
        "hours": 24,
        "body": f"""**Balík 2.2** · semester 2 · odhad **24 h** · **jadro práce** · prvý na vypustenie

## Čo to je

Stovky hypotéz polohy, posun odometriou, zabíjanie tých, čo prešli stenou. Map matching
časticovým filtrom.

## Hotovo, keď

- [ ] Filter beží v reálnom čase na cieľovom zariadení (**≥ 25 fps**)
- [ ] Beží ako samostatne vypínateľný režim a zapisuje sa do logu
- [ ] Odladený v replayi (#15), nie v chodbe

## Závisí od

#7 (1.4 Extraktor stien), #15 (2.1 Offline replay)

## Blokuje

#21 (2.7 Ablačná štúdia) — ale iba jednu konfiguráciu z nej.

## Ak sa nestíha

Toto je **prvá vec, ktorá sa vypúšťa** z celého semestra. Presunie sa do semestra 3 ako
kapitola „experiment", nie ako nasadený režim. Práca tým nestráca výsledok: ablačná tabuľka
baseline / A+B / značky je aj tak platná a odpovedá na výskumnú otázku, len s menším
rozsahom korekcií.

Riziko „nerozbehne sa v reálnom čase" odhalí replay v laboratóriu; fallback je A+B+značky.

## Odkazy

- [ADR 007 — Využitie modelu na lokalizáciu]({DOCS}/decisions/007-vyuzitie-modelu-na-lokalizaciu.md)""",
    },
    {
        "key": "2.3",
        "number": 17,
        "title": "2.3 Prezarovnanie na značkách za behu",
        "parent": "S2",
        "labels": ["semester-2", "engine"],
        "milestone": "Semester 2 — LS 2026/27",
        "semester": "S2 LS 2026/27",
        "hours": 10,
        "body": f"""**Balík 2.3** · semester 2 · odhad **10 h**

## Čo to je

Viac značiek po trase, automatické prezarovnanie pri uvidení.

## Prečo

Je to **liek na 15-sekundový útes z baseline**: pätnásť sekúnd zakrytej kamery spôsobí
chybu 13 až 36 metrov, ktorá sa ešte minútu po obnove zväčšuje. V budove plnej ľudí je to
normálna prevádzka, nie okrajový prípad. Značka je jediná vec nezávislá od mapy ARCore.

## Hotovo, keď

- [ ] Po 15 s zakrytej kamery sa poloha obnoví na najbližšej značke **do 5 s od jej uvidenia**
- [ ] Prezarovnanie je udalosť v logu, takže sa dá vyhodnotiť
- [ ] Beží ako samostatne vypínateľný režim

## Závisí od

#4 (1.1 Zameranie značiek)

## Blokuje

#21 (2.7 Ablačná štúdia)

## Odkazy

- [ADR 006 — Kotvenie a strata trackingu]({DOCS}/decisions/006-kotvenie-a-strata-trackingu.md)
- [Výsledky baseline testu]({DOCS}/2026-09-04-vysledky-baseline.md)""",
    },
    {
        "key": "2.4",
        "number": 18,
        "title": "2.4 Autorský pipeline",
        "parent": "S2",
        "labels": ["semester-2", "nastroj", "hra"],
        "milestone": "Semester 2 — LS 2026/27",
        "semester": "S2 LS 2026/27",
        "hours": 16,
        "body": f"""**Balík 2.4** · semester 2 · odhad **16 h** · druhý z troch prínosov práce

## Čo to je

Definícia stanovišťa a questu ako asset; editorové okno; predvyplnenie z `Rooms.json`;
validácia dosiahnuteľnosti po navmeshi.

## Prečo

**Bez tohto sa dvadsať stanovíšť nedá udržiavať.** Je to zároveň poistka proti riziku
„obsah od katedier nepríde" — pipeline umožní generický obsah.

## Hotovo, keď

- [ ] Nové stanovište sa pridá **v editore za pár minút**, bez zásahu do kódu
- [ ] Stanovištia a questy sú assety, nie ručne umiestnené objekty v scéne
- [ ] Editor predvyplní, čo vie, z `Rooms.json`
- [ ] Validácia povie, že stanovište je po navmeshi dosiahnuteľné

## Závisí od

Nič — `navmesh.blend` a `Rooms.json` existujú.

## Blokuje

#19 (2.5 Herná slučka)""",
    },
    {
        "key": "2.5",
        "number": 19,
        "title": "2.5 Herná slučka",
        "parent": "S2",
        "labels": ["semester-2", "hra"],
        "milestone": "Semester 2 — LS 2026/27",
        "semester": "S2 LS 2026/27",
        "hours": 21,
        "body": f"""**Balík 2.5** · semester 2 · odhad **21 h**

## Čo to je

Trasa stanovíšť, skenovanie značky, body, postup, obrazovky, obsah pre 4 stanovištia.

## Prečo je to zároveň meranie

Herné stanovište **je zameraná značka**, teda bod so známou pravdou. Návštevník si myslí,
že zbiera body; aplikácia zbiera dvojicu *(nazbieraná chyba, prejdená vzdialenosť)*
s referenciou. Herná mechanika a meracia infraštruktúra sú tá istá vec.

## Hotovo, keď

- [ ] **Štyri stanovištia prejde cudzí človek bez inštruktáže**
- [ ] Skenovanie značky na stanovišti zapíše meranie s referenciou
- [ ] Dĺžka hry cielene 20–30 min (batéria a prehrievanie)

## Závisí od

#18 (2.4 Autorský pipeline), #10 (1.7 Navigácia po navmeshi)

## Blokuje

#25 (3.2 Príprava nasadenia)

## Odkazy

- [ADR 008 — Rozšírenie rozsahu na navigáciu a hru]({DOCS}/decisions/008-rozsirenie-rozsahu-na-navigaciu-a-hru.md)""",
    },
    {
        "key": "2.6",
        "number": 20,
        "title": "2.6 Telemetria s odovzdaním",
        "parent": "S2",
        "labels": ["semester-2", "meranie"],
        "milestone": "Semester 2 — LS 2026/27",
        "semester": "S2 LS 2026/27",
        "hours": 10,
        "body": f"""**Balík 2.6** · semester 2 · odhad **10 h**

## Čo to je

Anonymné ID, súhlasová obrazovka, upload behu, **žiadne snímky z kamery na disk**.

## Prečo v semestri 2, nie v treťom

Ochrana osobných údajov je riziko s dopadom „zastavenie nasadenia". Rieši sa tu, **nie deň
pred udalosťou**. Bez uploadu sa navyše dáta zo stoviek návštevníkov nemajú ako dostať
z požičaných telefónov.

## Hotovo, keď

- [ ] Anonymné ID bez väzby na osobu
- [ ] Súhlas pri prvom spustení
- [ ] Beh sa nahrá na server; pri výpadku siete sa nestratí
- [ ] Overené, že na disk nejde žiadna snímka z kamery

## Závisí od

Nič — CSV telemetria z 0.1.x existuje.

## Blokuje

#26 (3.3 Udalosť)""",
    },
    {
        "key": "2.7",
        "number": 21,
        "title": "2.7 Ablačná štúdia",
        "parent": "S2",
        "labels": ["semester-2", "kriticka-cesta", "nevypusta-sa", "meranie", "teren"],
        "milestone": "Semester 2 — LS 2026/27",
        "semester": "S2 LS 2026/27",
        "hours": 14,
        "body": f"""**Balík 2.7** · semester 2 · odhad **14 h** · nevypúšťa sa

## Čo to je

**≥ 12 behov**: baseline / A+B / A+B+C / A+B+C+značky. Vyhodnotenie skriptami z balíka 1.3.

## Prečo sa nevypúšťa

Je to **jediný výstup, ktorý odpovedá na výskumnú otázku**: o koľko dokáže znalosť
existujúceho navigačného modelu budovy potlačiť drift VIO na bežnom telefóne, bez
inštalácie čohokoľvek do budovy.

Otázka je zvolená tak, aby mala odpoveď aj vtedy, keď výsledok bude „o málo". Neúspešná
korekcia je publikovateľný výsledok.

## Hotovo, keď

- [ ] **Ablačná tabuľka** s chybou v metroch a v % prejdenej dráhy pre štyri konfigurácie
- [ ] **≥ 3 behy na konfiguráciu** (spolu ≥ 12)
- [ ] Vyhodnotené `tools/frilens_eval.py`, výsledok ako dokument v `docs/`

## Závisí od

#15 (2.1 Replay), #8 (1.5 Korekcia A), #9 (1.6 Korekcia B), #16 (2.2 Korekcia C), #17 (2.3 Prezarovnanie)

Ak sa #16 vypustí, tabuľka má tri konfigurácie namiesto štyroch a **stále je platná**.

## Blokuje

#28 (3.5 Písanie práce)""",
    },
    {
        "key": "2.8",
        "number": 22,
        "title": "2.8 DOD 2027 — meranie v dave",
        "parent": "S2",
        "labels": ["semester-2", "meranie", "teren"],
        "milestone": "Semester 2 — LS 2026/27",
        "semester": "S2 LS 2026/27",
        "hours": 4,
        "due": "2027-02-28",
        "body": f"""**Balík 2.8** · semester 2 · odhad **4 h** · **pevný termín: február 2027**

## Čo to je

Jeden merací beh v budove plnej ľudí. **Nič sa nenasadzuje, nikto z návštevníkov appku
nevidí.**

## Prečo

Je to **jediná príležitosť odmerať tracking v prevádzkových podmienkach**, aké sa v prázdnej
chodbe nasimulovať nedajú. Deň otvorených dverí je raz za rok; DOD 2027 padne do prvých
týždňov semestra 2 a DOD 2028 je až po odovzdaní práce.

Aplikácia musí na konci semestra 1 **behať na telefóne a logovať**, inak sa tento balík
nedá odbehnúť.

## Hotovo, keď

- [ ] Beh odbehnutý počas DOD, CSV stiahnuté
- [ ] Vyhodnotené proti behom v prázdnej chodbe — koľko stojí dav
- [ ] Zistenie zapísané v `docs/`

## Závisí od

Aplikácia z konca semestra 1 (behá a loguje). Nič z tohto semestra.

## Odkazy

- [ADR 009 — Vyhodnotenie sa neviaže na DOD]({DOCS}/decisions/009-vyhodnotenie-sa-neviaze-na-den-otvorenych-dveri.md)""",
    },
    {
        "key": "2.9",
        "number": 23,
        "title": "2.9 Réžia semestra 2",
        "parent": "S2",
        "labels": ["semester-2", "rezia"],
        "milestone": "Semester 2 — LS 2026/27",
        "semester": "S2 LS 2026/27",
        "hours": 14,
        "body": """**Balík 2.9** · semester 2 · odhad **14 h** (12 % rozpočtu)

## Čo to je

Konzultácie, ADR, dokumentácia, semestrálna správa.

## Pozor na tento semester

Semester je nabitý **presne na strop, bez rezervy**. Poistkou je zoznam, čo sa vypúšťa
(najprv #16 časticový filter), nie optimizmus. Ak réžia začne požierať prácu, vypúšťa sa
podľa zoznamu — nešetrí sa na dokumentácii, lebo z nej vzniká text práce.

## Hotovo, keď

- [ ] Semestrálna správa odovzdaná
- [ ] Rozhodnutia semestra zapísané ako ADR
- [ ] Priebežná dokumentácia sedí so stavom aplikácie""",
    },
]

ISSUES += [
    # ------------------------------------------------------------ semester 3 ---
    {
        "key": "3.1",
        "number": 24,
        "title": "3.1 Odolnosť a záložná vetva",
        "parent": "S3",
        "labels": ["semester-3", "engine"],
        "milestone": "Semester 3 — ZS 2027/28",
        "semester": "S3 ZS 2027/28",
        "hours": 18,
        "body": f"""**Balík 3.1** · semester 3 · odhad **18 h** · september–október 2027

## Čo to je

2D pôdorys ako **plnohodnotná náhrada** pri strate trackingu aj na zariadeniach bez ARCore;
obnova session; hlášky, ktoré nelžú.

## Prečo

Na udalosti nie je vývojár pri každom telefóne. `AR Optional` znamená, že sa appka
nainštaluje aj tam, kde ARCore nie je — a tam musí niečo robiť, nie spadnúť.

## Hotovo, keď

- [ ] 2D pôdorys dovedie používateľa k miestnosti aj bez AR
- [ ] Strata trackingu prepne na pôdorys, nie na zamrznutú obrazovku
- [ ] Session sa obnoví po prepnutí appky na pozadie
- [ ] Žiadna hláška netvrdí, že poloha je známa, keď nie je

## Závisí od

#10 (1.7 Navigácia po navmeshi)

## Blokuje

#26 (3.3 Udalosť)

## Odkazy

- [ADR 004 — Zariadenia bez ARCore]({DOCS}/decisions/004-zariadenia-bez-arcore.md)""",
    },
    {
        "key": "3.2",
        "number": 25,
        "title": "3.2 Príprava nasadenia",
        "parent": "S3",
        "labels": ["semester-3", "hra"],
        "milestone": "Semester 3 — ZS 2027/28",
        "semester": "S3 ZS 2027/28",
        "hours": 18,
        "body": """**Balík 3.2** · semester 3 · odhad **18 h** · september–október 2027

## Čo to je

Obsah od katedier, zariadenia, dohoda so školami, prevádzkový postup pre obsluhu, skúšobný
beh so študentmi **týždeň vopred**.

## Poistky proti rizikám

- **Obsah od katedier nepríde** → termín pre katedry o mesiac skôr, než treba; autorský
  pipeline (#18) umožní generický obsah
- **Školy neprídu alebo zrušia** → osloviť viac škôl, než treba, a rozložiť na tri termíny

## Hotovo, keď

- [ ] Obsah pre stanovištia od katedier v projekte (alebo generický náhradný)
- [ ] Zariadenia zabezpečené podľa rozhodnutia #14
- [ ] Dohodnuté aspoň tri termíny s triedami
- [ ] Prevádzkový postup pre obsluhu napísaný — kto čo robí, keď sa niečo pokazí
- [ ] Skúšobný beh so študentmi odbehnutý týždeň pred prvou triedou

## Závisí od

#19 (2.5 Herná slučka), #14 (Rozhodnutie: distribúcia)

## Blokuje

#26 (3.3 Udalosť)""",
    },
    {
        "key": "3.3",
        "number": 26,
        "title": "3.3 Udalosť — pozvané triedy (nov 2027)",
        "parent": "S3",
        "labels": ["semester-3", "kriticka-cesta", "nevypusta-sa", "meranie", "teren"],
        "milestone": "Semester 3 — ZS 2027/28",
        "semester": "S3 ZS 2027/28",
        "hours": 12,
        "due": "2027-11-30",
        "body": f"""**Balík 3.3** · semester 3 · odhad **12 h** · **november 2027** · nevypúšťa sa

## Čo to je

Tri až štyri návštevy tried po 20–25 ľuďoch. Obsluha, zber telemetrie a dotazníkov,
**kontrolná skupina s papierovou mapou** — polovica triedy s aplikáciou, polovica s mapou,
tá istá trasa v ten istý čas.

## Prečo pozvané triedy a nie DOD

Termín si určuješ sám, takže najväčšie rozvrhové riziko projektu mizne. Vzorka je čistejšia:
trieda príde naraz a dá sa rozdeliť na dve skupiny idúce tou istou trasou v ten istý čas —
na dni otvorených dverí sa to nedá. A dá sa to zopakovať: ak prvá trieda odhalí chybu, druhá
príde o dva týždne.

## Hotovo, keď

- [ ] Aplikácia beží celý deň udalosti **bez zásahu vývojára**
- [ ] **≥ 50 dokončených behov** návštevníkov v telemetrii
- [ ] Kontrolná skupina s papierovou mapou odbehla tú istú trasu
- [ ] Dotazníky vyzbierané

## Závisí od

#24 (3.1 Odolnosť), #25 (3.2 Príprava nasadenia), #20 (2.6 Telemetria s odovzdaním)

## Blokuje

#27 (3.4 Vyhodnotenie)

## Ak sa nestíha

Vypúšťajú sa ďalšie triedy, **nie prvá**. Jedna trieda (20–25 ľudí, 2 h) je minimum, pod
ktoré sa ísť nedá. Ak október utečie, udalosť sa posunie na december a stále je pred
odovzdaním.

> Ak z #12 vyjde, že fakulta má jesenný DOD, tento balík sa naň presunie — mení sa názov
> udalosti, nie rozpočet ani obsah.

## Odkazy

- [ADR 009 — Vyhodnotenie sa neviaže na DOD]({DOCS}/decisions/009-vyhodnotenie-sa-neviaze-na-den-otvorenych-dveri.md)""",
    },
    {
        "key": "3.4",
        "number": 27,
        "title": "3.4 Vyhodnotenie",
        "parent": "S3",
        "labels": ["semester-3", "meranie"],
        "milestone": "Semester 3 — ZS 2027/28",
        "semester": "S3 ZS 2027/28",
        "hours": 18,
        "body": """**Balík 3.4** · semester 3 · odhad **18 h** · december 2027

## Čo to je

Drift medzi stanovišťami naprieč všetkými behmi; čas do miestnosti appka vs. mapa;
dotazník; pokrytie zariadení; miera dokončenia.

## Hotovo, keď

- [ ] Porovnanie času do miestnosti proti kontrolnej skupine, **so štatistickou významnosťou
      alebo s priznaním, že vzorka na ňu nestačí**
- [ ] **Dataset driftu** s pravdou zo stanovíšť, zverejniteľný ako príloha práce
- [ ] Pokrytie zariadení a miera dokončenia spočítané
- [ ] Vyhodnotené skriptami z #6 (1.3), nie ručne v tabuľke

## Závisí od

#26 (3.3 Udalosť), #6 (1.3 Vyhodnocovacie skripty)

## Blokuje

#28 (3.5 Písanie práce)""",
    },
    {
        "key": "3.5",
        "number": 28,
        "title": "3.5 Písanie práce",
        "parent": "S3",
        "labels": ["semester-3", "nevypusta-sa", "praca"],
        "milestone": "Semester 3 — ZS 2027/28",
        "semester": "S3 ZS 2027/28",
        "hours": 45,
        "due": "2028-01-31",
        "body": """**Balík 3.5** · semester 3 · odhad **45 h** · najväčší balík projektu · **nevypúšťa sa**

## Čo to je

Text, obrázky, tabuľky — **z priebežných analýz a ADR, nie z ničoho**.

## Prečo je 45 h realistických

Dokumentácia sa píše priebežne od začiatku projektu (balíky 1.8, 2.9, 3.6). K začiatku
písania existuje 9 ADR a 5+ analytických dokumentov. Text nevzniká v treťom semestri
z prázdneho súboru.

## Tri prínosy, ktoré má práca obhájiť

1. **Lokalizačný engine** — štyri korekcie ako samostatne vypínateľné režimy, s ablačným meraním
2. **Autorský pipeline** — trasy, stanovištia a questy z `navmesh.blend` a `Rooms.json` bez ručného umiestňovania
3. **Overenie v prevádzke** — dataset z reálnej udalosti, nie z prázdnej chodby s piatimi kolegami

## Hotovo, keď

- [ ] Práca odovzdaná **v termíne**
- [ ] Ablačná tabuľka z #21 v texte
- [ ] Dataset z #27 ako príloha
- [ ] Výskumná otázka zodpovedaná — aj keby odpoveď bola „o málo"

## Závisí od

#27 (3.4 Vyhodnotenie), #21 (2.7 Ablačná štúdia)

## Nevypúšťa sa

Text práce sa nevypúšťa a nekráti. **Ak sa niečo nestihne, nestihne sa funkcia, nie kapitola.**""",
    },
    {
        "key": "3.6",
        "number": 29,
        "title": "3.6 Réžia a obhajoba",
        "parent": "S3",
        "labels": ["semester-3", "rezia", "praca"],
        "milestone": "Semester 3 — ZS 2027/28",
        "semester": "S3 ZS 2027/28",
        "hours": 12,
        "body": """**Balík 3.6** · semester 3 · odhad **12 h**

## Čo to je

Konzultácie, oponentúra, príprava prezentácie.

## Hotovo, keď

- [ ] Reakcia na posudky vedúceho a oponenta pripravená
- [ ] Prezentácia na obhajobu hotová
- [ ] Demo aplikácie funguje na zariadení, ktoré ide na obhajobu

## Závisí od

#28 (3.5 Písanie práce)"""
    },
]
