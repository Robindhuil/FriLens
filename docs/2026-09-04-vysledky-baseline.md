# Výsledky baseline testu

**Verzia:** 0.1.5-alpha · **Dátum:** 2026-09-04 · **Zariadenie:** Xiaomi Redmi Note 10 Pro (M2101K6G), Android 11
**Log:** `frilens-20260904-145117.csv` · 465 s, 1763 riadkov

Prvý beh podľa [protokolu](2026-09-04-protokol-baseline-testu.md). Meria sa **samotný tracking**,
nie zhoda modelu s budovou — na to chýba zameraná značka.

## Test A — prejdená vzdialenosť proti pásmu

Odmeraný úsek **8,00 m**, štyri prechody, medzi nimi státie a otočka.

| úsek | `walked_m` | `path_raw_m` | skoky | straty |
|---|---:|---:|---:|---:|
| mark-1 → mark-2 | 7,60 | 8,41 | 0 | 0 |
| mark-3 → mark-4 | 7,97 | 8,02 | 0 | 0 |
| mark-5 → mark-6 | 7,97 | 8,08 | 0 | 0 |
| mark-7 → mark-8 | 7,59 | 8,03 | 0 | 0 |
| **priemer** | **7,78** | **8,14** | | |
| **chyba** | **−2,7 %** | **+1,7 %** | | |

Rozptyl `walked` je 0,38 m (7,59 až 7,97).

Úseky státia a otáčania medzi prechodmi:

| úsek | `walked_m` | `path_raw_m` |
|---|---:|---:|
| mark-2 → mark-3 (3,5 s) | 0,00 | 0,40 |
| mark-6 → mark-7 (6,5 s) | 0,91 | 1,63 |
| mark-8 → mark-9 (9,4 s) | 0,61 | 1,71 |
| mark-10 → mark-11 (9,4 s) | 0,91 | 2,88 |

### Čo z toho vyplýva

**Meranie vzdialenosti je použiteľné.** Chyba −2,7 % pri rozptyle pod 0,4 m na osemmetrovom
úseku je pre meranie driftu dostatočné. Podhodnotenie je očakávané — je to oneskorenie filtra na
konci úseku a [nerastie so vzdialenosťou](decisions/005-ako-merat-prejdenu-vzdialenost.md).

**Filter nezahadzuje skutočný signál.** Pri pokojne držanom telefóne je `raw` len +1,7 % nad
pásmom, teda takmer to isté ako `walked`. Nafúknutie o +42 %, ktoré ukázal beh `001103`, bola
manipulácia s telefónom, nie vlastnosť algoritmu. Rozdiel medzi tými dvomi stĺpcami je teda
skutočne mierou toho, ako sa s telefónom zaobchádzalo.

**Státie dáva nulu, keď je to státie.** Prvý úsek státia dal presne 0,00 m. Ostatné tri sú
otočky a preloženie telefónu, kde sa kamera naozaj presunula.

## Test B — zakrytie kamery

| začiatok | trvanie | príčina hlásená appkou | skoky, ktoré nasledovali |
|---:|---:|---|---|
| 100,9 s | 2,7 s | `ExcessiveMotion` | 3,68 · 2,79 m |
| 152,0 s | 4,3 s | `ExcessiveMotion` | 6,92 · 1,55 m |
| 171,4 s | 4,2 s | `ExcessiveMotion` | 5,45 · 1,82 m |
| 304,4 s | 5,6 s | `InsufficientLight` | 4,66 · 8,07 · 1,99 m |
| **328,6 s** | **15,1 s** | `InsufficientLight` | **13,36 · 21,56 · 35,68 m** |
| 442,1 s | 8,0 s | `InsufficientLight` | 2,55 m |

Za celý beh **18 skokov v objeme 131,97 m** oproti 227,81 m prejdeným. Takmer 60 %.

`InsufficientLight` tu neznamená tmu v chodbe, ale **dlaň na kamere** — zakrytá kamera nevidí
svetlo. Text na obrazovke „too dark here, find more light" je v tomto prípade zavádzajúci.

### Zistenie: dlhé zakrytie nie je jeden skok, ale obdobie nestability

Toto v protokole predpokladané nebolo a je to najzaujímavejší výsledok behu.

Pätnásťsekundová strata skončila v čase 343,7 s. Skoky neprišli hneď a nezostali malé:

| čas | oneskorenie po obnove | veľkosť |
|---:|---:|---:|
| 343,9 s | 0,2 s | 13,36 m |
| 361,6 s | 17,9 s | 21,56 m |
| 401,5 s | 57,8 s | **35,68 m** |

Tracker sa **ešte minútu po obnove opravoval, a odchýlky rástli**. Pri takej veľkosti nejde
o relokalizáciu, ktorá by opravovala smerom k pravde; je to tracker hľadajúci niť vo vlastnej
poškodenej mape.

Krátke zakrytia (5,6 s a 8,0 s) dopadli podstatne miernejšie — najväčší skok 8,07 m, respektíve
2,55 m. **Hranica leží niekde medzi ôsmimi a pätnástimi sekundami** a stojí za cielené
premeranie: zakrytie 5, 8, 10, 12, 15 s z toho istého miesta a sledovať nielen prvý skok, ale
celú minútu po obnove.

### Rozdiel oproti hypotéze z ADR 006

[ADR 006](decisions/006-kotvenie-a-strata-trackingu.md) predpokladal, že chyba bude zodpovedať
tomu, koľko človek prejde naslepo, a že rozhodovať bude prekryv s mapou. Skoky 21 a 36 metrov
sú **rádovo väčšie než akákoľvek prejdená vzdialenosť** počas zakrytia.

Predpoklad teda platí len pre krátke zakrytia. Pri dlhých sa poškodí samotná mapa a chyba už
s prejdenou vzdialenosťou nesúvisí.

## Čo appka zachytila správne

Celý priebeh je v logu a nič z toho nebolo tiché:

| | |
|---|---|
| `losses` | 6 |
| `blind_s` | 40,17 |
| `verified` | 0 od prvej straty až do konca |
| `origin_anchored` | 1 |

`origin_anchored = 1` je prvé potvrdenie, že kotvenie na `ARAnchor` na zariadení naozaj beží.

Zároveň to potvrdzuje, že **filter skokov po zmene v 0.1.5-alpha nerobí falošné poplachy**:
osemnásť skokov, najmenší 1,55 m, žiadny pod jeden meter. Predošlá verzia by ich pri tomto behu
nahlásila desiatky.

## Čo z toho plynie pre ďalší postup

**Značky po trase nie sú vylepšenie, sú nutnosť.** Softvérová oprava na pätnásť sekúnd slepoty
neexistuje — po nej je poloha zle o desiatky metrov a ARCore sa k ničomu použiteľnému sám
nevráti. Jediné, čo to spraví použiteľným, je prezarovnanie na značke.

**Text pri `InsufficientLight` treba doplniť.** Zakrytá kamera a tmavá chodba sú ten istý stav
a rada „nájdi viac svetla" v prvom prípade nedáva zmysel.

**Test C (dlhý beh) sa zatiaľ nedá čítať.** Úsek mark-14 → mark-15 má 96 m prejdených, ale aj
94,8 m odskočených a dve straty. Kým sa nezopakuje bez zakrývania kamery, o driftu pri bežnom
používaní nehovorí nič.
