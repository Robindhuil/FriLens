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
    /// Assembly-CSharp, ktorý asmdef testovacia assembly nevie referencovať. Rovnaký dôvod
    /// a rovnaký tvar ako <see cref="AlignmentMathVerifier"/>.
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
            var truthRotation = Quaternion.Euler(0f, 37.5f, 0f);
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
            var measuredA = Vector3.zero;
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
                new() { imageName = "a", modelPosition = Vector3.zero,
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
