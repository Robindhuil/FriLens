using UnityEngine;

namespace FriLens
{
    /// <summary>
    /// Measures the length of a path from sampled positions, at a fixed spatial resolution.
    ///
    /// Kept apart from <see cref="CameraTravel"/> and given the elapsed time as an argument
    /// rather than reading <c>Time.deltaTime</c>, so it can be driven with made-up data and
    /// checked. A filter whose only evidence is the walk it was tuned on is not a measurement,
    /// and this one decides the number the whole test divides by.
    ///
    /// See <c>FriLens > Verify Travel Filter</c> for what it is checked against.
    /// </summary>
    public struct PathResampler
    {
        Vector3 m_Smoothed;
        Vector3 m_LastKept;
        bool m_Started;

        /// <summary>Path length accumulated so far, in metres.</summary>
        public float Length { get; private set; }

        /// <summary>Filtered position, lagging the input by roughly the smoothing time.</summary>
        public Vector3 Smoothed => m_Smoothed;

        public void Restart(Vector3 position)
        {
            m_Smoothed = position;
            m_LastKept = position;
            m_Started = true;
            Length = 0f;
        }

        /// <summary>
        /// Carries the filter across a discontinuity without measuring it.
        ///
        /// A tracker relocalisation moves the reported position without the camera having gone
        /// anywhere. Left alone, the filter would spend the next second sliding towards the new
        /// pose and every centimetre of that slide would be billed as travel — the jump would be
        /// removed from one figure and quietly added to this one.
        /// </summary>
        public void Shift(Vector3 offset)
        {
            m_Smoothed += offset;
            m_LastKept += offset;
        }

        /// <summary>
        /// Feeds one sample in and returns the metres added by it, which is zero on most calls.
        /// </summary>
        /// <param name="position">Reported position, unfiltered.</param>
        /// <param name="deltaTime">Seconds since the previous sample.</param>
        /// <param name="smoothingSeconds">Time constant of the low-pass filter.</param>
        /// <param name="stepMeters">Resolution: how far the filtered position must move before a
        /// segment is counted.</param>
        public float Add(Vector3 position, float deltaTime, float smoothingSeconds, float stepMeters)
        {
            if (!m_Started)
            {
                Restart(position);
                return 0f;
            }

            // Exponential smoothing written against elapsed time rather than as a fixed
            // per-frame blend, so the filter behaves the same at 30 and at 60 fps and does not
            // change character when the frame rate drops.
            var blend = 1f - Mathf.Exp(-Mathf.Max(deltaTime, 0f) / Mathf.Max(smoothingSeconds, 0.001f));
            m_Smoothed = Vector3.Lerp(m_Smoothed, position, blend);

            var travelled = Vector3.Distance(m_Smoothed, m_LastKept);
            if (travelled < stepMeters)
                return 0f;

            m_LastKept = m_Smoothed;
            Length += travelled;
            return travelled;
        }
    }
}
