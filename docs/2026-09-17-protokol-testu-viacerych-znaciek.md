# Protokol testu kalibrácie z viacerých značiek

**Verzia:** 0.3.1-alpha · **Dátum:** 2026-09-17, upravené 2026-10-02

Tento test má jednu otázku: **prestalo sa zarovnanie hýbať, keď sa človek nehýbe?** Všetko
ostatné v `0.3.0-alpha` existuje kvôli tomu — solver zo štyroch stupňov voľnosti, zahadzovanie
observácií po skoku, brána kvality aj ukazovateľ dôvery.

Odpoveď je číslo, nie dojem, a porovnáva sa s tým, čo namerala `0.2.2-alpha` v tej istej
miestnosti. Tie čísla sú v [CHANGELOG](../CHANGELOG.md) a opakujú sa nižšie pri každom teste ako
riadok „čo sa prekonáva".

> **Testuje sa `0.3.1-alpha`, nie `0.3.0-alpha`.** V 0.3.0 sa pri cieli `any` druhá značka
> do fitu nedostala (burst vyberal vždy tú istú) a cieľ `M1` / `M2` neobmedzoval fit — Test E
> by meral staré správanie a Test F nie jednu značku. Podrobnosti v [CHANGELOG](../CHANGELOG.md).

Merací režim (`OnRequest`) je zapnutý a tak to má zostať: zarovnáva sa len na `Re-anchor`,
takže behy sú porovnateľné so staršími. **Navigačný režim (`Continuous`) sa v tomto builde
prepnúť nedá** — je to serializované pole `m_Policy` na `MarkerAlignment`, bez tlačidla. Ak sa
má testovať aj on, treba druhý build.

## Pred odchodom

- Značky `M1` a `M2` musia visieť tam, kde boli zamerané. **Ak sa s nimi pohlo, test nemeria
  nič** — model si drží ich staré polohy a rozdiel voči nim je práve to, čo sa tu číta ako chyba.
- Ísť **cez deň**. Značka v šere sa číta horšie a rozptyl v burste vyletí nad prah 2 cm, takže
  sa zarovnanie odmietne a nebude čo zapisovať.
- Meracie pásmo. Test G potrebuje skutočnú vzdialenosť medzi značkami, nie tú z modelu.
- Papier a pero. Z CSV sa dodatočne nedá zistiť, čo si v tej chvíli robil.
- Telefón nabitý. IL2CPP build s kamerou a UI Toolkitom vybíja rýchlo.
- **Na telefóne je `0.3.1-alpha`.** Overiť po spustení: prvý riadok CSV aj HUD hlási verziu.
  So starou verziou nemá zmysel začínať.
- Pred každým testom aj po ňom stlačiť **`Mark`** a zapísať si jeho číslo k testu. Podľa
  časov `mark-N` sa potom blok vyreže cez `--align-window`, inak sa trinásť zarovnaní Testu E
  zmieša s Testom F.

## Čo je na obrazovke nové

| prvok | čo hovorí |
|---|---|
| riadok `Alignment` | `M1 · 12 s ago · ±0,4 cm · 2 značky · 18 m od zarovnania` |
| `2 značky` vs `1 značka` | z koľkých polôh vznikol fit. **Jedna značka znamená, že fit spadol** — buď ich toľko bolo vidieť, alebo ho zamietla brána kvality |
| chvost za dôveru | `18 m od zarovnania` alebo `1x skok mapy`. Keď je prázdny, dôvera je plná |
| tlačidlo `any / M1 / M2` | z ktorej značky smie prísť ďalšie zarovnanie. Cyklí sa klepnutím |
| riadok `Marker` | pri cieli `any` a oboch značkách v zábere sa po každom zarovnaní prepne na druhú — **tak to má byť**, Re-anchor sa striedajú |
| prekryv zošedne | dôvera klesla na nulu. **Nikdy nezmizne** — ak zmizol, je to chyba, nie stlmenie |

## Test E — opakovateľnosť zo stojaceho miesta

Toto je ten test. Postaviť sa tak, aby boli **obe značky naraz v zábere**, a nehýbať sa.

1. Klepnúť `target` na `any`.
2. Stlačiť `Re-anchor`, počkať na `aligned`.
3. Zapísať si nejaký pevný bod, kde hrana prekryvu pretína stenu alebo podlahu. Odfotiť.
4. Zopakovať `Re-anchor` **trinásťkrát za sebou**, so stojacim telefónom, do tridsiatich sekúnd.
5. Po každom odčítať z riadku `Alignment`, či hlási `2 značky`, a odfotiť tú istú hranu.

| | `0.2.2-alpha` | čaká sa v `0.3.0-alpha` |
|---|---|---|
| rozptyl polohy `AlignmentRoot` v osi X | 7,36 m | **jednotky centimetrov** |
| rozptyl kurzu | 11,1° | **desatiny stupňa** |
| posun medzi dvomi po sebe idúcimi zarovnaniami | 0,1 – 2,5 m | **pod 5 cm** |

Ak niektoré zarovnanie hlási `1 značka`, hoci boli obe v zábere, **zapíš, ktoré to bolo** —
znamená to, že ho zamietla brána kvality, a to je samostatné zistenie, nie porucha testu.

Polohy a kurzy sa nečítajú z displeja, ale z CSV: stĺpce pri udalosti `aligned on …`.

Prvé zarovnanie po prepnutí cieľa na `any` môže ešte hlásiť `1 značka` — druhá značka v zásobe
nie je, kým na ňu neprišiel burst. Od druhého Re-anchor má byť `2 značky` stále; burst sa
strieda `M1`, `M2`, `M1`… Ak sa nestrieda, je to chyba a zapíš ju.

## Test F — odporujú si ešte dve značky?

Rovnaké miesto, obe značky v zábere.

1. Klepnúť `target`, kým nesvieti `M1`. `Re-anchor`. Odfotiť hranu prekryvu.
2. Klepnúť `target` na `M2`. `Re-anchor`. Odfotiť tú istú hranu.
3. Striedať päťkrát tam a späť.

`0.2.2-alpha` dávala medzi nimi **2,04 – 2,89 m a 4,2 – 7,3°**, hoci sú obe na jednej rovnej
stene. Očakáva sa, že rozdiel klesne na to, čo je skutočná chyba zamerania — jednotky centimetrov.

Pri cieli `M1` alebo `M2` sa od `0.3.1-alpha` do fitu berie **len tá značka**, takže riadok má
hlásiť `1 značka` a **tento test meria to staré správanie**.
Jeho zmysel je referencia: o koľko je fit z dvoch značiek lepší než ktorákoľvek z nich sama.

## Test G — sedí vzdialenosť značiek v modeli?

Odmerať pásmom skutočnú vzdialenosť medzi stredmi `M1` a `M2` a zapísať ju.

Model hovorí **6,79 m**. Solver vracia `baseline` — rozdiel medzi nameranou a modelovou
vzdialenosťou — a zapisuje ho do logu pri každom fite z dvoch a viac značiek.

| | čaká sa |
|---|---|
| `baseline` z logu | rovná sa (pásmo − 6,79 m) na centimetre |
| ak nie | buď je zamerané zle, alebo ARCore hlási polohu značky posunutú — a to rozhodne až Test E |

Toto je jediné číslo v projekte, ktoré priamo meria **zhodu modelu s budovou**. Mierka sa práve
preto nefituje: keby sa fitovala, solver by tento rozdiel pohltil a už by ho nebolo vidieť.

## Test H — chytí brána zlé čítanie?

Brána zamietne fit, ktorého najväčší zvyšok presiahne **25 % rozpätia značiek** — pri 6,79 m je
to 1,70 m.

Vyprovokovať sa dá tak, že sa jedna značka pozerá **z veľmi šikmého uhla** alebo cez odlesk.
Nie je zaručené, že sa to podarí; ak sa nepodarí za päť minút, nechaj to tak a napíš, že sa
nepodarilo. To je tiež výsledok.

Keď zamietnutie nastane, na obrazovke sa objaví `1 značka` a v logu je `markers 1`,
`gate-rejected <chyba> > <prah>` a pod ním riadok `log-warning … fit z 2 značiek zamietnutý`.
`seen` ukáže, či ARCore v tej chvíli hlásil obe značky ako `Tracking`.

## Test I — stlmenie dôvery

Zarovnať sa a **odísť**. Dôvera klesá s prejdenou dráhou; na nule je po **30 m**, alebo hneď po
prvom relokalizačnom skoku.

Sledovať:

- prekryv postupne **šedne**, nemizne,
- steny a nav plochy zostávajú **priesvitné** — ak sa stanú nepriehľadne bielymi, je to tá istá
  chyba, ktorá sa v `0.3.0-alpha` opravovala, a treba to zapísať,
- chvost riadku `Alignment` hlási `N m od zarovnania` alebo `Nx skok mapy`.

## Keď niečo nesedí — čo zapísať na papier

| čo vidíš | čo zapísať | čo to v logu rozlíši |
|---|---|---|
| `1 značka`, hoci boli obe v zábere | čas, číslo pokusu | `seen`, `inputs`, `gate-rejected`, `burst` v riadku `aligned on` |
| `sampling 0/30` sa nehýbe | ktorá značka, odkiaľ, svetlo | `log-warning … dropped N samples` |
| zarovnanie sa vôbec nestane | ako dlho, aký uhol | `log-warning … rozptyl … nad prahom` |
| prekryv skočí bez Re-anchor | čas, čo si robil | stĺpce `jumps`, `jumped_m`, `tracking-lost` |
| appka spadne | čas | `log-exception …`, a doma `pull-logs.ps1 -Logcat` čo najskôr |

## Čo si priniesť späť

- CSV z telefónu (`frilens-YYYYMMDD-HHmmss.csv`),
- fotky hrany prekryvu z testov E a F, pomenované podľa pokusu,
- nameranú vzdialenosť `M1`↔`M2` z pásma,
- poznámky: každé zarovnanie, ktoré hlásilo `1 značka`, a prečo,
- čísla `Mark` na začiatku a konci každého testu.

Doma:

```powershell
powershell -ExecutionPolicy Bypass -File tools\pull-logs.ps1 -Logcat
python tools\frilens_eval.py <cesta>\frilens-RRRRMMDD-HHMMSS.csv --marker-tape <pásmo> --align-window <S1>-<S2>
```

Logcat drží len posledné hodiny, takže ho treba stiahnuť v ten istý deň. CSV na telefóne zostáva.

Výsledky idú do `docs/` ako samostatný dokument, tak ako
[výsledky baseline](2026-09-04-vysledky-baseline.md), nie do tohto súboru.
