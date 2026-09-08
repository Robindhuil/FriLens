# Planning cez GitHub Projects

**Verzia:** 0.2.0-alpha · **Dátum:** 2026-09-08 · **Stav:** issues založené, board sa dorába skriptom
**Predpoklad:** [Plán inžinierskeho projektu](2026-09-05-plan-inzinierskeho-projektu.md)

Plán na tri semestre existoval ako dokument. Dokument sa dobre číta a zle sa v ňom robí:
nedá sa v ňom vidieť, čo je rozrobené, koľko hodín zo 125 už padlo a čo čaká na koho.
Tento dokument popisuje, kam sa plán preložil a ako sa v tom pracuje.

**Plán zostáva zdrojom pravdy.** Board je jeho vykonávacia vrstva. Keď sa mení rozsah,
mení sa najprv [plán](2026-09-05-plan-inzinierskeho-projektu.md), potom
`tools/planning/plan_frilens.py`, a až potom board.

---

## Ako je to rozdelené

| vrstva | čo v nej je | prečo tam |
|---|---|---|
| **Board** (Projects v2) | jeden, na účte, **naprieč všetkými repozitármi** | aby sa dalo naraz vidieť FriLens aj čokoľvek ďalšie; repozitáre rozlišuje vstavané pole *Repository* |
| **Issue** | jeden pracovný balík z plánu (1.1 … 3.6) | balík je najmenšia jednotka, ktorá má v pláne hodiny a akceptačné kritérium |
| **Epic** | jeden na semester (#1, #2, #3) | drží akceptačné kritériá semestra a zoznam „čo sa vypúšťa pri sklze" |
| **Sub-issue** | balík zavesený pod epic semestra | GitHub z toho počíta postup semestra sám |
| **Míľnik** | semester s dátumom (jan 2027, jún 2027, jan 2028) | jediné miesto s tvrdým termínom |
| **Label** | prierez cez semestre — `kriticka-cesta`, `teren`, `engine` … | otázky typu „čo všetko treba odbehnúť v budove" idú cez semestre |

Rozhodnutia, ktoré [plán](2026-09-05-plan-inzinierskeho-projektu.md) žiada zodpovedať
**pred začiatkom práce**, sú tiež issues (#12, #13, #14) s labelom `rozhodnutie`. Nemajú
hodiny — nie sú to balíky, rozpočet 375 h sa nimi nemení. Sú na boarde preto, lebo
odpoveď na ne mení plán a nesmú sa stratiť v e-maile.

---

## Polia boardu

Vstavaný **Status** a **Repository** plus päť vlastných:

| pole | typ | na čo |
|---|---|---|
| **Semester** | výber | S1 ZS 2026/27 · S2 LS 2026/27 · S3 ZS 2027/28 · Priebežné |
| **Balík** | text | `1.1`, `2.4` — drží poradie z plánu aj tam, kde sa zoznam preusporiada |
| **Odhad h** | číslo | z plánu, nemení sa |
| **Skutočnosť h** | číslo | koľko to naozaj trvalo |
| **Termín** | dátum | len tam, kde je tvrdý: #22 (DOD feb 2027), #26 (triedy nov 2027), #28 (odovzdanie) |

**Odhad vs. skutočnosť je celé jadro veci.** Rozpočet je 125 h na semester a je to strop,
nie odhad. Keď sa po štyroch balíkoch ukáže, že skutočnosť beží o tretinu vyššie, plán má
na to odpoveď pripravenú — zoznam *„ak sa nestíha, vypúšťa sa v tomto poradí"* v epicu
semestra. Bez zapisovania skutočných hodín sa ten zoznam nemá kedy použiť a semester sa
prekročí až vtedy, keď je neskoro.

---

## Pohľady, ktoré sa oplatí mať

Pohľady sa cez API vytvoriť nedajú, klikajú sa v boarde. Sú štyri a stoja dve minúty:

| pohľad | rozloženie | nastavenie |
|---|---|---|
| **Teraz** | Board | filter `status:open semester:"S1 ZS 2026/27"`, zoskupené podľa Status |
| **Rozpočet** | Table | zoskupené podľa *Semester*, súčet *Odhad h* a *Skutočnosť h* |
| **Naprieč projektmi** | Board | zoskupené podľa *Repository* — sem pribudnú ďalšie projekty |
| **Termíny** | Roadmap | podľa poľa *Termín* |

Vstavaný **Status** má po založení možnosti *Todo / In Progress / Done*. Pre tento projekt
sa oplatí premenovať na **Backlog / Tento týždeň / Robí sa / Blokované / Hotové** —
*Blokované* preto, že polovica balíkov na niečom visí a bez toho stĺpca sa to nedá odlíšiť
od „nikto sa tomu nevenuje".

---

## Ako sa v tom pracuje

**Týždenne** (≈ 9,5 h je týždenný strop):

1. Otvor pohľad **Teraz**, presuň do *Tento týždeň* toľko, koľko sa do 9,5 h zmestí.
2. Pozri, či niečo v *Blokované* už blokované nie je.

**Keď sa balík dokončí:**

1. Odškrtaj *Hotovo, keď* v tele issue — kritériá sú z plánu, nie vymyslené dodatočne.
2. Zapíš **Skutočnosť h**.
3. Zavri issue. Epic semestra sa prepočíta sám.

**Keď sa niečo naučíš:** nové zistenie ide do **analýzy** v `docs/`, zmena postupu do
**plánu**, voľba medzi dvoma cestami do **`decisions/`** ako ADR. Issue je na *stav práce*,
nie na poznatky — komentár v issue je najhoršie miesto, kde hľadať odpoveď o rok.

---

## Kritická cesta a čo blokuje čo

Každý issue má v tele sekcie **Závisí od** a **Blokuje** s odkazmi na čísla. Krátko:

```
S1  #4 značky ──► #5 zhoda ──► #8 A ──► ... ──► #21 ablácia
     │              #7 steny ──┘
     └──► #10 navigácia ──► #19 hra ──► #25 príprava
S2  #15 replay ──► #16 filter ──► #21 ablácia ──► #28 práca
S3  #24 odolnosť + #25 príprava ──► #26 TRIEDY ──► #27 vyhodnotenie ──► #28 práca
```

Na kritickej ceste sú **#4 (zameranie značiek)** a **#15 (offline replay)**. Oba majú label
`kriticka-cesta`. Nič v S1 sa nesmie začať pred #4.

---

## Skript

`tools/planning/gh_plan.py` drží board a issues v súlade s
`tools/planning/plan_frilens.py`. Je idempotentný — dá sa pustiť koľkokrát treba, dorobí
len to, čo chýba.

```bash
gh auth refresh -s project,read:project        # raz; board bez toho nejde
python3 tools/planning/gh_plan.py --all --dry-run
python3 tools/planning/gh_plan.py --all
```

**Issues z tohto plánu už v repozitári sú** (#1 – #29). Prvý ostrý beh im teda len dorobí
míľniky, farby labelov a popisy, a založí board — nezaloží ich znova.

**Projects v2 sú iba v GraphQL API**, nie v REST. To je dôvod, prečo board nezaložil agent
a prečo skript stojí na `gh`: `gh` má na GraphQL autorizáciu, ktorú treba raz rozšíriť
o scope `project`.

---

## Ďalší projekt na ten istý board

1. Skopíruj `tools/planning/plan_frilens.py` na `plan_<projekt>.py`.
2. Prepíš `REPO`, `MILESTONES` a `ISSUES`. `BOARD` **nechaj tak** — board je jeden.
3. `python3 tools/planning/gh_plan.py --plan tools/planning/plan_<projekt>.py --all`

Nový projekt sa objaví v pohľade **Naprieč projektmi** ako vlastná skupina, lebo ich
rozlišuje pole *Repository*. Vlastné pole „Projekt" preto netreba a nie je.
