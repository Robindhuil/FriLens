using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FriLens.EditorTools
{
    /// <summary>
    /// FriLens &gt; Verify Alignment Math.
    ///
    /// <see cref="MarkerAlignment"/> can only be tried for real on a phone in front of a printed
    /// marker, and by then a wrong pose solve looks exactly like a badly surveyed marker. These
    /// checks separate the two: they exercise the arithmetic against known answers so that a
    /// discrepancy in the field can be blamed on the marker rather than on the code.
    ///
    /// Kept as a menu item rather than a test assembly because the runtime scripts live in the
    /// predefined Assembly-CSharp, which an asmdef test assembly cannot reference.
    /// </summary>
    public static class AlignmentMathVerifier
    {
        [MenuItem("FriLens/Verify Alignment Math")]
        public static void Run()
        {
            var report = new StringBuilder();
            int failures = 0;

            failures += SolveLandsAnchorOnMeasuredPose(report);
            failures += IdentityAnchorGivesMeasuredPose(report);
            failures += RotationAverageSurvivesSignFlips(report);
            failures += PositionAverageIsTheMean(report);

            report.AppendLine();
            report.AppendLine(failures == 0 ? "ALL CHECKS PASSED" : failures + " CHECK(S) FAILED");

            if (failures == 0)
                Debug.Log(report.ToString());
            else
                Debug.LogError(report.ToString());
        }

        /// <summary>
        /// The whole point of the solve: put the root somewhere that lands the anchor exactly on
        /// the pose the tracker reported. Numbers are realistic - ra000's floor sits at Y = 5.15 m,
        /// so a marker 1.4 m up a wall in that building is near Y = 6.55 in model coordinates.
        /// </summary>
        static int SolveLandsAnchorOnMeasuredPose(StringBuilder report)
        {
            var anchorLocalPosition = new Vector3(-24.06f, 6.55f, -10.30f);
            var anchorLocalRotation = Quaternion.Euler(0f, 137f, 0f);
            var measuredPosition = new Vector3(0.42f, -0.13f, 2.87f);
            var measuredRotation = Quaternion.Euler(3f, -61f, 1.5f);

            MarkerAlignment.SolveRootPose(measuredPosition, measuredRotation,
                anchorLocalPosition, anchorLocalRotation, out var rootPosition, out var rootRotation);

            var landedPosition = rootPosition + rootRotation * anchorLocalPosition;
            var landedRotation = rootRotation * anchorLocalRotation;

            var positionError = Vector3.Distance(landedPosition, measuredPosition);
            var rotationError = Quaternion.Angle(landedRotation, measuredRotation);

            report.AppendLine($"solve: anchor lands {positionError * 1000f:F4} mm and {rotationError:F5} deg off target");

            int failures = 0;
            if (positionError > 1e-3f) { report.AppendLine("  FAIL position"); failures++; }
            if (rotationError > 1e-2f) { report.AppendLine("  FAIL rotation"); failures++; }
            return failures;
        }

        /// <summary>An anchor sitting at the root's own origin means the root takes the measured pose unchanged.</summary>
        static int IdentityAnchorGivesMeasuredPose(StringBuilder report)
        {
            var measuredPosition = new Vector3(0.42f, -0.13f, 2.87f);
            var measuredRotation = Quaternion.Euler(3f, -61f, 1.5f);

            MarkerAlignment.SolveRootPose(measuredPosition, measuredRotation,
                Vector3.zero, Quaternion.identity, out var rootPosition, out var rootRotation);

            var positionError = Vector3.Distance(rootPosition, measuredPosition);
            var rotationError = Quaternion.Angle(rootRotation, measuredRotation);
            report.AppendLine($"identity anchor: pos err {positionError * 1000f:F4} mm, rot err {rotationError:F5} deg");

            if (positionError > 1e-4f || rotationError > 1e-2f) { report.AppendLine("  FAIL"); return 1; }
            return 0;
        }

        /// <summary>
        /// q and -q are the same rotation, and ARCore hands back either. Summing them blindly
        /// cancels them out, so the average has to flip signs onto a common hemisphere first.
        /// The naive sum is printed alongside to show the trap is real, not theoretical.
        /// </summary>
        static int RotationAverageSurvivesSignFlips(StringBuilder report)
        {
            var truth = Quaternion.Euler(10f, 25f, -5f);
            var samples = new List<Quaternion>();

            var state = Random.state;
            Random.InitState(20260902);
            for (int i = 0; i < 30; i++)
            {
                var jittered = truth * Quaternion.Euler(
                    Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));

                if (i % 2 == 0)
                    jittered = new Quaternion(-jittered.x, -jittered.y, -jittered.z, -jittered.w);

                samples.Add(jittered);
            }
            Random.state = state;

            var averaged = MarkerAlignment.AverageRotation(samples);
            var error = Quaternion.Angle(averaged, truth);

            var naive = Vector4.zero;
            foreach (var sample in samples)
                naive += new Vector4(sample.x, sample.y, sample.z, sample.w);

            report.AppendLine($"rotation average over 30 sign-mixed samples with +-1 deg jitter: {error:F3} deg from truth");
            report.AppendLine($"  naive sum magnitude, for contrast: {naive.magnitude:F4} (a correct sum is near 30)");

            if (error > 1f) { report.AppendLine("  FAIL - sign handling is wrong"); return 1; }
            return 0;
        }

        static int PositionAverageIsTheMean(StringBuilder report)
        {
            var samples = new List<Vector3> { new(1f, 2f, 3f), new(3f, 4f, 5f), new(2f, 3f, 4f) };
            var mean = MarkerAlignment.AveragePosition(samples);
            report.AppendLine($"position average: {mean} (expected (2.0, 3.0, 4.0))");

            if (Vector3.Distance(mean, new Vector3(2f, 3f, 4f)) > 1e-5f) { report.AppendLine("  FAIL"); return 1; }
            return 0;
        }
    }
}
