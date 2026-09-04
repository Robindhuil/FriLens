# Analýza: čo je v modeli a odkiaľ vziať steny

**Verzia:** 0.1.5-alpha · **Dátum:** 2026-09-04

Vzniklo pri otázke, či sa dá znalosť modelu využiť na určovanie polohy
([ADR 007](decisions/007-vyuzitie-modelu-na-lokalizaciu.md)). Väčšina tamojších možností
potrebuje steny, tak bolo treba zistiť, či ich máme.

## Čo `navmesh.blend` obsahuje

343 meshov, 344 objektov. Rozdelené podľa rodín mien:

| rodina | počet | vrcholy | čo to je |
|---|---:|---:|---|
| `Teren`, `cesta`, `trava`, `Dlazba`, `Obrubnik`, … | ~20 | ~70 000 | **exteriér** — terén, cesty, zeleň okolo budovy |
| `ra*` | 127 | 8 242 | navigačné polygóny budovy A |
| `rb*` | 118 | 4 225 | navigačné polygóny budovy B |
| `rc*` | 36 | 2 790 | navigačné polygóny budovy C |
| `Object629` | 1 | 6 714 | neidentifikované, 44,9 × 35,8 × 5,4 m |
| `outside_*`, `terrace_*` | ~10 | ~4 000 | vonkajšie plochy a terasy |

Najväčšie meshe sú exteriérové (`Teren.001` má 24 550 vrcholov), čo je pre nás irelevantné —
merať sa má vnútro.

**Steny v modeli nie sú.** Ani stropy, ani zárubne. Je to navmesh plus krajinárske okolie.

## Steny sa dajú odvodiť z navmeshu

Modelovať ich netreba. Navmesh ich obsahuje implicitne.

**Hraničná hrana** navigačnej siete je hrana, ktorá patrí práve jednému trojuholníku. Znamená
to, že za ňou sa nedá pokračovať — teda že tam je stena. Vnútorné hrany patria dvom
trojuholníkom a tie sú len delením plochy, nie prekážkou.

Postup je teda: nájsť hrany s počtom incidentných trojuholníkov rovným jednej a vytiahnuť ich
zvisle nahor o výšku podlažia.

### Prečo dvere zostanú otvorené samy

Toto je vlastnosť, kvôli ktorej je metóda použiteľná bez ručného označovania.

Cez dvere sa dá prejsť, takže tade navigačná sieť **pokračuje** — hrana v mieste dverí patrí
dvom trojuholníkom a hraničná teda nie je. Nevytiahne sa a otvor zostane.

Presne opačne pri stene: tam sieť končí, hrana je hraničná a stena vznikne.

Znamená to, že kvalita odvodených stien je presne taká, ako kvalita navmeshu — čo je pre náš
účel správne, lebo navmesh je aj to, čo sa má merať.

### Čo z toho vypadne

Jeden extraktor dá naraz tri veci, ktoré ADR 007 potrebuje:

- **zvislé roviny** na porovnávanie s rovinami z ARCore (možnosť D),
- **bariéry** pre časticový filter (možnosť C),
- **dominantné smery** chodieb na zarovnanie kurzu (možnosť A) — stačí histogram normál
  odvodených stien.

### Známe obmedzenia metódy

- **Výška je odhad.** Navmesh nenesie informáciu o výške stropu; vytiahne sa konštanta na
  podlažie. Pre bariéry a smery to nevadí, pre porovnávanie rovín to znamená, že hornú hranu
  steny nemožno použiť.
- **Zariadenie a nábytok chýbajú.** Navmesh popisuje, kadiaľ sa dá prejsť. Skriňa pri stene
  polygón zoreže, takže vznikne „stena" pred skutočnou stenou. Pre časticový filter je to
  neškodné, pre ICP zavádzajúce.
- **Otvorené priestory.** V jedálni alebo prednáškovej sále je hraničná hrana ďaleko a stien
  málo. Tam metóda dá najmenej.
- **Sklenené steny a zábradlia** sú v navmeshi hranicou, ale kamera cez ne vidí. Pri
  porovnávaní rovín to bude nesúlad, ktorý netreba pripísať driftu.

## Čo z toho vyplýva pre ďalší postup

Krok „doniesť model budovy" nie je potrebný a nikdy nebol — rovnaký záver ako
[ADR 003](decisions/003-poza-znacky-z-nav-polygonov.md), len z inej strany. Tam išlo o pózu
značky, tu o steny; obe sú v navmeshi.

Extraktor stien nie je súčasťou baseline testu a stavať sa bude až po ňom.
