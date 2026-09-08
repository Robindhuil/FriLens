# Plán: steny, roh a prvé zameranie značky

**Verzia:** 0.2.0-alpha · **Dátum:** 2026-09-09 · **Stav:** plán pred návštevou fakulty

Prvá návšteva, po ktorej má appka merať **zhodu modelu s budovou**, nie len tracking. Doteraz
sa merala odometria; značka je to, čo tie dve veci spojí.

Miesto: **atrium / break room** — má štyri pravouhlé rohy, čo je presne to, čo zameranie
potrebuje.

## Prečo roh a nie stena

Roh dá **polohu aj natočenie naraz**. Na rovnej stene vieš rovinu a jeden smer, ale poloha
pozdĺž steny je neurčitá — nie je sa čoho chytiť.

Póza značky ale **nie je póza rohu**. Značka bude na jednej stene, kus od rohu, v nejakej
výške. Zameranie sú štyri čísla:

| číslo | odkiaľ |
|---|---|
| XZ rohu | z nav polygónu, presne |
| vzdialenosť pozdĺž steny od rohu | pásmo |
| výška od podlahy | pásmo |
| ktorým smerom stena hľadí | zo smeru hrany polygónu |

Nav polygóny ležia na **vnútornom líci stien** — pochádzajú z toho istého skenu
([ADR 003](decisions/003-poza-znacky-z-nav-polygonov.md)), preto sa roh dá odčítať priamo
z meshu.

**Pozor na mieste:** sokel, radiátor alebo skriňa polygón zorežú, a jeho roh potom nie je ten
viditeľný roh miestnosti. Treba to skontrolovať okom, nie predpokladať.

## Rozhodnutie, ktoré treba spraviť ako prvé: odkiaľ prídu steny

Toto určuje, **čo sa vlastne meria**, takže sa nedá odložiť na neskôr.

**Zo skenu (`fri_building`).** Nav plochy pochádzajú z toho istého skenu ako steny, takže sú
navzájom konzistentné. Vtedy „model je 20 cm vedľa" znamená jednu vec.

**Ručne domodelované.** Vzniknú **dva zdroje pravdy**, ktoré sa môžu rozchádzať, a tá istá veta
prestane mať zmysel — nebude jasné, ktorý model je vedľa.

Ak steny pôjdu zo skenu, je to lepší zdroj než odvodzovanie z hraničných hrán navmeshu
([analýza](2026-09-04-analyza-geometrie-a-stien.md)) a tá karta z boardu do veľkej miery
odpadá. Odvodené steny majú zmysel len ako náhrada, keď sken nie je k dispozícii.

## Stropy zakryjú obrazovku

Materiál prekryvu je unlit s `Render Face = Both`. Keď sa v miestnosti nakreslí strop, človek
stojí vnútri zavretej krabice a **cez kameru nevidí nič**.

Stropy preto buď nedávať, alebo im dať **vlastné prepínanie zvlášť od stien**. Inak to zabije
prvý pokus a bude to vyzerať ako chyba niekde inde.

## Poradie práce

Kroky 3 a 4 pred cestou sú rozdiel medzi jednou návštevou a dvomi.

1. **Nájsť miestnosť v navmeshi.** Extraktor má tlačidlo *List groups*. V scéne je teraz `ra0`;
   ak je atrium na `rb` alebo `rc`, treba prehodiť prekryv na to podlažie.
2. **Naimportovať steny** (a rozhodnúť odkiaľ, viď vyššie).
3. **Vybrať roh a odčítať jeho súradnice** z nav polygónu.
4. **Nastaviť `MarkerAnchor`**, naplniť knižnicu **odmeraným** rozmerom značky, zbuildiť
   a nainštalovať.
5. Nalepiť značku presne tam, kde sa plánovalo, a skenovať.

## Čo sledovať, keď to zarovná

Sú to **dve rôzne merania** a nesmú sa zliať do jedného dojmu.

**Hneď po zarovnaní, postojačky.** Sedí prekryv na stenách? Toto je presnosť modelu plus chyba
zamerania plus chyba rozmeru značky. Malo by to byť blízko nule.

**Ak je to vedľa, nikam nechoď** — najprv oprav zameranie. Chôdza po zlom zarovnaní dá číslo,
ktoré sa nedá prečítať, lebo v ňom bude drift aj konštantná chyba naraz.

**Až potom chôdza.** Ako rýchlo prekryv ujde. To je drift a to je to, čo sa má merať.

Riadok `Alignment` ukáže `±N cm / N°` — rozptyl vzoriek. Jednotky centimetrov a pod stupňom
znamenajú čisté zarovnanie; veľký rozptyl znamená, že sa meral šum a treba zopakovať.

## Jediná vec, ktorá potichu pokazí všetko

**Rozmer značky odmerať pravítkom na vytlačenom papieri**, nie prevziať z toho, čo šlo do tlače.
Tlačiarne mierku menia. Päť percent v tomto čísle je päť percent v mierke celého prekryvu
a na obrazovke to nie je vidieť — zarovnanie vyzerá čisto, rozptyl je malý a všetky vzdialenosti
sú o dvadsatinu vedľa.
