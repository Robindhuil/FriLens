# ADR 009 — Vyhodnotenie sa neviaže na deň otvorených dverí

**Verzia:** 0.2.0-alpha · **Dátum:** 2026-09-05 · **Stav:** prijaté

## Kontext

[ADR 008](008-rozsirenie-rozsahu-na-navigaciu-a-hru.md) postavil prácu na tom, že sa aplikácia
overí na reálnej udalosti, a tou udalosťou mal byť **deň otvorených dverí** — veď preň sa hra
navrhuje.

Kalendár to nedovolí. Deň otvorených dverí je **vo februári**, tretí semester projektu končí
**v januári**. V okne projektu ležia dva:

| | | |
|---|---|---|
| **DOD február 2027** | pár týždňov po konci semestra 1 | aplikácia existuje päť mesiacov a nemá čo nasadzovať |
| **DOD február 2028** | po odovzdaní práce | dáta z neho sa už do práce nedostanú |

Prvý je priskoro, druhý neskoro. Medzi nimi nie je tretí.

## Zvažované možnosti

**1. Zrýchliť a nasadiť na DOD 2027.** Znamenalo by minúť semester 1 na produktovú prácu namiesto
na zameranie značiek a meranie zhody modelu s budovou. Práca by prišla o svoj základ a demo by
bežalo na nepreverenom predpoklade, že model sedí na budovu. Presne tá chyba, pred ktorou varuje
[ADR 007](007-vyuzitie-modelu-na-lokalizaciu.md) pri kruhovosti: vyzeralo by to dobre a nemeralo
by to nič.

**2. Posunúť poradie semestrov** tak, aby tretí bol letný a končil v júni 2028. DOD 2028 by padol
do jeho stredu. Vecne najlepšie, ale závisí od študijného plánu, nie od projektu.

**3. Naplánovať vlastnú udalosť.** Tri až štyri pozvané triedy stredoškolákov po 20–25 ľuďoch
v novembri 2027, teda v druhom mesiaci semestra 3.

## Rozhodnutie

**Možnosť 3.** Hlavné vyhodnotenie prebehne na pozvaných triedach v novembri 2027. Možnosť 2
zostáva otvorená — ak ju študijný plán dovolí, plán sa na ňu prepíše bez zmeny obsahu práce.

Nie je to ústupok, je to lepší plán, a to z troch dôvodov.

**Termín je pod kontrolou.** Dátum dňa otvorených dverí neurčuje ani študent, ani vedúci projektu.
Keď sa naň nestihne, nestihne sa vôbec — je raz za rok. Pozvaná trieda sa preloží o dva týždne.
Tým z kritickej cesty projektu mizne jej jediný pevný bod.

**Vzorka je čistejšia.** Trieda príde naraz, dá sa rozdeliť na skupinu s aplikáciou a kontrolnú
s papierovou mapou, a obe idú tou istou trasou v ten istý čas. Na dni otvorených dverí to nejde:
návštevníci prichádzajú priebežne, chodia, kam chcú, a kontrolná skupina by sa musela robiť inokedy
a inak. Porovnanie „čas do miestnosti s aplikáciou proti papierovej mape" je pritom jeden z hlavných
výsledkov práce.

**Dá sa opakovať.** Ak prvá trieda odhalí chybu, druhá príde o dva týždne a meranie sa zopakuje.
Jednorazová udalosť túto možnosť nedáva a chyba v nej sa nedá napraviť ničím.

## Čo sa s dňami otvorených dverí urobí namiesto toho

**DOD február 2027 — merací beh, nič viac.** Nenasadzuje sa nič, žiaden návštevník appku neuvidí.
Ide sa odmerať tracking v budove plnej ľudí, čo je jediná príležitosť získať dáta z prevádzkových
podmienok, ktoré sa v prázdnej chodbe nasimulovať nedajú:
[baseline](../2026-09-04-vysledky-baseline.md) ukázal, že pätnásť sekúnd zakrytej kamery stojí
desiatky metrov, a dav je presne to prostredie, kde k tomu dochádza. Rozpočet 4 h, balík 2.8.

**DOD február 2028 — ostrá prevádzka po odovzdaní.** To, že projekt pokračuje aj po obhajobe, je
argument pre fakultu a dôvod, prečo sa oplatí do neho investovať. Nie je to súčasť práce a práca
naň nečaká.

## Dôsledky

**Aplikácia musí byť hotová do júna 2027**, teda do konca semestra 2. Udalosť je v novembri,
september a október sú na odolnosť a prípravu. Čo sa do júna nedostane do aplikácie, do práce sa
nedostane vôbec. Semester 2 preto stojí na strope 125 h bez rezervy a má napísané, čo sa z neho
vypúšťa ako prvé — časticový filter, nie hra ani meranie.

**Pribúda organizačná práca, ktorú softvér nenahradí:** dohodnúť školy, termíny a súhlasy
zákonných zástupcov s fotografovaním a so zberom telemetrie. Je to v balíku 3.2 a treba to začať
v lete, nie v októbri.

**Overiť ešte pred schválením zadania**, či fakulta nemá **jesenný termín dňa otvorených dverí**.
Ak áno, nahradí pozvané triedy a vyhodnotenie prebehne na skutočnom DOD vnútri semestra 3. Mení sa
tým názov udalosti v balíku 3.3, nie rozpočet ani poradie práce.

## Súvisiace

- [ADR 008](008-rozsirenie-rozsahu-na-navigaciu-a-hru.md) — rozšírenie rozsahu, z ktorého potreba
  overenia na reálnej udalosti vyplynula.
- [Plán inžinierskeho projektu](../2026-09-05-plan-inzinierskeho-projektu.md) — kalendár, rozpočty
  a poradie práce, ktoré z tohto rozhodnutia vychádzajú.
