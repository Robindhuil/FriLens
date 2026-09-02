# Changelog

Verzia projektu je konštanta `Version` v
[`Assets/_Game/Editor/AndroidBuilder.cs`](Assets/_Game/Editor/AndroidBuilder.cs). Odtiaľ sa
stampuje do `PlayerSettings.bundleVersion` aj do názvu priečinka s buildom. Je to jediné
miesto, kde sa mení.

Nový build: zdvihnúť `Version` aj `VersionCode`, dopísať riadok sem, spustiť
`FriLens > Build Android <verzia>`.

## [Unreleased]

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
  `Documents/Robin/unity/frilens/builds/android/<verzia>/` aj s `build-info.txt`.

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
- Reference Image Library je prázdna. ARCore potrebuje na zostavenie `.imgdb` aspoň jeden
  obrázok, takže build na nej môže zlyhať — overí sa až prvým buildom.
- Na snímkach z editora sa vľavo hore objavuje zmenšená kópia UI panelu. Vyzerá to na
  artefakt `ScreenCapture`, potvrdiť sa to dá až na telefóne.
