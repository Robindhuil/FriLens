# ADR 006 — Kotvenie prekryvu a strata trackingu

**Verzia:** 0.1.5-alpha · **Dátum:** 2026-09-04 · **Stav:** prijaté

## Kontext

Otázka z praxe: čo sa stane, keď zakryjem kameru, prejdem dva metre a odkryjem?

Odpoveď rozhoduje o tom, či je zvyšok merania použiteľný, tak stojí za to napísať ju presne.

Keď kamera nevidí, ARCore nemá čo sledovať a tracking padne. Póza **zamrzne** — virtuálna
kamera zostane tam, kde bola, kým sa človek pohne. Po odkrytí sú dve možnosti:

**Relokalizuje sa.** ARCore si drží vnútornú mapu bodov a rozpozná, kde je. Póza skočí na
skutočnú polohu a všetko sa opraví. Na obrazovke je to skok — nepríjemný, ale správny.

**Nerelokalizuje sa.** Nemá o čo sa oprieť a pokračuje zo zamrznutej pózy. Celý prekryv je
posunutý o toľko, koľko človek prešiel naslepo.

Rozhodujúca premenná **nie je vzdialenosť, ale prekryv s mapou**. Dvadsať metrov po zmapovanej
chodbe môže dopadnúť lepšie než dva metre za roh do miestnosti, ktorú kamera nikdy nevidela.
Je to podmienka *image-to-map* relokalizácie a v literatúre to je **kidnapped robot problem** —
v benchmarku `freiburg2_360_kidnap` sa kamera uprostred behu doslova zakryje a póza sa obnoví
až návratom na známe miesto. Čistá odometria sa zotaviť nedokáže, lebo nemá pamäť; SLAM áno.

## Prvý problém: ARCore sa zotaví, my nie

ARCore pri relokalizácii posúva **anchory** tak, aby zostali na tom istom fyzickom mieste.
Obyčajné súradnice v `Transform` neposunie — o tých nevie.

`AlignmentRoot` aj `Origin` boli obyčajné súradnice. Prekryv teda zostal zle práve vtedy, keď
sa tracker trafil. Merali sme svoju vlastnú chybu.

Pri `Origin` sa to dalo vidieť aj v číslach: bez kotvy som ho musel pri každom skoku ručne
posunúť, a v behu `001103` sa takých posunov nazbieralo **43 metrov naprieč 69 skokmi**.
Riadok `From marker` na konci hlásil 1,94 m a neznamenal nič.

## Druhý problém: zlyhanie je tiché

Toto je horšie než samotná chyba.

**Úspešnú relokalizáciu zmerať vieme** — je to skok, filter ju chytí, započíta do `jumps`
a nezaráta do prejdenej vzdialenosti.

**Neúspešnú nezmeriame vôbec.** Nenastane žiaden skok, póza plynulo pokračuje z nesprávneho
miesta a v CSV nie je ani stopa. Na obrazovke to vyzerá presne ako drift — teda ako to, čo
má test merať. Z logu sa nedá odlíšiť „model je nepresný" od „ARCore sa nezotavil".

## Rozhodnutie

**1. Prekryv aj `Origin` držať na `ARAnchor`.** `AnchoredRoot` položí koreň na pózu a hneď ho
nechá zakotviť; `CameraTravel` zakotví `Origin` pri každom reštarte. Vytvorenie kotvy je
asynchrónne, takže sa póza zapíše okamžite a kotva sa pripojí o chvíľu — viditeľné čakanie na
prekryv by bolo horšie než krátke okno bez korekcie. Keď kotvenie zlyhá, appka beží ďalej
s pôvodným správaním a povie to.

**2. Priznať, že po strate trackingu sa nemeria.** `TrackingContinuity` počíta straty a čas
naslepo od posledného zarovnania. Kým nejaká strata visí, riadok `Alignment` hlási
`unverified · N losses M s` a v CSV je `verified = 0`. Vyčistí to jedine zarovnanie na
vytlačenej značke — tá jediná nezávisí od mapy ARCore.

To zlyhanie tým nedetegujeme. Označíme okno, v ktorom mohlo nastať, čo je maximum, ktoré sa
z pózy dá poctivo povedať.

## Dôsledok pre metodiku

Značka nie je len začiatok merania, je to **liek na stratu trackingu**. Zakry kameru, choď kam
chceš, vráť sa k značke, stlač re-anchor — a si naviazaný na budovu, nie na to, čo si ARCore
myslí.

Z toho plynie, že **značiek má byť viac, rozmiestnených po trase**. Najhoršia možná chyba je
potom ohraničená úsekom medzi dvomi značkami, nie dĺžkou celého behu. Fáza 3a
implementačného plánu sa tým mení z „vytlačiť značku" na „vytlačiť značky a rozmiestniť ich".

## Čo ešte treba zmerať

Protokol, ktorý na túto otázku odpovie číslom:

> zakry kameru → prejdi *d* metrov → odkry → odčítaj posun prekryvu voči stene

pre *d* = 0, 1, 2, 5 m, zvlášť s pohľadom na zmapovanú stenu a zvlášť za roh. Hypotéza je, že
chyba **nekoreluje s *d*, ale s tým, či je cieľ v mape**. Ak to vyjde, je to samostatné
zistenie a nie je závislé na tom, či už existuje zameraná značka.

## Nezvolené

**Cloud Anchors / perzistentné kotvy.** AR Foundation 6 ich vie (`TrySaveAnchorAsync`,
`TryLoadAnchorAsync`) a vyriešili by aj obnovu medzi behmi. Vyžadujú sieť alebo úložisko
navyše a riešia inú otázku než tú, ktorú test kladie. Stojí za zváženie, keď bude značka
zameraná a bude sa merať opakovateľnosť medzi dňami.

**Odhad polohy z IMU počas zakrytia.** Dvojitá integrácia zrýchlenia rastie chybou kvadraticky;
za tie dve sekundy by odhad nebol lepší než to, čo ARCore spraví sám.

## Súvisiace

- [ADR 005](005-ako-merat-prejdenu-vzdialenost.md) — prečo `Origin` a prejdená dráha vôbec
  potrebujú zvláštne zaobchádzanie pri skokoch.
- [ARCore — Working with Anchors](https://developers.google.com/ar/develop/anchors)
- [Pozor: v ARCore 1.38 bolo hlásené, že sa anchory po strate trackingu nevrátia](https://github.com/google-ar/arcore-android-sdk/issues/1601)
  — treba overiť na zariadení, nie predpokladať.
