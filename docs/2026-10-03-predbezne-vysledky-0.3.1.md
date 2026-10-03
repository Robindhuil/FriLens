# Predbežné výsledky 0.3.1-alpha — voľný beh v break roome

**Verzia:** 0.3.1-alpha · **Dátum:** 2026-10-03 · **Zariadenie:** Xiaomi Redmi Note 10 Pro (M2101K6G), Android 11
**Log:** `frilens-20261003-131810.csv` · 570 s, 2171 riadkov · porovnanie: `frilens-20260909-134644.csv`, `frilens-20260909-135106.csv` (0.2.2-alpha)

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
~7 m chôdze medzi dvomi skenmi, nie chyba modelu. Rozlíšiť to bez pásma nejde — **chýba Test G**.

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

## Čo z toho plynie pre formálny test

- Test E robiť **chôdzou medzi značkami**: 13× `Re-anchor` striedavo pri `M1` a `M2`. Na mieste
  ešte raz skúsiť, či sa dajú obe chytiť ako `Tracking` z jedného miesta.
- **Test G zmerať** — bez pásma sa `baseline` nedá rozdeliť na chybu modelu a drift.
- Pred `Drop` nastaviť výšku oka.
- Keď mapa skáče, zapísať si, čo sa robilo a kam mierila kamera.

## Opravené pri vyhodnotení

`frilens_eval.py` nečítal CSV z 0.3.1: `SessionLogger` píše UTF-8 s BOM a hlavička sa
nerozpoznala. Opravené v `413e4f5`.
