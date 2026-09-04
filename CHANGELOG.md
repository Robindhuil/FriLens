# Changelog

Verzia projektu je konštanta `Version` v
[`Assets/_Game/Editor/AndroidBuilder.cs`](Assets/_Game/Editor/AndroidBuilder.cs). Odtiaľ sa
stampuje do `PlayerSettings.bundleVersion` aj do názvu priečinka s buildom. Je to jediné
miesto, kde sa mení.

Nový build: zdvihnúť `Version` aj `VersionCode`, dopísať riadok sem, spustiť
`FriLens > Build Android <verzia>`.

## [Unreleased]

## [0.1.5-alpha] — 2026-09-04

Prvý beh na 0.1.4-alpha, Redmi Note 10 Pro, 298 sekúnd. Prekryv bolo konečne vidieť
a tlačidlo na jeho skrytie sa použilo štyrikrát. Prevzorkovanie funguje. Filter skokov nie.

### Namerané

| | |
|---|---:|
| walked | 105,94 m |
| path_raw | 150,23 m |
| nafúknutie | **+42 %** |
| skoky | 69 (43,29 m) |

Najčistejší úsek behu — 44,9 s súvislej chôdze bez jediného skoku — dáva **walked 16,79 m
oproti raw 20,46 m, teda +22 %**. To je najlepší odhad samotného skreslenia, aký zatiaľ máme:
bez relokalizácií, bez manipulácie, len chôdza s telefónom v ruke.

Celých 42 % za beh je viac, lebo súčasťou behu bolo aj státie, mierenie a stláčanie tlačidiel.

### Fixed

- **Filter skokov označoval kroky za relokalizácie aj po oprave v 0.1.4-alpha.** Tá oprava
  fungovala len keď človek stojí. Delil som časom od poslednej zmeny pózy, lenže ten čas sa
  resetuje pri každom posune nad 4 mm — a pri chôdzi sa póza hýbe každý snímok, takže delič
  bol zase jeden snímok.

  Beh to ukázal ako učebnicový príklad. Skoky sa rozpadli na dve skupiny s medzerou medzi nimi:

  | | počet | spolu |
  |---|---:|---:|
  | ≥ 1 m — od 1,13 do 6,71 m | 10 | 31,61 m |
  | < 1 m — 59× medzi 0,19 a 0,93 m | 59 | 11,68 m |

  Tých 59 sú kroky. Absolútny strop na jeden meter trafil všetkých desať skutočných a ani
  jeden falošný, takže **rýchlostný test je preč**. Nedal sa spraviť správne: čas, za ktorý
  sa krok stal, sa z vykresľovacej slučky zmerať nedá, lebo ARCore dodáva pózy vlastným
  tempom. Cena je, že relokalizácia kratšia než meter sa započíta ako chôdza — čo stojí menej
  než meter na stometrovej dráhe.

### Added

- **Prekryv aj `Origin` sú kotvené na `ARAnchor`.** ARCore pri relokalizácii posúva anchory tak,
  aby zostali na tom istom fyzickom mieste; obyčajné súradnice v `Transform` neposunie, o tých
  nevie. Držali sme oboje ako obyčajné súradnice, takže prekryv zostal zle práve vtedy, keď sa
  tracker trafil.

  Pri `Origin` to bolo vidieť v číslach: bez kotvy som ho pri každom skoku posúval ručne a za
  beh `001103` sa nazbieralo **43 m takých posunov naprieč 69 skokmi**. Riadok `From marker` na
  konci hlásil 1,94 m a neznamenal nič.

  Nový `AnchoredRoot` položí koreň na pózu okamžite a kotvu pripojí hneď, ako ju ARCore vytvorí.
  Keď kotvenie zlyhá, appka beží ďalej s pôvodným správaním a povie to. Do scény pribudol
  `ARAnchorManager`, ktorý tam dovtedy nebol.

- **Po strate trackingu appka priznáva, že nemeria.** `TrackingContinuity` počíta straty a čas
  naslepo od posledného zarovnania. Kým nejaká visí, riadok `Alignment` hlási
  `unverified · N losses M s` a v CSV je `verified = 0`. Vyčistí to jedine zarovnanie na značke.

  Dôvod je v tom, že **zlyhaná relokalizácia je tichá**. Úspešnú zmerať vieme — je to skok
  a filter ju chytí. Pri neúspešnej nenastane skok vôbec: póza plynulo pokračuje z nesprávneho
  miesta a na obrazovke to vyzerá presne ako drift, teda ako to, čo má test merať. Z logu sa
  „model je nepresný" a „ARCore sa nezotavil" doteraz odlíšiť nedalo.

  Zlyhanie tým nedetegujeme. Označíme okno, v ktorom mohlo nastať, čo je maximum, ktoré sa
  z pózy dá poctivo povedať. Rozbor v
  [ADR 006](docs/decisions/006-kotvenie-a-strata-trackingu.md).

- **CSV má štyri nové stĺpce:** `blind_s`, `losses`, `verified`, `origin_anchored`.

### Zmena metodiky

Značka nie je len začiatok merania, je to **liek na stratu trackingu** — jediná vec, ktorá
nezávisí od mapy ARCore. Preto ich má byť **viac, rozmiestnených po trase**: najhoršia možná
chyba je potom ohraničená úsekom medzi dvomi značkami, nie dĺžkou celého behu. Fáza 3a sa mení
z „vytlačiť značku" na „vytlačiť značky a rozmiestniť ich".

## [0.1.4-alpha] — 2026-09-04

Prvé tri behy na Redmi Note 10 Pro s 0.1.3. Tracking funguje, prejdená vzdialenosť sa počíta
— **84,4 m za 195 sekúnd** v najdlhšom behu. Ale filter skokov robil falošné poplachy a samotné
meranie dráhy stálo na metóde, ktorá číslo systematicky nafukuje.

### Changed

- **Prejdená vzdialenosť sa už nesčítava snímok po snímku.** Poloha sa najprv vyhladí
  a segment sa pripočíta až vtedy, keď sa vyhladená poloha vzdiali o 0,30 m od naposledy
  podržaného bodu. Mávanie telefónom v stoji kmitá okolo stojacej strednej hodnoty, takže ho
  filter utlmí a prah zahodí; chôdza strednú hodnotu posúva a prejde.

  Nie je to kozmetika. Sčítavanie každého snímku je z definície správne a **systematicky
  nadhodnocuje**, lebo šum sa pri sčítavaní absolútnych hodnôt nikdy nevykráti. Pri GPS
  záznamoch trajektórií je to zmerané na jednotky percent pri bežnom vzorkovaní a až dvadsať
  percent pri najhustejšom. U nás k šumu pribúda ruka. Rozbor v
  [ADR 005](docs/decisions/005-ako-merat-prejdenu-vzdialenost.md).

- **Filter má vlastný test.** `PathResampler` je oddelený od `CameraTravel` a čas dostáva ako
  argument, takže sa dá pustiť na vymyslených dráhach so známou dĺžkou —
  `FriLens > Verify Travel Filter`. Ladiť ho na chodbe a vyhlásiť za dobrý by bolo dookola.

  Dvadsať metrov chôdze s dvomi centimetrami šumu vyjde raw ako **30,74 m**, teda o 54 % viac,
  a pri 30 fps ako 22,96 m. Prevzorkované je to v oboch prípadoch 19,5 m. Státie s mávaním
  1 Hz ±25 cm dá raw 20 m a prevzorkovane **0,00 m**.

  **Čísla z behov pred touto verziou sú raw.** Filter pustený spätne na uložené pózy z tých
  istých logov:

  | beh | trvanie | appka hlásila | prevzorkované z CSV |
  |---|---:|---:|---:|
  | 214544 | 138 s | — | 65,40 m |
  | 225741 | 191 s | 84,35 m | **65,25 m** |
  | 230108 | 38 s | 19,78 m | 13,34 m |

  Rekonštrukcia je hrubá — v CSV sú pózy štyrikrát za sekundu, kým na telefóne filter beží na
  snímkovej frekvencii — ale smer aj rád sú jednoznačné. **Tabuľka „namerané v behu 225741"
  nižšie je preto tiež raw** a skutočné úseky boli kratšie.

- **Do CSV pribudol stĺpec `path_raw_m`** — pôvodný súčet bez filtrovania, hneď vedľa
  `walked_m`. Na obrazovke je pod veľkým číslom drobné `raw`. Rozdiel medzi tými dvomi je
  presne tá ruka a ten šum, ktoré sa doteraz vykazovali ako chôdza.

- **Riadok `Tracking` hovorí, čo robiť, nie ako sa volá porucha.** `ExcessiveMotion` je teraz
  „move the phone more slowly", `InsufficientFeatures` „point at a wall with more detail".
  Je to odporúčanie priamo z dokumentácie ARCore a tu má váhu navyše: každý taký stav končí
  relokalizáciou, a relokalizácia je ten metrový skok overlayu.

  Tým sa vysvetľuje aj pozorovanie „agresívnejším mávaním sa gyroskop rozladil o meter".
  Gyroskop sa nerozladil. ARCore stratil tracking na `ExcessiveMotion` a relokalizoval sa —
  sú to tie isté metrové skoky, ktoré v logu behu `225741` sedia na 1,46 · 3,36 · 1,80 · 1,70 m.

- **UI je menšie.** Referenčné rozlíšenie panelu 360×780 → 430×930, čo celý HUD zmenší
  približne o šestinu.

### Fixed

- **Overlay nebolo vidieť, takže „Hide overlay" nemalo čo skryť.** Model má 80 m a leží desiatky
  metrov od počiatku sveta, kým AR session svoj svet vždy začína pri kamere. Bez značky tak
  overlay ostal 19 m nabok a 5 m nad hlavou — a `far clip plane` bola 20 m. Nikto ho nikdy
  nevidel; tlačidlo pritom fungovalo celý čas.

  Dve opravy: far plane na 120 m a `ProvisionalPlacement`, ktorý pri chýbajúcej značke položí
  podlahu modelu pod kameru. **Nie je to zarovnanie a nemeria to nič** — je to na to, aby sa
  dalo overiť, že sa overlay kreslí, dá skryť a že pri chôdzi ujde. Riadok `Alignment` to
  priznáva textom „dropped, not measured".

- **Riadky `Marker` a `Alignment` sa nedali prečítať.** Hodnota „none" vyzerala ako prázdno.
  Teraz je tam „waiting for marker", respektíve „dropped, not measured" — teda čo daný stav
  znamená pre dôveryhodnosť toho, čo je na obrazovke.

- **`far clip plane` bola 20 m na oboch kamerách.** Aj po správnom zarovnaní by sa z 80-metrovej
  chodby kreslila len štvrtina.

- **Filter skokov označoval bežnú chôdzu za relokalizáciu.** Medzi 65. a 69. sekundou jedného
  behu je zhluk 24 „skokov", každý 0,13–0,20 m, všetky rovnaké, všetky pri `SessionTracking`
  bez hlásenej poruchy. To nie sú relokalizácie, to sú kroky.

  Príčina bola v tom, ako som počítal rýchlosť. ARCore dodáva pózy tempom kamery, teda výrazne
  pomalšie, než sa vykresľuje. Na snímku, keď nová póza dorazí, sa objaví pohyb za celý
  medzičas naraz — a delením jedným `Time.deltaTime` vyjde rýchlosť niekoľkonásobne vyššia,
  než aká naozaj bola. Teraz sa delí časom od poslednej **skutočnej zmeny pózy**.

  Skutočné relokalizácie boli v tom behu štyri, každá metrová: 1,46 · 3,36 · 1,80 · 1,70 m.
  Tie absolútny strop na dĺžku kroku zachytí ďalej.

- **Počítadlo sa spúšťalo skôr, než dorazila prvá póza.** Session hlási `SessionTracking`
  o snímok či dva skôr, než driver zapíše pózu, a dovtedy je kamera na počiatku sveta. V behu
  `230108` sa tak `Origin` zafixoval na (0,0,0), `From marker` meral od miesta, kde nikto
  nestál, a skok na prvú skutočnú pózu sa započítal ako relokalizácia. Teraz sa čaká, kým sa
  póza naozaj pohne.

- **Re-anchor bez značky tvrdil „sampling 0/30" donekonečna.** Keď sa za dve sekundy nenazbiera
  ani jedna vzorka, stav sa vráti na `none`.

- **Riadok v logu miešal dva snímky.** `SessionLogger` mohol bežať skôr než `CameraTravel`,
  takže zapísal aktuálnu pozíciu kamery spolu s odvodenými hodnotami z predošlého snímku.
  V behu `230108` to vidno na dvoch riadkoch s rovnakou pozíciou a inou `from_origin`. Poradie
  skriptov je teraz pevné: `CameraTravel` (−50) → `SessionLogger` (50) → `DiagnosticsHud` (60).

### Namerané v behu 225741

Po štarte je odometria čistá. Medzi značkami, teda počas súvislej chôdze:

| úsek | čas | prejdené | skoky |
|---|---:|---:|---:|
| mark-2 → mark-3 | 4,7 s | 0,76 m | 0 |
| mark-3 → mark-4 | 14,1 s | 8,06 m | 0 |
| mark-4 → mark-5 | 30,1 s | 13,62 m | 0 |
| mark-5 → mark-6 | 36,5 s | 21,07 m | 0 |

**43 metrov súvislej chôdze bez jediného skoku.** Zhluk falošných skokov aj skutočné
relokalizácie padli do prvých 85 sekúnd, keď sa telefónom manipulovalo.

## [0.1.3-alpha] — 2026-09-03

Vydané ako [v0.1.3-alpha](https://github.com/Robindhuil/FriLens/releases/tag/v0.1.3-alpha).

Prvý beh na **Redmi Note 10 Pro** s 0.1.2-alpha ukázal dve veci: oprava pózy funguje, a meranie
vzdialenosti má dve chyby, ktoré by v teréne skreslili výsledok.

### Fixed

- **Prejdená vzdialenosť zostávala nulová.** `CameraTravel` začínal počítať až po zosúladení
  a bez vytlačenej značky k zosúladeniu nikdy nedôjde. V logu z 55-minútového behu tak bolo
  `walked_m = 0.000`, hoci póza kamery sa preukázateľne hýbala v rozsahu 8 metrov. Počítadlo sa
  teraz spustí samo pri prvom snímku; zosúladenie ho naďalej vynuluje, takže číslo, ktoré test
  číta, je stále „od zosúladenia".

  Vedľajší efekt je dôležitejší než samotná oprava: **desaťmetrová kontrola sa dá spraviť bez
  značky**, teda ešte pred fázami 3a a 3b.

- **Relokalizácie ARCore sa počítali ako chôdza.** V tom istom behu boli tri skoky rýchlejšie
  než 3 m/s — najrýchlejší 2.85 m za 0.26 s, teda 10 m/s. Spolu **5.43 m zo 77 m, sedem percent
  dráhy**. Drift sa meria ako percento prejdenej vzdialenosti, takže sedem percent chyby ide
  priamo do osi merania. Kroky nad 4 m/s sa už do vzdialenosti nerátajú.

### Added

- **Počítadlo skokov.** Zahodené kroky sa nestrácajú — počítajú sa zvlášť a HUD ich ukáže vedľa
  priamej vzdialenosti (`6.2 m · 3 jumps 5.4 m`). Sú to momenty, keď ARCore opravil sám seba,
  a na obrazovke práve vtedy prekryv viditeľne skočí. Odlišuje to nález „prekryv sa vzďaľoval
  postupne" od „tracker sa prelokalizoval", čo sú dve rôzne príčiny.
- Stĺpce `jumps` a `jumped_m` v CSV, medzi `from_origin_m` a `since_align_s`.

### Opravené po review

Prehliadka logiky pred buildom našla štyri veci, z toho dve by pokazili meranie:

- **`CameraTravel` začínal počítať skôr, než naskočil tracking.** Kamera je dovtedy na
  počiatku sveta, takže `Origin` sa zafixoval na (0,0,0) a `From marker` by ukazoval
  vzdialenosť od nuly, nie od miesta, kde človek stál. Skok na prvú skutočnú pózu sa navyše
  započítal ako relokalizácia. Počítadlo teraz čaká na `SessionTracking`.
- **Zber vzoriek pre zosúladenie nemal časový limit.** Keď značka zmizla zo záberu uprostred
  série, rozobraté vzorky tam zostali a po návrate sa spriemerovali s novými — možno cez
  relokalizáciu, z inej vzdialenosti a uhla. Vyšlo by z toho zosúladenie, ktoré vyzerá
  odmerane a nie je. Po dvoch sekundách bez použiteľnej vzorky sa séria zahodí.
- **Filter skokov stál len na rýchlosti.** Dlhý snímok — zadrhnutie alebo prvý snímok po
  návrate z pozadia — spraví z dvojmetrového skoku zdanlivú prechádzku. Pribudol absolútny
  strop na dĺžku kroku.
- **`CameraTravel.Reset()` sa volalo ako Unity správa.** Editor volá `Reset()` sám pri
  pridaní komponentu alebo pri „Reset" v inšpektore. Premenované na `RestartFrom()`.

### Zmenené UI

HUD prekreslený podľa návrhu, ktorý preberá vizuálny jazyk z FriWorld-Hub: papier a inkoust,
tvrdé obrysy, tieň ako plná posunutá vrstva. Celé UI je po anglicky, rovnako ako CSV a logy.

- **Farby sú tokeny v USS a nikde inde.** C# nastavuje triedy, nikdy farby — aj bodka
  v tabletke režimu má triedy `pill-dot--ok` / `--idle` / `--accent`. Prefarbenie je jeden
  súbor, nie hľadanie po kóde.
- **`DiagnosticsHudView`** je jediná trieda, ktorá siaha na vizuálny strom. Nič nevytvára, len
  prepína triedy a texty na hierarchii, ktorá už existuje v UXML. `DiagnosticsHud` vie, čo
  čísla znamenajú; view vie, ako vyzerajú.
- **Preview prepína celý HUD jedným volaním** — banner do čierna so šrafou, číselník stlmený,
  hodnoty neutrálne, hlavné číslo na *not measuring*, Re-anchor vypnutý. Nedá sa skončiť
  napoly v Preview a vyzerať ako AR.
- Riadok `Device` a počítadlo skokov doplnené do návrhu, ktorý ich ešte nemal.
- Písma: Fredoka (nadpisy), Nunito (texty), JetBrains Mono (hodnoty). Mono má funkčný dôvod —
  hodnoty sa menia niekoľkokrát za sekundu a proporcionálne písmo by riadkami trhalo.
  Google dnes dáva Fredoka a Nunito len ako variabilné, takže rez 700 robí `-unity-font-style`.
- Deväť ikon z návrhu plus vygenerovaná ikona pre `Device`.

#### Štyri pasce UI Toolkitu, na ktoré sa narazilo

Všetky tiché — nič nevypíše chybu, len to vyzerá zle:

1. **UI Toolkit chce `UnityEngine.TextCore.Text.FontAsset`, nie `TMPro.TMP_FontAsset`.** TMP
   asset na tej istej ceste sa nenačíta a text sa jednoducho nevykreslí. Bez varovania, bez
   náhradného rezu.
2. **`var()` funguje pre farby, ale nie pre `-unity-font-definition`.** Font za premennou
   zmizne rovnako ticho. Cesty k fontom sú preto vypísané celé pri každom použití.
3. **`border-radius` kláti vodorovný a zvislý polomer zvlášť**, takže `999px` na širokom
   nízkom prvku spraví elipsu, nie pilulku. Polomery sú polovica výšky prvku.
4. **Prvky sa v riadku samy nezmenšujú.** Dlhá hodnota stlačila ikonu z 26 px na 3 px a jej
   glyf vyliezol von; hodnota `Alignment` zase pretiekla kartu o 30 px. Pevné rozmery
   dostali `flex-shrink: 0`, hodnota `flex-shrink: 1`.

## [0.1.2-alpha] — 2026-09-03

Vydané ako [v0.1.2-alpha](https://github.com/Robindhuil/FriLens/releases/tag/v0.1.2-alpha).
Obsahuje aj 0.1.1-alpha, ktorá sa samostatne nevydala.

### Testovacie zariadenia

| Telefón | ARCore | Poznámka |
|---|---|---|
| **Redmi Note 10 Pro** | ✅ trackuje | zariadenie, na ktorom sa bude testovať ďalej |
| **Redmi 11T** | ✅ trackuje | log z neho potvrdil chybu s `InputActionManager` |
| **Redmi 14C** | ❌ netrackuje | session sa spustí, kamera beží, tracking sa nikdy neustáli |

Diagnostika hardvéru. Po 0.1.1-alpha sa kamera konečne zapla — oprava so štartom AR rigu
zabrala — ale session naďalej neopustila `SessionInitializing` a póza kamery zostala presne
nulová. Táto verzia nič neopravuje; dáva appke schopnosť povedať prečo.

### Added

- **Riadok `Device` v HUD-e.** ARCore robí motion tracking cez VIO, čo bez gyroskopu nejde.
  Bez neho sa session otvorí, kamera nabehne a tracking sa nikdy neustáli — na obrazovke
  na nerozoznanie od session, ktorá je len pomalá. Ak `SystemInfo.supportsGyroscope` hlási
  nepravdu, HUD napíše **„no gyroscope — AR cannot track"** načerveno.
- **Časovač na zaseknutú session.** `SessionInitializing` nehlási žiadne zlyhanie —
  `notTrackingReason` zostáva `None`, lebo sa nič nepokazilo, tracking len nikdy
  nedokonverguje. Ako oranžové slovo to vyzeralo ako „ešte pracujem" donekonečna. Po 20
  sekundách HUD prepne na **„stuck initializing N s"** načerveno. Je to nález, nie stav.
- **Model telefónu a senzory do prvého riadku logu** — `device`, `android`, `gyro`, `accel`,
  `gfx`. Bez toho sa log, kde tracking nikdy nenaskočil, nedá odlíšiť od logu zo zariadenia,
  ktoré trackovať nikdy nevedelo.

### Changed

- **Appka už neverí `CheckAvailability()` naslepo.** Tá metóda odpovedá na otázku „dá sa tu
  použiť ARCore API", nie „vie toto zariadenie trackovať" — a na prvú stačí mať nainštalované
  Google Play Services for AR, čo ide aj na telefón bez potrebného hardvéru. Pribudli dve
  poistky:

  1. **Chýbajúci gyroskop sa kontroluje skôr než ARCore.** Motion tracking je vizuálno-
     inerciálny, takže bez gyroskopu niet z čoho počítať inerciálnu polovicu. Appka ide rovno
     do Preview a napíše prečo, namiesto toho, aby čakala na session, ktorá nikdy nenabehne.
  2. **Časový limit na `SessionInitializing`.** Zariadenie môže gyroskop mať a aj tak nikdy
     nedokonvergovať — necertifikovaný telefón bez kalibračného profilu robí presne to. Po 25
     sekundách appka AR vzdá a prepne do Preview s vysvetlením. Zostať v AR režime navždy
     znamená živý obraz z kamery a zamrznutý prekryv, čo vyzerá ako rozbitá appka namiesto
     nevhodného telefónu.

### Poznámka k 0.1.1-alpha

Oprava so štartom AR rigu bola v 0.1.1-alpha označená ako príčina toho, že session neopúšťala
`SessionInitializing`. **Nebola.** Build 0.1.0-alpha z releases — teda bez oboch opráv —
na inom telefóne trackuje správne, takže príčinou bol hardvér, nie poradie zapínania rigu.
Zmena v kóde zostáva, lebo vypínať `ARCameraManager` pod bežiacou session je aj tak zlé, ale
ako oprava tohto problému bola pripísaná neprávom.

### Oprava `InputActionManager` je potvrdená

Log z telefónu, ktorý ARCore zvláda, s buildom **0.1.0-alpha z releases** — teda bez oboch
opráv:

```
0.270   Ar, SessionInitializing, None
4.572   Ar, SessionTracking,     None      ← ARCore trackuje
…       55 sekúnd v SessionTracking
59.982  Ar, SessionInitializing, InsufficientLight
```

Po celý ten čas, vrátane 55 sekúnd v `SessionTracking`:

```
cam_x = cam_y = cam_z = 0.0000
cam_yaw = cam_pitch = cam_roll = 0.00
walked_m = 0.000
```

**ARCore trackoval bezchybne a póza sa aj tak do aplikácie nedostala.** Presne to je chyba,
ktorú `InputActionManager` opravuje: `TrackedPoseDriver` číta pózu cez `InputActionReference`
do `XRI Default Input Actions` a tie akcie bez neho nikto nezapne. Diagnóza sedela.

Zároveň sa ukázalo, že stĺpec `not_tracking_reason` funguje — pri zhoršenom svetle sa objaví
`InsufficientLight`. Na Redmi tam bolo `None` počas celej doby, čo teda naozaj znamenalo
„nič nezlyhalo", nie „appka to nevie prečítať". Dve rôzne poruchy, dva rôzne obrazy.

## [0.1.1-alpha] — 2026-09-03

Opravné vydanie. Prvý beh 0.1.0-alpha na telefóne ukázal, že AR strana nefungovala vôbec;
oboje spôsobili zmeny z fáz 2 a 5.

### Fixed

- **Kamera sa nikdy nehýbala.** V logoch z 0.1.0-alpha bola pozícia aj rotácia kamery presne
  `0.0000` po celý čas, vo všetkých piatich behoch. `TrackedPoseDriver` na `Main Camera` berie
  pózu cez `InputActionReference` do `XRI Default Input Actions`, a také akcie sa samy
  nezapnú — zapínal ich `InputActionManager`, ktorý bol vo fáze 2 odstránený ako zvyšok po
  tap-to-place. Bez neho by `walked_m` zostalo nulové aj po prejdení celej fakulty a celé
  meranie by nemeralo nič. Komponent je späť.

- **ARCore nikdy neopustil `SessionInitializing`.** `SessionModeController` vypínal celý AR
  rig hneď v `Start()`, teda aj `ARCameraManager`, kým bežala kontrola dostupnosti. `ARSession`
  je samostatný objekt a spustí session už v prvom snímku, takže ARCore naštartoval bez
  kamery a späť sa nechytil: čierne pozadie, žiadne snímky a `notTrackingReason` zostalo
  `None`, takže na obrazovke nebolo vidno dôvod. AR rig sa teraz nevypína vôbec; `PreviewRig`
  je v scéne vypnutý rovno a zapne ho až Preview vetva.

### Changed

- APK sa pomenúva `FriLens-<verzia>.apk` s pomlčkou namiesto medzery. GitHub premieňa medzeru
  v názve assetu na bodku, takže s pomlčkou sa adresa na stiahnutie dá odvodiť priamo
  z verzie a nemusí sa nikde opisovať.
- **Verziu stampuje build callback, nie len menu položka** (`VersionStamp.cs`). Držať verziu
  v kóde malo zabrániť tomu, aby sa Player Settings rozišli so skutočnosťou, ale samo to
  fungovalo len pre buildy spustené z menu FriLens. Build z Unity dialógu ten kód nikdy
  nespustil a potichu vydal predošlé číslo verzie — prvé APK 0.1.1-alpha vyšlo označené ako
  0.1.0-alpha, s opravami vnútri a zlým menom na obale. `IPreprocessBuildWithReport` sa
  spustí pri každom builde, nech ho začne kto chce.

### Overené na 0.1.0-alpha

Aj napriek tomu, že AR nefungovalo, prvý beh potvrdil:

- **Redmi 14C ARCore podporuje** — `CheckAvailability()` prešlo, appka išla do AR režimu.
  Predpoklad [ADR 004](docs/decisions/004-zariadenia-bez-arcore.md), že ho nepodporuje,
  neplatil. Rozhodnutie o `AR Optional` a Preview režime zostáva v platnosti pre iné
  zariadenia.
- HUD sa vykresľuje správne na výšku, nič nie je odrezané a **zmenšená kópia panelu, ktorá
  strašila v editorových snímkach, na telefóne nie je** — bol to artefakt `ScreenCapture`.
- Orientácia je zamknutá, appka pýta povolenie na kameru, CSV sa píše a všetky tlačidlá
  zapisujú udalosti (`mark-1`, `overlay-hidden`, `overlay-shown`, `realign-requested`).

## [0.1.0-alpha] — 2026-09-02

Prvý build. Nič z toho zatiaľ nebežalo na telefóne.

### Added

- Extraktor navigačných plôch z `navmesh.blend` — `FriLens > Extract Nav Meshes`, okno
  s výberom podlažia podľa prefixu, aj dávka cez všetkých deväť vnútorných podlaží.
  Vygenerované `ra0`–`ra3`, `rb0`–`rb3`, `rc0`.
- Zosúladenie prekryvu podľa vytlačenej značky. Póza sa priemeruje cez 30 snímok, nie
  odčíta z jedného — jeden snímok nesie šum trackera, nie chybu v zameraní značky.
- Diagnostický HUD v UI Toolkit: režim, stav trackingu, značka, čas od zosúladenia
  s rozptylom vzoriek, prejdená vzdialenosť, priama vzdialenosť od značky. Tlačidlá
  re-anchor, skrytie prekryvu a označenie meracieho bodu.
- Zápis behu do CSV v `Application.persistentDataPath`, 4 riadky za sekundu plus riadok
  pri každej udalosti.
- Preview režim pre telefóny bez ARCore — prekryv proti obyčajnému pozadiu s otočnou
  kamerou. Umožňuje overiť build, mesh, materiál, UI aj log na zariadení, ktoré AR nevie.
- `FriLens > Verify Alignment Math` — kontroly matematiky zosúladenia proti známym
  odpovediam, vrátane pasce so znamienkom kvaterniónu.
- Držanie obrazovky zapnutej po celú session. Zamknutie obrazovky ukončí AR session
  a zosúladenie po ňom už meria drift novej session.
- `FriLens > Build Android <verzia>` — stampne verziu, zbuilduje APK a uloží ho do
  `Documents/Robin/unity/frilens/<verzia>/` aj s `build-info.txt`.

### Overené na APK

Prvý build prešiel. `aapt dump badging` na `FriLens 0.1.0-alpha.apk` (43 MB):

| | |
|---|---|
| package | `sk.uniza.fri.frilens` |
| versionName / versionCode | `0.1.0-alpha` / `1` |
| minSdkVersion / targetSdk | 25 / 36 |
| native-code | `arm64-v8a` |
| orientácia | portrait |
| `com.google.ar.core` | **`optional`** — nainštaluje sa aj na zariadenie bez ARCore |
| `com.google.ar.core.InstallActivity` | prítomná, teda aj tok na doinštalovanie Play Services for AR |
| permissions | `CAMERA`, `INTERNET` |
| debuggable | nie, release build |

Vydané ako [v0.1.0-alpha](https://github.com/Robindhuil/FriLens/releases/tag/v0.1.0-alpha).
Repozitár bol pri tejto príležitosti zverejnený — release assety z privátneho repozitára sa
bez prihlásenia stiahnuť nedajú a tlačidlo na
[FriWorld-Hub](https://github.com/Robindhuil/FriWorld-Hub) by nefungovalo. Pred zverejnením
prekontrolované, že v repozitári nie je keystore, `.env` ani žiadny kľúč; `navmesh.blend` je
gitignorovaný, takže 300 MB sken sa nezverejnil.

### Changed

- ARCore prepnutý z `AR Required` na `AR Optional`. Pri `Required` sa appka na zariadení
  bez ARCore ani nenainštaluje, takže by sa na dostupnom telefóne neoverilo nič.
- ARCore `Depth` z `Required` na `Optional` — test nekreslí okluziu, požadovať depth API by
  len zúžilo zoznam telefónov.
- Min SDK z 30 na 25. Nižšie Unity 6000.4 nedovolí.
- Orientácia zamknutá na Portrait.
- Scéna presunutá a premenovaná na `Assets/_Game/Scenes/FriLensTest.unity`.

### Removed

- Šablónové UI, `Object Spawner`, `Screen Space Ray Interactor`, `Directional Light`,
  `ARPlaneManager`, `ARRaycastManager`, `InputActionManager`. `ARPlaneManager` išiel preč
  nielen kvôli CPU — kreslil prechodové štvorce po podlahe, teda po ploche, ktorej hranu má
  test čítať.
- Balíček `com.unity.xr.arkit` aj s osirenými assetmi. iOS sa nerobí.
- `XR Origin` odpojený od šablónového prefabu, čím počet závislostí scény na
  `MobileARTemplateAssets` a `Samples` klesol zo 16 na 1.

### Známe obmedzenia

- **Test v teréne sa v tejto verzii spraviť nedá.** Chýba vytlačená značka, jej zameraná
  póza a telefón s ARCore.
- `MarkerAnchor` stojí na počiatku, takže po zosúladení pristane prekryv na nezmyselnom
  mieste. Očakávané, nie chyba.
- Reference Image Library je prázdna, takže appka nemá čo sledovať. Obava, že prázdna
  knižnica zhodí build, sa **nepotvrdila** — build prešiel a v APK jednoducho nie je žiadna
  `.imgdb`.
- Na snímkach z editora sa vľavo hore objavuje zmenšená kópia UI panelu. Vyzerá to na
  artefakt `ScreenCapture`, potvrdiť sa to dá až na telefóne.
