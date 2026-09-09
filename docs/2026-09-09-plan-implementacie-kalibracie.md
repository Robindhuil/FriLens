# Plán implementácie: kalibrácia z viacerých značiek

> **Pre agentov:** POVINNÁ PODZRUČNOSŤ — na vykonanie použi `superpowers:subagent-driven-development`
> (odporúčané) alebo `superpowers:executing-plans`. Kroky sú zaškrtávacie (`- [ ]`).

**Verzia:** 0.2.2-alpha · **Dátum:** 2026-09-09 · **Stav:** plán pred implementáciou

**Cieľ:** Nahradiť zarovnanie z natočenia jednej značky fitom z polôh viacerých značiek, aby
sa prekryv nehýbal, keď sa nehýbe človek.

**Architektúra:** Čistý statický solver so 4 stupňami voľnosti (posun `XYZ` + kurz), zbierač
observácií, ktorý ich zahadzuje pri relokalizačnom skoku, a dve politiky nad tou istou
matematikou. Podrobne v [návrhu](2026-09-09-navrh-kalibracie-viac-znaciek.md)
a [ADR 010](decisions/010-kurz-z-poloh-znaciek-sklon-z-gravitacie.md).

**Technológie:** Unity 6000.4.11f1, C#, AR Foundation, UI Toolkit. **Žiadne testovacie
assembly** — runtime skripty žijú v predefinovanom `Assembly-CSharp`, ktorý asmdef testovacia
assembly nevie referencovať. Overuje sa editorovými menu položkami, ako `AlignmentMathVerifier`
a `TravelFilterVerifier`.

---

## Súbory

| súbor | zodpovednosť |
|---|---|
| `Assets/_Game/Scripts/Runtime/AlignmentSolver.cs` *(nový)* | čistá matematika fitu; bez scény, bez `MonoBehaviour` |
| `Assets/_Game/Editor/AlignmentSolverVerifier.cs` *(nový)* | menu `FriLens > Verify Alignment Solver` |
| `Assets/_Game/Scripts/Runtime/MarkerObservations.cs` *(nový)* | zber, brána kvality, zahadzovanie |
| `Assets/_Game/Scripts/Runtime/AlignmentConfidence.cs` *(nový)* | dôvera z dráhy, skokov, času, zvyšku |
| `Assets/_Game/Scripts/Runtime/MarkerAlignment.cs` *(mení sa)* | orchestrácia a politika |
| `Assets/_Game/Scripts/Runtime/DiagnosticsHud.cs` *(mení sa)* | logovanie a zobrazenie dôvery |
| `Assets/_Game/Editor/SceneWiring.cs` *(mení sa)* | doplniť `m_Overlays` a `m_Ceiling` |

### Ako sa spúšťa overenie

Vo všetkých úlohách je „spusti overovač" toto:

1. Klikni do okna Unity a počkaj, kým dobehne kompilácia.
2. Menu **`FriLens > Verify Alignment Solver`**.
3. Prečítaj Console. Úspech je jeden `Log` končiaci `ALL CHECKS PASSED`; zlyhanie je `LogError`
   so zoznamom `FAIL` riadkov.

---

## Úloha 1: `AlignmentSolver` — fit kurzu a posunu z polôh

**Súbory:**
- Vytvoriť: `Assets/_Game/Scripts/Runtime/AlignmentSolver.cs`
- Vytvoriť: `Assets/_Game/Editor/AlignmentSolverVerifier.cs`

- [ ] **Krok 1: Napísať padajúci overovač**

Vytvor `Assets/_Game/Editor/AlignmentSolverVerifier.cs`:

```csharp
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FriLens.EditorTools
{
    /// <summary>
    /// FriLens &gt; Verify Alignment Solver.
    ///
    /// Fit sa dá na telefóne vyskúšať až pred vytlačenou značkou, a vtedy zle spočítaný kurz
    /// vyzerá presne ako zle nalepená značka. Tieto kontroly tie dve veci oddelia: preženú
    /// aritmetiku známymi odpoveďami, takže rozpor v teréne sa dá pripísať značke, nie kódu.
    ///
    /// Menu položka, nie testovacia assembly: runtime skripty žijú v predefinovanom
    /// Assembly-CSharp, ktorý asmdef testovacia assembly nevie referencovať.
    /// </summary>
    public static class AlignmentSolverVerifier
    {
        [MenuItem("FriLens/Verify Alignment Solver")]
        public static void Run()
        {
            var report = new StringBuilder();
            int failures = 0;

            failures += RecoversAKnownTransform(report);
            failures += YawSignMatchesUnity(report);
            failures += TwoMarkersReportBaselineError(report);
            failures += RefusesFewerThanTwo(report);
            failures += CollinearMarkersStillSolve(report);
            failures += FitIsLevelByConstruction(report);

            report.AppendLine();
            report.AppendLine(failures == 0 ? "ALL CHECKS PASSED" : failures + " CHECK(S) FAILED");

            if (failures == 0)
                Debug.Log(report.ToString());
            else
                Debug.LogError(report.ToString());
        }

        /// <summary>
        /// Jadro: vezmi známu transformáciu, prežeň ňou modelové polohy, a solver ju má nájsť
        /// späť. Čísla sú realistické — kotvy break roomu sú na X −21,902 a Y 2,194.
        /// </summary>
        static int RecoversAKnownTransform(StringBuilder report)
        {
            var truthYaw = 37.5f;
            var truthRotation = Quaternion.Euler(0f, truthYaw, 0f);
            var truthPosition = new Vector3(4.2f, -1.9f, -20.6f);

            var model = new List<Vector3>
            {
                new(-21.902f, 2.194f, 5.328f),
                new(-21.902f, 2.194f, 12.121f),
                new(-18.400f, 2.050f, 13.900f),
            };

            var pairs = new List<AlignmentSolver.Correspondence>();
            foreach (var m in model)
                pairs.Add(new AlignmentSolver.Correspondence
                {
                    imageName = "m" + pairs.Count,
                    modelPosition = m,
                    measuredPosition = truthPosition + truthRotation * m,
                });

            if (!AlignmentSolver.TrySolve(pairs, out var result))
            {
                report.AppendLine("known transform: FAIL - solver refused three markers");
                return 1;
            }

            var positionError = Vector3.Distance(result.rootPose.position, truthPosition);
            var yawError = Quaternion.Angle(result.rootPose.rotation, truthRotation);

            report.AppendLine($"known transform: pos err {positionError * 1000f:F4} mm, "
                + $"yaw err {yawError:F5} deg, worst residual {result.worstResidualMeters * 1000f:F4} mm");

            int failures = 0;
            if (positionError > 1e-3f) { report.AppendLine("  FAIL position"); failures++; }
            if (yawError > 1e-2f) { report.AppendLine("  FAIL yaw"); failures++; }
            if (result.worstResidualMeters > 1e-3f) { report.AppendLine("  FAIL residual"); failures++; }
            return failures;
        }

        /// <summary>
        /// Unity je ľavotočivé a otočenie okolo Y má opačné znamienko než bežné pravotočivé
        /// vzorce. Táto kontrola je jediný dôvod, prečo sa znamienko neodvodzuje v hlave.
        /// </summary>
        static int YawSignMatchesUnity(StringBuilder report)
        {
            var truthRotation = Quaternion.Euler(0f, 90f, 0f);

            var pairs = new List<AlignmentSolver.Correspondence>
            {
                new() { imageName = "a", modelPosition = Vector3.zero,
                        measuredPosition = Vector3.zero },
                new() { imageName = "b", modelPosition = new Vector3(0f, 0f, 1f),
                        measuredPosition = truthRotation * new Vector3(0f, 0f, 1f) },
            };

            if (!AlignmentSolver.TrySolve(pairs, out var result))
            {
                report.AppendLine("yaw sign: FAIL - solver refused two markers");
                return 1;
            }

            var yawError = Quaternion.Angle(result.rootPose.rotation, truthRotation);
            report.AppendLine($"yaw sign: +90 deg recovered with {yawError:F5} deg error "
                + $"(rotated forward is {truthRotation * Vector3.forward})");

            if (yawError > 1e-2f) { report.AppendLine("  FAIL - sign is inverted"); return 1; }
            return 0;
        }

        /// <summary>
        /// Dve značky nechajú jeden zvyšok: rozdiel nameranej a modelovej dĺžky spojnice. To je
        /// chyba modelu na tej stene a je to výsledok merania, nie chyba fitu.
        /// </summary>
        static int TwoMarkersReportBaselineError(StringBuilder report)
        {
            var a = new Vector3(-21.902f, 2.194f, 5.328f);
            var b = new Vector3(-21.902f, 2.194f, 12.121f);

            // Nameraná dvojica je o 12 cm dlhšia než modelová.
            var measuredA = new Vector3(0f, 0f, 0f);
            var measuredB = new Vector3(0f, 0f, (b - a).magnitude + 0.12f);

            var pairs = new List<AlignmentSolver.Correspondence>
            {
                new() { imageName = "M1", modelPosition = a, measuredPosition = measuredA },
                new() { imageName = "M2", modelPosition = b, measuredPosition = measuredB },
            };

            if (!AlignmentSolver.TrySolve(pairs, out var result))
            {
                report.AppendLine("baseline error: FAIL - solver refused two markers");
                return 1;
            }

            report.AppendLine($"baseline error: reported {result.baselineErrorMeters * 100f:F2} cm "
                + "(expected 12.00 cm)");

            if (Mathf.Abs(result.baselineErrorMeters - 0.12f) > 1e-3f)
            {
                report.AppendLine("  FAIL");
                return 1;
            }
            return 0;
        }

        /// <summary>Z jednej polohy sa kurz určiť nedá, tak solver musí odmietnuť, nie hádať.</summary>
        static int RefusesFewerThanTwo(StringBuilder report)
        {
            var one = new List<AlignmentSolver.Correspondence>
            {
                new() { imageName = "M1", modelPosition = Vector3.one, measuredPosition = Vector3.zero },
            };

            var solvedOne = AlignmentSolver.TrySolve(one, out _);
            var solvedNone = AlignmentSolver.TrySolve(new List<AlignmentSolver.Correspondence>(), out _);

            report.AppendLine($"refusal: one marker solved={solvedOne}, zero markers solved={solvedNone} "
                + "(both must be False)");

            if (solvedOne || solvedNone) { report.AppendLine("  FAIL"); return 1; }
            return 0;
        }

        /// <summary>
        /// Tri značky na jednej stene ležia v priamke. Kurz a posun sú tým stále určené —
        /// nedourčený ostáva len sklon, ktorý sa nefituje. Solver teda nesmie zlyhať.
        /// </summary>
        static int CollinearMarkersStillSolve(StringBuilder report)
        {
            var truthRotation = Quaternion.Euler(0f, -22f, 0f);
            var truthPosition = new Vector3(1f, 2f, 3f);

            var pairs = new List<AlignmentSolver.Correspondence>();
            for (int i = 0; i < 3; i++)
            {
                var m = new Vector3(-21.902f, 2.194f, 5.328f + 3.4f * i);
                pairs.Add(new AlignmentSolver.Correspondence
                {
                    imageName = "m" + i,
                    modelPosition = m,
                    measuredPosition = truthPosition + truthRotation * m,
                });
            }

            if (!AlignmentSolver.TrySolve(pairs, out var result))
            {
                report.AppendLine("collinear: FAIL - solver refused three collinear markers");
                return 1;
            }

            var yawError = Quaternion.Angle(result.rootPose.rotation, truthRotation);
            report.AppendLine($"collinear: yaw err {yawError:F5} deg");

            if (yawError > 1e-2f) { report.AppendLine("  FAIL"); return 1; }
            return 0;
        }

        /// <summary>
        /// Fit má len kurz, takže výsledok je vzpriamený bez toho, aby ho niekto staval.
        /// Kontroluje sa, lebo keby sa do rotácie niekedy dostal sklon, prejavilo by sa to
        /// ako svah v prekryve a hľadalo by sa to zle.
        /// </summary>
        static int FitIsLevelByConstruction(StringBuilder report)
        {
            var pairs = new List<AlignmentSolver.Correspondence>
            {
                new() { imageName = "a", modelPosition = new Vector3(0f, 0f, 0f),
                        measuredPosition = new Vector3(0f, 0.31f, 0f) },
                new() { imageName = "b", modelPosition = new Vector3(0f, 0f, 6.793f),
                        measuredPosition = new Vector3(2.1f, 0.29f, 6.4f) },
            };

            if (!AlignmentSolver.TrySolve(pairs, out var result))
            {
                report.AppendLine("level: FAIL - solver refused two markers");
                return 1;
            }

            var tilt = Vector3.Angle(result.rootPose.rotation * Vector3.up, Vector3.up);
            report.AppendLine($"level: fitted up is {tilt:F5} deg off vertical");

            if (tilt > 1e-3f) { report.AppendLine("  FAIL - fit introduced tilt"); return 1; }
            return 0;
        }
    }
}
```

- [ ] **Krok 2: Spustiť overovač a overiť, že padá**

Spusti `FriLens > Verify Alignment Solver`.

Očakávané: **kompilácia neprejde**, Console hlási `CS0103` / `The name 'AlignmentSolver' does
not exist`. To je správne zlyhanie — menu položka sa ani neobjaví, kým sa typ nevytvorí.

- [ ] **Krok 3: Napísať `AlignmentSolver`**

Vytvor `Assets/_Game/Scripts/Runtime/AlignmentSolver.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FriLens
{
    /// <summary>
    /// Spočíta pózu modelu z polôh značiek. Natočenie značiek sa nepoužije vôbec — dôvody
    /// v ADR 010.
    ///
    /// Hľadá tuhú transformáciu so štyrmi stupňami voľnosti: posun XYZ a kurz okolo zvislice.
    /// Sklon sa nefituje, takže výsledok je vzpriamený z konštrukcie a gravitácia sa naň
    /// nemusí aplikovať dodatočne.
    ///
    /// Mierka sa nefituje zámerne. Keby áno, model by sa natiahol tak, aby rozdiel medzi
    /// nameranou a modelovou vzdialenosťou značiek pohltil — a ten rozdiel je práve to, čo
    /// projekt meria. Vracia sa preto ako <see cref="Result.baselineErrorMeters"/>.
    ///
    /// Čistá matematika bez scény, aby sa dala overiť bez telefónu cez
    /// FriLens &gt; Verify Alignment Solver.
    /// </summary>
    public static class AlignmentSolver
    {
        /// <summary>Jedna značka: kde ju čaká model a kde ju ohlásil tracker.</summary>
        public struct Correspondence
        {
            /// <summary>Meno v knižnici referenčných obrázkov, kvôli hláseniu zvyškov.</summary>
            public string imageName;

            /// <summary>Poloha kotvy v súradniciach modelu, teda lokálne voči rootu.</summary>
            public Vector3 modelPosition;

            /// <summary>Poloha nameraná trackerom, v session space.</summary>
            public Vector3 measuredPosition;
        }

        public struct Result
        {
            /// <summary>Póza, ktorú má dostať AlignmentRoot.</summary>
            public Pose rootPose;

            /// <summary>Z koľkých značiek fit vznikol.</summary>
            public int markerCount;

            /// <summary>Najväčší zvyšok cez všetky značky, v metroch.</summary>
            public float worstResidualMeters;

            /// <summary>Meno značky, na ktorej ten najväčší zvyšok je.</summary>
            public string worstResidualImage;

            /// <summary>
            /// Pri presne dvoch značkách rozdiel nameranej a modelovej dĺžky ich spojnice,
            /// kladný keď je nameraná dlhšia. Inak 0 — pri troch a viac to jedno číslo
            /// neexistuje a hovoria za to zvyšky.
            /// </summary>
            public float baselineErrorMeters;
        }

        /// <summary>
        /// Vráti false, keď je značiek menej než dve. Z jednej polohy sa kurz určiť nedá
        /// a hádať ho je horšie než to priznať; volajúci má v tom prípade použiť
        /// <see cref="MarkerAlignment.SolveRootPose"/> s natočením tej jednej značky.
        /// </summary>
        public static bool TrySolve(IReadOnlyList<Correspondence> pairs, out Result result)
        {
            result = default;
            if (pairs == null || pairs.Count < 2)
                return false;

            var modelCentre = Vector3.zero;
            var measuredCentre = Vector3.zero;
            foreach (var pair in pairs)
            {
                modelCentre += pair.modelPosition;
                measuredCentre += pair.measuredPosition;
            }
            modelCentre /= pairs.Count;
            measuredCentre /= pairs.Count;

            // Kurz z vodorovných zložiek vycentrovaných bodov. Otočenie okolo Y v Unity mapuje
            // (x, z) na (x cos + z sin, -x sin + z cos), z čoho maximalizovaný člen vyjde
            // C cos + S sin a optimum je atan2(S, C). Znamienko overuje YawSignMatchesUnity.
            float s = 0f;
            float c = 0f;
            foreach (var pair in pairs)
            {
                var a = pair.modelPosition - modelCentre;
                var o = pair.measuredPosition - measuredCentre;
                s += o.x * a.z - o.z * a.x;
                c += a.x * o.x + a.z * o.z;
            }

            // Všetky značky v jednom bode: kurz je nedourčený. Nula je aspoň predvídateľná.
            var yaw = (Mathf.Abs(s) < 1e-9f && Mathf.Abs(c) < 1e-9f)
                ? 0f
                : Mathf.Atan2(s, c) * Mathf.Rad2Deg;

            var rotation = Quaternion.Euler(0f, yaw, 0f);
            var position = measuredCentre - rotation * modelCentre;

            result.rootPose = new Pose(position, rotation);
            result.markerCount = pairs.Count;

            foreach (var pair in pairs)
            {
                var landed = position + rotation * pair.modelPosition;
                var residual = Vector3.Distance(landed, pair.measuredPosition);
                if (residual > result.worstResidualMeters)
                {
                    result.worstResidualMeters = residual;
                    result.worstResidualImage = pair.imageName;
                }
            }

            if (pairs.Count == 2)
            {
                var modelSpan = Vector3.Distance(pairs[0].modelPosition, pairs[1].modelPosition);
                var measuredSpan = Vector3.Distance(pairs[0].measuredPosition, pairs[1].measuredPosition);
                result.baselineErrorMeters = measuredSpan - modelSpan;
            }

            return true;
        }
    }
}
```

- [ ] **Krok 4: Spustiť overovač a overiť, že prejde**

Spusti `FriLens > Verify Alignment Solver`.

Očakávané: jeden `Log` končiaci `ALL CHECKS PASSED`, šesť riadkov s číslami.

Ak padne `yaw sign`, znamienko je obrátené — vymeň `s += o.x * a.z - o.z * a.x;`
za `s += o.z * a.x - o.x * a.z;` a spusti znova. **Nemeň nič iné**; ostatné kontroly by
inak zamaskovali, ktorá zmena pomohla.

- [ ] **Krok 5: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/AlignmentSolver.cs Assets/_Game/Editor/AlignmentSolverVerifier.cs
git commit -m "feat(align): solver kurzu a posunu z polôh značiek"
```

---

## Úloha 2: `MarkerObservations` — zber a brána kvality

**Súbory:**
- Vytvoriť: `Assets/_Game/Scripts/Runtime/MarkerObservations.cs`
- Zmeniť: `Assets/_Game/Editor/AlignmentSolverVerifier.cs` (pridať kontroly)

- [ ] **Krok 1: Napísať padajúce kontroly**

V `AlignmentSolverVerifier.Run()` pridaj za existujúce volania:

```csharp
            failures += ObservationsKeepOnlyTheNewestPerMarker(report);
            failures += ObservationsRejectWideSpread(report);
            failures += ObservationsDropOnJump(report);
            failures += ObservationsDropWhenStale(report);
```

A na koniec triedy pridaj:

```csharp
        /// <summary>Druhé pozorovanie tej istej značky prepíše prvé, nie pridá vedľa neho.</summary>
        static int ObservationsKeepOnlyTheNewestPerMarker(StringBuilder report)
        {
            var store = new MarkerObservations();
            store.Offer("M1", new Vector3(1f, 0f, 0f), 0.001f, segment: 0, atTime: 0f);
            store.Offer("M1", new Vector3(2f, 0f, 0f), 0.001f, segment: 0, atTime: 1f);

            var kept = store.Current(segment: 0, now: 1f, maxAgeSeconds: 60f);
            report.AppendLine($"newest wins: {kept.Count} observation(s), "
                + $"x = {(kept.Count > 0 ? kept[0].measuredPosition.x : float.NaN)} (expected 1 and 2)");

            if (kept.Count != 1 || Mathf.Abs(kept[0].measuredPosition.x - 2f) > 1e-4f)
            {
                report.AppendLine("  FAIL");
                return 1;
            }
            return 0;
        }

        /// <summary>Burst s rozhádzanou polohou sa neprijme; je to meranie šumu, nie značky.</summary>
        static int ObservationsRejectWideSpread(StringBuilder report)
        {
            var store = new MarkerObservations { MaxSpreadMeters = 0.02f };
            var accepted = store.Offer("M1", Vector3.zero, spreadMeters: 0.05f, segment: 0, atTime: 0f);

            report.AppendLine($"spread gate: 5 cm spread accepted={accepted} (must be False)");

            if (accepted) { report.AppendLine("  FAIL"); return 1; }
            return 0;
        }

        /// <summary>
        /// Pozorovania spred a spoza relokalizačného skoku sú v dvoch rôznych mapách. Fit cez
        /// ne dá pózu, ktorá nepatrí ani jednej — beh 134644 mal skok 4,818 m.
        /// </summary>
        static int ObservationsDropOnJump(StringBuilder report)
        {
            var store = new MarkerObservations();
            store.Offer("M1", Vector3.zero, 0.001f, segment: 0, atTime: 0f);

            var afterJump = store.Current(segment: 1, now: 1f, maxAgeSeconds: 60f);
            report.AppendLine($"jump gate: {afterJump.Count} observation(s) survived a jump (expected 0)");

            if (afterJump.Count != 0) { report.AppendLine("  FAIL"); return 1; }
            return 0;
        }

        /// <summary>Staré pozorovanie je z inej chvíle driftu a mieša sa zle s čerstvým.</summary>
        static int ObservationsDropWhenStale(StringBuilder report)
        {
            var store = new MarkerObservations();
            store.Offer("M1", Vector3.zero, 0.001f, segment: 0, atTime: 0f);

            var fresh = store.Current(segment: 0, now: 30f, maxAgeSeconds: 60f);
            var stale = store.Current(segment: 0, now: 90f, maxAgeSeconds: 60f);

            report.AppendLine($"age gate: at 30 s {fresh.Count} kept, at 90 s {stale.Count} kept "
                + "(expected 1 and 0)");

            if (fresh.Count != 1 || stale.Count != 0) { report.AppendLine("  FAIL"); return 1; }
            return 0;
        }
```

- [ ] **Krok 2: Spustiť overovač a overiť, že padá**

Spusti `FriLens > Verify Alignment Solver`.

Očakávané: kompilácia neprejde, `The name 'MarkerObservations' does not exist`.

- [ ] **Krok 3: Napísať `MarkerObservations`**

Vytvor `Assets/_Game/Scripts/Runtime/MarkerObservations.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FriLens
{
    /// <summary>
    /// Drží poslednú prijatú observáciu každej značky a zahadzuje tie, ktoré prestali platiť.
    ///
    /// Zámerne bez MonoBehaviour a bez Time.time: čas aj číslo úseku trackingu sa podávajú
    /// zvonku, takže sa celá trieda dá overiť bez scény a bez telefónu.
    ///
    /// Najdôležitejšie je zahadzovanie pri relokalizačnom skoku. Pozorovania spred a spoza
    /// skoku sú v dvoch rôznych mapách a fit cez ne dá pózu, ktorá nepatrí ani jednej.
    /// </summary>
    public class MarkerObservations
    {
        public struct Observation
        {
            public string imageName;
            public Vector3 measuredPosition;
            public float spreadMeters;
            public int segment;
            public float time;
        }

        /// <summary>Nad týmto rozptylom polohy sa burst neprijme. Namerané býva 0,1 – 1,2 cm.</summary>
        public float MaxSpreadMeters { get; set; } = 0.02f;

        readonly Dictionary<string, Observation> m_ByImage = new();

        /// <summary>Koľko značiek je práve v zásobe, bez ohľadu na platnosť.</summary>
        public int Count => m_ByImage.Count;

        /// <summary>
        /// Ponúkne observáciu. Vráti, či sa prijala. Prijatá prepíše staršiu tej istej značky.
        /// </summary>
        public bool Offer(string imageName, Vector3 measuredPosition, float spreadMeters,
            int segment, float atTime)
        {
            if (string.IsNullOrEmpty(imageName))
                return false;

            if (spreadMeters > MaxSpreadMeters)
                return false;

            m_ByImage[imageName] = new Observation
            {
                imageName = imageName,
                measuredPosition = measuredPosition,
                spreadMeters = spreadMeters,
                segment = segment,
                time = atTime,
            };
            return true;
        }

        /// <summary>
        /// Observácie, ktoré sa smú použiť: z aktuálneho úseku trackingu a nie staršie než
        /// <paramref name="maxAgeSeconds"/>.
        ///
        /// Vracia vlastný zoznam pri každom volaní. Zdieľaná vyrovnávacia pamäť by ušetrila
        /// alokáciu, ale metóda beží pri dokončení burstu, nie po snímkoch, takže niet čo
        /// šetriť — a dva výsledky držané naraz by sa ticho prepísali. Presne o to sa porezala
        /// prvá kontrola, ktorá ju použila.
        /// </summary>
        public List<Observation> Current(int segment, float now, float maxAgeSeconds)
        {
            var usable = new List<Observation>();
            foreach (var observation in m_ByImage.Values)
            {
                if (observation.segment != segment)
                    continue;
                if (now - observation.time > maxAgeSeconds)
                    continue;
                usable.Add(observation);
            }
            return usable;
        }

        /// <summary>Zabudne všetko. Volá sa pri strate trackingu.</summary>
        public void Clear() => m_ByImage.Clear();
    }
}
```

- [ ] **Krok 4: Spustiť overovač a overiť, že prejde**

Spusti `FriLens > Verify Alignment Solver`. Očakávané: `ALL CHECKS PASSED`, desať riadkov.

- [ ] **Krok 5: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/MarkerObservations.cs Assets/_Game/Editor/AlignmentSolverVerifier.cs
git commit -m "feat(align): zber observácií so zahadzovaním pri skoku a veku"
```

---

## Úloha 3: Zapojiť solver do `MarkerAlignment`

**Súbory:**
- Zmeniť: `Assets/_Game/Scripts/Runtime/MarkerAlignment.cs`

- [ ] **Krok 1: Pridať polia a závislosti**

Za `[SerializeField] bool m_LevelWithGravity = true;` pridaj:

```csharp
        [Tooltip("Zdroj čísla úseku trackingu a prejdenej dráhy. Bez neho sa observácie "
            + "nezahodia pri relokalizačnom skoku a fit môže miešať dve mapy.")]
        [SerializeField] CameraTravel m_Travel;

        [Tooltip("Sekundy, po ktorých sa observácia značky prestane počítať do fitu.")]
        [SerializeField] float m_ObservationMaxAgeSeconds = 60f;

        [Tooltip("Najväčší rozptyl polohy v burste, ktorý sa ešte prijme, v metroch.")]
        [SerializeField] float m_MaxPositionSpreadMeters = 0.02f;

        readonly MarkerObservations m_Observations = new();
```

- [ ] **Krok 2: Vystaviť výsledok fitu pre HUD a log**

Za `public float LevelledDegrees { get; private set; }` pridaj:

```csharp
        /// <summary>Z koľkých značiek vznikol posledný fit. 1 znamená núdzový režim z natočenia.</summary>
        public int FitMarkerCount { get; private set; }

        /// <summary>Najväčší zvyšok posledného fitu v metroch, alebo -1 keď sústava nebola preurčená.</summary>
        public float FitWorstResidualMeters { get; private set; } = -1f;

        /// <summary>Pri presne dvoch značkách rozdiel nameranej a modelovej dĺžky spojnice.</summary>
        public float FitBaselineErrorMeters { get; private set; }
```

- [ ] **Krok 3: Uložiť observáciu pri každom hotovom burste**

V `ApplyAlignment()`, hneď za riadok `var rotation = AverageRotation(m_Rotations);`, pridaj:

```csharp
            // Burst dobehol celý v stave Tracking, takže je to platné pozorovanie bez ohľadu
            // na to, či sa z neho hneď bude riešiť. Rozptyl polohy rozhodne, či sa prijme.
            var spread = 0f;
            foreach (var sample in m_Positions)
                spread = Mathf.Max(spread, Vector3.Distance(sample, position));

            m_Observations.MaxSpreadMeters = m_MaxPositionSpreadMeters;
            m_Observations.Offer(m_BurstImageName, position, spread,
                m_Travel != null ? m_Travel.RelocalisationJumps : 0, Time.time);
```

- [ ] **Krok 4: Riešiť z viacerých značiek, keď sú**

Nahraď blok od `SolveRootPose(position, rotation, ...)` po priradenie `LastRootPose` týmto:

```csharp
            var segment = m_Travel != null ? m_Travel.RelocalisationJumps : 0;
            var usable = m_Observations.Current(segment, Time.time, m_ObservationMaxAgeSeconds);

            var pairs = new List<AlignmentSolver.Correspondence>();
            foreach (var observation in usable)
            {
                var surveyed = AnchorNamed(observation.imageName);
                if (surveyed == null)
                    continue;

                pairs.Add(new AlignmentSolver.Correspondence
                {
                    imageName = observation.imageName,
                    modelPosition = m_AlignmentRoot.InverseTransformPoint(surveyed.position),
                    measuredPosition = observation.measuredPosition,
                });
            }

            Vector3 rootPosition;
            Quaternion rootRotation;

            if (AlignmentSolver.TrySolve(pairs, out var fit))
            {
                // Fit má len kurz, takže je vzpriamený z konštrukcie a gravitácia sa naň
                // neaplikuje — nie je čo stavať.
                rootPosition = fit.rootPose.position;
                rootRotation = fit.rootPose.rotation;

                FitMarkerCount = fit.markerCount;
                FitWorstResidualMeters = fit.markerCount > 2 ? fit.worstResidualMeters : -1f;
                FitBaselineErrorMeters = fit.baselineErrorMeters;
                LevelledDegrees = 0f;
            }
            else
            {
                // Jedna značka: kurz sa z jednej polohy určiť nedá, tak sa vezme z jej
                // natočenia ako pred ADR 010 a postaví sa gravitáciou.
                SolveRootPose(position, rotation, anchorLocalPosition, anchorLocalRotation,
                    out rootPosition, out rootRotation);

                LevelledDegrees = m_LevelWithGravity
                    ? LevelRootPose(position, anchorLocalPosition, ref rootPosition, ref rootRotation)
                    : 0f;

                FitMarkerCount = 1;
                FitWorstResidualMeters = -1f;
                FitBaselineErrorMeters = 0f;
            }

            LastMeasuredPose = new Pose(position, rotation);
            LastRootPose = new Pose(rootPosition, rootRotation);
```

- [ ] **Krok 5: Pridať pomocnú metódu na kotvu podľa mena**

Za metódu `AnchorFor(ARTrackedImage image)` pridaj:

```csharp
        /// <summary>
        /// Zameraná kotva podľa mena obrázka. Oproti <see cref="AnchorFor"/> nepotrebuje
        /// sledovaný obrázok, lebo fit pracuje s uloženými pozorovaniami, nie s tým, čo je
        /// práve v zábere.
        /// </summary>
        Transform AnchorNamed(string imageName)
        {
            foreach (var marker in m_Markers)
                if (marker.anchor != null && marker.imageName == imageName)
                    return marker.anchor;

            return null;
        }
```

- [ ] **Krok 6: Zahodiť observácie pri strate trackingu**

Do `Realign()` na začiatok tela **nepridávaj nič** — prezarovnanie má observácie využiť, nie
zahodiť.

`MarkerAlignment` nemá `OnEnable` ani `OnDisable`, len `Awake()` a `Update()`. Odber sa preto
zapíše na koniec `Awake()`:

```csharp
            if (m_Continuity != null)
                m_Continuity.Lost += OnTrackingLost;
```

a odhlási v novom `OnDestroy()`:

```csharp
        void OnDestroy()
        {
            if (m_Continuity != null)
                m_Continuity.Lost -= OnTrackingLost;
        }
```

a pridaj metódu:

```csharp
        /// <summary>
        /// Po strate trackingu je poloha každej uloženej značky odhad z mapy, ktorá sa medzitým
        /// mohla prekresliť. Zahodiť ich je lacnejšie než fit cez pomiešané súradnice.
        /// </summary>
        void OnTrackingLost(UnityEngine.XR.ARSubsystems.NotTrackingReason reason)
        {
            m_Observations.Clear();
        }
```

Ak `MarkerAlignment` ešte nemá referenciu na `TrackingContinuity`, pridaj ju medzi
serializované polia v sekcii `[Header("Scene")]`:

```csharp
        [Tooltip("Zdroj udalosti o strate trackingu. Bez neho sa observácie po strate nezahodia.")]
        [SerializeField] TrackingContinuity m_Continuity;
```

a v `OnDisable()` odhlás:

```csharp
            if (m_Continuity != null)
                m_Continuity.Lost -= OnTrackingLost;
```

- [ ] **Krok 7: Overiť kompiláciu**

Klikni do Unity, počkaj na kompiláciu. Console nesmie hlásiť žiadny `CS`.
Spusti `FriLens > Verify Alignment Solver` — musí stále hlásiť `ALL CHECKS PASSED`.

- [ ] **Krok 8: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/MarkerAlignment.cs
git commit -m "feat(align): zarovnať z polôh všetkých videných značiek"
```

---

## Úloha 4: Dva režimy

**Súbory:**
- Zmeniť: `Assets/_Game/Scripts/Runtime/MarkerAlignment.cs`

- [ ] **Krok 1: Pridať prepínač režimu**

Za `[SerializeField] bool m_AlignOnFirstSighting = true;` pridaj:

```csharp
        /// <summary>Kedy sa zarovnanie prepočítava.</summary>
        public enum UpdatePolicy
        {
            /// <summary>Len na Re-anchor. Prekryv sa medzi stlačeniami nehýbe, takže drift
            /// je vidieť ako útek — to je meranie, kvôli ktorému projekt vznikol.</summary>
            OnRequest,

            /// <summary>Pri každom novom prijatom pozorovaní. Prekryv sa v miestnosti drží
            /// sám; veľkosť každej opravy sa zapíše, takže drift sa číta ako veľkosť fixu.</summary>
            Continuous,
        }

        [Tooltip("OnRequest je merací režim a je predvolený, aby sa behy dali porovnávať so "
            + "staršími. Continuous je navigačný.")]
        [SerializeField] UpdatePolicy m_Policy = UpdatePolicy.OnRequest;

        [Tooltip("Najkratší odstup medzi dvomi automatickými zarovnaniami v navigačnom režime. "
            + "Bez neho by burst štartoval znova hneď po dobehnutí a log by sa zaplnil.")]
        [SerializeField] float m_ContinuousIntervalSeconds = 2f;

        /// <summary>Aktuálna politika; prepína ju HUD.</summary>
        public UpdatePolicy Policy
        {
            get => m_Policy;
            set => m_Policy = value;
        }

        /// <summary>O koľko metrov posunul prekryv posledný automatický fix, alebo 0.</summary>
        public float LastCorrectionMeters { get; private set; }
```

- [ ] **Krok 2: Merať veľkosť opravy a v navigačnom režime sa prezarovnávať sám**

V `ApplyAlignment()`, tesne pred `if (m_Anchored != null)`, pridaj:

```csharp
            // Veľkosť opravy je drift nazbieraný od minulého fitu, odčítaný ako číslo namiesto
            // odhadu okom. V meracom režime je to skok, ktorý si niekto vyžiadal; v navigačnom
            // je to samotný výsledok merania.
            LastCorrectionMeters = LastAlignmentTime < 0f
                ? 0f
                : Vector3.Distance(m_AlignmentRoot.position, rootPosition);
```

Na koniec `Update()` (alebo tam, kde sa vyhodnocuje stav) pridaj:

```csharp
            // V navigačnom režime nikto nič netlačí: kým je značka v zábere, prekryv sa opravuje
            // sám. Odstup je tam preto, že burst dobehne za sekundu a bez neho by hneď štartoval
            // ďalší — log by sa zaplnil a prekryv by sa neustále prepisoval.
            //
            // Pozor: keď je v zábere len jedna značka, fit spadne na jej natočenie a zdedí jeho
            // šum. Navigačný režim je preto použiteľný až tam, kde vidieť dve.
            if (m_Policy == UpdatePolicy.Continuous
                && State == AlignmentState.Aligned
                && TrackedMarker != null
                && TrackedMarker.trackingState == TrackingState.Tracking
                && Time.time - LastAlignmentTime >= m_ContinuousIntervalSeconds)
            {
                Realign();
            }
```

Blok patrí **pred** riadok `if (State != AlignmentState.Sampling) return;`, inak sa naň
nikdy nedostane.

- [ ] **Krok 3: Overiť kompiláciu**

Klikni do Unity. Console bez `CS`. `FriLens > Verify Alignment Solver` stále `ALL CHECKS PASSED`.

- [ ] **Krok 4: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/MarkerAlignment.cs
git commit -m "feat(align): merací a navigačný režim nad tou istou matematikou"
```

---

## Úloha 5: `AlignmentConfidence` a zošednutie prekryvu

**Súbory:**
- Vytvoriť: `Assets/_Game/Scripts/Runtime/AlignmentConfidence.cs`
- Zmeniť: `Assets/_Game/Editor/SceneWiring.cs`

- [ ] **Krok 1: Napísať `AlignmentConfidence`**

Vytvor `Assets/_Game/Scripts/Runtime/AlignmentConfidence.cs`:

```csharp
using UnityEngine;

namespace FriLens
{
    /// <summary>
    /// Odhaduje, nakoľko sa dá prekryvu ešte veriť.
    ///
    /// Aplikácia dnes ukazuje prekryv rovnako sebavedome sekundu po zarovnaní aj po 46 metroch
    /// chôdze a relokalizačnom skoku 4,8 m. Pritom o oboje vie. Toto z tých čísel spraví jedno.
    ///
    /// Prekryv sa nikdy neskrýva. Zmiznutie by v teréne vyzeralo ako pád aplikácie a mlčky by
    /// zobralo možnosť pozrieť sa, ako veľmi je vedľa — čo je pri meraní tá zaujímavá informácia.
    /// </summary>
    public class AlignmentConfidence : MonoBehaviour
    {
        [SerializeField] MarkerAlignment m_Alignment;
        [SerializeField] CameraTravel m_Travel;

        [Tooltip("Metre prejdené od zarovnania, po ktorých už prekryv nemá dôveru. Baseline "
            + "merania dávajú drift rádovo jednotky percent dráhy, takže 30 m je asi meter.")]
        [SerializeField] float m_DistrustAfterMeters = 30f;

        [Tooltip("Relokalizačný skok posunie celú mapu pod prekryvom naraz. Jeden stačí na to, "
            + "aby zarovnanie prestalo platiť.")]
        [SerializeField] int m_DistrustAfterJumps = 1;

        /// <summary>1 tesne po zarovnaní, 0 keď mu už netreba veriť.</summary>
        public float Trust { get; private set; } = 1f;

        /// <summary>Prečo dôvera klesla, na jeden riadok do HUD-u.</summary>
        public string Reason { get; private set; } = "";

        void Update()
        {
            if (m_Alignment == null || m_Alignment.LastAlignmentTime < 0f)
            {
                Trust = 0f;
                Reason = "nezarovnané";
                return;
            }

            var walked = m_Travel != null ? m_Travel.DistanceWalked : 0f;
            var jumps = m_Travel != null ? m_Travel.RelocalisationJumps : 0;

            var byDistance = m_DistrustAfterMeters <= 0f
                ? 1f
                : Mathf.Clamp01(1f - walked / m_DistrustAfterMeters);

            var byJumps = jumps >= m_DistrustAfterJumps ? 0f : 1f;

            Trust = Mathf.Min(byDistance, byJumps);

            if (jumps >= m_DistrustAfterJumps)
                Reason = jumps + "× skok mapy";
            else if (byDistance < 1f)
                Reason = walked.ToString("F0") + " m od zarovnania";
            else
                Reason = "";
        }
    }
}
```

- [ ] **Krok 2: Doplniť wiring prekryvov do `SceneWiring`**

`DiagnosticsHud` má dnes v scéne prázdne `m_Overlays` a `m_Ceiling = null`, takže tlačidlá
`Hide overlay` a `strop` menia len svoj vzhľad. Bez toho nie je čím zošednutie spraviť.

`SceneWiring` má `Set(Object target, string field, Object value)` na jednu referenciu
a `SetFloat`, ale nič na pole. Pridaj k nim tretí pomocník v tom istom štýle — cez
`SerializedObject`, nie cez reflexiu:

```csharp
        /// <summary>
        /// Pole referencií. Rovnaký postup ako <see cref="Set"/>, len sa najprv nastaví dĺžka;
        /// SerializedProperty inak drží starý počet prvkov a priradenie by ticho vypadlo.
        /// </summary>
        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning($"SceneWiring: {target.name} nemá pole {field}.", target);
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            so.ApplyModifiedProperties();
        }
```

Potom do metódy, ktorá zapája `DiagnosticsHud`, pridaj:

```csharp
            var nav = GameObject.Find("NavOverlay");
            var walls = GameObject.Find("WallOverlay");
            var ceiling = GameObject.Find("CeilingOverlay");

            // Strop zámerne nie je medzi prekryvmi: má vlastné tlačidlo, lebo je jediná časť,
            // ktorá dokáže zakryť celú obrazovku.
            SetArray(hud, "m_Overlays", new Object[]
            {
                nav != null ? nav.GetComponent<Renderer>() : null,
                walls != null ? walls.GetComponent<Renderer>() : null,
            });

            Set(hud, "m_Ceiling", ceiling != null ? ceiling.GetComponent<Renderer>() : null);
```

- [ ] **Krok 3: Spustiť wiring a overiť v inšpektore**

Spusti `FriLens > Wire Scene`. V inšpektore objektu `HUD` musí `Diagnostics Hud` mať
`Overlays` s dvomi položkami (`NavOverlay`, `WallOverlay`) a `Ceiling` nastavené na
`CeilingOverlay`.

- [ ] **Krok 4: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/AlignmentConfidence.cs Assets/_Game/Editor/SceneWiring.cs Assets/_Game/Scenes/FriLensTest.unity
git commit -m "feat(hud): ukazovateľ dôvery zarovnania a zapojenie prekryvov"
```

---

## Úloha 6: Logovanie fitu

**Súbory:**
- Zmeniť: `Assets/_Game/Scripts/Runtime/DiagnosticsHud.cs`

- [ ] **Krok 1: Rozšíriť udalosť zarovnania**

V `OnAligned()` nahraď riadok začínajúci `+ "; levelled "` týmto:

```csharp
                    + "; levelled " + Number(m_Alignment.LevelledDegrees) + " deg"

                    // Z koľkých značiek fit vznikol, aké veľké sú jeho zvyšky a o koľko posunul
                    // prekryv. Ten posun je drift nazbieraný od minulého fitu — v navigačnom
                    // režime je to jediné miesto, kde sa drift dá odčítať.
                    + "; markers " + m_Alignment.FitMarkerCount
                    + "; residual " + Number(m_Alignment.FitWorstResidualMeters)
                    + "; baseline " + Number(m_Alignment.FitBaselineErrorMeters)
                    + "; correction " + Number(m_Alignment.LastCorrectionMeters);
```

A vedľa `Axis` pridaj pomocník na jedno číslo, nech sa `ToString` s kultúrou neopakuje
päťkrát:

```csharp
        /// <summary>
        /// Jedno číslo do menovky udalosti. Rovnaký dôvod ako <see cref="Axis"/>: telefón píše
        /// desatinnú čiarku a log je CSV.
        /// </summary>
        static string Number(float value) =>
            value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
```

- [ ] **Krok 2: Overiť kompiláciu**

Klikni do Unity. Console bez `CS`.

- [ ] **Krok 3: Commit**

```bash
git add Assets/_Game/Scripts/Runtime/DiagnosticsHud.cs
git commit -m "feat(log): zapísať počet značiek, zvyšky a veľkosť opravy"
```

---

## Úloha 7: Build a terénne overenie

**Súbory:**
- Zmeniť: `Assets/_Game/Editor/AndroidBuilder.cs`
- Zmeniť: `CHANGELOG.md`

- [ ] **Krok 1: Zdvihnúť verziu**

V `AndroidBuilder.cs` zdvihni `Version` na `"0.3.0-alpha"` a `VersionCode` na `13`.
Menšia číslica by klamala: mení sa spôsob zarovnania, nie jeho detail.

- [ ] **Krok 2: Dopísať do `CHANGELOG.md`**

Novú sekciu `## [0.3.0-alpha] — nevydané` s tým, čo úlohy 1–6 spravili, a odkazom na
[návrh](2026-09-09-navrh-kalibracie-viac-znaciek.md) a [ADR 010](decisions/010-kurz-z-poloh-znaciek-sklon-z-gravitacie.md).

- [ ] **Krok 3: Build**

Klikni do Unity, počkaj na kompiláciu. Menu musí hlásiť `Build Android 0.3.0-alpha`.
Spusti tú položku.

- [ ] **Krok 4: Terénne overenie v break roome**

Tretiu značku netreba — dve stačia na to, aby sa zmena prejavila.

1. Prepni na merací režim. Postav sa k M1, `Re-anchor`, potom prejdi pohľadom na M2 a znova.
   HUD má hlásiť `markers 2`.
2. **Prezarovnaj desaťkrát zo stojaceho miesta.** Toto je hlavná kontrola: v behu `134644`
   sa prekryv pri tomto rozhádzal o 7,36 m a kurz o 11,1°. Po zmene má stáť.
3. Prepni cieľ z M1 na M2 a späť. Skok, ktorý bol 2,04 – 2,89 m, má výrazne klesnúť.
4. Prepni na navigačný režim, prejdi sa po miestnosti a späť. Prekryv sa má opraviť sám,
   bez stlačenia čohokoľvek.
5. Odíď z miestnosti a vráť sa. Prekryv má cestou zošednúť a po návrate sa opraviť.

- [ ] **Krok 5: Prečítať log**

V udalostiach `aligned on …` skontroluj:

| pole | čo má byť |
|---|---|
| `markers` | `2`, keď boli obe v zábere |
| `baseline` | rozdiel dĺžok v metroch — **to je chyba modelu na 6,79 m steny** |
| `correction` | pri desiatich zarovnaniach zo stojaceho miesta rádovo centimetre, nie metre |
| `levelled` | `0.00` pri fite z dvoch značiek; fit je vzpriamený z konštrukcie |

- [ ] **Krok 6: Commit a vydanie**

```bash
git add Assets/_Game/Editor/AndroidBuilder.cs CHANGELOG.md ProjectSettings/ProjectSettings.asset
git commit -m "chore(build): 0.3.0-alpha, versionCode 13"
git push origin main
```

Potom vydanie na GitHub Releases a bump `src/content/frilens.ts` vo `friworld-web` podľa
postupu v [`docs/README.md`](README.md).

---

## Po review: čo sa oproti tomuto plánu zmenilo

Plán je záznam toho, ako sa to stavalo, a kód sa od neho po audite v štyroch veciach odchýlil.
Kto číta úlohy 1–6 ako predlohu, má vedieť o týchto opravách — inak ich zavedie znova.

- **Číslo úseku nie je `RelocalisationJumps`.** To sa pri každom zarovnaní nuluje
  (`CameraTravel.RestartFrom`, volané z `DiagnosticsHud.OnAligned`), takže identifikátor
  osciloval a observácie mizli aj bez skoku. Pribudlo `CameraTravel.JumpGeneration`, ktoré
  rastie monotónne a nikdy sa nenuluje.

- **Fit sa zamieta, keď mu model odporuje.** `AlignmentSolver` hlási `modelSpanMeters`
  a `MarkerAlignment.FitLooksSane` zamietne fit, ktorého chyba presiahne `m_MaxFitErrorFraction`
  (0,25) rozpätia značiek. Bez toho hrubo zlé čítanie polohy — a ARCore vie tú istú značku
  ohlásiť o metre inde — vyrobilo sebavedomý dvojznačkový fit s kurzom o desiatky stupňov vedľa.

- **Zvyšok sa hlási aj pri dvoch značkách.** Sústava je aj tam preurčená a rozdiel dĺžok sa
  medzi značky rozdelí; overovač to potvrdzuje číslom 6,00 cm pri 12 cm chybe základne.
  Pôvodná podmienka `markerCount > 2` tú informáciu zahadzovala.

- **Stlmenie prekryvu sa s pôvodnou farbou násobí.** Zápis šedej do `_BaseColor` prepisoval
  celú hodnotu vrátane alfy, takže priesvitné plochy (`0,3` a `0,35`) sa stali nepriehľadne
  bielymi a testujúci skončil v zavretej krabici.

## Čo tento plán nerobí

- **Nepridáva tretiu značku.** Solver ju zvládne bez zmeny kódu — pribudne len položka
  v `m_Markers` a zameraná kotva v scéne. Zameranie je ručná práca podľa
  [plánu zamerania](2026-09-09-plan-zamerania-znacky.md).
- **Nerieši drift mimo break roomu.** Cesta k tomu je v
  [ADR 007](decisions/007-vyuzitie-modelu-na-lokalizaciu.md): kurz na smery chodieb, väzba na
  výšku podlahy, map matching. Každá ako samostatný vypnuteľný režim, každá samostatný plán.
- **Nerieši dav ľudí.** Značka je vo výške hláv a pohybujúci sa ľudia sú zlé body pre VIO.
  Je to na boarde ako samostatná karta.
