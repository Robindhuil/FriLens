# Návrh: kalibrácia z viacerých značiek

**Verzia:** 0.2.2-alpha · **Dátum:** 2026-09-09 · **Stav:** návrh pred implementáciou

Zarovnanie z jednej značky je nepoužiteľné a je to odmerané, nie odhadnuté. Tento dokument
popisuje, čím sa nahradí.

Rozhodnutie, o ktoré sa celý návrh opiera — prečo sa natočenie značky zahadzuje — je
v [ADR 010](decisions/010-kurz-z-poloh-znaciek-sklon-z-gravitacie.md).

## Čo sa namerlo

Behy `frilens-20260909-134644` a `-135106` na `0.2.2-alpha`, obe s pózami značiek v logu.

**Zarovnanie je nestabilné aj bez toho, aby sa človek pohol.** Trinásť `Re-anchor` za sebou
na M1 zo stojaceho miesta, za tridsať sekúnd:

| veličina | rozptyl |
|---|---|
| poloha `AlignmentRoot` v osi X | **7,36 m** |
| kurz | **11,1°** |
| posun medzi dvomi po sebe idúcimi zarovnaniami | 0,1 – 2,5 m |

Nie je to drift. Je to šum jedného zarovnania, a je to dôvod, prečo sa prekryv „pokazil" aj
bez prepnutia značky.

**Značky si odporujú.** Rozdiel medzi zarovnaním z M1 a z M2: 2,04 m a 4,19° v prvom behu,
2,89 m a 7,29° v druhom. Normály oboch značiek — ležiacich na jednej rovnej stene, teda
nutne rovnobežné — sa v skoršom behu líšili o **8,0°**, a z toho **8,3° bolo vo vodorovnej
rovine**, čiže v kurze.

**Chôdza rozbije mapu pod prekryvom.** Po 45,9 m chôdze po RC prišla relokalizácia a posunula
mapu naraz o **4,818 m**. Poloha tej istej nepohnutej značky sa v session space rozišla
až o **5,02 m**.

**Naklonenie je vyriešené.** Zrovnanie podľa gravitácie z `0.2.2-alpha` funguje; `levelled`
hlási 0,5 – 16°, čiže mal čo opravovať.

### Prečo to tak je

Poloha sledovaného obrázka je presná na **0,11 – 1,20 cm**. Jeho natočenie na **jednotky
stupňov**. Nie je to náhoda: naklonenie roviny mimo obrazovú rovinu je zle podmienený smer
odhadu pózy rovinného terča a prejavuje sa ako **odchýlka, nie šum**, takže priemerovaním
cez burst nezmizne.

Model má počiatok **22,7 m od kotvy** (`−21,902 · 2,194 · 5,328`). Kotva sa pri zarovnaní
pripne presne na značku a model sa okolo nej otočí, takže chyba kurzu rastie so vzdialenosťou
od značky: 5° je 0,87 m na desiatich metroch.

Literatúra to potvrdzuje ako známy jav, nie ako chybu tohto projektu — zdroje na konci.

## Čo sa stavia

**Cieľ:** prekryv, ktorý sa nehýbe, keď sa nehýbeš, a ktorý povie, kedy mu už netreba veriť.

**Nie je cieľom:** presnosť mimo break roomu. Značky budú len v ňom, takže po odchode
z miestnosti sa drift nemá čím opraviť. Appka to má priznať, nie skrývať.

### Rozdelenie

| jednotka | zodpovednosť | závisí od |
|---|---|---|
| `AlignmentSolver` *(nový, statický)* | z dvojíc „kotva v modeli ↔ nameraná poloha" a zvislice spočíta pózu modelu | nič |
| `MarkerObservations` *(nový)* | drží poslednú prijatú observáciu každej značky a zahadzuje neplatné | `TrackingContinuity`, `CameraTravel` |
| `MarkerAlignment` *(zmenšuje sa)* | sleduje obrázky, plní observácie, rozhoduje **kedy** riešiť, aplikuje | oba vyššie, `AnchoredRoot` |
| `AlignmentConfidence` *(nový)* | z dráhy, skokov, času a zvyšku fitu spraví jedno číslo dôvery | `CameraTravel` |

`MarkerAlignment.cs` má dnes 546 riadkov a robí výber obrázka, zber vzoriek, priemerovanie,
riešenie, aplikovanie aj výber cieľa. Pridať doň ešte zber cez viacero značiek, fit, bránu
a politiku by z neho spravilo neudržateľný kus. Čistá statická matematika s editorovým
overovačom je pritom vzor, ktorý projekt už má dvakrát — `SolveRootPose` s
`Verify Alignment Math` a `PathResampler` s `Verify Travel Filter`.

## `AlignmentSolver`

Hľadá tuhú transformáciu so **štyrmi stupňami voľnosti**: posun `XYZ` a kurz okolo zvislice.
Sklon sa nefituje, berie sa z gravitácie.

Vstup je `N` dvojíc `(aᵢ, oᵢ)` — poloha kotvy v súradniciach modelu a nameraná poloha tej
istej značky v session space. Minimalizuje sa `Σ |R·aᵢ + t − oᵢ|²` cez kurz `R` a posun `t`.

Riešenie je uzavreté, cez ťažiská:

1. `ā`, `ō` — ťažiská oboch množín.
2. Kurz z vodorovných zložiek vycentrovaných bodov; klasický 2D Procrustes v rovine `XZ`.
3. `t = ō − R·ā`. Zvislá zložka z toho vypadne sama, lebo kurz na `Y` nesiaha.

> **Znamienková konvencia kurzu sa neodvodzuje v hlave.** Unity je ľavotočivé a otočenie
> okolo `Y` má opačné znamienko, než na aké je človek zvyknutý z pravotočivých vzorcov.
> Overí ju číselne `Verify Alignment Solver` na syntetických dátach so známou odpoveďou.

### Koľko značiek, toľko presnosti

| počet | čo dá | zvyšok |
|---|---|---|
| **1** | kurz sa z jednej polohy určiť nedá — vezme sa z natočenia tej značky, ako dnes | žiadny |
| **2** | kurz zo spojnice, ~0,1° namiesto 5° | **rozdiel nameranej a modelovej dĺžky spojnice** |
| **3+ mimo priamky** | preurčená sústava | zvyšok na každej značke zvlášť |

Jedna značka teda degraduje presne na dnešné správanie. Nič sa nezhorší, keď je vidieť len
jednu, a počet značiek je **parameter, nie predpoklad**.

Dve značky na jednej stene v rovnakej výške ležia na vodorovnej priamke. Ich spojnica určí
kurz aj posun, ale **otočenie okolo tej priamky ostane voľné** — a to je práve sklon, ktorý
už rieši gravitácia. Preto dvojica na jednej stene stačí, kým je zvislica k dispozícii;
tretia značka mimo priamky robí sústavu preurčenou a dá zvyšky, nie nový stupeň voľnosti.

### Mierka sa zámerne nefituje

Keby sa fitovala, model by sa natiahol tak, aby chybu merania pohltil — a tá chyba je to,
čo sa má merať. Transformácia je preto tuhá a **rozdiel dĺžok sa hlási ako výsledok**, nie
odstraňuje. Pri dvoch značkách je to jediný zvyšok sústavy a je to chyba modelu na 6,79 m
steny ako jediné číslo.

### Váženie sa nezavádza

Rozptyly polôh vyšli 0,11 – 1,20 cm, teda porovnateľné. Vážený najmenší štvorec by pridal
parameter navyše bez merateľného zisku. Ak sa niekedy pridá značka výrazne inej veľkosti
alebo pozorovaná z výrazne inej vzdialenosti, vráti sa to sem ako doplnok.

## `MarkerObservations`

Observácia je `(meno značky, poloha v session space, čas, rozptyl polohy, id úseku
trackingu)`. Na značku sa drží **posledná**; staršia sa prepíše.

**Prijme sa**, keď burst dobehol celý v stave `Tracking` a rozptyl polohy je pod prahom
(východisko 2 cm, nastaviteľné).

**Zahodí sa** pri ktoromkoľvek z:

- **relokalizačný skok** — `CameraTravel.RelocalisationJumps` sa zvýši,
- **strata trackingu** — `TrackingContinuity.Lost`,
- **vek** nad prah (východisko 60 s).

Skok je z toho najdôležitejší. Observácie spred a spoza neho sú v **dvoch rôznych mapách**
a fit cez ne dá pózu, ktorá nepatrí ani jednej. Beh `134644` mal skok 4,818 m — presne táto
pasca, a bez tejto brány by sa do fitu dostala.

Rozptyl natočenia (`SampleSpreadDegrees`) prestáva byť kritérium prijatia. Natočenie sa
zahadzuje, takže jeho rozptyl už nie je dôvod burst odmietnuť; loguje sa ďalej ako meranie.

## Dva režimy

Tá istá matematika, iná politika, kedy sa spúšťa.

**Merací.** Rieši sa výhradne na `Re-anchor`. Nikdy sám. Dnešné správanie s lepším solverom,
aby sa behy dali porovnávať so staršími.

**Navigačný.** Rieši sa automaticky pri každej novej prijatej observácii. Prekryv sa tým
v miestnosti drží sám a po návrate do nej sa opraví bez stlačenia čohokoľvek.

**Veľkosť každej automatickej opravy sa zapíše do logu.** Drift sa tým nestratí — prestane
sa čítať ako útek prekryvu a začne ako veľkosť fixu, čo je presnejšie, lebo útek sa dá
odčítať len okom, kým fix je číslo.

## `AlignmentConfidence`

Sleduje, čo sa od posledného zarovnania dialo: prejdená dráha, počet skokov, čas a zvyšok
posledného fitu. Z toho spraví jedno číslo, ktoré HUD ukazuje a podľa ktorého prekryv po
prekročení prahu **zošedne**.

Pri zarovnaní z jednej značky **zvyšok neexistuje** — sústava nie je preurčená. Vtedy sa
dôvera počíta len z dráhy, skokov a času, a HUD to má povedať otvorene: „1 značka, zvyšok
neznámy" je poctivejšie než dosadiť nulu, ktorá by vyzerala ako dokonalý fit.

Prekryv nezmizne nikdy. Zmiznutie by v teréne vyzeralo ako pád aplikácie a mlčky by zobralo
možnosť pozrieť sa, ako veľmi je vedľa — čo je pri meraní tá zaujímavá informácia.

**Vyžaduje to opraviť wiring v scéne.** `DiagnosticsHud` má dnes prázdne `m_Overlays`
a `m_Ceiling = null`, takže tlačidlá `Hide overlay` a `strop` menia len svoj vlastný vzhľad
a žiadny renderer neprepnú. Bez toho nie je čím zošednutie spraviť. Je to na boarde ako
samostatná karta a ide sem, lebo to táto práca potrebuje.

## Ako sa to obsluhuje

Nestojí sa pred jednou značkou. **Prejde sa pohľadom po miestnosti** a aplikácia pozbiera,
čo uvidí; HUD hlási `2/3 značky`.

Tým padá viazanosť na to, ako sa človek pred značku postaví a v akej výške ju naskenuje.
Stanovisko a výška ovplyvňovali **natočenie**, ktoré sa zahadzuje — na **polohu** značky
nemajú vplyv.

## Overenie

`AlignmentSolver` je čistá funkcia, takže sa dá overiť bez telefónu. Menu položka
`FriLens > Verify Alignment Solver`, rovnako ako dve existujúce:

- syntetické sady so známou odpoveďou — transformácia sa zadá, body sa ňou prehnú a solver
  ju má nájsť späť,
- **degenerované prípady**: jedna značka, dve značky, tri v priamke,
- **znamienko kurzu** v Unity, číselne,
- **replay z existujúcich logov** — `img pos` je v nich od `0.2.1-alpha`, takže sa dá
  spočítať, čo by fit dal, a porovnať s tým, čo aplikácia vtedy naozaj spravila.

Ten replay je najcennejší: sú to skutočné dáta z fakulty, nie vymyslené, a odpoveď je známa
z toho, čo bolo vidieť na obrazovke.

## Čo tento návrh nerieši

- **Chôdza mimo break roomu.** Značky budú len v ňom. Po odchode sa drift nemá čím opraviť
  a `AlignmentConfidence` to má priznať zošednutím.

  **Riešením ale nie sú značky po celej budove.** Cieľ je opačný: **jedno miesto, kde sa
  človek zosynchronizuje s fakultou, a potom chodí, kade chce.** Drift sa má potláčať tak, aby
  sa nestaval, nie prelepovať fixmi každých pár desiatok metrov. Cesta k tomu je rozhodnutá
  v [ADR 007](decisions/007-vyuzitie-modelu-na-lokalizaciu.md): väzby z modelu v poradí
  **kurz na smery chodieb → väzba na výšku podlahy → map matching časticovým filtrom**, každá
  ako samostatný, defaultne vypnutý režim, aby sa beh s ňou a bez nej dal porovnať.

  Zopár značiek v odstupoch po chodbách je možný **doplnok** neskôr, keď bude zmerané, koľko
  z driftu tie väzby naozaj zoberú. Nie je to plán a nie je to náhrada za ne.
- **Cloud Anchors ani Geospatial API.** Viazalo by to prácu na cudziu službu a na pokrytie,
  ktoré vo vnútri fakulty nie je overené.
- **Automatické zameranie novej značky.** Póza každej novej sa naďalej zadáva ručne podľa
  [plánu zamerania](2026-09-09-plan-zamerania-znacky.md).

- **Dav ľudí.** Na dni otvorených dverí bude v miestnosti plno a to zasiahne oboje naraz:
  značka vo výške 1,41 m je **presne v úrovni hláv**, takže ju telá zakryjú, a pohybujúci sa
  ľudia sú zlé body pre VIO, takže sa zhorší aj tracking medzi zarovnaniami. Tento návrh s tým
  nepočíta a merania z prázdnej miestnosti sa na plnú neprenášajú. Zaznamenané na boarde.

## Zdroje

- [google-ar/arcore-android-sdk#792](https://github.com/google-ar/arcore-android-sdk/issues/792)
  a [#1662](https://github.com/google-ar/arcore-android-sdk/issues/1662) — nestabilita
  natočenia sledovaného obrázka hlásená priamo v SDK; poloha drží, natočenie skáče.
- [BIMxAR: BIM-Empowered Augmented Reality](https://arxiv.org/pdf/2204.03207) — chyba
  registrácie rastie so vzdialenosťou od značky a od kamery; menšia značka znamená väčšiu
  chybu; typický drift v budove ±0,9 – 2,0 m.
- [Investigation of ArUco Marker Placement for Planar Indoor Localization](https://arxiv.org/html/2509.17345v1)
  — viac značiek naraz výrazne zlepší presnosť a nad päť sa zisk vytráca; presnosť prudko
  klesá so vzdialenosťou (16× horšie z 3 m než z 1,3 m).
- [IndoorAtlas — AR a vnútorná lokalizácia](https://www.indooratlas.com/blog/a-unique-blend-of-augmented-reality-ar-and-indoor-positioning/)
  — inerciálne systémy potrebujú pravidelné opravné fixy, inak sa rozídu; značka dá presný
  fix, ale po dostatočnej vzdialenosti treba ďalší.
