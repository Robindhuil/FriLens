# Changelog

Verzia projektu je konštanta `Version` v
[`Assets/_Game/Editor/AndroidBuilder.cs`](Assets/_Game/Editor/AndroidBuilder.cs). Odtiaľ sa
stampuje do `PlayerSettings.bundleVersion` aj do názvu priečinka s buildom. Je to jediné
miesto, kde sa mení.

Nový build: zdvihnúť `Version` aj `VersionCode`, dopísať riadok sem, spustiť
`FriLens > Build Android <verzia>`.

## [Unreleased]

## [0.1.3-alpha] — 2026-09-03

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
