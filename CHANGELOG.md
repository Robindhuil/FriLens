# Changelog

Verzia projektu je konštanta `Version` v
[`Assets/_Game/Editor/AndroidBuilder.cs`](Assets/_Game/Editor/AndroidBuilder.cs). Odtiaľ sa
stampuje do `PlayerSettings.bundleVersion` aj do názvu priečinka s buildom. Je to jediné
miesto, kde sa mení.

Nový build: zdvihnúť `Version` aj `VersionCode`, dopísať riadok sem, spustiť
`FriLens > Build Android <verzia>`.

## [Unreleased]

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
