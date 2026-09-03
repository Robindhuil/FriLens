using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace FriLens
{
    /// <summary>
    /// Puts the navigation overlay onto the real building by matching one printed marker.
    ///
    /// The alignment is deliberately one-shot. ARCore updates a tracked image's pose every
    /// frame and it jitters by centimetres, so following it live would make the overlay
    /// shimmer and would hide the very thing the test measures: how far the overlay walks
    /// away from the building over time. Instead the pose is sampled over a burst of frames,
    /// averaged, applied once, and then left alone until someone asks for a re-anchor.
    ///
    /// Averaging is not a nicety either. A single frame carries the tracker's noise, so an
    /// alignment built on it measures that noise rather than the error in the marker's
    /// surveyed pose. Without it the "constant error at the marker" row of the result table
    /// would not come out constant.
    ///
    /// <see cref="SampleSpreadMeters"/> and <see cref="SampleSpreadDegrees"/> report how far
    /// the samples scattered, which is what separates a real offset from tracker noise when
    /// reading the result.
    /// </summary>
    public class MarkerAlignment : MonoBehaviour
    {
        public enum AlignmentState
        {
            /// <summary>No marker seen yet, or waiting for a re-anchor request.</summary>
            Waiting,

            /// <summary>Marker is in view and frames are being collected.</summary>
            Sampling,

            /// <summary>An averaged pose has been applied.</summary>
            Aligned
        }

        [Header("Scene")]
        [SerializeField] ARTrackedImageManager m_TrackedImageManager;

        [Tooltip("Root of the overlay. Everything under it moves as one when alignment is applied.")]
        [SerializeField] Transform m_AlignmentRoot;

        [Tooltip("Empty object placed at the marker's pose in model coordinates.")]
        [SerializeField] Transform m_MarkerAnchor;

        [Header("Sampling")]
        [Tooltip("Frames of tracked pose to average before applying an alignment.")]
        [SerializeField, Range(1, 120)] int m_SampleCount = 30;

        [Tooltip("Reference image to align to. Leave empty to accept the first tracked image.")]
        [SerializeField] string m_ReferenceImageName = "";

        [Tooltip("Align automatically the first time the marker is seen.")]
        [SerializeField] bool m_AlignOnFirstSighting = true;

        [Tooltip("Seconds without a usable sample after which a half-collected burst is thrown "
            + "away rather than continued.")]
        [SerializeField] float m_SampleGapTimeoutSeconds = 2f;

        readonly List<Vector3> m_Positions = new();
        readonly List<Quaternion> m_Rotations = new();

        bool m_Enabled;
        bool m_WarnedAboutUnsetAnchor;
        float m_LastSampleTime;

        /// <summary>
        /// Raised right after an averaged pose has been applied. Distance walked has to start
        /// counting from here rather than from the button press, because the burst of samples
        /// takes a moment and anything walked during it belongs to the new alignment.
        /// </summary>
        public event System.Action Aligned;

        public AlignmentState State { get; private set; } = AlignmentState.Waiting;

        /// <summary>Frames collected so far in the current burst, out of <see cref="SampleTarget"/>.</summary>
        public int SamplesCollected => m_Positions.Count;

        public int SampleTarget => m_SampleCount;

        /// <summary>Seconds since the last alignment was applied, or -1 if there has not been one.</summary>
        public float TimeSinceAlignment => LastAlignmentTime < 0f ? -1f : Time.time - LastAlignmentTime;

        public float LastAlignmentTime { get; private set; } = -1f;

        /// <summary>Largest distance of any sample from the averaged position, in metres.</summary>
        public float SampleSpreadMeters { get; private set; }

        /// <summary>Largest angle between any sample and the averaged rotation, in degrees.</summary>
        public float SampleSpreadDegrees { get; private set; }

        /// <summary>The marker currently being tracked, or null.</summary>
        public ARTrackedImage TrackedMarker { get; private set; }

        void Awake()
        {
            m_Enabled = true;

            if (m_TrackedImageManager == null)
            {
                Debug.LogError($"{nameof(MarkerAlignment)}: no {nameof(ARTrackedImageManager)} assigned.", this);
                m_Enabled = false;
            }

            if (m_AlignmentRoot == null || m_MarkerAnchor == null)
            {
                Debug.LogError($"{nameof(MarkerAlignment)}: alignment root or marker anchor is not assigned.", this);
                m_Enabled = false;
            }
            else if (!m_MarkerAnchor.IsChildOf(m_AlignmentRoot))
            {
                Debug.LogError($"{nameof(MarkerAlignment)}: '{m_MarkerAnchor.name}' must be under "
                    + $"'{m_AlignmentRoot.name}', otherwise moving the root does not move the anchor.", this);
                m_Enabled = false;
            }
        }

        void Update()
        {
            if (!m_Enabled)
                return;

            TrackedMarker = FindMarker();

            if (State == AlignmentState.Waiting && m_AlignOnFirstSighting && TrackedMarker != null
                && LastAlignmentTime < 0f)
            {
                State = AlignmentState.Sampling;
            }

            if (State != AlignmentState.Sampling)
                return;

            // Poses reported while the tracker is only guessing would poison the average, so
            // limited tracking contributes nothing and the burst simply waits.
            if (TrackedMarker == null || TrackedMarker.trackingState != TrackingState.Tracking)
            {
                // Waiting is fine for a moment, but a burst left half full while the marker is
                // out of view is a trap: when it comes back the average would mix poses from
                // before and after — possibly across a relocalisation, from a different distance
                // and angle — and produce an alignment that looks measured and is not. Old
                // samples are dropped rather than continued.
                if (m_Positions.Count > 0 && Time.time - m_LastSampleTime > m_SampleGapTimeoutSeconds)
                {
                    Debug.LogWarning($"{nameof(MarkerAlignment)}: dropped {m_Positions.Count} samples, "
                        + $"the marker was out of view for more than {m_SampleGapTimeoutSeconds:F0} s.", this);
                    m_Positions.Clear();
                    m_Rotations.Clear();
                }

                return;
            }

            m_Positions.Add(TrackedMarker.transform.position);
            m_Rotations.Add(TrackedMarker.transform.rotation);
            m_LastSampleTime = Time.time;

            if (m_Positions.Count >= m_SampleCount)
                ApplyAlignment();
        }

        /// <summary>
        /// Starts a fresh burst of samples and re-aligns once it fills. Wired to the re-anchor
        /// button; in the field this gets used often.
        /// </summary>
        public void Realign()
        {
            if (!m_Enabled)
                return;

            m_Positions.Clear();
            m_Rotations.Clear();
            m_LastSampleTime = Time.time;
            State = AlignmentState.Sampling;
        }

        ARTrackedImage FindMarker()
        {
            foreach (var image in m_TrackedImageManager.trackables)
            {
                if (!string.IsNullOrEmpty(m_ReferenceImageName)
                    && image.referenceImage.name != m_ReferenceImageName)
                    continue;

                return image;
            }

            return null;
        }

        void ApplyAlignment()
        {
            var position = AveragePosition(m_Positions);
            var rotation = AverageRotation(m_Rotations);

            SampleSpreadMeters = 0f;
            foreach (var sample in m_Positions)
                SampleSpreadMeters = Mathf.Max(SampleSpreadMeters, Vector3.Distance(sample, position));

            SampleSpreadDegrees = 0f;
            foreach (var sample in m_Rotations)
                SampleSpreadDegrees = Mathf.Max(SampleSpreadDegrees, Quaternion.Angle(sample, rotation));

            WarnIfAnchorLooksUnset();

            // Read the anchor's pose relative to the root. It does not change when the root moves,
            // so reading it fresh on every alignment is safe and avoids caching something that
            // would go stale if the anchor were ever re-surveyed at runtime.
            var anchorLocalPosition = m_AlignmentRoot.InverseTransformPoint(m_MarkerAnchor.position);
            var anchorLocalRotation = Quaternion.Inverse(m_AlignmentRoot.rotation) * m_MarkerAnchor.rotation;

            SolveRootPose(position, rotation, anchorLocalPosition, anchorLocalRotation,
                out var rootPosition, out var rootRotation);

            m_AlignmentRoot.SetPositionAndRotation(rootPosition, rootRotation);

            LastAlignmentTime = Time.time;
            State = AlignmentState.Aligned;

            Debug.Log($"{nameof(MarkerAlignment)}: aligned on {m_Positions.Count} samples, "
                + $"spread {SampleSpreadMeters * 100f:F1} cm / {SampleSpreadDegrees:F2} deg.", this);

            m_Positions.Clear();
            m_Rotations.Clear();

            Aligned?.Invoke();
        }

        void WarnIfAnchorLooksUnset()
        {
            if (m_WarnedAboutUnsetAnchor)
                return;

            if (m_MarkerAnchor.localPosition != Vector3.zero || m_MarkerAnchor.localRotation != Quaternion.identity)
                return;

            m_WarnedAboutUnsetAnchor = true;
            Debug.LogWarning($"{nameof(MarkerAlignment)}: '{m_MarkerAnchor.name}' is still at the origin with no "
                + "rotation. The overlay will land somewhere meaningless until the marker's surveyed pose "
                + "is entered.", this);
        }

        /// <summary>
        /// Solves the root pose that lands an anchor, sitting at a fixed local pose inside that
        /// root, onto a measured world pose. In other words root = measured · anchorLocal⁻¹.
        /// </summary>
        public static void SolveRootPose(
            Vector3 measuredPosition, Quaternion measuredRotation,
            Vector3 anchorLocalPosition, Quaternion anchorLocalRotation,
            out Vector3 rootPosition, out Quaternion rootRotation)
        {
            rootRotation = measuredRotation * Quaternion.Inverse(anchorLocalRotation);
            rootPosition = measuredPosition - rootRotation * anchorLocalPosition;
        }

        public static Vector3 AveragePosition(List<Vector3> values)
        {
            var sum = Vector3.zero;
            foreach (var value in values)
                sum += value;
            return sum / values.Count;
        }

        /// <summary>
        /// Component-wise quaternion average with sign alignment. Exact averaging needs the
        /// eigenvector of the accumulated outer product; this approximation is indistinguishable
        /// from it while the samples sit within a few degrees of each other, which is the case
        /// for a marker held steady in view. The sign flip matters because q and -q are the same
        /// rotation and summing them blindly cancels them out.
        /// </summary>
        public static Quaternion AverageRotation(List<Quaternion> values)
        {
            var reference = values[0];
            var sum = Vector4.zero;

            foreach (var value in values)
            {
                var sign = Quaternion.Dot(reference, value) < 0f ? -1f : 1f;
                sum += sign * new Vector4(value.x, value.y, value.z, value.w);
            }

            sum.Normalize();
            return new Quaternion(sum.x, sum.y, sum.z, sum.w);
        }
    }
}
