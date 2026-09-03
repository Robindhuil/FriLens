using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FriLens
{
    /// <summary>
    /// Holds the overlay at a world pose through an <see cref="ARAnchor"/> rather than by writing
    /// the transform once and hoping.
    ///
    /// The difference matters most in the case this project cannot avoid. Cover the camera, walk
    /// two metres, uncover: ARCore loses tracking, then either recognises where it is and corrects
    /// its pose, or does not. When it does correct itself, it moves every anchor with it so that
    /// each one stays on the same physical spot. It cannot do that for a plain transform, because
    /// a plain transform is just three floats it has never heard of — so an overlay positioned
    /// that way stays wrong precisely when the tracker got it right.
    ///
    /// The root is placed immediately and anchored a moment later, because creating an anchor is
    /// asynchronous and a visible delay before the overlay appears would be worse than a short
    /// window without correction. If anchoring fails the overlay still works; it simply stops
    /// following ARCore's corrections, and <see cref="IsAnchored"/> says so.
    /// </summary>
    public class AnchoredRoot : MonoBehaviour
    {
        [SerializeField] ARAnchorManager m_AnchorManager;

        [Tooltip("Root of the overlay. Everything under it moves as one.")]
        [SerializeField] Transform m_Root;

        ARAnchor m_Anchor;

        /// <summary>Whether the root is currently held by an anchor ARCore maintains.</summary>
        public bool IsAnchored => m_Anchor != null;

        /// <summary>Raised once the root has actually been attached to an anchor.</summary>
        public event Action Anchored;

        public Transform Root => m_Root;

        void Awake()
        {
            if (m_Root == null)
                Debug.LogError($"{nameof(AnchoredRoot)}: no root assigned.", this);

            if (m_AnchorManager == null)
                Debug.LogWarning($"{nameof(AnchoredRoot)}: no {nameof(ARAnchorManager)} assigned. The "
                    + "overlay will be placed but will not follow ARCore's corrections after a "
                    + "tracking loss.", this);
        }

        /// <summary>
        /// Moves the root to a world pose and re-anchors it there, replacing any previous anchor.
        /// </summary>
        public void PlaceAt(Pose pose)
        {
            if (m_Root == null)
                return;

            // Detach before moving. Left under the old anchor, the root would be dragged around by
            // a correction meant for a pose it no longer holds.
            m_Root.SetParent(null, true);
            m_Root.SetPositionAndRotation(pose.position, pose.rotation);

            ReleaseAnchor();

            if (m_AnchorManager == null || !m_AnchorManager.enabled)
                return;

            _ = AnchorAt(pose);
        }

        async Awaitable AnchorAt(Pose pose)
        {
            ARAnchor anchor;

            try
            {
                var result = await m_AnchorManager.TryAddAnchorAsync(pose);
                if (!result.status.IsSuccess())
                {
                    Debug.LogWarning($"{nameof(AnchoredRoot)}: could not create an anchor "
                        + $"({result.status}). The overlay will not follow tracker corrections.", this);
                    return;
                }

                anchor = result.value;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{nameof(AnchoredRoot)}: anchoring threw. {exception.Message}", this);
                return;
            }

            // The await spans frames, so everything it was started for may be gone: the component
            // disabled, the object destroyed, or a second alignment already applied. Any of those
            // makes this anchor rubbish, and attaching to it would drag the overlay back to a pose
            // the tester has already replaced.
            if (this == null || !isActiveAndEnabled || m_Anchor != null)
            {
                if (anchor != null)
                    m_AnchorManager.TryRemoveAnchor(anchor);
                return;
            }

            m_Anchor = anchor;
            m_Root.SetParent(anchor.transform, true);

            Anchored?.Invoke();
        }

        void ReleaseAnchor()
        {
            if (m_Anchor == null)
                return;

            if (m_AnchorManager != null)
                m_AnchorManager.TryRemoveAnchor(m_Anchor);

            m_Anchor = null;
        }

        void OnDestroy()
        {
            // The root outlives this component, and an orphaned parent would take the overlay with
            // it when the anchor is cleaned up.
            if (m_Root != null)
                m_Root.SetParent(null, true);

            ReleaseAnchor();
        }
    }
}
