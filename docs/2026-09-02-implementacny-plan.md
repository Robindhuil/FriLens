# Implementačný plán

**Dátum:** 2026-09-02 · **Stav:** návrh · **Predpoklad:** [analýza stavu](2026-09-02-stav-projektu-a-analyza-navmeshu.md)

Cieľ testu sa nemení oproti pôvodnému dokumentu: **zistiť, ako presne sedí navmesh na
skutočnú fakultu a ako rýchlo to odchádza pri chôdzi.** Nie navigácia, nie okluzia, jedna
značka, jedna plocha.

Fázy sú zoradené tak, že každá končí niečím overiteľným. Fázy 1–5 sú práca pri počítači,
fáza 6 je v teréne.

---

## Fáza 0 — Hygiena projektu — ✅ hotové (okrem overenia na zariadení)

Krátke veci, ktoré blokujú build alebo špinia repozitár.

- [x] `Assets/Models/*.blend` do `.gitignore` ([ADR 002](decisions/002-verzovanie-modelov.md))
- [x] Aktívny build target prepnutý na **Android**
- [x] Bundle identifier `sk.uniza.fri.frilens`, company `FRI UNIZA`, product `FriLens`
- [x] ARCore **AR Required** — už bolo nastavené
- [x] ARCore **Depth: Required → Optional**. Test nekreslí okluziu, takže požadovať depth
      API by len zúžilo zoznam telefónov, na ktoré sa appka dá nainštalovať.
- [x] Min SDK **30 → 25**. Pôvodný dokument chcel 24, lebo ARCore beží od Androidu 7.0,
      ale Unity 6000.4 nižšie než 25 odmieta: *„Minimum supported Android API level is 25
      (Android 7.1 Nougat)."* 25 je teda podlaha.
- [x] Orientácia zamknutá na **Portrait** (autorotácia vypnutá vo všetkých troch zvyšných
      smeroch) — otočenie počas chôdze je artefakt, ktorý test nepotrebuje
- [x] `Screen.sleepTimeout = NeverSleep` cez `KeepScreenAwake` na objekte `FriLens` v scéne.
      Každé zamknutie obrazovky ukončí AR session a zosúladenie po ňom už meria drift novej
      session, nie tej testovanej.
- [x] Odinštalovaný `com.unity.xr.arkit` vrátane osirených `AR Kit Loader.asset`,
      `AR Kit Settings.asset` a záznamu v `EditorBuildSettings`

`com.unity.xr.interaction.toolkit` **zostáva zatiaľ nainštalovaný** — šablónová scéna na ňom
stojí (`Screen Space Ray Interactor`, `Object Spawner`). Odstráni sa vo fáze 2, keď scéna tie
komponenty už mať nebude; skôr by z nich boli missing scripty.

### Stav po fáze 0

```
Active build target : Android
Company / Product   : FRI UNIZA / FriLens
Bundle id           : sk.uniza.fri.frilens
Min SDK             : AndroidApiLevel25
Scripting backend   : IL2CPP
Architectures       : ARM64
Graphics APIs       : OpenGLES3
Orientation         : Portrait
ARCore              : requirement=Required depth=Optional
ARKit package       : removed
KeepScreenAwake     : on 'FriLens'
```

**Neoverené:** či prázdny build prejde na telefón a spustí sa. Vyžaduje zariadenie
s ARCore a zapnutým USB ladením (diera K6) a nedá sa odbaviť od stola.

---

## Fáza 1 — Extrakcia navigačných plôch — ✅ hotové

`Assets/_Game/Editor/NavMeshExtractorWindow.cs`, menu `FriLens/Extract Nav Meshes`
(okno s výberom prefixu) a `FriLens/Extract Nav Meshes (all floors)` (dávka cez všetkých
deväť podlaží).

Čo robí:

1. Načíta prefab z `Assets/Models/navmesh.blend`.
2. Vyfiltruje `MeshFilter`-y, ktorých meno obsahuje `_nav_` a začína zadaným prefixom
   (`ra0`, `rb1`, `rc0`, …). Tlačidlo **List groups** vypíše, aké prefixy v modeli existujú,
   aby sa nemusel hádať.
3. Zlúči ich do jedného `Mesh` cez `CombineMeshes`, kde každá inštancia nesie
   `filter.transform.localToWorldMatrix`. Tá reťaz siaha až po koreň modelu, takže sa
   **zapečie aj rotácia 270° X** (diera K4).
4. Uloží ako `Assets/_Game/Generated/Nav/<prefix>_nav.asset`.
5. Vypíše počet trojuholníkov, rozmery, stred a rozsah Y.

Dve veci, ktoré sa dali ľahko pokaziť a sú ošetrené:

- **Index buffer.** Najväčšie podlažie (`rc0`) má 2 885 vrcholov, takže 16-bit stačí.
  Skript prepne na `UInt32` sám, ak by rozpočet vrcholov prekročil 60 000.
- **Počiatok.** Mesh zostáva v súradniciach modelu, neposúva sa do nuly. Zosúladenie vo
  fáze 3 s tým počíta a póza značky je v tom istom priestore.

### Výsledok

| asset | vrcholy | trojuholníky | rozmery X × Y × Z (m) | rozsah Y (m) |
|---|---:|---:|---|---|
| `ra0_nav` | 1 933 | 1 259 | 21.06 × 3.97 × 80.54 | 4.81 – 8.78 |
| `ra1_nav` | 1 865 | 1 196 | 21.06 × 3.94 × 73.93 | 8.39 – 12.33 |
| `ra2_nav` | 2 087 | 1 470 | 21.23 × 3.99 × 73.93 | 11.97 – 15.96 |
| `ra3_nav` | 2 805 | 1 687 | 21.23 × 4.35 × 73.93 | 15.55 – 19.90 |
| `rb0_nav` | 1 554 | 1 083 | 31.40 × 7.26 × 48.75 | 3.29 – 10.55 |
| `rb1_nav` | 902 | 654 | 30.54 × 3.70 × 43.04 | 10.46 – 14.16 |
| `rb2_nav` | 904 | 643 | 30.59 × 3.70 × 43.26 | 14.07 – 17.77 |
| `rb3_nav` | 867 | 623 | 24.60 × 4.25 × 43.26 | 17.10 – 21.35 |
| `rc0_nav` | 2 885 | 1 890 | 48.88 × 7.37 × 45.52 | 0.69 – 8.07 |

Počty trojuholníkov aj rozmery sedia na hodnoty namerané priamo na zdrojovom modeli, čo
znamená, že zlúčenie nič nestratilo. Orientácia overená zvlášť: **560 z 1 933 vrcholov
`ra0_nav` leží do 5 cm od `Y = 5.15 m`**, teda na jednej vodorovnej podlahovej rovine.
Keby sa rotácia nezapiekla, toto číslo by bolo nula a plocha by stála na hrane.

Suterén, terasy a exteriér sa zámerne neextrahujú — nie sú súčasťou tohto testu a každý
z nich by potreboval vlastné rozhodnutie, čo je ešte jedna plocha.

---

## Fáza 2 — Vyčistenie scény — ✅ hotové (okrem overenia na zariadení)

Scéna presunutá a premenovaná: `Assets/Scenes/SampleScene.unity` →
**`Assets/_Game/Scenes/FriLensTest.unity`**. Vlastný obsah patrí do `_Game/`, rovnako ako
vo FriWorlde. Build settings obsahujú už len ju.

### Odstránené

- `UI` — celé šablónové menu (Create/Delete/Options Button, Options Modal, Coaching UI,
  Greeting Prompt, DebugMenu, Object Menu Animator)
- `Object Spawner`, `Screen Space Ray Interactor` — „ťukni a polož kocku"
- `Directional Light` — prekryv je unlit, svetlo nemá čo osvetľovať
- `ARPlaneManager` — detekcia rovín kreslila prechodové štvorce práve po podlahe, teda po
  ploche, ktorej hranu má test čítať. K tomu stojí CPU.
- `ARRaycastManager`, `InputActionManager` — existovali len pre tap-to-place

### Odpojenie od šablónového prefabu

`XR Origin (AR Rig)` bol inštanciou prefabu z `Assets/Samples/…/AR Starter Assets/`, takže
odstránené komponenty boli len overrides a scéna si ďalej ťahala závislosti na šablóne.
Po `UnpackPrefabInstance(Completely)` klesol počet závislostí scény na `MobileARTemplateAssets`
a `Samples` **zo 16 na 1**.

Tá jedna je `Assets/Samples/…/Starter Assets/XRI Default Input Actions.inputactions`, ktorú
používa `TrackedPoseDriver` na `Main Camera` na polohu a rotáciu kamery. **Zložka
`Assets/Samples` sa preto zatiaľ nedá zmazať** — najskôr by ju musel nahradiť vlastný
`.inputactions`. Bez zariadenia sa to overiť nedá, takže to nechávam tak.

### Výsledná hierarchia

```
AR Session
EventSystem
XR Origin (AR Rig)         + XROrigin, ARTrackedImageManager → FriLensMarkers.asset
  └ Camera Offset
      └ Main Camera        + ARCameraManager, ARCameraBackground, TrackedPoseDriver
FriLens                    + KeepScreenAwake
AlignmentRoot              ← koreň prekryvu, hýbe ním zosúladenie
  ├ NavOverlay             ra0_nav (1 259 tris), NavOverlay.mat, lokálne Y = +0.03
  └ MarkerAnchor           póza značky v súradniciach modelu — zatiaľ nenastavená (fáza 3b)
```

Missing scriptov v scéne: 0.

### Vytvorené assety

| Asset | Poznámka |
|---|---|
| `Assets/_Game/AR/FriLensMarkers.asset` | Reference Image Library, **zatiaľ prázdna** |
| `Assets/_Game/Materials/NavOverlay.mat` | URP/Unlit, Transparent, Cull Off, ZWrite off, queue 3000, `_BaseColor` RGBA(0, 0.85, 1, 0.35) |

`autoFocusRequested` na `ARCameraManager` bolo v šablóne už zapnuté — bez autofokusu sa
značka z dvoch metrov chytá nespoľahlivo, takže tu nebolo čo meniť.

### Čo z fázy 4 už padlo sem

Materiál aj posadenie 3 cm nad podlahu museli vzniknúť spolu s `NavOverlay`, inak by objekt
nemal čo kresliť. Fáze 4 zostáva ladenie farby oproti skutočnej podlahe, čo sa aj tak dá
robiť až na mieste.

Obojstranné vykreslenie preto, že prekryv je jediná plocha: pozerať sa na ňu zhora pri
státí na nej a spredu od dverí musí dať to isté. `ZWrite` vypnutý, nech si prekrývajúce sa
polygóny nevystrihujú diery.

### Nedokončené a vedomé

- **`Diagnostics` Canvas nevznikol.** Plán ho tu uvádzal, ale prázdny canvas je mŕtve
  závažie — vznikne vo fáze 5 aj s obsahom.
- **`MarkerAnchor` je na počiatku.** Kým fáza 3b nenastaví pózu značky, prekryv sa po
  zosúladení objaví na nezmyselnom mieste. To je očakávané, nie chyba.
- **Reference Image Library je prázdna.** ARCore potrebuje na zostavenie svojej `.imgdb`
  aspoň jeden obrázok, takže build môže na prázdnej knižnici zlyhať. Overiť sa to dá až
  buildom; obrázok tam aj tak musí pribudnúť vo fáze 3a.

**Neoverené:** či sa appka spustí na telefóne a beží kamera. Rovnako ako pri fáze 0 to
vyžaduje zariadenie (diera K6).

---

## Fáza 3 — Značka a zosúladenie

Toto je najzdĺhavejšia a najdôležitejšia časť. Kroky 5 a 6 pôvodného dokumentu platia
nezmenené; sem patrí len to, čo k nim pribudlo.

### 3a. Fyzická značka

- [ ] Vybrať miesto lokalizovateľné v modeli — roh miestnosti, zárubňa, roh schodiska
- [ ] Vytlačiť **matne**, nalepiť naplocho na tvrdý podklad
- [ ] **Odmerať vytlačenú značku pravítkom** a ten rozmer zadať do Reference Image Library.
      Nie rozmer poslaný do tlače. Chyba 5 % v rozmere značky je chyba 5 % v mierke celého
      prekryvu.

### 3b. Póza značky v modeli

Určuje sa **priamo z hrán navigačných polygónov** — tie ležia na vnútornom líci stien,
lebo pochádzajú z toho istého skenu ([ADR 003](decisions/003-poza-znacky-z-nav-polygonov.md)).
Model budovy sa nedonáša.

1. Značku nalepiť na miesto, ktoré polygón zachytáva — **roh miestnosti alebo rovný úsek
   steny**. Zárubňa, výklenok ani stĺp v polygóne nie sú a ich pozícia sa z neho vyčítať
   nedá.
2. Vodorovnú pozíciu a natočenie odčítať z hrany či rohu polygónu vo vygenerovanom
   `<podlažie>_nav.asset`.
3. Výšku odmerať pravítkom od podlahy a pripočítať k výške podlahy daného podlažia
   (`ra000` je na `Y = 5.15 m`).
4. Umiestniť `MarkerAnchor` na výslednú pózu.

### 3c. Skript zosúladenia — ✅ hotové (okrem overenia na zariadení)

`Assets/_Game/Scripts/Runtime/MarkerAlignment.cs`, na objekte `FriLens` v scéne. Napojený
na `ARTrackedImageManager`, `AlignmentRoot` a `MarkerAnchor`.

**Jednorazové zosúladenie, nie sledovanie.** Kým je stav `Sampling` a značka je v stave
`TrackingState.Tracking`, komponent zbiera jej pózu **jeden vzorok za snímok, 30 snímok**,
spriemeruje a raz aplikuje. Potom sa už nehýbe.

Priemerovanie nie je kozmetika. ARCore aktualizuje pózu sledovaného obrázka každý snímok
a skáče o jednotky centimetrov. Zosúladenie z jedného snímku meria ten šum, nie chybu
v zameraní značky — a riadok „chyba už pri značke, konštantná" z tabuľky v kroku 10 by
konštantný nevyšiel.

Sledovať pózu naživo by bolo horšie: prekryv by sa chvel a hlavne by sa **schoval presne
ten jav, ktorý test meria** — ako ďaleko prekryv odíde od budovy počas chôdze.

**Matematika.** Póza kotvy voči koreňu je pevná, takže hľadaný koreň je
`root = measured · anchorLocal⁻¹`:

```csharp
rootRotation = measuredRotation * Quaternion.Inverse(anchorLocalRotation);
rootPosition = measuredPosition - rootRotation * anchorLocalPosition;
```

**Rozptyl vzoriek.** `SampleSpreadMeters` a `SampleSpreadDegrees` hlásia najväčšiu odchýlku
vzorky od priemeru. To je práve to číslo, ktorým sa pri čítaní výsledku odlíši skutočný
posun od šumu trackera. Zapisuje sa aj do konzoly pri každom zosúladení.

**`Realign()`** je verejná metóda pre tlačidlo re-anchor. Prvé zosúladenie prebehne
automaticky pri prvom uvidení značky (`m_AlignOnFirstSighting`), ďalšie len na požiadanie.

**Varovanie na nenastavenú kotvu.** Ak `MarkerAnchor` stále sedí na počiatku bez rotácie,
komponent to raz nahlási — inak by prekryv pristál na nezmyselnom mieste bez vysvetlenia.

#### Odchýlka od plánu: tlačidlo zamrznúť/rozmraziť nevzniklo

Plán ho žiadal, „aby sa prekryv nehýbal počas fotenia". Pri jednorazovom zosúladení sa
prekryv nehýbe nikdy — stojí v priestore session a to, čo sa hýbe, je kamera. Tlačidlo by
bolo bez funkcie. Skryť prekryv sa dá tlačidlom z fázy 5, ktoré plní iný účel a ten zostáva.

#### Overenie matematiky

`FriLens/Verify Alignment Math` (`Assets/_Game/Editor/AlignmentMathVerifier.cs`) — štyri
kontroly proti známym odpovediam:

```
solve: anchor lands 0.0001 mm and 0.00000 deg off target
identity anchor: pos err 0.0000 mm, rot err 0.00000 deg
rotation average over 30 sign-mixed samples with +-1 deg jitter: 0.000 deg from truth
  naive sum magnitude, for contrast: 0.0374 (a correct sum is near 30)
position average: (2.00, 3.00, 4.00) (expected (2.0, 3.0, 4.0))

ALL CHECKS PASSED
```

Tretia kontrola stojí za vysvetlenie. `q` a `−q` sú tá istá rotácia a ARCore vracia raz
jedno, raz druhé. Naivný súčet ich vyruší — vidno to na tom riadku „naive sum magnitude
0.0374" tam, kde by mala byť hodnota blízko 30. Priemer preto najprv preklopí znamienka na
jednu pologuľu. Bez toho by zosúladenie tu a tam dalo úplný nezmysel a v teréne by to
vyzeralo ako zle zameraná značka.

Kontroly sú menu položka, nie testovací asmdef: runtime skripty žijú v preddefinovanej
`Assembly-CSharp`, na ktorú sa testovacia assembly s asmdef odkázať nedá.

**Neoverené:** či po namierení na značku prekryv skočí na miesto a či opakované zosúladenie
z rovnakého miesta trafí to isté (rozdiel pod 1–2 cm). Vyžaduje značku (fáza 3a), jej pózu
(fáza 3b) a zariadenie (diera K6).

---

## Fáza 4 — Vykreslenie

- Materiál: **URP/Unlit**, Surface Type Transparent, **Render Face = Both**
- Prekryv posadiť **2–3 cm nad podlahu** — inak bude blikať proti nej (z-fighting)
- Farba s vysokým kontrastom voči skutočnej podlahe fakulty
- Žiadne tiene, žiadne osvetlenie — `Directional Light` môže zo scény preč

**Hotovo, keď:** plocha je na telefóne čitateľná a nebliká.

---

## Fáza 5 — Diagnostika

Bez čísel sa z testu stane „vyzerá to trochu mimo".

Na obrazovke:

- Stav trackingu z `ARSession.state` a `ARSession.notTrackingReason`
- Čas od posledného rozpoznania značky
- **Prejdená vzdialenosť od zosúladenia** — integrovaná z pozície AR kamery po snímkoch
- Priama vzdialenosť od značky (iné číslo než prejdená vzdialenosť, obe treba)
- Tlačidlá: re-anchor, skryť prekryv (nech vidno, čo je pod ním)

Do súboru (`Application.persistentDataPath`, CSV): časová pečiatka, pozícia a rotácia
kamery, stav trackingu, prejdená vzdialenosť, značka udalosti pri stlačení tlačidla.

Dôvod na súbor: fotky z terénu nezachytia priebeh, len okamihy. Bez logu sa nedá spätne
povedať, kedy presne tracking vypadol.

**Hotovo, keď:** po prechádzke je na telefóne CSV, ktorý sa dá stiahnuť a otvoriť.

---

## Fáza 6 — Test v teréne

Protokol z pôvodného dokumentu, s jednou úpravou (diera K7).

- [ ] Zosúladiť pri značke a **hneď pozrieť zblízka** — to je chyba modelu a značky, bez driftu
- [ ] **Pravítkom overiť jeden reálny rozmer** (šírka chodby by mala byť 3.20 m
      pri `ra000_corridor_3_nav_1`) — priamy test predpokladu, že model je 1:1
- [ ] Prejsť trasu a pozerať hranu prekryvu pri stene po **10, 25, 50 a 100 metroch
      prejdenej vzdialenosti** — nie vzdialenosti od značky. Najdlhšia rovná chodba má
      30.8 m, takže 100 m je okruh cez viac chodieb, nie priamka.
- [ ] Fotiť cez appku, nie spamäti
- [ ] Na konci sa vrátiť k značke, zosúladiť znova a pozrieť, či to skočí späť

### Ako čítať výsledok

Nezmenené oproti pôvodnému dokumentu, doplnený jeden riadok:

| čo vidíš | príčina |
|---|---|
| chyba už pri značke, konštantná | pozícia alebo rotácia značky v modeli je zle určená |
| chyba rastie s dĺžkou chodby, prekryv sa „rozťahuje" | zlá mierka — rozmer značky alebo model nie je 1:1 |
| prekryv je pootočený a odchýlka rastie so vzdialenosťou | rotácia značky, alebo VIO drift v yaw |
| chyba rastie s prejdenou vzdialenosťou, tvar sedí | bežný VIO drift — očakávaj 1–2 % prejdenej dráhy |
| **hrana sedí v jednej miestnosti a nesedí vo vedľajšej** | **nav polygóny nesedia na stenách tak, ako sa predpokladá** |

Posledný riadok je nový. Podľa [ADR 003](decisions/003-poza-znacky-z-nav-polygonov.md) je
nepravdepodobný — plochy pochádzajú z toho istého skenu ako budova a ležia na líci stien.
Ak napriek tomu nastane, znamená to, že predpoklad, na ktorom stojí celé určenie pózy
značky, neplatí. To je informácia, ktorú test má vedieť odovzdať.

---

## Otvorené otázky

Tieto tri menia poradie práce, nie jej obsah.

1. **Ktoré podlažie testovať prvé?** `ra` podlažie 0 má najdlhšiu chodbu (30.8 m)
   a najjednoduchší pôdorys. `rc` podlažie 0 má jedáleň a prednáškové sály, teda veľké
   otvorené priestory, kde sa drift prejaví inak. Návrh: začať `ra` podlažím 0.
2. **Portrait alebo landscape?** Portrait je prirodzenejší na chôdzu, landscape lepšie
   ukáže hranu pri stene. Návrh: nechať portrait, orientáciu zamknúť.

Otázka „kedy doniesť `fri_building`" odpadla — [ADR 003](decisions/003-poza-znacky-z-nav-polygonov.md).

---

## Čo tento test zámerne nerieši

Nezmenené: okluzia, navigácia, prekryv dát z `Rooms.json`, viac podlaží a prechod medzi
nimi, správanie pri bežnom používaní, iOS.
