# ADR 010 — Kurz z polôh značiek, sklon z gravitácie

**Verzia:** 0.2.2-alpha · **Dátum:** 2026-09-09 · **Stav:** prijaté

## Kontext

Zarovnanie prekryvu bralo od začiatku **celú pózu** z jednej sledovanej značky: polohu aj
natočenie. Znelo to samozrejme — značka je jediné, čo je nezávislé od ARCore mapy, tak nech
teda povie všetko.

Meranie na fakulte 2026-09-09 ukázalo, že tá samozrejmosť neplatí. Sledovaný obrázok dáva
dve veličiny s úplne inou kvalitou:

| veličina | nameraná presnosť |
|---|---|
| poloha značky | **0,11 – 1,20 cm** |
| natočenie značky | **jednotky stupňov** |

Trinásť zarovnaní na tú istú značku zo stojaceho miesta za tridsať sekúnd rozhádzalo prekryv
o **7,36 m** v jednej osi a kurz o **11,1°**. Dve značky na jednej rovnej stene — teda s nutne
rovnobežnými normálami — hlásili normály líšiace sa o **8,0°**, z toho **8,3° vo vodorovnej
rovine**. Ani jedno nie je fyzicky možné, takže to nie sú vlastnosti steny, ale odhadu.

Nie je to náhoda ani chyba tohto projektu. Naklonenie roviny mimo obrazovú rovinu je **zle
podmienený smer** odhadu pózy rovinného terča, a prejavuje sa ako **odchýlka, nie šum** —
takže priemerovanie cez burst, ktoré poctivo odstraňuje šum polohy, s ním nespraví nič.
Nestabilita natočenia sledovaného obrázka je hlásená priamo v ARCore SDK
([#792](https://github.com/google-ar/arcore-android-sdk/issues/792),
[#1662](https://github.com/google-ar/arcore-android-sdk/issues/1662)).

Model má počiatok 22,7 m od kotvy, takže chyba kurzu rastie so vzdialenosťou od značky:
5° je 0,87 m na desiatich metroch.

## Rozhodnutie

**Natočenie značky sa do zarovnania nepoužije.** Berie sa z nej len poloha, a orientácia sa
skladá z dvoch iných zdrojov:

- **sklon** — z gravitácie. Session space ARCore je gravitačne zarovnaný z IMU, pitch a roll
  sú presné na zlomok stupňa a nedriftujú. Model je postavený vzpriamene, takže zvislica je
  spoločná a priamo použiteľná.
- **kurz** — zo **spojnice polôh dvoch a viacerých značiek**. Polohy sú presné na desatiny
  centimetra, takže smer medzi značkami vzdialenými 6,79 m vyjde na ~0,1°.

Transformácia je tuhá so štyrmi stupňami voľnosti: posun `XYZ` a kurz. **Mierka sa nefituje.**

Jedna viditeľná značka je výnimka: kurz sa z jednej polohy určiť nedá, tak sa v tom prípade
vezme z jej natočenia ako doteraz. Degraduje to presne na staré správanie, takže počet
značiek je parameter, nie predpoklad.

## Prečo sa mierka nefituje

Keby sa fitovala, model by sa natiahol tak, aby rozdiel medzi nameranou a modelovou
vzdialenosťou značiek pohltil. Ten rozdiel je ale **chyba modelu voči budove** — presne to,
čo projekt meria. Fit mierky by výsledok merania potichu zjedol a nahradil dojmom, že všetko
sedí.

Zostáva preto ako **zvyšok sústavy** a hlási sa ako číslo. Pri dvoch značkách je to jediný
zvyšok a je to chyba modelu na 6,79 m steny.

## Dôsledok

**Prekryv prestáva závisieť na tom, ako sa človek pred značku postaví.** Stanovisko, výška
a uhol pohľadu ovplyvňovali natočenie, ktoré sa zahadzuje; na polohu značky nemajú vplyv.
Obsluha sa tým mení z „postav sa presne pred značku" na „prejdi pohľadom po miestnosti".

**Dve značky na jednej stene stačia**, kým je zvislica k dispozícii. Ležia na vodorovnej
priamke a otočenie okolo nej ostáva voľné — ale to je práve sklon, ktorý rieši gravitácia.
Tretia značka mimo priamky nepridá stupeň voľnosti, pridá **zvyšky na každej značke**, teda
meranie modelu na viacerých miestach naraz.

**Rozptyl natočenia prestáva byť dôvod odmietnuť burst.** Loguje sa ďalej, ale ako meranie
chyby značky, nie ako kritérium kvality. Bránou kvality sa stáva rozptyl polohy.

## Nezvolené

**Vážený fit podľa neistoty jednotlivých značiek.** Namerané rozptyly polôh sú 0,11 – 1,20 cm,
teda porovnateľné; váhy by pridali parameter bez merateľného zisku. Vráti sa to sem, ak
pribudne značka výrazne inej veľkosti alebo pozorovaná z výrazne inej vzdialenosti.

**Priemerovanie natočení viacerých značiek.** Priemer ôsmich stupňov rozporu je stále chybný
o jednotky stupňov, len sa už nedá povedať o koľko. Poloha je o dva rády lepší zdroj a je
k dispozícii z tých istých pozorovaní.

**Sledovanie značky naživo namiesto burstu.** Sľubovalo by to stabilitu, ale schovalo by to
odchýlku do plynulého pohybu prekryvu, ktorý sa nedá odčítať. Burst zostáva; mení sa len to,
čo sa z neho použije.

## Súvisiace

- [Návrh kalibrácie z viacerých značiek](../2026-09-09-navrh-kalibracie-viac-znaciek.md) —
  ako sa to implementuje, komponenty, brána kvality a overenie.
- [ADR 006 — Kotvenie a strata trackingu](006-kotvenie-a-strata-trackingu.md) — prečo sa
  observácie musia zahodiť pri relokalizačnom skoku.
- [Plán zamerania značky](../2026-09-09-plan-zamerania-znacky.md) — ako sa póza značky
  zameriava; sekcia o orientácii je zastaraná a opravuje sa samostatne.
