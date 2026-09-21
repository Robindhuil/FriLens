# Prezentácia návrhu témy — scenár

**Verzia:** 0.2.2-alpha · **Dátum:** 2026-09-21 · **Stav:** pripravené na odprezentovanie
**Dĺžka:** 15 slidov, ~14 minút hovorenia
**Zdroj slidov:** [`prezentacia/2026-09-21-navrh-temy/`](prezentacia/2026-09-21-navrh-temy/)

Druhá, **externá** verzia prezentácie. Prvá
([scenár](2026-09-17-prezentacia-pre-veduceho.md),
[slidy](prezentacia/2026-09-17-navrh-projektu/)) hovorí o projekte tak, ako naozaj je — s verziami,
nameranými číslami a hotovými balíkmi. Táto ho predkladá **ako návrh témy**, teda ako prácu, ktorá
sa ešte len začne.

Obe sú platné, každá pre iné publikum. Nemiešať.

| | Interná (17. 9.) | **Návrh témy (21. 9.)** |
|---|---|---|
| Publikum | kto vie, v akom stave projekt je | vedúci projektu pri predkladaní témy |
| Čas slovies | minulý a prítomný | **budúci** |
| Dôkaz, že problém je reálny | vlastné namerané čísla z behov | princíp VIO, rešerš a rádový odhad z ukážky |
| Slide „čo už beží" | áno, s verziou a zoznamom hotového | **nie je** |
| Hodiny | súčty balíkov (124 / 125 / 123) | zaokrúhlené na päťky (125 / 125 / 125) |

---

## Pravidlá, ktoré držia návrh v žánri

Sú to zároveň pravidlá pre každú ďalšiu úpravu týchto slidov:

1. **Budúci čas.** Jediné tri výnimky sú overenie termínov DOD, vyskúšaná AR ukážka a citovaná
   literatúra.
2. **Žiadna stopa po implementácii.** Verzie, názvy súborov a scén, názvy podlaží a miestností,
   rozmer značky, nález o sklone značky. Ani v poznámkach prednášajúceho.
3. **Žiadny screenshot aplikácie ani HUD-u.** Vizuály sú schémy, tabuľky a kalendár.
4. **Žiadne namerané číslo.** V decku smú byť len dve triedy čísel: z literatúry (rádové, s
   priznaním, že sú rádové) a z plánu (hodiny, počty behov, veľkosť vzorky).
5. **Rádový odhad sa označuje ako rádový odhad.** Na slide `preco-tazke` je veta „je to rádový
   odhad, nie meranie" a nesmie z neho zmiznúť.

Kontrola pred prezentovaním:

```bash
grep -riE 'navmesh|blend|unity|arcore|alpha|0\.[12]\.|csv|hud|rc0|ra[0-9]|rb[0-9]|redmi|2,7|13,4|21,6|35,7' \
  docs/prezentacia/2026-09-21-navrh-temy/slides/
```

Musí vrátiť prázdno. (`ARKit` je povolená výnimka — je v zozname toho, čo zostáva mimo rozsahu.)

---

## Poradie slidov a čas

| # | id | Slide | čas | Čo si má poslucháč odniesť |
|---:|---|---|---:|---|
| 1 | `cover` | Titulka | 30 s | Návrh témy, tri semestre, Android |
| 2 | `pouzitie` | Na čo to bude | 70 s | Konkrétne použitie na DOD, aj mimo neho |
| 3 | `preco-tazke` | Prečo to nie je len naprogramovať navigáciu | 70 s | AR si mapu stavia sama, chyba je neohraničená; nikto to tu nezmeral |
| 4 | `senzorika` | Prečo nestačí iná senzorika | 45 s | Čo je presné, chce hardvér do budovy; nič nedá orientáciu |
| 5 | `model` | Návrh: model budovy ako mapa | 70 s | SLAM sa mení na určovanie polohy voči známej mape |
| 6 | `otazka` | Výskumná otázka | 40 s | Otázka má odpoveď aj pri neúspechu korekcie |
| 7 | `korekcie` | Ako sa drift potlačí | 70 s | A, B, C, D ako vypínateľné režimy; riziko kruhovosti |
| 8 | `vystupy` | Čo z práce vznikne | 45 s | Engine, autorský nástroj, dataset; a čo zostáva mimo |
| 9 | `hra` | Hra ako meracia infraštruktúra | 60 s | Stanovište je značka; 800 meraní za popoludnie |
| 10 | `kalendar` | Kde sa bude vyhodnocovať | 65 s | Oba DOD mimo okna, jesenný neexistuje; pozvané triedy |
| 11 | `s1` | Semester 1 | 60 s | 125 h; prvý balík odomyká všetko ostatné |
| 12 | `s2` | Semester 2 | 55 s | 125 h; aplikácia musí byť hotová do júna 2027 |
| 13 | `s3` | Semester 3 | 55 s | 125 h; 45 h na text vďaka priebežnej dokumentácii |
| 14 | `rizika` | Riziká a čo s nimi | 50 s | Aj najhoršie riziko má platnú náhradnú tému |
| 15 | `zaver` | Čo potrebujem od vedúceho | 55 s | Dve rozhodnutia a schválenie |

Súčet je **13 minút 40 sekúnd**. Pri krátení na desať minút sa vypúšťa v poradí `senzorika`,
`vystupy`, `rizika`. Nikdy `preco-tazke`, `otazka`, `kalendar` a `zaver`.

---

## Čo sa oproti plánu upresnilo

Dve otázky zo sekcie *Predpoklady, ktoré treba potvrdiť* v
[pláne](2026-09-05-plan-inzinierskeho-projektu.md) sú zodpovedané a prezentácia s tým už počíta:

- **Jesenný termín dňa otvorených dverí fakulta nemá.** Vyhodnotenie sa na DOD naviazať nedá vôbec;
  pozvané triedy v novembri 2027 sú základná varianta, nie núdzová. Otvorená zostáva len druhá
  polovica tej otázky — či sa dá posunúť poradie semestrov tak, aby tretí končil v júni 2028.
- **Iba Android.** Prestáva to byť otázka a stáva sa rozhodnutím o rozsahu. iOS a ARKit sú
  v zozname toho, čo zámerne zostáva mimo (slide `vystupy`), a návštevníci s iPhonom sú riadok
  v tabuľke rizík (slide `rizika`) s protiopatrením: požičané zariadenia fakulty a priznanie
  obmedzenia ako hranice platnosti výsledkov.

> Plán a [ADR 009](decisions/009-vyhodnotenie-sa-neviaze-na-den-otvorenych-dveri.md) obe odpovede
> zatiaľ nesú ako otvorené. Treba ich tam doplniť samostatne; tento dokument ich len zaznamenáva.

Na vedúceho tým zostávajú dve rozhodnutia namiesto troch, a druhé z nich je teraz dôležitejšie:
pri inštalácii z obchodu vypadne z Android-only vzorky každý návštevník s iPhonom, kým požičané
zariadenia fakulty ten problém rušia úplne.

---

## Miesta, kde treba doplniť údaje

Na titulnom slide `[Meno Priezvisko]` a `[meno vedúceho]`. Nič iné zástupné v decku nie je.

---

## Otázky, na ktoré sa treba pripraviť

**„Skúšali ste už niečo?"** Odpovedať pravdivo. Rešerš a vyskúšaná AR ukážka sú na slide
`preco-tazke`; čokoľvek nad to sa nezamlčiava, len sa o tom nehovorí prv, než sa spýta.

**„Prečo nepoužijete hotové riešenie?"** Existujú nástroje na sledovanie polohy v priestore z 3D
skenu, ale chcú sken z inej technológie než navigačný model fakulty — a hlavne by práca merala
cudzí nástroj, nie vlastný prínos. Ako porovnávacia referencia dobré, ako jadro práce slabé.

**„Čo keď korekcie nezaberú?"** Ablačná tabuľka je výsledok aj vtedy. Preto je otázka postavená
ako „o koľko", nie ako „či".

**„Nie je hra len ozdoba?"** Slide `hra` — stanovište je bod so známou pravdou. Bez hry desiatky
behov odchodených samým sebou, s hrou stovky behov od ľudí, ktorí telefón držia inak.

**„Nie je toho na tri semestre priveľa?"** Každý semester má napísané, čo sa z neho vypustí pri
sklze, a z toho poradia vyplýva, že sa vypúšťa hra a najprácnejšia korekcia, nie meranie.

**„Ako viete, že fakulta ten model má?"** Pýtal som sa a pozrel som sa naň. To je pravda a stačí.
