# ADR 004 — Zariadenia bez ARCore

**Verzia:** 0.1.0-alpha · **Dátum:** 2026-09-02 · **Stav:** prijaté

> **Oprava 2026-09-03:** predpoklad, na ktorom toto ADR stojí, **neplatil**. Build
> 0.1.0-alpha na Redmi 14C nabehol do AR režimu — `CheckAvailability()` prešlo a session sa
> spustila. Rozhodnutie sa napriek tomu nemení: `AR Optional` a Preview režim sú správne pre
> zariadenia, ktoré ARCore naozaj nemajú, a Preview zostáva užitočný na ukážku bez budovy.
> Zmenilo sa len to, že test v teréne už nečaká na iný telefón.
>
> Pozor pri čítaní výsledkov: ak Redmi 14C nie je v Googlom zozname certifikovaných
> zariadení a ARCore na ňom beží len vďaka nainštalovaným Play Services for AR, potom
> nemá overenú kalibráciu kamery a IMU. Namerané čísla môžu hovoriť o kalibrácii telefónu,
> nie o modeli. Na záverečné čísla treba porovnanie s certifikovaným zariadením.

## Kontext

Projekt bol postavený na predpoklade, že testovací telefón ARCore podporuje. Telefón, ktorý
je k dispozícii — **Redmi 14C** — ho podľa Googlovho zoznamu nepodporuje.

Doterajšie nastavenie bolo `ARCore Requirement = Required`. To zapíše do manifestu
`com.google.ar.core: required`, takže sa appka na nepodporovanom zariadení **ani
nenainštaluje**. Zo stavu „nemáme telefón na test" sa tým stáva „nemáme telefón, na ktorom
by sa dalo čokoľvek z appky vyskúšať", čo je zbytočne horšie.

## Čo sa tým nemení

**Samotný test sa bez ARCore spraviť nedá.** Meranie driftu potrebuje 6DoF tracking kamery
a na Androide to nevie poskytnúť nič iné. Žiadny náhradný režim to neobíde. Fázy 3a, 3b, 4
a 6 implementačného plánu zostávajú viazané na zariadenie zo
[zoznamu podporovaných ARCore zariadení](https://developers.google.com/ar/devices).

Toto ADR nerieši, ako test spraviť. Rieši, aby sa všetko ostatné dalo overiť na telefóne,
ktorý máme.

## Zvažované možnosti

**A. Nechať `AR Required` a čakať na podporovaný telefón.** Nulová práca, ale do príchodu
takého telefónu sa neoverí ani build, ani načítanie meshu, ani UI, ani zápis logu. Všetko by
sa potom ladilo naraz, v jednej relácii, pravdepodobne v teréne.

**B. `AR Optional` a runtime detekcia.** Appka sa nainštaluje všade. Pri štarte sa cez
`ARSession.CheckAvailability()` zistí, čo zariadenie vie, a podľa toho sa zapne buď AR
režim, alebo náhradný.

## Rozhodnutie

**Možnosť B.** `ARCore Requirement` prepnutý z `Required` na `Optional`.

Aplikácia má dva režimy:

- **AR** — zariadenie ARCore podporuje a `Google Play Services for AR` je nainštalované.
  Bežný chod: sledovanie značky, zosúladenie, prekryv nad skutočnou podlahou.
- **Preview** — zariadenie ARCore nepodporuje, alebo je podpora nedostupná. Prekryv sa
  vykreslí proti obyčajnému pozadiu s ovládateľnou kamerou.

Režim sa vyberá za behu, nie prepínačom v builde. Jeden APK beží na oboch.

## Dôsledky

**Dobré.** Na Redmi 14C sa dá overiť všetko okrem AR: že build prejde, že sa mesh načíta
a vyzerá správne na skutočnej obrazovke, že UI funguje pod prstom, že sa log zapisuje
a dá sa stiahnuť. Keď sa podporovaný telefón objaví, ostáva na ňom vyskúšať jedinú novú
vec — sledovanie značky.

**Dobré.** Preview režim je zároveň spôsob, ako niekomu ukázať, čo appka kreslí, bez toho,
aby sa muselo ísť do budovy.

**Zlé.** Dva režimy sú dva stavy, ktoré sa môžu rozísť. UI musí vždy jasne povedať, v ktorom
je — inak sa raz stane, že niekto bude na podporovanom telefóne skúmať, prečo prekryv
nereaguje na značku, hoci appka bežala v Preview.

**Pozor pri čítaní výsledku.** Nič v Preview režime nevypovedá o presnosti zosúladenia.
Je to zobrazovač meshu, nie test.

**Pozor na Play Services for AR.** Zariadenie môže byť na zozname podporovaných a ARCore
aj tak nebeží, ak nie je nainštalovaná samostatná aplikácia *Google Play Services for AR*.
`CheckAvailability()` tento stav vracia zvlášť (`NeedsInstall`) a UI ho musí odlíšiť od
„zariadenie to nevie", lebo prvé sa dá vyriešiť z Play, druhé nie.
