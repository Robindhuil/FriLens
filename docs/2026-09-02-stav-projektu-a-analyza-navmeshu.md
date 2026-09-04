# Stav projektu a analýza `navmesh.blend`

**Verzia:** 0.1.8-alpha · **Dátum:** 2026-09-02, stav dier aktualizovaný 2026-09-04 · **Unity:** 6000.4.11f1

Východiskový dokument: [`FriWorld/docs/2026-08-29-frilens-ar-test.md`](../../FriWorld/docs/2026-08-29-frilens-ar-test.md).
Tento dokument opisuje, čo v projekte skutočne je — nie čo bolo naplánované. Kde sa
skutočnosť rozchádza s plánom, je to výslovne uvedené.

---

## 1. Stav Unity projektu

Projekt vznikol z **AR Mobile šablóny**, takže väčšina krokov 1–3 pôvodného dokumentu je hotová.

### Balíčky

| Balíček | Verzia | Poznámka |
|---|---|---|
| `com.unity.xr.arfoundation` | 6.4.3 | ✓ |
| `com.unity.xr.arcore` | 6.4.3 | ✓ |
| `com.unity.xr.arkit` | 6.4.3 | nepotrebný, iOS nerobíme |
| `com.unity.xr.management` | 4.5.3 | ✓ |
| `com.unity.xr.interaction.toolkit` | 3.4.1 | zo šablóny, pre tento test nepotrebný |
| `com.unity.render-pipelines.universal` | 17.4.0 | URP, zhoda s FriWorldom |
| `com.unity.ai.navigation` | — | **nie je nainštalovaný** |

`com.unity.ai.navigation` napokon **nie je potrebný** — pozri sekciu 3. Pôvodný dokument ho
označil za povinný, lebo predpokladal, že navmesh budeme piecť. Nebudeme.

### Android

Android Build Support **je nainštalovaný** (`PlaybackEngines/AndroidPlayer`).

Tabuľka nižšie je stav **pri prieskume**. Čo z nej fáza 0 zmenila, je v
[pláne](2026-09-02-implementacny-plan.md#fáza-0--hygiena-projektu--✅-hotové-okrem-overenia-na-zariadení).

| Nastavenie | Hodnota | Verdikt |
|---|---|---|
| Min SDK | `AndroidApiLevel30` | prísnejšie než ARCore vyžaduje (24); zužuje okruh zariadení |
| Target SDK | `Auto` | ✓ |
| Scripting backend | IL2CPP | ✓ |
| Target architectures | ARM64 | ✓ |
| Graphics API | `OpenGLES3` (auto vypnuté) | ✓ Vulkan už odstránený |
| Color space | Linear | ✓ |
| XR loader Android | ARCore zapnutý | ✓ |
| Aktívny build target | **StandaloneWindows64** | ✗ prepnúť na Android |
| Bundle identifier | **`com.unity.template.ar_mobile`** | ✗ šablónové default, prepísať |
| Orientácia | Portrait | zvážiť, pozri plán |

### Scéna

`Assets/Scenes/SampleScene.unity`, 5 koreňových objektov:

```
AR Session
Directional Light
EventSystem
UI                        ← Create/Delete/Options Button, Options Modal, Coaching UI,
                            Greeting Prompt, DebugMenu, Object Menu Animator
XR Origin (AR Rig)
  └ Camera Offset
      ├ Object Spawner
      ├ Main Camera
      └ Screen Space Ray Interactor
```

Celé UI aj `Object Spawner` sú šablónové „polož kocku na rovinu" veci. Pre tento test sú
na zahodenie — pozri plán, fáza 2.

### Verzovanie

Git repozitár existuje, remote `github.com/Robindhuil/FriLens`, jeden commit. **Git LFS je
nakonfigurovaný správne** — `.gitattributes` pokrýva `*.blend`, `*.fbx` aj bežné textúry.
114 súborov je momentálne v strome ako zmenené; sú to šablónové binárky, ktoré prvý commit
uložil surovo a LFS filter ich teraz prevádza na pointery. Commitnutím sa presunú do LFS,
čo je žiaduce.

---

## 2. Čo je v `navmesh.blend`

`Assets/Models/navmesh.blend`, **300 MB**. Meno klame — nie je to jedno podlažie a nie je
to len navmesh.

```
343 meshov · 100 501 vrcholov · 160 957 trojuholníkov · 50 materiálov
koreňový objekt otočený o 270° okolo X (Blender Z-up → Unity Y-up)
celkové rozmery: 95.4 × 23.4 × 136.4 m
```

### Rozdelenie obsahu

| Skupina | Meshov | Trojuholníkov | Podiel trojuholníkov |
|---|---:|---:|---:|
| navigačné plochy (`*_nav_*`) | 310 | ~13 600 | 8 % |
| exteriér a dekorácia | 33 | ~147 400 | **92 %** |

Tých 33 objektov sú terén, cesty, tráva, obrubníky, dlažba, žľaby na vodu — `Teren.001`
(47 236 tris), `cesta.002` (45 095), `trava.002` (23 499), `Object629.002` (12 785). Pre AR
test sú úplne zbytočné a tvoria drvivú väčšinu geometrie.

### Navigačné plochy podľa podlaží

Pomenovanie: `r<budova><podlažie><miestnosť>_<názov>_nav_<index>`, napríklad
`ra000_corridor_3_nav_1` alebo `rb213_nav_1`.

| Skupina | Meshov | Tris | Rozsah Y (m) |
|---|---:|---:|---|
| `ra` podlažie 0 | 24 | 1 259 | 4.81 – 8.78 |
| `ra` podlažie 1 | 34 | 1 196 | 8.39 – 12.33 |
| `ra` podlažie 2 | 34 | 1 470 | 11.97 – 15.96 |
| `ra` podlažie 3 | 35 | 1 687 | 15.55 – 19.90 |
| `rb` podlažie 0 | 28 | 1 083 | 3.29 – 10.55 |
| `rb` podlažie 1 | 30 | 654 | 10.46 – 14.16 |
| `rb` podlažie 2 | 33 | 643 | 14.07 – 17.77 |
| `rb` podlažie 3 | 27 | 623 | 17.10 – 21.35 |
| `rc` podlažie 0 | 36 | 1 890 | 0.69 – 8.07 |
| `rb` suterén | 17 | — | 3.29 – 7.06 |
| terasy (`rb_terrace`, `terrace_*`) | 5 | 670 | 0.00 – 7.12 |
| exteriér (`outside_*`) | 7 | 1 849 | 2.58 – 7.07 |

**Dôsledok:** podlažia sa v Y prekrývajú, lebo schodiská patria do oboch. `ra` podlažie 0
končí na 8.78 m, podlažie 1 začína na 8.39 m. Orezanie podlažia **musí ísť podľa mena, nie
podľa výšky** — tak, ako to pôvodný dokument správne predpokladal, len teraz vieme, že
prefixy v mene to umožňujú triviálne.

Budovy stoja na rôznej výške terénu: `rc` prízemie je na Y ≈ 0.7 m, `rb` na 3.3 m, `ra` na
4.8 m. Nejde o chybu modelu, terén stúpa.

### Charakter navigačných plôch

Overené na `ra` podlaží 0:

- Plochy miestností a chodieb sú **ploché polygóny** — rozptyl Y vrcholov je 0.0000 m.
  Nejde o upečený navmesh, sú to ručne kreslené podlahové polygóny.
- Schodiská sú 3D (`ra000_main_stair_nav_1`, rozptyl Y 3.59 m).
- Trojuholníkov je na plochu veľmi málo — `ra000_corridor_1_nav_1` má 5 trojuholníkov na
  9.75 m chodby, `ra000_elevator_nav_1` má 6. To zodpovedá ručne kresleným obdĺžnikom.

Rozmery pôsobia ako reálne metre:

| Objekt | Rozmery (m) |
|---|---|
| `ra000_corridor_3_nav_1` | 3.20 × 30.76 |
| `ra000_corridor_2_nav_1` | 5.97 × 23.63 |
| `ra000_men_restroom_nav_1` | 3.56 × 5.62 |
| `ra003_nav_1` | 5.95 × 24.43 |
| výška podlažia `ra` | 3.58 |

Šírka chodby 3.2 m, výška podlažia 3.58 m a pôdorys podlažia 21 × 80.5 m sú hodnoty, aké
fakulta reálne má. Model je s veľkou pravdepodobnosťou 1:1 v metroch — ale **pravítko na
mieste to musí potvrdiť**, lebo presne toto je chyba, ktorú test hľadá.

### Väzba na `Rooms.json`

Kódy miestností v menách plôch (`ra301_nav_1`) zodpovedajú kľúčom v
`FriWorld/Assets/Resources/Rooms.json` (`"name": "RA301"`). Pre tento test to nepotrebujeme,
ale je to hotová cesta k prekryvu dát o miestnostiach v ďalšej iterácii.

---

## 3. Čo sa mení oproti pôvodnému plánu

Pôvodný dokument, krok 4, označil za najdôležitejší riadok celého testu pečenie navmeshu
s `agentRadius` okolo 0.01 — aby hrana plochy sadla na stenu a nebola odsadená o 20 cm ako
vo FriWorlde.

**Tento krok odpadá.** Geometria v `navmesh.blend` nie je upečený navmesh, sú to ručne
kreslené polygóny. Žiadny agent radius sa na ne nikdy neaplikoval, takže žiadne odsadenie
neexistuje. Odpadá tým aj obava, že pečenie s polomerom 0.01 zabije voxelizáciu.

Dôsledky:

- `com.unity.ai.navigation` netreba.
- `NavMesh.CalculateTriangulation()` netreba — mesh je priamo v assete.
- Otázka, či tie ručne kreslené polygóny sedia na steny, je **zodpovedaná**: autor modelu
  potvrdil, že sedia a že pochádzajú z toho istého skenu ako geometria budovy. Hrana
  polygónu je teda vnútorné líce steny a dá sa použiť ako referencia aj na určenie pózy
  značky — pozri [ADR 003](decisions/003-poza-znacky-z-nav-polygonov.md).

---

## 4. Kritické diery

Zoradené podľa toho, čo najskôr zastaví prácu.

### K1 — Chýba geometria budovy — ✅ vyriešené 2026-09-02

Pôvodná obava: krok 5 pôvodného dokumentu žiada „zistiť pozíciu aj rotáciu značky
v modeli", a v projekte sú len podlahové polygóny bez stien a zárubní.

Autor modelu potvrdil, že **polygóny sedia na steny a pochádzajú z toho istého skenu**.
Hrana polygónu je teda vnútorné líce steny. Póza značky sa dá určiť z nej:

- vodorovná pozícia a natočenie z hrany alebo rohu polygónu,
- výška pravítkom na mieste, pripočítaná k známej výške podlahy (`ra000` je na `Y = 5.15 m`).

`fri_building.blend` sa preto donášať nebude. Podrobne aj s obmedzeniami, ktoré z toho
plynú pre výber miesta na značku, v [ADR 003](decisions/003-poza-znacky-z-nav-polygonov.md).

### K2 — 300 MB blend v repozitári

LFS je nastavený, takže commit prejde, ale GitHub dáva na free účte 1 GB LFS priestoru
a 1 GB prenosu mesačne. Jeden commit tohto súboru zožerie 30 % kvóty, každý re-export
ďalších 300 MB.

Pritom 92 % tých dát sú terén a cesty, ktoré nepotrebujeme. Riešenie je v
[ADR 002](decisions/002-verzovanie-modelov.md).

### K3 — Import `.blend` vyžaduje nainštalovaný Blender

Unity číta `.blend` tak, že na pozadí spustí Blender. Na stroji bez Blendera sa projekt
neotvorí korektne — mesh assety budú prázdne. Pre repozitár, ktorý má prežiť viac ako
jeden počítač, je to krehké. Riešenie tiež v ADR 002.

### K4 — Rotácia koreňa 270° okolo X — ✅ vyriešené 2026-09-02

Extraktor rotáciu zapeká cez `filter.transform.localToWorldMatrix`. Overené číslom:
560 z 1 933 vrcholov `ra0_nav` leží do 5 cm od jednej vodorovnej roviny. Keby sa
nezapiekla, bola by to nula.

Importér natáča koreňový objekt o 270° v X, aby preložil Blenderov Z-up do Unity Y-up.
Pri extrakcii meshu sa táto rotácia **musí zapiecť do vrcholov**. Ak sa na to zabudne,
prekryv bude v AR otočený o 90° a nebude zjavné prečo.

### K5 — Značka neexistuje — ⚠️ čiastočne, stav k 0.1.8-alpha

**Hotové pri stole:** štyri obrázky (`Assets/_Game/AR/Markers/frilens-M1..M4.png`) a nástroj
`FriLens > Marker Library`, ktorý naplní knižnicu a zadá fyzický rozmer.

**Chýba to fyzické:** vytlačiť, nalepiť, odmerať rám pravítkom a zamerať pózu do `MarkerAnchor`.

Kým to nie je, appka **meria tracker, nie zhodu modelu s budovou** — a `Re-anchor` je vypnuté
s poznámkou `no markers yet`, aby to nevyzeralo ako pokazené zarovnanie.

Oproti pôvodnému zneniu pribudlo jedno: **značiek má byť viac, rozmiestnených po trase**.
Je to jediná vec nezávislá od mapy ARCore, takže je zároveň liekom na stratu trackingu
([ADR 006](decisions/006-kotvenie-a-strata-trackingu.md)).

### K6 — Testovacie zariadenie — ✅ vyriešené 2026-09-03

| Telefón | ARCore | Poznámka |
|---|---|---|
| **Redmi Note 10 Pro** | ✅ trackuje | zariadenie, na ktorom sa bude testovať ďalej |
| **Redmi 11T** | ✅ trackuje | log z neho potvrdil chybu s `InputActionManager` |
| **Redmi 14C** | ❌ netrackuje | session sa spustí, kamera beží, tracking sa nikdy neustáli |

Zariadenie na test teda je a **fáza 6 nie je blokovaná hardvérom**.

Redmi 14C je zvláštny prípad, ktorý stojí za zapamätanie: `CheckAvailability()` na ňom
prejde a appka skončí v AR režime, hoci trackovať nikdy nezačne. Tá metóda totiž odpovedá
na „dá sa tu použiť ARCore API", nie „vie toto zariadenie trackovať" — a na prvé stačí mať
nainštalované Google Play Services for AR. Appka to od 0.1.2-alpha rieši sama: kontroluje
gyroskop skôr než ARCore a má časový limit na štart session.

Zostáva výhrada k presnosti. Ak testovací telefón nie je v
[Googlom zozname podporovaných zariadení](https://developers.google.com/ar/devices),
nemá overenú kalibráciu kamery a IMU a namerané čísla môžu hovoriť o kalibrácii telefónu,
nie o modeli. Preto sa oplatí zopakovať meranie na oboch fungujúcich telefónoch — ak dajú
podobné čísla, kalibrácia nie je dominantný zdroj chyby.

Appka zostáva `AR Optional` s Preview režimom pre zariadenia, ktoré ARCore nezvládnu —
[ADR 004](decisions/004-zariadenia-bez-arcore.md).

### K7 — Trasa na 100 m priamo neexistuje — ✅ obídené 2026-09-04

Protokol sa upravil presne tak, ako to odsek nižšie navrhoval: meria sa **prejdená
vzdialenosť**, nie vzdialenosť od značky. V praxi to stačí — behy `174812` a `145117` prešli
195 m, respektíve 228 m, cez viac chodieb.

Pôvodná analýza:

Pôvodný dokument chce merať odchýlku po 10, 25, 50 a 100 metroch. Najdlhšia rovná chodba
v modeli je `ra000_corridor_3_nav_1` s dĺžkou **30.8 m**; celé podlažie `ra` má pôdorys
21 × 80.5 m. Sto metrov po priamke na jednom podlaží nie je kde prejsť.

Test to prežije, ale protokol treba upraviť: merať po *prejdenej* vzdialenosti (integrovanej
z pozície kamery), nie po vzdialenosti od značky, a trasu viesť ako okruh cez viac chodieb.
Zároveň to znamená, že riadok „chyba rastie s dĺžkou chodby" z tabuľky v kroku 10 sa dá
overiť len na 30-metrovom úseku — chyba mierky sa tam prejaví trikrát slabšie, než dokument
predpokladal.

### K8 — Šablónové zvyšky — ✅ vyriešené 2026-09-02

Fáza 0 a 2. Bundle identifier je `sk.uniza.fri.frilens`, build target Android, šablónové UI
a `Object Spawner` sú preč a závislosti scény na `MobileARTemplateAssets` klesli zo 16 na 1.

---

## 5. Čo je naopak lepšie, než sme čakali

- Navigačné plochy sú **už rozdelené po miestnostiach a podlažiach** cez pomenovanie.
  Orezanie na jedno podlažie je filter na reťazec, nie geometrická operácia.
- Nav geometria je **veľmi ľahká** — celé `ra` podlažie 0 má 1 259 trojuholníkov. Na
  telefóne to nie je ani zaokrúhľovacia chyba.
- Player settings pre Android sú prakticky hotové, vrátane odstráneného Vulkanu.
- Kódy miestností sedia s `Rooms.json`, čo otvára ďalší krok bez ďalšej práce.

---

## Ďalej

Plán implementácie: [`2026-09-02-implementacny-plan.md`](2026-09-02-implementacny-plan.md).
Rozhodnutia: [`decisions/`](decisions/).
