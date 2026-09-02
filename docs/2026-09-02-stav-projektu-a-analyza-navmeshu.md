# Stav projektu a analýza `navmesh.blend`

**Dátum:** 2026-09-02 · **Unity:** 6000.4.11f1 · **Stav:** prieskum dokončený, implementácia nezačatá

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
- Zostáva iná otázka, ktorú pôvodný dokument nemohol položiť: **sedia tie ručne kreslené
  polygóny na steny?** Ak ich autor kreslil od oka s rezervou, hrana bude odsadená rovnako
  ako pri pečení, len nekonzistentne. Toto je teraz najväčšia neznáma testu a nedá sa
  overiť inak než porovnaním s geometriou budovy — ktorú v tomto projekte nemáme.

---

## 4. Kritické diery

Zoradené podľa toho, čo najskôr zastaví prácu.

### K1 — Chýba geometria budovy, bez nej sa nedá určiť póza značky

Krok 5 pôvodného dokumentu žiada „zistiť pozíciu aj rotáciu značky v modeli". V projekte
sú **len navigačné plochy**. Steny, zárubne, rohy miestností — nič z toho tu nie je. Bez
nich sa póza značky dá určiť len odhadom z podlahových polygónov, čo je presne ten druh
chyby, ktorý má test merať.

Budova existuje vo FriWorlde: `FriWorld/Assets/3Dmodels/static/fri_building/fri_building.blend`
(90 MB), plus `interior_objects.blend` (9 MB).

Treba: doniesť `fri_building` do FriLens **iba ako editorový pomocník** (nie do buildu) a
overiť, že má rovnaký počiatok súradníc ako `navmesh.blend`. Duplikované mená objektov
(`.002` sufixy) naznačujú, že oba vyšli z jedného Blender súboru, takže počiatok
pravdepodobne sedí — ale je to predpoklad, nie fakt.

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

### K4 — Rotácia koreňa 270° okolo X

Importér natáča koreňový objekt o 270° v X, aby preložil Blenderov Z-up do Unity Y-up.
Pri extrakcii meshu sa táto rotácia **musí zapiecť do vrcholov**. Ak sa na to zabudne,
prekryv bude v AR otočený o 90° a nebude zjavné prečo.

### K5 — Značka neexistuje

Žiadna Reference Image Library v projekte, žiadny vytlačený obrázok, žiadne zmerané rozmery.
Toto je tvrdá podmienka testu a je celá pred nami. Pozri kroky 5–6 pôvodného dokumentu —
tam sa nič nemení a nič sa nedá skrátiť.

### K6 — Nie je potvrdené testovacie zariadenie

Min SDK je nastavené na 30 (Android 11). Treba konkrétny telefón zo zoznamu podporovaných
ARCore zariadení, so zapnutým USB ladením.

### K7 — Trasa na 100 m priamo neexistuje

Pôvodný dokument chce merať odchýlku po 10, 25, 50 a 100 metroch. Najdlhšia rovná chodba
v modeli je `ra000_corridor_3_nav_1` s dĺžkou **30.8 m**; celé podlažie `ra` má pôdorys
21 × 80.5 m. Sto metrov po priamke na jednom podlaží nie je kde prejsť.

Test to prežije, ale protokol treba upraviť: merať po *prejdenej* vzdialenosti (integrovanej
z pozície kamery), nie po vzdialenosti od značky, a trasu viesť ako okruh cez viac chodieb.
Zároveň to znamená, že riadok „chyba rastie s dĺžkou chodby" z tabuľky v kroku 10 sa dá
overiť len na 30-metrovom úseku — chyba mierky sa tam prejaví trikrát slabšie, než dokument
predpokladal.

### K8 — Šablónové zvyšky

Bundle identifier je stále `com.unity.template.ar_mobile`, aktívny build target je Windows,
scéna je plná šablónového UI. Nič z toho nie je ťažké, ale nič z toho sa nespraví samo.

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
