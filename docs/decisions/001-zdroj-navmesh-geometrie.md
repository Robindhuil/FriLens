# ADR 001 — Zdroj navigačnej geometrie: nepiecť, extrahovať

**Dátum:** 2026-09-02 · **Stav:** prijaté

## Kontext

Pôvodný plán ([`FriWorld/docs/2026-08-29-frilens-ar-test.md`](../../../FriWorld/docs/2026-08-29-frilens-ar-test.md),
krok 4) počítal s tým, že v FriLens naimportujeme model zo skenu, upečieme naň `NavMeshSurface`
s `agentRadius` okolo 0.01 a výsledok cez `NavMesh.CalculateTriangulation()` premeníme na
obyčajný mesh. Malý polomer preto, aby hrana plochy sadla na stenu a dala sa v teréne
porovnať s lištou.

Analýza `Assets/Models/navmesh.blend` ukázala, že tento predpoklad neplatí. Súbor obsahuje
310 hotových navigačných plôch, ktoré niekto **ručne nakreslil v Blenderi** — sú to ploché
polygóny (rozptyl Y vrcholov 0.0000 m) s minimom trojuholníkov (chodba dlhá 9.75 m má
5 trojuholníkov). Nie je to výstup z pečenia.

## Zvažované možnosti

**A. Piecť podľa pôvodného plánu.** Vyžaduje `com.unity.ai.navigation`, model budovy
v projekte a riešenie problému, že `agentRadius` 0.01 zráža voxel size na ~1.6 mm, čo na
budove veľkosti 95 × 136 m znamená neúnosný čas a pamäť pri pečení.

**B. Extrahovať existujúce plochy.** Editorový skript prejde prefab z `navmesh.blend`,
vyfiltruje plochy podľa prefixu v mene, zapečie transformácie do vrcholov a uloží jeden
kombinovaný `Mesh` asset na podlažie.

## Rozhodnutie

**Možnosť B.**

## Dôvody

- Odpadá celý problém s agent radiusom aj s voxelizáciou.
- Odpadá závislosť na `com.unity.ai.navigation`.
- Rozdelenie na podlažia je zadarmo — pomenovanie `r<budova><podlažie><miestnosť>_..._nav_<n>`
  ho dáva ako filter na reťazec. Rezanie podľa výšky by bolo nesprávne, lebo podlažia sa
  v Y prekrývajú kvôli schodiskám.
- Výsledok je rádovo ľahší: celé `ra` podlažie 0 má 1 259 trojuholníkov.

## Dôsledky

**Dobré.** Menej krokov, menej balíčkov, rýchlejšia iterácia. Extrakcia je deterministická
a opakovateľná z jedného tlačidla.

**Zlé.** Presúva sa tým otázka presnosti. Pri pečení by sme vedeli, že hrana je odsadená
presne o `agentRadius`. Pri ručne kreslených polygónoch to z geometrie nevyplýva.

> **Doplnené 2026-09-02:** autor modelu potvrdil, že polygóny sedia na steny a pochádzajú
> z toho istého skenu ako geometria budovy. Odsek vyššie tým prestal byť otvorenou otázkou
> a `fri_building.blend` sa donášať nebude — pozri [ADR 003](003-poza-znacky-z-nav-polygonov.md).
> Rozhodnutie tohto ADR sa nemení.

**Pozor.** Importér natáča koreň o 270° okolo X (Blender Z-up → Unity Y-up). Extrakcia
musí túto rotáciu zapiecť do vrcholov, inak bude prekryv v AR otočený o 90°.
