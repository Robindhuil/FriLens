# FriLens — dokumentácia

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

Východiskový návrh testu žije vo FriWorlde:
[`FriWorld/docs/2026-08-29-frilens-ar-test.md`](../../FriWorld/docs/2026-08-29-frilens-ar-test.md).
Tam, kde sa s ním tunajšie dokumenty rozchádzajú, je rozdiel výslovne vysvetlený.

## Ako túto dokumentáciu udržiavať

- Nové zistenie o projekte ide do **analýzy**, nie do plánu.
- Zmena postupu ide do **plánu**.
- Voľba medzi dvoma cestami, ktorú by sa niekto o mesiac spýtal „prečo takto?",
  ide do **`decisions/`** ako nové ADR. Staré ADR sa neprepisuje — dostane stav
  `nahradené ADR NNN`.
- Dátum v názve súboru je dátum vzniku, nie poslednej úpravy.
