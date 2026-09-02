using UnityEngine;
using UnityEngine.InputSystem;

namespace FriLens
{
    /// <summary>
    /// Orbit camera for <see cref="SessionModeController.SessionMode.Preview"/>, the mode a phone
    /// without ARCore falls into.
    ///
    /// Its job is not to look impressive. It is to let the parts of the app that have nothing to
    /// do with tracking — mesh loading, material, HUD, logging — be checked on real hardware
    /// instead of only in the editor. Drag to orbit, pinch or scroll to zoom, two fingers to pan.
    ///
    /// The rig frames the overlay on enable, so whichever floor is loaded fills the screen without
    /// anyone having to hunt for it.
    /// </summary>
    public class PreviewCameraRig : MonoBehaviour
    {
        [SerializeField] Transform m_Pivot;
        [SerializeField] Camera m_Camera;

        [Tooltip("Renderer framed when the rig switches on.")]
        [SerializeField] Renderer m_FrameTarget;

        [Header("Feel")]
        [SerializeField] float m_OrbitDegreesPerPixel = 0.22f;
        [SerializeField] float m_PanMetersPerPixel = 0.06f;
        [SerializeField] float m_ZoomPerScrollUnit = 0.06f;
        [SerializeField] float m_MinDistance = 3f;
        [SerializeField] float m_MaxDistance = 260f;
        [SerializeField] float m_MinPitch = 5f;
        [SerializeField] float m_MaxPitch = 89f;

        float m_Yaw = 35f;
        float m_Pitch = 55f;
        float m_Distance = 90f;

        Vector2 m_LastPointer;
        float m_LastPinchDistance;

        void OnEnable()
        {
            Frame();
        }

        /// <summary>Points the rig at the framed renderer and backs off far enough to see all of it.</summary>
        public void Frame()
        {
            if (m_FrameTarget == null || m_Pivot == null)
                return;

            var bounds = m_FrameTarget.bounds;
            m_Pivot.position = bounds.center;

            // Largest horizontal extent decides the distance; a floor is wide and flat, so its
            // height is never what limits the view.
            var extent = Mathf.Max(bounds.extents.x, bounds.extents.z);
            m_Distance = Mathf.Clamp(extent * 2.4f, m_MinDistance, m_MaxDistance);

            Apply();
        }

        void Update()
        {
            if (m_Pivot == null || m_Camera == null)
                return;

            ReadTouch();
            ReadMouse();
            Apply();
        }

        void ReadTouch()
        {
            var screen = Touchscreen.current;
            if (screen == null)
                return;

            var touches = screen.touches;
            int active = 0;
            Vector2 first = default, second = default;

            foreach (var touch in touches)
            {
                if (!touch.press.isPressed)
                    continue;

                if (active == 0) first = touch.position.ReadValue();
                else if (active == 1) second = touch.position.ReadValue();

                active++;
                if (active == 2) break;
            }

            if (active == 1)
            {
                if (m_LastPinchDistance > 0f)
                {
                    // Coming back from a pinch: reset the anchor so the view does not jump when
                    // the second finger lifts.
                    m_LastPointer = first;
                    m_LastPinchDistance = 0f;
                }

                Orbit(first - m_LastPointer);
                m_LastPointer = first;
                return;
            }

            if (active == 2)
            {
                var midpoint = (first + second) * 0.5f;
                var pinch = Vector2.Distance(first, second);

                if (m_LastPinchDistance > 0f)
                {
                    Zoom((m_LastPinchDistance - pinch) * m_ZoomPerScrollUnit);
                    Pan(midpoint - m_LastPointer);
                }

                m_LastPointer = midpoint;
                m_LastPinchDistance = pinch;
                return;
            }

            m_LastPinchDistance = 0f;
        }

        void ReadMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            var position = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame)
                m_LastPointer = position;

            if (mouse.leftButton.isPressed)
            {
                Orbit(position - m_LastPointer);
                m_LastPointer = position;
            }
            else if (mouse.middleButton.isPressed)
            {
                Pan(position - m_LastPointer);
                m_LastPointer = position;
            }

            var scroll = mouse.scroll.ReadValue().y;
            if (!Mathf.Approximately(scroll, 0f))
                Zoom(-scroll * m_ZoomPerScrollUnit);
        }

        void Orbit(Vector2 delta)
        {
            m_Yaw += delta.x * m_OrbitDegreesPerPixel;
            m_Pitch = Mathf.Clamp(m_Pitch - delta.y * m_OrbitDegreesPerPixel, m_MinPitch, m_MaxPitch);
        }

        void Pan(Vector2 delta)
        {
            // Pan speed scales with distance, otherwise it crawls when zoomed out and overshoots
            // when zoomed in.
            var scale = m_PanMetersPerPixel * (m_Distance / 60f);
            m_Pivot.position -= m_Pivot.right * (delta.x * scale) + m_Pivot.up * (delta.y * scale);
        }

        void Zoom(float amount)
        {
            m_Distance = Mathf.Clamp(m_Distance * (1f + amount), m_MinDistance, m_MaxDistance);
        }

        void Apply()
        {
            var rotation = Quaternion.Euler(m_Pitch, m_Yaw, 0f);
            m_Pivot.rotation = rotation;
            m_Camera.transform.position = m_Pivot.position - rotation * Vector3.forward * m_Distance;
            m_Camera.transform.rotation = rotation;
        }
    }
}
