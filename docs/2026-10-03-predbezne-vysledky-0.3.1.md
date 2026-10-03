# Predbežné výsledky 0.3.1-alpha — voľný beh v break roome

**Verzia:** 0.3.1-alpha · **Dátum:** 2026-10-03 · **Zariadenie:** Xiaomi Redmi Note 10 Pro (M2101K6G), Android 11
**Logy:** `frilens-20261003-131810.csv` (570 s), `frilens-20261003-134724.csv` (286 s, s videom) · porovnanie: `frilens-20260909-134644.csv`, `frilens-20260909-135106.csv` (0.2.2-alpha)

**Toto nie je beh podľa [protokolu](2026-09-17-protokol-testu-viacerych-znaciek.md).** Cieľ bol
celý čas `any`, `Mark` padol raz, pásmo `M1`↔`M2` sa nemeralo a k behu nie sú poznámky. Čísla
nižšie sú orientačné a výklad niektorých úsekov je odvodený len z logu. Formálne testy E–I
ostávajú otvorené.

Čísla sú z `tools/frilens_eval.py` a z ručného prechodu udalostí v logu.

## Ako beh vyzeral

Chodilo sa striedavo ku každej značke zvlášť: `M1` sa skenovala z okolia `(4,4; 6,0)` v session
space, `M2` z `(5,3; −0,6)`, teda asi 6,7 m od seba. 15 zarovnaní, 10 z nich z dvoch značiek.

## 1. Fit z dvoch značiek drží

| | 0.2.2 (2026-09-09) | 0.3.1 |
|---|---|---|
| rozpätie polohy `AlignmentRoot` v X | 3,89 – 7,36 m | **3,9 – 4,8 cm** |
| rozpätie kurzu | 9,2 – 13,5° | **0,24 – 0,27°** |
| posun medzi dvomi po sebe idúcimi zarovnaniami | 0,06 – 3,2 m | **0,4 – 8,3 cm** |

Stĺpec 0.3.1 je z dvoch okien bez skoku mapy:

- **147–160 s, postojačky pri `M1`.** Štyri zarovnania, X 3,9 cm, Z 8,9 cm, kurz 0,24°. Druhou
  značkou bola zakaždým tá istá observácia `M2` zo 100 s, takže okno meria hlavne opakovateľnosť
  čítania `M1`.
- **Celé dvojice s chôdzou medzi značkami.** 100 s (`M1` → `M2`) a 174 s (`M1` → `M2`): rozdiel
  **3,7 cm a 0,12°**. 507 s a 520 s: **6,7 cm a 0,27°**. Toto je silnejší výsledok, lebo obe
  observácie sú v každom fite nové.

Rozdiel oproti 0.2.2 je dva rády, čo zodpovedá predpokladu z
[ADR 010](decisions/010-kurz-z-poloh-znaciek-sklon-z-gravitacie.md): kurz zo spojnice polôh je
presný, kurz z natočenia jednej značky nie.

## 2. Jedna značka je stále zlá

Dve zarovnania len z `M1` z toho istého miesta (30 s a 88 s) sa rozchádzajú o **0,61 m a 1,6°**.
Jednoznačková vetva je nezmenená a toto potvrdzuje, že zlepšenie v bode 1 prináša druhá značka,
nie náhoda behu.

## 3. Striedanie značiek funguje, súčasné sledovanie oboch nie

Oprava z 0.3.1 sa prejavila: pri cieli `any` sa zarovnania striedali `M1`, `M2`, `M1` a fit
vzal obe.

Pole `seen` však **ani raz neukázalo obe značky v stave `Tracking` naraz** — vždy jedna
`Tracking`, druhá `Limited`. Fit teda stál na čerstvej observácii jednej značky a staršej
(do 60 s) druhej.

Pravdepodobné vysvetlenie: 180 mm značka z diaľky, z ktorej by sa obe zmestili do záberu, je pre
ARCore primalá na aktívne sledovanie a drží len poslednú známu pózu. **Overené to nie je** —
z logu sa nedá zistiť, či sa vôbec niekedy stálo tak, aby boli obe v zábere.

Dôsledok pre Test E: postup „postaviť sa pred obe a nehýbať sa" asi nepôjde. Chôdza medzi
značkami funguje a dáva výsledky z bodu 1.

## 4. `baseline`

10 fitov z dvoch značiek: priemer **−9,3 cm**, rozpätie **−14,7 až +1,0 cm**. Záporné znamená,
že namerané polohy značiek sú bližšie k sebe než v modeli (6,79 m).

Rozptyl 15 cm je aj medzi čerstvými dvojicami. Najpravdepodobnejšie je to drift nazbieraný na
~7 m chôdze medzi dvomi skenmi.

### Doplnené pásmom 2026-10-03

Celá západná stena, roh **A** → roh **B**, nameraná pásmom: **9,86 m**.

| | dĺžka steny | vzdialenosť stredov `M1`–`M2` |
|---|---:|---:|
| pásmo (stredy odvodené: 9,86 − 2 × 1,50) | **9,860 m** | **6,860 m** |
| model (rohy z nav polygónu `Z 3,828` a `Z 13,621`) | 9,793 m | 6,793 m |
| ARCore (model + priemerný `baseline` −0,093) | — | 6,70 m |

Z toho dve rôzne chyby s opačným znamienkom:

- **Model je o 6,7 cm kratší než budova (−0,68 %).** Keďže je každá značka zameraná od svojho
  rohu, celý rozdiel dĺžky steny padne do vzdialenosti medzi nimi — presne tak, ako to plán
  zamerania zamýšľal. Toto je prvé priame číslo zhody modelu s budovou.
- **ARCore vidí značky o 16 cm bližšie, než sú (−2,3 %, v rozpätí −3,1 až −0,8 %).** To sedí na
  baseline test z 0.1.8, kde `walked_m` vyšla −2,7 % proti pásmu. Dve nezávislé merania teda
  ukazujú, že ARCore na tomto telefóne podhodnocuje vzdialenosti o 2–3 %. Rozptyl medzi fitmi je
  na tom navrch drift.

Mierka sa vo fite zámerne nefituje
([návrh](2026-09-09-navrh-kalibracie-viac-znaciek.md)), takže chyba mierky ARCore sa v prekryve
prejaví ako rozchod rastúci so vzdialenosťou od značky — pri 2,3 % asi 23 cm na desať metrov.

**Platí za predpokladu**, že značky naozaj visia na 1,41 m od svojich rohov a že pásmo meralo
roh v tom istom mieste, kde ho má nav polygón (vnútorné líce steny, nie sokel). Ak sa na mieste
ukáže inak, prepočíta sa to; pásmo `M1`↔`M2` priamo (Test G) to overí.

## 5. Drift po chôdzi mimo miestnosti

Medzi fitmi z dvoch značiek v 334 s a 507 s sa odišlo z miestnosti (`mark-1` v 433 s, 8,3 m od
počiatku) a vrátilo, spolu okolo 45 m chôdze. Nové zarovnanie posunulo prekryv o **0,48 m
a 1,2°**.

Je to prvé číslo driftu, pri ktorom sú obe koncové zarovnania z dvoch značiek, takže chyba
zarovnania v ňom tvorí len centimetre. Je to jeden pár, nie štatistika.

## 6. Skoky mapy bez straty trackingu

**Medzi 219 a 266 s prišlo šesť relokalizačných skokov** (3,9; 8,0; 1,3; 6,5; 7,8; 9,9 m) a ARCore
pri žiadnom nehlásil stratu trackingu. Kamera v session space preskakovala medzi `z ≈ −1`
a `z ≈ +5,5`, teda medzi okolím `M2` a `M1`. Veľkosti skokov okolo 6,5 – 8 m sedia na vzdialenosť
medzi značkami.

**Hypotéza, nie zistenie:** miestnosť je pozdĺž západnej steny symetrická a ARCore si zamieňa jej
dva konce. Na overenie treba vedieť, čo sa vtedy robilo a kam mierila kamera — v logu to nie je.

**Skok 12,5 m v 521 s** prišiel sekundu po zarovnaní. Appka sa zachovala podľa návrhu: nový úsek
trackingu zahodil staré observácie, o 3 s sa zarovnala na `M2` z jednej značky a v 536 s znova
z dvoch. Kurz rootu sa pri tom zmenil o 89°, čiže ARCore mapu nielen posunul, ale aj otočil.

## 7. Disk na navmeshi — nepoužiteľný

`probe-1` v 430 s hlási podlahu modelu 21,5 cm pod podlahou odvodenou z výšky oka. Výška oka ale
ostala na predvolených **1,25 m** a nebola nastavená podľa toho, ako sa telefón držal. Číslo
preto nič nehovorí.

## Varovania z appky

Jedno: v 29 s zahodených 9 vzoriek, značka bola na viac než 2 s mimo záberu. Brána kvality nič
nezamietla a žiadny burst neprekročil rozptyl 2 cm.

## Druhý beh — značky preložené, s videom

**Log:** `frilens-20261003-134724.csv` · 286 s, 1164 riadkov, 44 zarovnaní, žiadny skok mapy
a žiadna strata trackingu. **Video:** záznam obrazovky `Screenrecorder-2026-10-03-13-50-11-819.mp4`,
115 s, pokrýva zhruba 167 – 282 s logu. Pred behom boli značky na stene preložené presnejšie;
o koľko, nie je zapísané.

Skenovalo sa z 50 – 75 cm. Overlay bol väčšinu videa skrytý a HUD v kompaktnom režime, takže riadok
`Alignment` na videu nie je.

### Postojačky: jedna značka proti dvom v tom istom behu

| okno | čo | zarovnaní | rozpätie X | rozpätie Z | rozpätie kurzu |
|---|---|---:|---:|---:|---:|
| 12 – 30 s | len `M1`, Re-anchor bez pohybu | 11 | **3,19 m** | 1,64 m | **9,1°** |
| 45 – 62 s | `M2` + uložená `M1` | 8 | **3,1 cm** | 1,7 cm | **0,09°** |
| 241 – 252 s | `M1` + uložená `M2` | 6 | **2,1 cm** | 2,0 cm | **0,06°** |

Prvý riadok je Test E na jednej značke — a vyšiel rovnako zle ako v 0.2.2. Pritom sa **poloha
`M1` počas tých jedenástich burstov hýbala o menej než 1,5 cm**; celý rozptyl prekryvu robí
natočenie značky. To je [ADR 010](decisions/010-kurz-z-poloh-znaciek-sklon-z-gravitacie.md)
potvrdené na jednom behu: poloha značky je presná, natočenie nie.

### Video sedí na zvyšky fitu

- **Po fite z dvoch značiek** je nakreslená doska posunutá voči vytlačenej o **2 – 3 cm** — pri
  `M2` (~186 s, zvyšok v logu 1,8 cm) aj pri `M1` (~247 s, zvyšok 3,4 cm). Fit rozdelí nesúlad
  dĺžky spojnice medzi obe značky a presne to je na obraze vidieť.
- **Po zarovnaní len z `M1` a prechode 6 m k `M2`** (~184 s, pred novým zarovnaním) je doska `M2`
  vedľa asi o **20 cm**. Sedí to na chybu kurzu jednoznačkového zarovnania: 1,8° na 6,8 m je
  21 cm.

### Nové zistenie: prvé čítanie značky je skreslené a rozptyl to neodhalí

Pole `img pos` v riadku zarovnania je priemerná poloha značky z burstu v session space. Keď sa
sleduje v čase:

| | prvé bursty po zbadaní | ustálené (zvyšok behu) |
|---|---|---|
| `M1` | 5,8 s a 8,9 s: o **12 cm vyššie**, rozptyl 7,4 a 13,3 cm — **zamietnuté** bránou rozptylu | od 12,3 s stabilná v rámci ~1,5 cm počas 260 s |
| `M2` | 40,6 s a 42,5 s: o **44 cm** inde v Z a 18 cm vo výške, **rozptyl 0,2 – 0,3 cm — prijaté** | od 45,7 s, po prechodnom burste s rozptylom 30,9 cm |

ARCore teda po prvom zbadaní značky ohlási pózu, ktorá je **vnútorne konzistentná, ale zlá**,
a opraví ju až o niekoľko sekúnd, keď sa kamera pohne. Brána rozptylu chytila `M1`, `M2` nie.
Dôsledok: fity v 40,6 a 42,5 s stáli na zlej polohe `M2` a nasledujúci fit preskočil o
**1,53 m a 3,9°**. Brána kvality to nezachytila, lebo chyba bola kolmo na stenu a dĺžka spojnice
sa takmer nezmenila (`baseline` −0,127 → −0,122).

Ako to riešiť, je otvorené — napríklad neprijať observáciu, kým značka nie je sledovaná aspoň
niekoľko sekúnd, alebo kým sa dva bursty z rôznych miest nezhodnú.

### Drift v rámci miestnosti

`M1` leží pri počiatku session space a jej nameraná poloha sa za 260 s nepohla o viac než
~1,5 cm. `M2`, 7 m od počiatku, sa medzi ustálenými burstami túlala o **~13 cm** vo vodorovnej
rovine a 11 cm vo výške. Fity z dvoch značiek preto medzi sebou kolíšu o desiatky centimetrov
podľa toho, koľko sa medzi skenmi nachodilo: 187 s → 227 s, ~25 m chôdze, posun **0,41 m a 1,0°**.

### `baseline` po preložení

Bez fitov so skreslenou `M2` (40,6 – 44,5 s): priemer **−4,5 cm** (−2,5 až −6,8 cm, n = 15
fitov z ustálených observácií). V prvom behu −9,3 cm. Ak značky teraz visia presne na 1,41 m od
rohov, skutočná vzdialenosť je 6,86 m a ARCore ju podhodnocuje o ~1,7 %. Bez pásma medzi
značkami ostáva toto predpoklad.

## Čo z toho plynie pre formálny test

- Test E robiť **chôdzou medzi značkami**: 13× `Re-anchor` striedavo pri `M1` a `M2`. Na mieste
  ešte raz skúsiť, či sa dajú obe chytiť ako `Tracking` z jedného miesta.
- **Prvý burst po príchode k značke zahodiť** — po zbadaní pár sekúnd počkať a trochu pohnúť
  telefónom, až potom `Re-anchor`. Kým to nerieši kód, je to na človeku.
- HUD prepnúť na `full`, aby bol riadok `Alignment` vidieť aj na videu.
- **Test G zmerať aj tak** — dĺžka steny dáva vzdialenosť značiek len nepriamo, cez predpoklad, že visia presne na 1,41 m.
- Pred `Drop` nastaviť výšku oka.
- Keď mapa skáče, zapísať si, čo sa robilo a kam mierila kamera.

## Opravené pri vyhodnotení

`frilens_eval.py` nečítal CSV z 0.3.1: `SessionLogger` píše UTF-8 s BOM a hlavička sa
nerozpoznala. Opravené v `413e4f5`.
