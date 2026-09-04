# ADR 007 — Využitie znalosti modelu na lokalizáciu

**Verzia:** 0.1.5-alpha · **Dátum:** 2026-09-04 · **Stav:** prijaté (poradie), otvorené (rozsah)

## Kontext

Máme 3D model budovy. Otázka znie, či sa dá použiť nielen na vykreslenie prekryvu, ale aj na
určovanie polohy telefónu.

Odpoveď je áno a mení podstatu úlohy. Doteraz robíme **SLAM**: postav si mapu a sleduj sa
v nej. Chyba nemá o čo sa oprieť, takže rastie s prejdenou vzdialenosťou. So známym modelom je
to **lokalizácia voči existujúcej mape** — chyba je zhora ohraničená presnosťou modelu, nie
dĺžkou chôdze. Drift prestáva byť neohraničený.

## Prečo je kamera nutná

Otázka, či sa poloha nedá určiť aj bez kamery, je legitímna a odpoveď treba mať zapísanú.

Akcelerometer meria zrýchlenie, gyroskop uhlovú rýchlosť. Poloha je **dvojitý integrál**
zrýchlenia, takže konštantná chyba v odhade biasu rastie **kvadraticky**. MEMS senzory
v telefóne majú bias taký, že samotná inerciálna navigácia je po pár sekundách mimo o metre.

Kamera dodáva to, čo IMU nemá: priame pozorovanie statických bodov sveta. Chyba sa tým
neintegruje, ale ohraničí. Preto visual-**inertial** odometria — obe polovice sú nutné.

| metóda | presnosť | prečo nestačí sama |
|---|---|---|
| GNSS | 3–10 m vonku | v budove nefunguje, orientáciu nedá |
| IMU sama | metre za sekundy | kvadratický rast chyby |
| krokomer + heading | ~5 % dráhy | orientácia driftuje, žiadne 6DoF |
| magnetometer | jednotky stupňov | v budove s oceľou nepoužiteľný ako kompas |
| WiFi RTT (802.11mc) | 1–2 m | AP s podporou, orientáciu nedá |
| BLE majáky | 1–5 m | hardvér do budovy |
| ultrazvuk / akustika | cm | reproduktory v budove a ticho na chodbe |
| UWB | 10–30 cm | hardvér na oboch stranách, Redmi ho nemá |

Nič z toho nedá **orientáciu**, a bez nej sa prekryv nedá vykresliť ani teoreticky. Všetko okrem
UWB je navyše rádovo hrubšie než hrúbka steny, ktorú máme merať.

Dve z toho stoja za pozornosť ako **doplnok**, nie náhrada:

**Barometer.** Rozlíšenie podlažia je inak prekvapivo ťažké a barometer to spraví takmer
zadarmo. Máme viacpodlažný model (`ra0`–`ra3`), takže to bude treba.

**Magnetické fingerprinty.** Oceľ v konštrukcii vytvára stabilné lokálne anomálie poľa, ktoré
fungujú ako odtlačok miesta. Nepotrebujú žiadnu infraštruktúru, len jeden zameriavací prechod.
Sú hrubé a orientáciu nedajú, ale ako **poistka proti tichému zlyhaniu relokalizácie**
([ADR 006](006-kotvenie-a-strata-trackingu.md)) by fungovali: povedia „si na zlom mieste", aj
keď o tom ARCore nevie.

## Hlavné obmedzenie: kruhovosť

FriLens meria, **ako presne model sedí na realite**. Ak sa pomocou modelu začne opravovať póza,
meria sa model modelom. Chyba sa schová do korekcie a vyjde, že všetko sedí výborne.

To je zásadné a určuje to celé poradie práce nižšie.

Nie je to však dôvod korekcie nerobiť. Je to dôvod **oddeliť ich do vlastného režimu** a merať
obe vetvy zvlášť. Tým sa z práce stane lepšia otázka než pôvodná: nie „aký je drift", ale
**„o koľko ho vie znalosť modelu potlačiť"**.

## Zvažované možnosti

Zoradené podľa pomeru prínos ku práci.

**A. Zarovnanie kurzu na smery chodieb.** Budova je Manhattan world — chodby na seba kolmé.
Najhoršia zložka VIO chyby je drift kurzu, lebo sa premieta do polohy úmerne prejdenej dráhe:
jeden stupeň na 50 m je skoro meter bokom. Keď je odhadovaný kurz pár stupňov od najbližšieho
smeru chodby, je to skoro isto drift. Desiatky riadkov kódu.

**B. Väzba na výšku podlahy.** Telefón je vždy 1,2–1,7 m nad podlahou, ktorej výšku poznáme pre
každé miesto. Zvislý drift sa dá priamo zrezať. Funguje už s tým, čo máme.

**C. Map matching časticovým filtrom.** Človek neprejde stenou. Držať stovky hypotéz o polohe,
posúvať ich odometriou a zabíjať tie, ktoré by prešli stenou. Vo vetvenej sieti chodieb sa
neistota zbalí rýchlo.

Referencia je [Woodman & Harle (2008), *Pedestrian localisation for indoor
environments*](https://www.semanticscholar.org/paper/Pedestrian-localisation-for-indoor-environments-Woodman-Harle/437739f2b3e2bffbbc3b59a09c2b25e952fbf443)
— IMU na nohe, model budovy a časticový filter dali **0,55 m pri 90 % spoľahlivosti** pri
chôdzi vpred, aj s riešením viacerých podlaží, schodísk a symetrie prostredia. Naše východisko
je lepšie než ich, lebo namiesto IMU na nohe máme VIO.

**D. Roviny z ARCore proti stenám modelu.** ARCore deteguje roviny; zvislé sú steny. Nasadiť
usporiadanie detegovaných rovín na steny v modeli dá pózu. Zaujímavé preto, že by to bolo
prezarovnanie **kdekoľvek v budove**, nie len pri vytlačenej značke. `ARPlaneManager` sme
vypli vo fáze 2, dá sa vrátiť.

**E. Depth API a ICP.** [ARCore Depth API](https://developers.google.com/ar/develop/depth)
počíta hĺbku **z pohybu**, bez ToF senzoru; podľa Googlu ju podporuje vyše 87 % aktívnych
zariadení a presná je od pol metra po zhruba päť metrov. Z hĺbky vznikne mračno bodov, ktoré sa
dá ICP-čkom nasadiť na mesh.

Toto je jediná možnosť, ktorá je **obojsmerná**: meria polohu aj nepresnosť modelu naraz, čo je
presne to, na čo sa práca pýta. Zároveň je najprácnejšia.

**F. Hotové riešenie.** [Vuforia Area Targets](https://developer.vuforia.com/library/vuforia-engine/environments/area-targets/area-targets/)
robí sledovanie polohy v celom priestore z 3D skenu. Chce sken z Matterportu alebo LiDARu, nie
náš navmesh. Ako referencia na porovnanie dobré; ako jadro práce slabé, lebo by meralo Vuforiu,
nie nás.

## Rozhodnutie

**1. Baseline má prednosť.** Najprv sa odmeria drift **bez akejkoľvek pomoci modelu**
([protokol](../2026-09-04-protokol-baseline-testu.md)). Bez toho čísla nemá porovnanie o čo
oprieť a všetko ostatné je neobhájiteľné.

**2. Každá korekcia je samostatný, defaultne vypnutý režim.** Beh s korekciou a beh bez nej sa
merajú zvlášť a porovnávajú sa. Nikdy nie jeden beh, v ktorom je zapnuté všetko.

**3. Poradie, keď na to príde:** A → B → C. Prvé dve sú takmer zadarmo a útočia na dominantné
zložky chyby. D a E sú samostatné kapitoly, nie inkrementy.

**4. F sa nepoužije ako jadro**, prípadne len ako porovnávacia referencia.

## Čo to odomyká a čo na to treba

A, C, D a E potrebujú **steny**. V modeli nie sú — je v ňom navmesh a exteriérový terén
([analýza](../2026-09-04-analyza-geometrie-a-stien.md)).

Modelovať ich však netreba: **hraničné hrany navmeshu sú steny**, a dvere v nich zostanú
otvorené samy. Podrobne v tej istej analýze. Z jedného extraktora tým vypadnú zvislé roviny,
bariéry pre časticový filter aj dominantné smery chodieb.

## Súvisiace

- [ADR 003](003-poza-znacky-z-nav-polygonov.md) — prečo model budovy netreba a čo z navmeshu
  vyčítať ide.
- [ADR 006](006-kotvenie-a-strata-trackingu.md) — strata trackingu, kotvenie a tiché zlyhanie
  relokalizácie, ktoré by časť týchto možností riešila.
