# ADR 008 — Rozšírenie rozsahu na navigáciu a hru

**Verzia:** 0.2.0-alpha · **Dátum:** 2026-09-05 · **Stav:** navrhnuté, čaká na potvrdenie vedúcim projektu

## Kontext

FriLens vznikol s úmyselne úzkym zadaním. `docs/README.md` ho hovorí priamo:

> Nie navigačná appka. Nie prekryv miestností. Jedna značka, jedna plocha, vlastné oči.

To bolo správne. Úzke zadanie je dôvod, prečo za tri týždne vznikol overený merací prístroj
a nie polovičná aplikácia — a prečo sa dá o driftu hovoriť číslami
([výsledky baseline](../2026-09-04-vysledky-baseline.md)).

Projekt sa však stáva **inžinierskym projektom na tri semestre**, teda 375 hodín práce
s obhajobou na konci. Meranie driftu samo o sebe nie je téma na tri semestre; je to prvá
kapitola. Zároveň sa objavil konkrétny cieľ použitia: **AR hra a navigácia na deň otvorených
dverí fakulty.**

Otázka teda nie je, či rozsah rozšíriť, ale ako, aby sa tým nezničilo to, čo projekt doteraz
robí dobre.

## Zvažované možnosti

**1. Nechať FriLens prístrojom a hru postaviť ako samostatný projekt.** Čisté oddelenie,
ale hra by potrebovala celý lokalizačný reťazec znova a merací prístroj by prestal mať
používateľa. Dva projekty, jeden zmysel.

**2. Prepísať FriLens na aplikáciu a meranie zahodiť.** Najrýchlejšia cesta k demu a najhoršia
k obhajobe. Bez meraní je hra bakalárska práca s pekným videom a otázka „aký je prínos?" nemá
odpoveď.

**3. Rozšíriť rozsah a meranie ponechať ako režim.** Aplikácia navigáciou aj hrou, ale
diagnostický HUD, CSV telemetria a vypínateľné korekčné režimy zostávajú a sú tým, čím sa práca
obhajuje.

## Rozhodnutie

**Možnosť 3.** FriLens prestáva byť len meracím prístrojom a stáva sa aplikáciou, ktorej jadrom
je lokalizácia voči existujúcemu modelu budovy. Meranie sa neruší — mení sa z účelu projektu na
jeho **trvalú vlastnosť**.

Konkrétne to znamená štyri záväzky:

**1. Diagnostický režim zostáva navždy.** HUD a CSV telemetria sa nevypnú ani v „produkčnej"
verzii pre návštevníkov; iba sa skryjú za prepínač. Beh, ktorý sa nedá spätne prečítať, je pre
prácu bezcenný.

**2. Každá korekcia polohy je samostatne vypínateľný režim.** Nezmenené oproti
[ADR 007](007-vyuzitie-modelu-na-lokalizaciu.md) a platí to aj po rozšírení rozsahu. Kruhovosť —
meranie modelu modelom — je pri navigačnej aplikácii ešte lákavejšia než pri meracom prístroji,
lebo zapnuté korekcie vyzerajú na obrazovke lepšie.

**3. Herné stanovište je zameraná značka.** Toto je vecné jadro rozhodnutia a dôvod, prečo
rozšírenie rozsahu meranie neničí, ale zosilňuje. Stanovište je bod so známou pravdou, takže
každý príchod naň dáva dvojicu *(nazbieraná chyba, prejdená vzdialenosť)* s referenciou.
Návštevník zbiera body, aplikácia zbiera meranie. Sú to tie isté dve sekundy.

Znamená to aj obmedzenie na návrh hry: **stanovište bez značky nevznikne.** Herná mechanika,
ktorá by značku nepotrebovala, sa buď doplní o značku, alebo sa nepoužije.

**4. Baseline sa nesmie stratiť.** Čísla z vetvy 0.1.x sú jediný bod, voči ktorému sa dá
akékoľvek zlepšenie porovnať. Protokol behu bez korekcií zostáva reprodukovateľný aj po tom, čo
appka bude robiť niečo úplne iné.

## Dôsledky

**Prepisuje sa hlavička `docs/README.md`.** Veta „Nie navigačná appka. Nie prekryv miestností."
prestáva platiť a musí zmiznúť — ale s odkazom sem, nie ticho. Kto číta staršie dokumenty, musí
vedieť, kedy a prečo sa zadanie zmenilo.

**Staršie dokumenty sa neprepisujú.** [Plán](../2026-09-02-implementacny-plan.md),
[protokol](../2026-09-04-protokol-baseline-testu.md) aj
[výsledky](../2026-09-04-vysledky-baseline.md) popisujú, čo sa naozaj robilo, a zostávajú platné
ako záznam. Nová práca ide do
[plánu inžinierskeho projektu](../2026-09-05-plan-inzinierskeho-projektu.md).

**Fáza 7 pôvodného plánu sa stáva jadrom práce**, nie prílohou. Poradie A → B → C z
[ADR 007](007-vyuzitie-modelu-na-lokalizaciu.md) zostáva nezmenené.

**Pribúda vrstva, ktorá doteraz nebola: obsah.** Stanovištia, questy a texty sú dáta, nie kód,
a treba na ne autorský nástroj. Ručné umiestňovanie dvadsiatich stanovíšť v Unity scéne je cesta,
ktorá sa neudrží — rovnaká chyba ako piecť navmesh namiesto extrakcie
([ADR 001](001-zdroj-navmesh-geometrie.md)).

**Riziko, ktoré tým vzniká:** rozsah hry je nekonečný a rozsah práce nie je. Herné funkcie sú
príťažlivejšie na programovanie než časticový filter a budú sa samy tlačiť dopredu. Poistkou je
zoznam „ak sa nestíha, vypúšťa sa" v každom semestri
[plánu](../2026-09-05-plan-inzinierskeho-projektu.md) — a to, že z neho vyplýva, že sa vypúšťa
hra, nie meranie.

## Súvisiace

- [ADR 007](007-vyuzitie-modelu-na-lokalizaciu.md) — lokalizácia pomocou modelu, poradie korekcií
  a prečo je kruhovosť hlavné nebezpečenstvo.
- [ADR 006](006-kotvenie-a-strata-trackingu.md) — strata trackingu a značky ako jediný liek.
  Rozhodnutie, že stanovište je značka, je priamym dôsledkom.
- [Plán inžinierskeho projektu](../2026-09-05-plan-inzinierskeho-projektu.md) — rozpis práce,
  ktorý z tohto rozhodnutia vychádza.
