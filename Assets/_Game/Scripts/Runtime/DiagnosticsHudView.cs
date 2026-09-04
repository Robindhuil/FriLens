using System;
using UnityEngine.UIElements;

namespace FriLens
{
    public enum HudMode
    {
        /// <summary>Availability check still running.</summary>
        Checking,

        /// <summary>AR session live; the numbers mean something.</summary>
        Ar,

        /// <summary>Overlay drawn without AR. Nothing on screen measures anything.</summary>
        Preview
    }

    public enum HudRow { Tracking, Marker, Alignment, FromMarker, Device }

    public enum ValueState { Ok, Warn, Bad, Idle }

    /// <summary>
    /// The only class that touches the HUD's visual tree.
    ///
    /// It never measures and never creates elements — it sets text and flips classes on a
    /// hierarchy that already exists in the UXML. Colours are not set from here either: each
    /// state is a class and the actual values live in the USS, so a repaint is one file rather
    /// than a hunt through code.
    ///
    /// The split matters because the measuring side runs several times a second. Keeping the
    /// view dumb means a change to how something looks cannot break what it reports.
    /// </summary>
    public sealed class DiagnosticsHudView
    {
        const int RowCount = 5;

        /// <summary>Values longer than this wrap to two lines at a smaller size.</summary>
        const int LongValueLength = 18;

        readonly VisualElement m_Root;
        readonly VisualElement m_Hazard;
        readonly VisualElement m_PillDot;
        readonly Label m_PillText;
        readonly Label m_ModeTitle;
        readonly Label m_ModeReason;

        readonly Label[] m_Values = new Label[RowCount];
        readonly VisualElement[] m_Icons = new VisualElement[RowCount];
        readonly VisualElement[] m_Dots = new VisualElement[RowCount];

        readonly Label m_Walked;
        readonly Label m_WalkedNote;
        readonly VisualElement m_WalkedBand;
        readonly Label m_Footer;

        readonly Button m_Reanchor;
        readonly Label m_ReanchorNote;
        readonly Button m_Overlay;
        readonly Label m_OverlayText;
        readonly VisualElement m_OverlayGlyph;
        readonly Button m_Mark;
        readonly Button m_Drop;
        readonly Button m_CompactToggle;

        public event Action Reanchor;
        public event Action Mark;

        /// <summary>Raised when the floor-probe button is pressed.</summary>
        public event Action Drop;

        /// <summary>
        /// Whether the readings are collapsed out of the way of the camera image.
        ///
        /// Not persisted anywhere. A test run is short, and a panel that remembered being
        /// hidden would eventually have somebody staring at a camera feed wondering why no
        /// numbers appear.
        /// </summary>
        public bool IsCompact { get; private set; }

        /// <summary>Raised with the overlay's new visibility.</summary>
        public event Action<bool> OverlayToggled;

        public bool OverlayVisible { get; private set; } = true;

        public DiagnosticsHudView(VisualElement documentRoot)
        {
            m_Root = documentRoot.Q("hud-root")
                ?? throw new ArgumentException("No element named 'hud-root' in the document.",
                    nameof(documentRoot));

            m_Hazard = m_Root.Q("banner-hazard");
            m_PillDot = m_Root.Q("mode-dot");
            m_PillText = m_Root.Q<Label>("mode-pill-text");
            m_ModeTitle = m_Root.Q<Label>("mode-title");
            m_ModeReason = m_Root.Q<Label>("mode-reason");

            BindRow(HudRow.Tracking, "tracking");
            BindRow(HudRow.Marker, "marker");
            BindRow(HudRow.Alignment, "alignment");
            BindRow(HudRow.FromMarker, "from-marker");
            BindRow(HudRow.Device, "device");

            m_Walked = m_Root.Q<Label>("value-walked");
            m_WalkedNote = m_Root.Q<Label>("walked-note");
            m_WalkedBand = m_Root.Q("walked-band");
            m_Footer = m_Root.Q<Label>("footer");

            m_Reanchor = m_Root.Q<Button>("btn-reanchor");
            m_ReanchorNote = m_Root.Q<Label>("btn-reanchor-note");
            m_Overlay = m_Root.Q<Button>("btn-overlay");
            m_OverlayText = m_Root.Q<Label>("btn-overlay-text");
            m_OverlayGlyph = m_Root.Q("btn-overlay-glyph");
            m_Mark = m_Root.Q<Button>("btn-mark");
            m_Drop = m_Root.Q<Button>("btn-drop");
            m_CompactToggle = m_Root.Q<Button>("btn-compact");

            m_Reanchor.clicked += () => Reanchor?.Invoke();
            m_Mark.clicked += () => Mark?.Invoke();
            m_Drop.clicked += () => Drop?.Invoke();
            m_Overlay.clicked += ToggleOverlay;
            m_CompactToggle.clicked += () => SetCompact(!IsCompact);
        }

        void BindRow(HudRow row, string key)
        {
            var i = (int)row;
            m_Values[i] = m_Root.Q<Label>("value-" + key);
            m_Icons[i] = m_Root.Q("icon-" + key);
            m_Dots[i] = m_Root.Q("dot-" + key);
        }

        /// <summary>
        /// Switches the whole HUD to a mode. Preview is deliberately one call that rewrites
        /// everything — banner, rows, headline number, footer and the re-anchor button — so
        /// there is no way to end up half in preview and looking like AR.
        /// </summary>
        public void SetMode(HudMode mode, string reason)
        {
            m_Root.EnableInClassList("hud--preview", mode == HudMode.Preview);
            m_Hazard.EnableInClassList("is-hidden", mode != HudMode.Preview);
            m_ModeReason.text = reason;

            switch (mode)
            {
                case HudMode.Checking:
                    m_ModeTitle.text = "CHECKING";
                    m_PillText.text = "CHECKING";
                    SetPillTone("idle");
                    SetReanchorAvailable(false);
                    break;

                case HudMode.Ar:
                    m_ModeTitle.text = "AR";
                    m_PillText.text = "MEASURING";
                    SetPillTone("ok");
                    SetReanchorAvailable(true);
                    break;

                case HudMode.Preview:
                    m_ModeTitle.text = "PREVIEW — NOT A TEST";
                    m_PillText.text = "MEASURING NOTHING";
                    SetPillTone("accent");
                    SetReanchorAvailable(false);

                    SetRow(HudRow.Tracking, "unavailable", ValueState.Idle);
                    SetRow(HudRow.Marker, "not seen", ValueState.Idle);
                    SetRow(HudRow.Alignment, "not measuring", ValueState.Idle);
                    SetRow(HudRow.FromMarker, "—", ValueState.Idle);
                    SetWalkedText("not measuring", ValueState.Idle);
                    SetWalkedNote("");
                    break;
            }
        }

        void SetPillTone(string tone)
        {
            m_PillDot.EnableInClassList("pill-dot--idle", tone == "idle");
            m_PillDot.EnableInClassList("pill-dot--ok", tone == "ok");
            m_PillDot.EnableInClassList("pill-dot--accent", tone == "accent");
        }

        void SetReanchorAvailable(bool available)
        {
            m_Reanchor.SetEnabled(available);
            m_ReanchorNote.EnableInClassList("is-hidden", available);
        }

        public void SetRow(HudRow row, string text, ValueState state)
        {
            var i = (int)row;

            var value = m_Values[i];
            value.text = text;
            value.EnableInClassList("v--long", text.Length > LongValueLength);
            ApplyState(value, state);

            // The icon frame carries the state as well as the value, so the row reads at a
            // glance from the shape and colour of the frame without parsing the text.
            var icon = m_Icons[i];
            icon.EnableInClassList("row-icon--active", state == ValueState.Ok);
            icon.EnableInClassList("row-icon--warn", state == ValueState.Warn);
            icon.EnableInClassList("row-icon--bad", state == ValueState.Bad);

            m_Dots[i].EnableInClassList("is-hidden", state != ValueState.Ok);
        }

        public void SetWalked(float meters, ValueState state)
        {
            SetWalkedText(meters.ToString("0.0") + " m", state);
        }

        /// <summary>
        /// Second line under the headline figure. An empty string hides it, so a caller with
        /// nothing to add does not have to leave a blank line behind.
        /// </summary>
        public void SetWalkedNote(string text)
        {
            m_WalkedNote.text = text;
            m_WalkedNote.EnableInClassList("is-hidden", string.IsNullOrEmpty(text));
        }

        void SetWalkedText(string text, ValueState state)
        {
            m_Walked.text = text;
            ApplyState(m_Walked, state);

            // The accent band under the number lights only while the figure is trustworthy.
            m_WalkedBand.EnableInClassList("is-hidden", state != ValueState.Ok);
        }

        static void ApplyState(Label label, ValueState state)
        {
            label.EnableInClassList("v--ok", state == ValueState.Ok);
            label.EnableInClassList("v--warn", state == ValueState.Warn);
            label.EnableInClassList("v--bad", state == ValueState.Bad);
            label.EnableInClassList("v--idle", state == ValueState.Idle);
        }

        /// <summary>
        /// Collapses the readings to a single line, or brings them back.
        ///
        /// One class on the root does the whole thing; the USS decides what survives. Hiding
        /// elements one by one from here would put the decision in two files and guarantee they
        /// disagree the first time a row is added.
        /// </summary>
        public void SetCompact(bool compact)
        {
            IsCompact = compact;
            m_Root.EnableInClassList("hud--compact", compact);
            m_CompactToggle.text = compact ? "full" : "compact";
        }

        void ToggleOverlay()
        {
            SetOverlayVisible(!OverlayVisible);
            OverlayToggled?.Invoke(OverlayVisible);
        }

        /// <summary>Sets the button's appearance without raising <see cref="OverlayToggled"/>.</summary>
        public void SetOverlayVisible(bool visible)
        {
            OverlayVisible = visible;
            m_OverlayText.text = visible ? "Hide overlay" : "Show overlay";
            m_OverlayGlyph.EnableInClassList("glyph--overlay-hide", visible);
            m_OverlayGlyph.EnableInClassList("glyph--overlay-show", !visible);
        }

        public void SetLog(string fileName, int rows, int marks)
        {
            m_Footer.RemoveFromClassList("footer--alert");
            m_Footer.text = $"log: {fileName} · {rows} rows · {marks} marks";
        }

        public void SetLogNotWriting()
        {
            m_Footer.AddToClassList("footer--alert");
            m_Footer.text = "log: not writing";
        }
    }
}
