# FriLens — dokumentácia

**Verzia:** 0.1.0-alpha · **Stav:** fázy 0–2, 3c a 5 hotové; nič z toho zatiaľ nebežalo na telefóne

Samostatný Unity projekt oddelený od `FriWorld`. Jediná otázka, na ktorú má odpovedať:
**ako presne sa navmesh premietne do skutočnej fakulty a ako rýchlo to odchádza, keď sa
človek prejde.**

Nie navigačná appka. Nie prekryv miestností. Jedna značka, jedna plocha, vlastné oči.

## Aktuálne dokumenty

| Dokument | Čo je v ňom |
|---|---|
| [Stav projektu a analýza navmeshu](2026-09-02-stav-projektu-a-analyza-navmeshu.md) | čo v projekte skutočne je, čo obsahuje `navmesh.blend`, kritické diery |
| [Implementačný plán](2026-09-02-implementacny-plan.md) | fázy 0–6, od hygieny projektu po test v teréne |
| [ADR 001 — Zdroj navigačnej geometrie](decisions/001-zdroj-navmesh-geometrie.md) | prečo nepiecť navmesh, ale extrahovať existujúce plochy |
| [ADR 002 — Verzovanie modelov](decisions/002-verzovanie-modelov.md) | čo robiť s 300 MB blend súborom |
| [ADR 003 — Póza značky z nav polygónov](decisions/003-poza-znacky-z-nav-polygonov.md) | prečo model budovy netreba a čo to znamená pre výber miesta na značku |
| [ADR 004 — Zariadenia bez ARCore](decisions/004-zariadenia-bez-arcore.md) | AR Optional, runtime detekcia a náhradný Preview režim |

Východiskový návrh testu žije vo FriWorlde:
[`FriWorld/docs/2026-08-29-frilens-ar-test.md`](../../FriWorld/docs/2026-08-29-frilens-ar-test.md).
Tam, kde sa s ním tunajšie dokumenty rozchádzajú, je rozdiel výslovne vysvetlený.

## Verzie a buildy

Verzia je konštanta `Version` v
[`Assets/_Game/Editor/AndroidBuilder.cs`](../Assets/_Game/Editor/AndroidBuilder.cs). Je to
jediné miesto, kde sa mení — odtiaľ sa stampuje do `PlayerSettings.bundleVersion` aj do
názvu priečinka s buildom. Player Settings sa preto ručne needitujú; editor ich vie prepísať
pri každom uložení a build, ktorého manifest nesedí s názvom priečinka, je horší než žiadna
verzia.

Build sa spúšťa cez menu **`FriLens > Build Android <verzia>`** a skončí v:

```
Documents/Robin/unity/frilens/<verzia>/
    FriLens <verzia>.apk
    build-info.txt      ← verzia, dátum, Unity, bundle id, min SDK, backend, ABI, scény
```

Nový build:

1. zdvihnúť `Version` **aj** `VersionCode` v `AndroidBuilder.cs`,
2. dopísať, čo sa zmenilo, do [`CHANGELOG.md`](../CHANGELOG.md),
3. spustiť menu položku.

Prvý IL2CPP build trvá aj desiatky minút a editor je počas neho nereagujúci. Ďalšie sú
podstatne rýchlejšie.

**Každý dokument nesie v hlavičke verziu**, ku ktorej sa vzťahuje. Keď sa verzia zdvihne
a dokument sa vecne zmení, prepíše sa mu aj verzia v hlavičke.

## Ako túto dokumentáciu udržiavať

- Nové zistenie o projekte ide do **analýzy**, nie do plánu.
- Zmena postupu ide do **plánu**.
- Voľba medzi dvoma cestami, ktorú by sa niekto o mesiac spýtal „prečo takto?",
  ide do **`decisions/`** ako nové ADR. Staré ADR sa neprepisuje — dostane stav
  `nahradené ADR NNN`.
- Dátum v názve súboru je dátum vzniku, nie poslednej úpravy.
