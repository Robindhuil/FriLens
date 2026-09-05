using System.Text;
using UnityEditor;
using UnityEngine;

namespace FriLens.EditorTools
{
    /// <summary>
    /// Drives <see cref="PathResampler"/> with made-up trajectories whose true length is known,
    /// and prints what it measured against what it should have.
    ///
    /// The resampled path length is the denominator of every drift figure this project will
    /// report. Tuning it on a walk down a corridor and declaring it good is circular: the walk
    /// has no ground truth either. Synthetic paths do, so this is the only place the filter can
    /// actually be wrong in a way that shows.
    ///
    /// The wave cases are the ones worth reading. They are the field observation — "moving the
    /// phone while standing adds distance" — written down as a number.
    /// </summary>
    public static class TravelFilterVerifier
    {
        const float Smoothing = 0.35f;
        const float Step = 0.3f;

        [MenuItem("FriLens/Verify Travel Filter")]
        public static void Verify()
        {
            var report = new StringBuilder();
            report.AppendLine($"PathResampler — smoothing {Smoothing:F2} s, step {Step:F2} m");
            report.AppendLine();

            var failures = 0;

            failures += StraightWalk(report, noise: 0f, fps: 60f);
            failures += StraightWalk(report, noise: 0.02f, fps: 60f);
            failures += StraightWalk(report, noise: 0.02f, fps: 30f);
            failures += Jump(report);

            report.AppendLine();
            report.AppendLine("Standing still, waving the phone. True travel of the person: 0 m.");
            failures += Wave(report, hz: 2f, amplitude: 0.25f, tolerance: 0.6f);
            failures += Wave(report, hz: 1f, amplitude: 0.25f, tolerance: 0.6f);

            // Not a pass or fail. A slow, wide sweep of the arm moves the camera as far as a
            // step does and at a rate walking also produces, so no filter separates them. The
            // number is here so the limit is documented rather than discovered in the field.
            report.AppendLine();
            report.AppendLine("Known limit — a slow wide sweep is indistinguishable from walking:");
            Wave(report, hz: 0.4f, amplitude: 0.4f, tolerance: float.MaxValue);

            report.AppendLine();
            report.AppendLine(failures == 0
                ? "All checks passed."
                : $"{failures} CHECK(S) FAILED.");

            if (failures == 0)
                Debug.Log(report.ToString());
            else
                Debug.LogError(report.ToString());
        }

        /// <summary>Walks 20 m in a straight line, optionally through noise.</summary>
        static int StraightWalk(StringBuilder report, float noise, float fps)
        {
            const float speed = 1.3f;
            const float truth = 20f;

            var random = new System.Random(12345);
            var resampler = new PathResampler();
            resampler.Restart(Vector3.zero);

            var dt = 1f / fps;
            var raw = 0f;
            var previous = Vector3.zero;

            for (var t = 0f; t < truth / speed; t += dt)
            {
                var clean = new Vector3(speed * t, 0f, 0f);
                var sample = clean + Noise(random, noise);

                raw += Vector3.Distance(sample, previous);
                previous = sample;

                resampler.Add(sample, dt, Smoothing, Step);
            }

            // The filter lags by roughly its time constant, so the last stretch of the walk is
            // still being caught up with when the walk ends and never crosses the threshold.
            // At 1.3 m/s that is under half a metre, and it does not grow with distance.
            var tolerance = 0.8f;
            var error = Mathf.Abs(resampler.Length - truth);

            report.AppendLine($"walk 20 m, noise {noise * 100f:F0} cm, {fps:F0} fps"
                + $"   resampled {resampler.Length:F2} m   raw {raw:F2} m"
                + $"   {Verdict(error <= tolerance)}");

            return error <= tolerance ? 0 : 1;
        }

        /// <summary>Stands still and oscillates, which is a person holding a phone.</summary>
        static int Wave(StringBuilder report, float hz, float amplitude, float tolerance)
        {
            const float seconds = 20f;
            const float dt = 1f / 60f;

            var resampler = new PathResampler();
            resampler.Restart(Vector3.zero);

            var raw = 0f;
            var previous = Vector3.zero;

            for (var t = 0f; t < seconds; t += dt)
            {
                var sample = new Vector3(0f, 0f, amplitude * Mathf.Sin(2f * Mathf.PI * hz * t));

                raw += Vector3.Distance(sample, previous);
                previous = sample;

                resampler.Add(sample, dt, Smoothing, Step);
            }

            var passed = resampler.Length <= tolerance;
            report.AppendLine($"wave {hz:F1} Hz, +/-{amplitude * 100f:F0} cm, {seconds:F0} s"
                + $"   resampled {resampler.Length:F2} m   raw {raw:F2} m"
                + (tolerance == float.MaxValue ? "" : $"   {Verdict(passed)}"));

            return tolerance == float.MaxValue || passed ? 0 : 1;
        }

        /// <summary>
        /// Walks 5 m, suffers a 3 m relocalisation, walks 5 m more. The jump is not travel and
        /// must not appear in the length under any name, including as a slow slide afterwards.
        /// </summary>
        static int Jump(StringBuilder report)
        {
            const float speed = 1.3f;
            const float dt = 1f / 60f;
            const float truth = 10f;

            var resampler = new PathResampler();
            resampler.Restart(Vector3.zero);

            var offset = Vector3.zero;

            for (var t = 0f; t < truth / speed; t += dt)
            {
                var walked = speed * t;

                // Halfway through, the tracker renumbers the world by three metres sideways.
                if (walked >= 5f && offset == Vector3.zero)
                {
                    offset = new Vector3(0f, 0f, 3f);
                    resampler.Shift(offset);
                }

                resampler.Add(new Vector3(walked, 0f, 0f) + offset, dt, Smoothing, Step);
            }

            var error = Mathf.Abs(resampler.Length - truth);
            report.AppendLine($"walk 10 m with a 3 m relocalisation at halfway"
                + $"   resampled {resampler.Length:F2} m   {Verdict(error <= 0.8f)}");

            return error <= 0.8f ? 0 : 1;
        }

        static Vector3 Noise(System.Random random, float meters)
        {
            if (meters <= 0f)
                return Vector3.zero;

            return new Vector3(
                (float)(random.NextDouble() * 2d - 1d) * meters,
                (float)(random.NextDouble() * 2d - 1d) * meters,
                (float)(random.NextDouble() * 2d - 1d) * meters);
        }

        static string Verdict(bool passed) => passed ? "ok" : "FAILED";
    }
}
