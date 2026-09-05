# ADR 005 — Ako merať prejdenú vzdialenosť

**Verzia:** 0.1.4-alpha · **Dátum:** 2026-09-03 · **Stav:** prijaté

## Kontext

Prejdená vzdialenosť je os, voči ktorej sa meria drift. Vetu „overlay bol 30 cm vedľa" sa dá
prečítať len s druhou polovicou: „po 47 metroch chôdze". Ak je menovateľ nafúknutý, je
nafúknutá aj celá kvalita, ktorú z merania vyvodíme.

Po prvých behoch v teréne prišlo pozorovanie:

> „Telefón nemeria úplne tak počet prejdenej vzdialenosti. Pohybom mobilu v stojacej polohe
> pridáva vzdialenosť. Nie že by to bolo niečo zlé, ale tá prejdená vzdialenosť je zavádzajúca."

Doteraz to `CameraTravel` počítal najpriamočiarejšie, ako sa dá: každý snímok pripočítal
vzdialenosť od predošlej pózy. To je správne z definície dĺžky krivky a **systematicky to
nadhodnocuje**.

## Prečo nadhodnocuje

Nie je to chyba v kóde, je to vlastnosť sčítavania dĺžky trajektórie zo vzoriek. Každá vzorka
nesie šum merania a šum sa pri sčítavaní **nevykráti** — vždy pridáva, nikdy neuberá, lebo
sčítavame absolútne hodnoty. Čím hustejšie vzorkujeme, tým viac šumu nazbierame.

Literatúra k trajektóriám to má zmerané. Pri GPS záznamoch pohybu zvierat vychádza dĺžka dráhy
pri bežných intervaloch vzorkovania nadhodnotená o jednotky percent a pri tých najhustejších
až o dvadsať percent — a to je čistý šum prístroja, žiadny skutočný pohyb. Opačný extrém platí
tiež: veľmi riedke vzorkovanie dĺžku podhodnocuje, lebo skratkuje zákruty. Medzi tým je
rozlíšenie, pri ktorom číslo zodpovedá tomu, čo nás zaujíma.

U nás je to horšie než pri GPS, lebo k šumu pribúda druhý zdroj: **ruka**. ARCore hlási pózu
kamery a kamera je na konci ruky. Keď stojíme a mávneme telefónom, kamera sa naozaj presunula
— nič v póze nedokáže odlíšiť mávnutie od kroku.

## Zvažované možnosti

**A. Nechať tak a napísať do metodiky „drž telefón pevne".** Nulová práca. Ale číslo zostane
nekontrolovateľné: nikdy sa nedozvieme, koľko z nameraných 84 m bola chôdza a koľko ruka.

**B. Premietnuť dráhu do vodorovnej roviny.** Zdvíhanie a spúšťanie telefónu by vypadlo.
Nerieši to však mávnutie do strany, ktoré je vodorovné, a rozbilo by to meranie na schodoch.

**C. Prevzorkovať dráhu pred sčítaním.** Štandardné riešenie práve na tento problém: polohu
najprv vyhladiť dolnopriepustným filtrom a segment pripočítať až vtedy, keď sa vyhladená poloha
vzdiali o pevný krok od naposledy podržaného bodu.

Mávanie kmitá okolo stojacej strednej hodnoty — filter ho utlmí a čo zostane, zahodí prah
kroku. Chôdza strednú hodnotu **posúva**, čo prežije oboje. Cenou je hrubšie rozlíšenie a mierne
podhodnotenie v ostrých zákrutách; oboje je malé oproti odstránenému skresleniu.

## Rozhodnutie

**Možnosť C**, s dvomi číslami namiesto jedného.

- `DistanceWalked` — vyhladené a prevzorkované. Toto je menovateľ pre drift.
  Časová konštanta filtra 0,35 s, krok prevzorkovania 0,30 m.
- `PathRawMeters` — pôvodný súčet snímok po snímku, ponechaný na porovnanie.

Obe idú do CSV (`walked_m`, `path_raw_m`) a obe sú na obrazovke: veľké číslo je prevzorkované,
pod ním drobným písmom `raw`. **Rozdiel medzi nimi je samostatný výsledok** — je to presne tá
ruka a ten šum, ktoré sa doteraz vykazovali ako chôdza. Kto chce vidieť, čo filter robí, nech
sa postaví, zamáva telefónom a pozerá, ktoré z tých dvoch čísel rastie.

Relokalizačné skoky sa spracujú **pred** filtrom. Keby sa nechal filter skok dobiehať, strávil
by tým ďalšiu sekundu a prevzorkovanie by to vyúčtovalo ako chôdzu — skok by sa odčítal z
jedného čísla a potichu pripočítal do druhého. Pri skoku sa preto o rovnaký vektor posunie
vyhladená poloha, naposledy podržaný bod aj `Origin`.

## Overenie

Filter sedí v `PathResampler` oddelene od `CameraTravel` a čas dostáva ako argument, takže sa
dá pustiť na vymyslených dráhach so **známou skutočnou dĺžkou**. Ladiť ho na chodbe a vyhlásiť
za dobrý by bolo dookola: tá chôdza tiež nemá referenciu.

`FriLens > Verify Travel Filter`:

| dráha | skutočnosť | prevzorkované | raw |
|---|---:|---:|---:|
| rovná chôdza, bez šumu, 60 fps | 20 m | 19,45 m | 20,00 m |
| rovná chôdza, šum 2 cm, 60 fps | 20 m | 19,49 m | **30,74 m** |
| rovná chôdza, šum 2 cm, 30 fps | 20 m | 19,46 m | 22,96 m |
| chôdza s 3 m relokalizáciou v polovici | 10 m | 9,44 m | — |
| státie, mávanie 2 Hz ±25 cm, 20 s | 0 m | 0,00 m | 39,78 m |
| státie, mávanie 1 Hz ±25 cm, 20 s | 0 m | 0,00 m | 20,00 m |

Dva riadky stoja za prečítanie zvlášť.

**Dvadsať metrov s dvomi centimetrami šumu vyjde ako 30,74 m.** To je nadhodnotenie o 54 %
z čistého šumu — chôdza je v oboch prípadoch tá istá. Pri 30 fps to je 22,96 m, teda 15 %.
Presne tá závislosť od hustoty vzorkovania, ktorú popisuje literatúra, len zmeraná na našom
kóde.

To isté na skutočných dátach. Filter pustený spätne na pózy uložené v CSV z behov s 0.1.3:

| beh | trvanie | appka hlásila | prevzorkované z CSV |
|---|---:|---:|---:|
| 214544 | 138 s | — | 65,40 m |
| 225741 | 191 s | 84,35 m | **65,25 m** |
| 230108 | 38 s | 19,78 m | 13,34 m |

Rekonštrukcia je hrubá — v CSV sú pózy štyrikrát za sekundu, kým na telefóne filter beží na
snímkovej frekvencii, takže hodnoty z tabuľky budú skôr mierne nízke. Smer aj rád sú však
jednoznačné: **tých 84,4 m bolo v skutočnosti okolo 65 m.**

**Státie a mávanie dáva 0,00 m prevzorkovane oproti 20–40 m raw.** To je pozorovanie z terénu
prepísané do čísla.

Podhodnotenie 0,5 m na dvadsiatich metroch je oneskorenie filtra na konci dráhy. **Nerastie so
vzdialenosťou** — je to konštantný chvost, nie chyba úmerná dĺžke.

## Čo to nerieši

**Meriame kameru, nie človeka.** Pomalý oblúk rukou cez pol metra posunie kameru rovnako ako
krok a žiadne spracovanie pózy tie dva prípady nerozlíši. Držať telefón pokojne zostáva
súčasťou metodiky, len už nie jedinou poistkou.

Prah 0,30 m znamená, že číslo skáče po tridsiatich centimetroch. Pre meranie driftu na
desiatkach metrov je to bezvýznamné.

A je hranica, za ktorou filter nepomôže. Pomalý široký oblúk rukou — 0,4 Hz, ±40 cm — prejde
ako **4,82 m za dvadsať sekúnd státia**. Kamera sa naozaj presúva rovnako ďaleko a rovnako
rýchlo ako pri chôdzi; žiadne spracovanie pózy tie dva prípady nerozlíši. Rýchle mávanie filter
zahodí celé, pomalé kývanie nie.

## Súvisiace

Konvencia z literatúry k VIO, ktorou sa riadime pri vyhodnotení: drift sa uvádza ako
**percento z prejdenej dráhy**, nie v absolútnych metroch. Presne preto na kvalite menovateľa
záleží. Rozdelenie chyby na globálnu (ATE) a lokálnu (RPE) je popísané v
[tutoriále k vyhodnocovaniu VIO od Zhanga a Scaramuzzu](https://rpg.ifi.uzh.ch/docs/IROS18_Zhang.pdf).

Meranie skokov a ich oddelenie od chôdze rieši [ADR 004](004-zariadenia-bez-arcore.md) len
okrajovo; samotný filter skokov je popísaný v CHANGELOG-u pri 0.1.4-alpha.
