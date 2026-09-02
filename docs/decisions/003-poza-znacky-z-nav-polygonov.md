# ADR 003 — Póza značky sa určí z nav polygónov, model budovy netreba

**Verzia:** 0.1.0-alpha · **Dátum:** 2026-09-02 · **Stav:** prijaté

## Kontext

[Analýza](../2026-09-02-stav-projektu-a-analyza-navmeshu.md) označila za najvážnejšiu dieru
(K1), že v projekte je len 310 podlahových polygónov a žiadne steny. Krok 5 pôvodného návrhu
testu žiada určiť pozíciu a rotáciu značky v modeli, a to sa bez zárubní a rohov miestností
robiť nedá. Navrhované riešenie bolo doniesť `fri_building.blend` (90 MB) z FriWorldu ako
editorový pomocník a najprv overiť, či má rovnaký počiatok súradníc.

Druhá diera bola, že nikto neoveril, či sú ručne kreslené polygóny konzistentne na stenách —
[ADR 001](001-zdroj-navmesh-geometrie.md) to označil za najväčšiu neznámu presnosti testu.

Autor modelu potvrdil: **polygóny sedia na steny a pochádzajú z toho istého skenu ako
geometria budovy.**

## Rozhodnutie

`fri_building.blend` sa do FriLens **nedonesie**. Póza značky sa určí priamo z hrán
navigačných polygónov.

Konkrétne:

- **Vodorovná pozícia a natočenie** — z hrany alebo rohu polygónu. Ak hrana leží na
  vnútornom líci steny, potom je aj značka nalepená na tú stenu v tej istej rovine.
- **Výška** — pravítkom na mieste, pripočítaná k známej výške podlahy. Podlaha `ra000`
  je na `Y = 5.15 m`, overené na vygenerovanom `ra0_nav` (560 z 1 933 vrcholov leží do
  5 cm od tejto roviny).

## Dôsledky

**Dobré.** Odpadá 90 MB blend v projekte, odpadá overovanie zhody počiatkov súradníc,
a odpadá závislosť na druhom projekte. Fáza 3b implementačného plánu už nie je blokovaná.

**Obmedzenie na výber miesta pre značku.** Referenciou môže byť **len to, čo podlahový
polygón zachytáva** — roh miestnosti alebo rovný úsek steny. Zárubňa, výklenok, stĺp či
lišta v polygóne nie sú a ich pozíciu z neho vyčítať nemožno. Pôvodný dokument ponúkal
zárubňu ako jednu z možností; tá odpadá.

**Zostáva overiť pravítkom.** To, že polygóny sedia na steny v modeli, nie je to isté ako
že model je 1:1 voči skutočnosti. Šírka `ra000_corridor_3_nav_1` je 3.20 m a to je priamy
test — ak nameraná šírka sedí, mierka aj lícovanie sú v poriadku naraz.

**Riadok v tabuľke výsledkov ostáva.** „Hrana sedí v jednej miestnosti a nesedí vo
vedľajšej" je teraz nepravdepodobný scenár, nie očakávaný — ale ak nastane, znamená to,
že predpoklad z tohto ADR neplatí, a to je informácia, ktorú test má vedieť odovzdať.
