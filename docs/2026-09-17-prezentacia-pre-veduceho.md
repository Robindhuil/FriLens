# Prezentácia pre vedúceho projektu — scenár

**Verzia:** 0.2.2-alpha · **Dátum:** 2026-09-17 · **Stav:** návrh na odprezentovanie
**Dĺžka:** 16 slidov, 13 až 14 minút hovorenia · **Zdroj:**
[plán inžinierskeho projektu](2026-09-05-plan-inzinierskeho-projektu.md),
[ADR 007](decisions/007-vyuzitie-modelu-na-lokalizaciu.md),
[ADR 008](decisions/008-rozsirenie-rozsahu-na-navigaciu-a-hru.md),
[ADR 009](decisions/009-vyhodnotenie-sa-neviaze-na-den-otvorenych-dveri.md),
[výsledky baseline](2026-09-04-vysledky-baseline.md)

Zdroj slidov je v [`prezentacia/2026-09-17-navrh-projektu/`](prezentacia/2026-09-17-navrh-projektu/):
`deck.json` je index a poradie, `slides/<id>.html` je jeden slide. Poznámky prednášajúceho sú
v každom slide v elemente `<aside>`.

---

## Na čo prezentácia odpovedá

Nie je to prehľad projektu. Je to **návrh témy**, ktorý má od vedúceho získať tri veci:

1. schválenie rozšírenia rozsahu z meracieho prístroja na aplikáciu s navigáciou a hrou
   ([ADR 008](decisions/008-rozsirenie-rozsahu-na-navigaciu-a-hru.md)),
2. odpoveď na tri predpoklady, ktoré menia plán skôr, než sa začne pracovať,
3. potvrdenie, že výskumná otázka je obhájiteľná téma na tri semestre.

Preto je stavaná tak, že **prvá tretina je meranie, nie nápad**. Vedúci má do štyroch minút
vedieť, že problém je reálny a že je zmeraný na zariadení, nie prevzatý z literatúry.

---

## Poradie slidov, čas a čo na ktorom stojí

| # | id | Slide | čas | Čo si má poslucháč odniesť |
|---:|---|---|---:|---|
| 1 | `cover` | FriLens | 45 s | Prototyp beží rok, dnes je to návrh témy |
| 2 | `problem` | Meraný problém: pätnásť sekúnd slepoty | 70 s | 13,4 / 21,6 / 35,7 m po 15 s zakrytia — vlastné meranie |
| 3 | `alternativy` | Prečo nestačí iná senzorika | 55 s | Všetko dosť presné chce hardvér do budovy; nič nedá orientáciu |
| 4 | `napad` | Riešenie: model budovy ako mapa | 60 s | SLAM sa mení na lokalizáciu voči známej mape |
| 5 | `otazka` | Výskumná otázka | 40 s | Otázka má odpoveď aj pri neúspechu korekcie |
| 6 | `korekcie` | Štyri korekcie, každá meraná zvlášť | 75 s | A, B, C, D ako vypínateľné režimy; riziko kruhovosti |
| 7 | `prinosy` | Tri prínosy, každý obhájiteľný samostatne | 55 s | Engine, pipeline, dataset — a čo zámerne zostáva mimo |
| 8 | `hotove` | Východiskový stav: čo už beží | 70 s | Nezačína sa od nuly; −2,7 % overené pásmom |
| 9 | `hra` | Hra ako meracia infraštruktúra | 65 s | Stanovište je značka; 800 meraní za popoludnie |
| 10 | `kalendar` | Kalendár: kedy sa dá merať | 70 s | Oba DOD ležia mimo použiteľného času, preto pozvané triedy |
| 11 | `s1` | Semester 1 — zhoda modelu a navigácia | 70 s | 124 h, prvý balík odomyká všetko ostatné |
| 12 | `s2` | Semester 2 — potlačenie driftu a hra | 70 s | 125 h na strope; appka musí byť hotová do júna |
| 13 | `s3` | Semester 3 — nasadenie a písanie práce | 65 s | 123 h; 45 h na text je realistických vďaka priebežným ADR |
| 14 | `cesta` | Kritická cesta | 45 s | Na nej sú dve veci: značky a offline replay |
| 15 | `rizika` | Riziká a čo s nimi | 60 s | Aj najhoršie riziko má platnú náhradnú tému |
| 16 | `otazky` | Čo potrebujem od vedúceho | 60 s | Tri otázky a prosba o schválenie rozsahu |

Súčet hovorenia je **13 minút 15 sekúnd**. Zvyšok z pätnástich minút je rezerva na otázky
medzi slidmi. Ak treba skrátiť na desať minút, vypúšťa sa v tomto poradí: `alternativy`,
`cesta`, `prinosy`. Nikdy `problem`, `otazka`, `kalendar` a `otazky` — na tých stojí celý návrh.

---

## Čísla, ktoré v prezentácii zaznejú, a odkiaľ sú

Žiadne číslo v prezentácii nie je odhad. Toto je ich pôvod:

| Číslo | Kde zaznie | Zdroj |
|---|---|---|
| 13,36 / 21,56 / 35,68 m po 15,1 s zakrytia | slide 2 | [výsledky baseline](2026-09-04-vysledky-baseline.md), beh `145117` |
| 18 skokov v objeme 131,97 m na 227,81 m chôdze | slide 2 | tamtiež |
| −2,7 % chyba merania prejdenej dráhy na 8 m | slide 8 | tamtiež, test A, overené pásmom |
| zvislý drift až 3 m (`cam_y` −2,94 m) | poznámky, slide 6 | tamtiež, beh `174812` |
| sklon značky 2,6° až 5,7°, rozdiel normál M1 a M2 8,0° | slide 8 | [CHANGELOG 0.2.2-alpha](../CHANGELOG.md) |
| 0,78 m na 10 m pri 4,5° chybe sklonu | slide 8 | tamtiež |
| 0,55 m pri 90 % spoľahlivosti (časticový filter) | slide 6 | Woodman & Harle 2008, cez [ADR 007](decisions/007-vyuzitie-modelu-na-lokalizaciu.md) |
| 1 stupeň driftu kurzu na 50 m ≈ 1 m bokom | slide 6 | [ADR 007](decisions/007-vyuzitie-modelu-na-lokalizaciu.md), možnosť A |
| šírka chodby 3,20 m podľa modelu | slide 11 | [plán](2026-09-05-plan-inzinierskeho-projektu.md), akceptačné kritérium S1 |
| 124 / 125 / 123 h | slides 11–13 | [plán](2026-09-05-plan-inzinierskeho-projektu.md), súčty balíkov |

Presnosti iných metód na slide 3 (GNSS, WiFi RTT, BLE, UWB) sú prevzaté z tabuľky v
[ADR 007](decisions/007-vyuzitie-modelu-na-lokalizaciu.md) a sú rádové, nie namerané.

---

## Miesta, kde treba doplniť údaje

Na titulnom slide sú tri zástupné texty v hranatých zátvorkách:

- `[Meno Priezvisko]` — autor,
- `[meno vedúceho]` — navrhovaný vedúci projektu.

Nič iné v prezentácii zástupné nie je.

---

## Tri otázky, s ktorými sa z miestnosti neodchádza

Sú to tie isté tri predpoklady, ktoré má
[plán](2026-09-05-plan-inzinierskeho-projektu.md) v sekcii *Predpoklady, ktoré treba potvrdiť
pred schválením zadania*. Prezentácia ich dáva na posledný slide naschvál — sú to skutočné
otázky, nie rečnícke.

1. **Má fakulta jesenný termín dňa otvorených dverí, alebo sa dá poradie semestrov posunúť tak,
   aby tretí končil v júni 2028?** Ak áno, vyhodnotenie prebehne na skutočnom DOD s najväčšou
   možnou vzorkou a plán sa prepíše bez zmeny obsahu práce.
2. **Android, alebo aj iOS?** Návrat iOS je cez AR Foundation priechodný, ale stojí Mac,
   vývojársky účet a približne 25 h, ktoré v rozpočte 375 h nie sú.
3. **Ako sa aplikácia dostane k návštevníkom?** Požičané zariadenia fakulty znamenajú lepšie
   dáta a menšiu vzorku, inštalácia z obchodu opak. Určuje to veľkosť N v semestri 3.

---

## Otázky, na ktoré sa treba pripraviť

Nie sú na slidoch, ale padnú:

**„Prečo nepoužijete hotové riešenie?"** Vuforia Area Targets robí sledovanie polohy z 3D skenu,
ale chce sken z Matterportu alebo LiDARu, nie náš navmesh — a hlavne by meralo Vuforiu, nie nás.
Ako porovnávacia referencia dobré, ako jadro práce slabé
([ADR 007](decisions/007-vyuzitie-modelu-na-lokalizaciu.md), možnosť F).

**„Čo keď korekcie nezaberú?"** Ablačná tabuľka je výsledok aj vtedy. Neúspešná korekcia je
publikovateľný výsledok; nefunkčná aplikácia nie je. Preto je otázka formulovaná ako „o koľko",
nie ako „či".

**„Nie je hra len ozdoba, ktorá zožerie čas?"** Herné stanovište je zameraná značka, teda bod so
známou pravdou. Bez hry mám desiatky behov, ktoré si odchodím sám; s hrou stovky behov od ľudí,
ktorí telefón držia inak. Sú to lepšie dáta, nie horšie
([ADR 008](decisions/008-rozsirenie-rozsahu-na-navigaciu-a-hru.md)).

**„Nie je to príliš veľa na tri semestre?"** Každý semester má napísané, **čo sa z neho vypustí
pri sklze**, a poradie vypúšťania. Z toho poradia vyplýva, že sa vypúšťa hra a najprácnejšia
korekcia, nie meranie. Rozpočet je 9,5 h týždenne a súčty balíkov sa doň zmestia.

**„Prečo sa meria zakrytá kamera a nie bežné používanie?"** Lebo v budove plnej ľudí je zakrytá
kamera bežné používanie. Krátke zakrytia (do 8 s) dopadli mierne; hranica leží niekde medzi 8 a
15 sekundami a jej cielené premeranie je súčasťou semestra 1.
