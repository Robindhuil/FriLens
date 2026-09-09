# Plán: steny, roh a prvé zameranie značky

**Verzia:** 0.2.0-alpha · **Dátum:** 2026-09-09 · **Stav:** plán pred návštevou fakulty

Prvá návšteva, po ktorej má appka merať **zhodu modelu s budovou**, nie len tracking. Doteraz
sa merala odometria; značka je to, čo tie dve veci spojí.

Miesto: **`rc000_break_room`** — v modeli sa volá tak, atrium tam nie je. Je na podlaží `rc0`,
nie `ra0`, takže prekryv bolo treba prehodiť.

Štyri pravouhlé rohy to ale nemá: **východná strana je rozstrapkaná asi o 25 cm**, sú v nej
výklenky a dvere. Použiteľná je západná stena a rohy na jej koncoch.

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

## Zamerané — dve značky na západnej stene

Podlaha `Y = 0,694 m`, steny do `Y = 7,08 m`. Západná stena beží od rohu **A** (`Z 3,828`,
stretáva sa s južnou stenou) po roh **B** (`Z 13,621`, severná stena). Obe sú dlhé a rovné.

| značka | poloha stredu | umiestnenie |
|---|---|---|
| `frilens-M1` | X −21,902 · Y 2,194 · Z 5,328 | 1,50 m od rohu **A** |
| `frilens-M2` | X −21,902 · Y 2,194 · Z 12,121 | 1,50 m od rohu **B** |

Výška stredu 1,50 m nad podlahou pri oboch. Tri rovnaké čísla, menej na zapamätanie pri stene.

### Prečo každá od svojho rohu, a nie obe od jedného

Pôvodne som navrhoval merať obe od rohu A. Bola to horšia voľba, a nielen preto, že deväť metrov
sa pásmom meria zle.

Zarovnáva sa vždy **na jednu značku**, nie na obe naraz. Keby boli obe odmerané od rohu A, obe by
zdedili tú istú chybu a **dali by identické zarovnanie** — z rozdielu medzi nimi by sa nedalo
prečítať nič.

Keď je každá viazaná na svoj roh, rozdiel medzi zarovnaním z M1 a z M2 je **priamo to, o koľko
sa model rozchádza so skutočnosťou** na dĺžke steny. To je meranie, kvôli ktorému projekt
existuje, a vypadne zadarmo.

Cena je, že prekryv pri prezarovnaní z jednej značky na druhú **poskočí** o ten rozdiel. To nie
je chyba, to je ten výsledok — a je lepšie ho vidieť než schovať.

### Kam presne siaha pásmo

ARCore kladie pose sledovaného obrázka do jeho **stredu**, takže 1,50 m platí pre stred
180 mm obrysu. Stred sa však meria zle — je to imaginárny bod uprostred bieleho papiera.

V PDF je obrys presne v strede A4 (okraje 15,0 / 15,2 mm bokom, 58,5 / 58,6 mm hore a dole),
takže stred obrysu je aj stredom listu. Merať sa dá jednoduchšie: **na spodnú a bočnú linku
obrysu, o 90 mm bližšie.**

| čo | odkiaľ kam | koľko |
|---|---|---|
| výška | podlaha → spodná linka obrysu | 1,41 m |
| od rohu | roh → bližšia zvislá linka obrysu | 1,41 m |

Dve rovnaké čísla a obe končia na vytlačenej linke, nie na odhadnutom bode.

Vodorovnosť sa overí tým, že **oba spodné rohy obrysu sú 1,41 m nad podlahou**. Ak sa líšia,
značka je natočená a zarovnanie ten uhol zdedí.

Od okraja papiera sa nemeria a papier sa neorezáva: tlačiareň má posun (namerané okraje
15 / 14 / 16 / 17 mm). Pravda je vytlačený obrys, nie list.

### Orientácia značky — overená, nie odhadnutá

AR Foundation kladie sledovaný obrázok do **lokálnej roviny XZ**, normála mieri po lokálnej
**−Y** a „hore" na papieri je lokálne **+Z**. Nie +Z ako normála, ako by sa čakalo.

Odčítané zo simulačného providera (`SimulatedTrackedImage` stavia quad s vrcholmi
`(±x, 0, ±y)` a normálou `-Vector3.up`) a potom **skontrolované číselne**: výsledná normála
vyšla `(1,00, 0, 0)`, teda do miestnosti, a hore `(0, 1,00, 0)`. Zámena by otočila celý prekryv
o 90° a nič na obrazovke by nepovedalo prečo.

### Dosky na kontrolu

Na oboch pózach je v scéne **viditeľná doska s textúrou tej istej značky**. Po zarovnaní má
nakreslená doska pristáť presne na vytlačenej — medzera medzi nimi je chyba zarovnania
v centimetroch, čitateľná okom. To je lepšia kontrola než akékoľvek číslo na HUD-e.

## Rozhodnutie, ktoré treba spraviť ako prvé: odkiaľ prídu steny

Toto určuje, **čo sa vlastne meria**, takže sa nedá odložiť na neskôr.

> **Vyriešené 2026-09-09.** Steny aj stropy pribudli priamo do `navmesh.blend`, pomenované po
> miestnostiach vedľa nav polygónov — `_wall_`, `_ceiling_`. Sú z toho istého skenu, takže sú
> s nav plochami konzistentné z podstaty a odvodzovanie z hraničných hrán odpadá.

Pôvodná úvaha: sken dáva jeden zdroj pravdy, ručné domodelovanie dva, ktoré sa môžu rozchádzať —
a potom nie je jasné, ktorý model je vedľa.

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

## Rozmer značky — overené

Vytlačené 2026-09-08 na Brother MFC-B7810DW z PDF `tlac-frilens-M*-180mm.pdf`, mierka 100 %,
bez prispôsobovania strane. **Nameraných presne 180 mm**, čiže tlačiareň neškáluje.

Do `FriLens > Marker Library` teda ide **0.18**.

Platí to pre značky vytlačené na tejto tlačiarni týmto postupom. Iná tlačiareň alebo iné
nastavenie mierky znamená nové meranie — číslo sa neprenáša.

## Jediná vec, ktorá potichu pokazí všetko

**Rozmer značky odmerať pravítkom na vytlačenom papieri**, nie prevziať z toho, čo šlo do tlače.
Tlačiarne mierku menia. Päť percent v tomto čísle je päť percent v mierke celého prekryvu
a na obrazovke to nie je vidieť — zarovnanie vyzerá čisto, rozptyl je malý a všetky vzdialenosti
sú o dvadsatinu vedľa.

**Meria sa vonkajší obrys**, teda tenká linka úplne na kraji, nie hrubý rámik okolo vzoru.
Knižnica deklaruje rozmer *celého obrázka*; rámik má 928 z 1024 pixelov, takže zámena je
desaťpercentná chyba. Značky majú tú linku pridanú práve preto — biely okraj na bielom papieri
sa odmerať nedá — a inštrukcia je vytlačená priamo na nich.

Odmerať **obe strany**. Ak sa líšia, tlačiareň škálovala nerovnomerne a taká značka sa ako
metrická referencia použiť nedá.
