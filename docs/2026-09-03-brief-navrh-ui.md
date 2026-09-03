# Brief pre návrh UI

**Verzia:** 0.1.1-alpha · **Dátum:** 2026-09-03 · **Stav:** zadanie odovzdané, návrh ešte nie je

Zadanie pre návrhára HUD-u. Je písané tak, aby stálo samo o sebe — kto ho číta, nemá tento
repozitár pred sebou. Opisuje stav UI k 0.1.1-alpha, teda to, čo je v
[`DiagnosticsHud.uxml`](../Assets/_Game/UI/DiagnosticsHud.uxml),
[`DiagnosticsHud.uss`](../Assets/_Game/UI/DiagnosticsHud.uss) a
[`DiagnosticsHud.cs`](../Assets/_Game/Scripts/Runtime/DiagnosticsHud.cs).

---

## Čo je to za aplikáciu

Nie je to spotrebiteľská appka. Je to **merací prístroj** — jednoúčelová testovacia
aplikácia. Odpovedá na jedinú otázku: *ako presne sa navmesh (navigačná plocha fakulty)
premietne cez AR do skutočnej budovy a ako rýchlo sa to rozchádza, keď sa človek prejde.*

Používateľ je jeden človek — autor. Chodí s telefónom po chodbách fakulty, na kontrolných
bodoch tlačí tlačidlo a výsledok potom číta z CSV. Nie je to pre verejnosť a nikdy nebude.

Z toho plynie hierarchia obrazovky: **obsah je kamera a prekryv navigačnej plochy.** HUD je
nástroj nad ním. Nesmie ho zakrývať viac, než nutne musí.

## Podmienky, v ktorých sa to číta

Toto určuje takmer všetko:

- Na chodbe, telefón v natiahnutej ruke, **za chôdze**.
- Často proti presvetlenému oknu na konci chodby. Preto **nepriehľadné plochy pod textom**,
  nie holé labely nad obrazom z kamery.
- Tlačidlá sa musia trafiť **bez pozerania** — oči sú na budove, nie na displeji.
- Displej je zamknutý **na výšku**.
- Testovacie zariadenie je lacný telefón (Redmi 14C). Nespoliehať sa na jemné odtiene, tenké
  rezy písma ani na vysokú hustotu pixelov.
- Ovláda sa jednou rukou — palec dosiahne spodnú tretinu displeja.

## Technické mantinely

Postavené v **Unity UI Toolkit** (štýly v USS). Je to podmnožina CSS, nie CSS:

- flexbox áno, **CSS grid nie**
- gradienty, tiene, blur, `backdrop-filter` — **nie sú k dispozícii**
- animácie len ako jednoduché prechody
- ikony musia byť priložené ako textúry alebo SVG assety, nie ikonový font
- vlastný font sa musí priložiť ako súbor

Návrh preto musí stáť na **ploche, farbe, veľkosti a rozostupe**, nie na efektoch. Ak si
niektorý prvok blur alebo gradient vyžaduje, treba to výslovne povedať, aby sa dal nahradiť
plochou farbou.

## Čo musí byť na obrazovke

### 1. Banner režimu (hore)

Aplikácia si pri štarte sama zisťuje, čo telefón vie. Tri stavy:

- **CHECKING** — neutrálny, prvé dve sekundy
- **AR** — normálny prevádzkový stav
- **PREVIEW — NIE JE TO TEST** — telefón nemá ARCore. Prekryv sa kreslí proti prázdnemu
  pozadiu a **nemeria vôbec nič**

Toto je najdôležitejšie rozlíšenie v celom UI. Preview vyzerá takmer rovnako ako AR a
skôr či neskôr z neho niekto — aj autor o mesiac — skúsi čítať presnosť. **Musí byť nemožné
pomýliť si ich.** Dnes to rieši oranžové pozadie proti tmavo-tyrkysovému; nech to návrh
rieši aspoň tak dôrazne.

Pod nadpisom je jedna veta vysvetľujúca, prečo padol tento režim — napríklad
„Google Play Services for AR chýbajú". Môže mať dva riadky.

### 2. Číselník (pod bannerom)

Päť riadkov, názov vľavo, hodnota vpravo:

| Riadok | Príklad hodnoty | Poznámka |
|---|---|---|
| Tracking | `tracking` alebo `SessionInitializing · InsufficientLight` | hodnota môže byť dlhá, musí sa zmestiť |
| Marker | `not seen` / `in view` / `Limited` | |
| Alignment | `sampling 12/30` alebo `48 s ago · ±2.4 cm / 1.1°` | dlhá hodnota |
| **Walked** | `18.6 m` | **hlavné číslo** |
| From marker | `12.1 m` | |

`Walked` je os celého testu — odchýlka rastie s prejdenou vzdialenosťou. Musí byť vizuálne
nadradený ostatným riadkom.

Každá hodnota má **štyri stavy**, ktoré chcem v návrhu vidieť všetky:

- normálny
- **warn** — žltá/oranžová (napr. prebieha zber vzoriek)
- **bad** — červená (tracking sa stratil)
- **idle** — stlmená, keď sa práve nemeria nič

### 3. Tlačidlá (dole, na dosah palca)

- **Re-anchor** — primárne. Znovu zosúladí prekryv podľa značky. V režime Preview je
  **neaktívne** (a musí tak aj vyzerať).
- **Hide overlay / Show overlay** — prepínač, mení si text.
- **Mark** — zapíše očíslovanú značku do logu. Tlačí sa na každom kontrolnom bode počas chôdze.

Tlačia sa poslepiačky za chôdze, takže terč musí byť veľký. Dnes majú výšku 62 px.

### 4. Pätička

Jeden malý riadok: `log: frilens-2026-09-03.csv · 412 rows · 5 marks`, alebo
`log: not writing`. Môže byť potlačená, ale musí zostať čitateľná — je to jediná kontrola,
či sa meranie vôbec zapisuje.

## Čo od návrhu chcem

- Portrait, jeden telefónny rám.
- **Štyri stavy obrazovky:** CHECKING · AR pred zosúladením (hodnoty idle) · AR zosúladený
  a merajúci · PREVIEW.
- Farebnú paletu s konkrétnymi hodnotami, vrátane warn, bad a idle.
- Veľkosti písma a rozostupy v px.
- Ak navrhuje ikony, tak ktoré a kam.

## Čo nemeniť

- **Nič sa nesmie skryť** do menu, záložky ani za gesto. Všetko podstatné naraz na obrazovke —
  za chôdze sa nikto nebude preklikávať.
- **Preview sa nesmie dať pomýliť s AR.**
- **Nepridávať funkcie.** Žiadne nastavenia, história ani export. Prístroj má tri tlačidlá
  a tak to má zostať.

## Súčasný stav — východisko, nie mantinel

Funkčné, nie pekné. Dnešné hodnoty:

| Prvok | Hodnota |
|---|---|
| Panely | čierna `rgb(10,12,16)` na 86 % krytie, zaoblenie 12 px |
| Banner AR | `rgb(8,42,52)` na 90 % |
| Banner Preview | `rgb(94,44,8)` na 92 % |
| Akcent (nadpis režimu, hlavné číslo) | `#78E2FF` |
| Warn | `#FFBA60` |
| Bad | `#FF7A7A` |
| Text | `#EDF2F7` |
| Primárne tlačidlo | `rgb(14,116,144)` |
| Písmo | nadpis režimu 22 · veta pod ním 15 · názov riadku 15 · hodnota 19 · hlavné číslo 27 · tlačidlo 17 · pätička 12 |

Ber to ako východiskový bod. Prepracovať sa môže všetko okrem vecí vymenovaných vyššie
v sekcii *Čo nemeniť*.
