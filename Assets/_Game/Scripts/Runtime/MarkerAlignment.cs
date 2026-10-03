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

        /// <summary>
        /// One printed marker: the name it has in the reference image library, and an empty object
        /// sitting at its surveyed pose in model coordinates.
        /// </summary>
        [System.Serializable]
        public struct SurveyedMarker
        {
            [Tooltip("Name in the reference image library, e.g. frilens-M1.")]
            public string imageName;

            [Tooltip("Empty object at the marker's pose in model coordinates.")]
            public Transform anchor;
        }

        [Tooltip("Every marker that has been printed, stuck up and surveyed. More than one is the "
            + "point: a marker is the only thing independent of ARCore's map, so it is also the "
            + "remedy for a tracking loss — and one at the far end of a corridor is no use in the "
            + "middle of it.")]
        [SerializeField] SurveyedMarker[] m_Markers;

        [Tooltip("Optional. Holds the root on an ARAnchor so it follows ARCore's corrections after "
            + "a tracking loss. Without it the root is written once and stays put.")]
        [SerializeField] AnchoredRoot m_Anchored;

        [Tooltip("Zdroj čísla úseku trackingu a prejdenej dráhy. Bez neho sa observácie "
            + "nezahodia pri relokalizačnom skoku a fit môže miešať dve mapy.")]
        [SerializeField] CameraTravel m_Travel;

        [Tooltip("Zdroj udalosti o strate trackingu. Bez neho sa observácie po strate nezahodia.")]
        [SerializeField] TrackingContinuity m_Continuity;

        [Header("Sampling")]
        [Tooltip("Frames of tracked pose to average before applying an alignment.")]
        [SerializeField, Range(1, 120)] int m_SampleCount = 30;

        [Tooltip("Align automatically the first time the marker is seen.")]
        [SerializeField] bool m_AlignOnFirstSighting = true;

        /// <summary>Kedy sa zarovnanie prepočítava.</summary>
        public enum UpdatePolicy
        {
            /// <summary>
            /// Len na Re-anchor. Prekryv sa medzi stlačeniami nehýbe, takže drift je vidieť
            /// ako útek — to je meranie, kvôli ktorému projekt vznikol.
            /// </summary>
            OnRequest,

            /// <summary>
            /// Sám, kým je značka v zábere. Prekryv sa v miestnosti drží; veľkosť každej opravy
            /// sa zapíše, takže drift sa číta ako veľkosť fixu namiesto úteku.
            /// </summary>
            Continuous,
        }

        [Tooltip("OnRequest je merací režim a je predvolený, aby sa behy dali porovnávať so "
            + "staršími. Continuous je navigačný.")]
        [SerializeField] UpdatePolicy m_Policy = UpdatePolicy.OnRequest;

        [Tooltip("Najkratší odstup medzi dvomi automatickými zarovnaniami v navigačnom režime. "
            + "Bez neho by burst štartoval znova hneď po dobehnutí a log by sa zaplnil.")]
        [SerializeField] float m_ContinuousIntervalSeconds = 2f;

        [Tooltip("Take the overlay's tilt from gravity instead of from the marker, keeping only "
            + "its heading and position. A tracked image's out-of-plane tilt is the weak part of "
            + "the estimate and it biases rather than scatters, so it survives averaging. Off "
            + "measures the marker's raw answer; on is what the overlay should be tested with.")]
        [SerializeField] bool m_LevelWithGravity = true;

        [Tooltip("Seconds without a usable sample after which a half-collected burst is thrown "
            + "away rather than continued.")]
        [SerializeField] float m_SampleGapTimeoutSeconds = 2f;

        [Tooltip("Sekundy, po ktorých sa observácia značky prestane počítať do fitu.")]
        [SerializeField] float m_ObservationMaxAgeSeconds = 60f;

        [Tooltip("Najväčší rozptyl polohy v burste, ktorý sa ešte prijme, v metroch.")]
        [SerializeField] float m_MaxPositionSpreadMeters = 0.02f;

        [Tooltip("Nad akou časťou rozpätia značiek sa fit zamietne. 0,25 je pri 6,79 m steny "
            + "asi 1,7 m — veľkoryso nad skutočnou chybou modelu a ďaleko pod hrubým zlým "
            + "čítaním polohy, aké ARCore vie ohlásiť.")]
        [SerializeField, Range(0.02f, 1f)] float m_MaxFitErrorFraction = 0.25f;

        [Header("Ustálenie značky")]
        [Tooltip("Burst sa nezačne, kým značka nie je ustálená. Prvá póza po zbadaní býva "
            + "konzistentná, ale zlá: beh 20261003-134724 mal M2 o 44 cm vedľa pri rozptyle "
            + "0,3 cm a brána rozptylu ju prepustila.")]
        [SerializeField] bool m_RequireSettle = true;

        [Tooltip("Najkratší čas sledovania značky pred prvým burstom, v sekundách.")]
        [SerializeField] float m_SettleMinSeconds = 2f;

        [Tooltip("O koľko stupňov sa musí zmeniť smer pohľadu kamery na značku. ARCore opravil "
            + "hĺbku M2 až po pohybe do strany, nie čakaním — hypotéza z jedného behu, overuje ju "
            + "surový záznam značiek.")]
        [SerializeField] float m_SettleMinSweepDegrees = 15f;

        [Tooltip("Kamera na meranie paralaxy. Prázdne znamená Camera.main.")]
        [SerializeField] Transform m_Camera;

        /// <summary>Ako ďaleko je značka od toho, aby sa jej poloha brala vážne.</summary>
        struct SettleState
        {
            public float since;
            public Vector3 firstDirection;
            public float sweepDegrees;
            public bool settled;
        }

        readonly Dictionary<string, SettleState> m_Settle = new();
        int m_SettleSegment;

        readonly List<Vector3> m_Positions = new();
        readonly List<Quaternion> m_Rotations = new();
        readonly MarkerObservations m_Observations = new();

        /// <summary>
        /// Which marker the burst in progress is made of. A burst has to belong to one marker:
        /// the two in the break room are 6.79 m apart on the same wall and both fit in the
        /// camera from across the room, and an average taken across the pair is a pose that
        /// belongs to neither of them.
        /// </summary>
        string m_BurstImageName = "";

        string m_TargetImageName = "";

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

        /// <summary>
        /// The averaged marker pose the last alignment was built from, in session space.
        ///
        /// Kept because it is the only place the tracker's own answer survives. Everything after
        /// an alignment describes where the overlay ended up; without the pose it was built from,
        /// an overlay in the wrong place cannot be told apart from a marker read in the wrong
        /// orientation, and the two need opposite fixes.
        /// </summary>
        public Pose LastMeasuredPose { get; private set; }

        /// <summary>The root pose the last alignment produced, in session space.</summary>
        public Pose LastRootPose { get; private set; }

        /// <summary>
        /// How far the last alignment had to be stood upright, in degrees, or 0 when levelling is
        /// off. This is the marker's tilt error read straight off, so the correction doubles as
        /// the measurement of the thing it corrects.
        /// </summary>
        public float LevelledDegrees { get; private set; }

        /// <summary>Z koľkých značiek vznikol posledný fit. 1 znamená núdzový režim z natočenia.</summary>
        public int FitMarkerCount { get; private set; }

        /// <summary>
        /// Najväčší zvyšok posledného fitu v metroch, alebo -1 keď fit vznikol z jednej značky
        /// a zvyšok naozaj neexistuje. Dosadiť tam nulu by vyzeralo ako dokonalý fit, čo je
        /// horšie než priznať, že sa nemeral.
        ///
        /// Od dvoch značiek vyššie zvyšok existuje: dve dávajú štyri vodorovné rovnice pre tri
        /// neznáme, takže sústava je preurčená a rozdiel dĺžok sa medzi ne rozdelí.
        /// </summary>
        public float FitWorstResidualMeters { get; private set; } = -1f;

        /// <summary>
        /// Na ktorej značke je ten najväčší zvyšok. Pri troch a viac je to práve tá informácia,
        /// kvôli ktorej má tretia značka zmysel — samotné číslo nepovie, ktorá strana miestnosti
        /// sa s modelom rozchádza.
        /// </summary>
        public string FitWorstResidualImage { get; private set; } = "";

        /// <summary>
        /// Pri presne dvoch značkách rozdiel nameranej a modelovej dĺžky spojnice. To je chyba
        /// modelu na tom úseku steny a je to výsledok merania, nie chyba zarovnania.
        /// </summary>
        public float FitBaselineErrorMeters { get; private set; }

        /// <summary>
        /// O koľko metrov posunulo prekryv posledné zarovnanie. Je to drift nazbieraný od
        /// predošlého fitu, odčítaný ako číslo namiesto odhadu okom — v navigačnom režime je to
        /// jediné miesto, kde sa drift dá prečítať, lebo útek sa priebežne maže.
        /// </summary>
        public float LastCorrectionMeters { get; private set; }

        /// <summary>
        /// Z čoho vznikol posledný fit: každá použitá observácia s vekom a rozptylom, napr.
        /// <c>M1 0.0s 0.4cm + M2 3.1s 0.6cm</c>. Samotný počet značiek nepovie, či druhá
        /// prišla zo státia pred troma sekundami alebo spred minúty z iného miesta.
        /// </summary>
        public string FitInputs { get; private set; } = "";

        /// <summary>
        /// Chyba fitu, ktorý zamietla brána kvality, v metroch; -1 keď zamietnutý nebol.
        /// Bez nej sa <c>1 značka</c> z brány nedá odlíšiť od <c>1 značka</c> z nevidenej.
        /// </summary>
        public float RejectedFitErrorMeters { get; private set; } = -1f;

        /// <summary>Prah brány pri poslednom zamietnutí, v metroch.</summary>
        public float RejectedFitAllowedMeters { get; private set; }

        /// <summary>
        /// Či sa burst posledného zarovnania prijal do zásoby observácií. Burst s rozptylom
        /// nad prahom zarovná, ale do fitu z viacerých značiek nevstúpi.
        /// </summary>
        public bool LastBurstAccepted { get; private set; }

        /// <summary>
        /// Značka sa ustálila: meno, sekundy od prvého zbadania a uhol, o ktorý sa zmenil pohľad.
        /// HUD to zapíše do logu, aby sa dalo porovnať s pózou v surovom zázname.
        /// </summary>
        public event System.Action<string, float, float> MarkerSettled;

        /// <summary>
        /// Burst čaká, kým sa značka v zábere ustáli. HUD vtedy radí pohnúť telefónom do strany;
        /// bez toho by „sampling 0/30" vyzeral ako zaseknutý.
        /// </summary>
        public bool WaitingForSettle { get; private set; }

        public bool IsSettled(string imageName) =>
            !m_RequireSettle || (m_Settle.TryGetValue(imageName ?? "", out var state) && state.settled);

        /// <summary>O koľko stupňov sa zatiaľ zmenil pohľad na značku od jej prvého zbadania.</summary>
        public float SettleSweepDegrees(string imageName) =>
            m_Settle.TryGetValue(imageName ?? "", out var state) ? state.sweepDegrees : 0f;

        public float SettleMinSweepDegrees => m_SettleMinSweepDegrees;

        /// <summary>Pre surový záznam značiek v <see cref="SessionLogger"/>.</summary>
        public ARTrackedImageManager TrackedImageManager => m_TrackedImageManager;

        /// <summary>Aktuálna politika prepočtu; prepína ju HUD.</summary>
        public UpdatePolicy Policy
        {
            get => m_Policy;
            set => m_Policy = value;
        }

        /// <summary>The marker currently being tracked, or null.</summary>
        public ARTrackedImage TrackedMarker { get; private set; }

        /// <summary>
        /// The marker the next alignment has to come from, or empty for whichever is seen.
        ///
        /// Which marker produced a given alignment cannot be left to whichever ARCore happened
        /// to report first, because the difference between the alignment from one marker and
        /// from the other is the measurement being taken.
        /// </summary>
        public string TargetImageName => m_TargetImageName;

        /// <summary>
        /// The marker the current alignment was solved from, or empty if there has not been one.
        /// </summary>
        public string AlignedImageName { get; private set; } = "";

        /// <summary>Image name of the marker being tracked, or empty.</summary>
        public string TrackedImageName =>
            TrackedMarker != null ? TrackedMarker.referenceImage.name : "";

        /// <summary>
        /// How many images the tracker has been given to look for.
        ///
        /// Zero until the printed markers exist, and that is not a detail: with an empty library
        /// ARCore has nothing to recognise, so pressing re-anchor collects no samples and times
        /// out. The button has to say that rather than look ready and do nothing.
        /// </summary>
        public int ReferenceImageCount =>
            m_TrackedImageManager != null && m_TrackedImageManager.referenceLibrary != null
                ? m_TrackedImageManager.referenceLibrary.count
                : 0;

        void Awake()
        {
            m_Enabled = true;

            if (m_TrackedImageManager == null)
            {
                Debug.LogError($"{nameof(MarkerAlignment)}: no {nameof(ARTrackedImageManager)} assigned.", this);
                m_Enabled = false;
            }

            if (m_AlignmentRoot == null || m_Markers == null || m_Markers.Length == 0)
            {
                Debug.LogError($"{nameof(MarkerAlignment)}: alignment root or marker list is empty.", this);
                m_Enabled = false;
                return;
            }

            foreach (var marker in m_Markers)
            {
                if (marker.anchor == null)
                {
                    Debug.LogError($"{nameof(MarkerAlignment)}: marker '{marker.imageName}' has no "
                        + "anchor. Its pose is what the whole alignment is solved from.", this);
                    m_Enabled = false;
                }
                else if (!marker.anchor.IsChildOf(m_AlignmentRoot))
                {
                    Debug.LogError($"{nameof(MarkerAlignment)}: '{marker.anchor.name}' must be under "
                        + $"'{m_AlignmentRoot.name}', otherwise moving the root does not move it.", this);
                    m_Enabled = false;
                }
            }

            if (m_Continuity != null)
                m_Continuity.Lost += OnTrackingLost;
            else
                Debug.LogWarning($"{nameof(MarkerAlignment)}: no {nameof(TrackingContinuity)} "
                    + "assigned. Pozorovania značiek prežijú stratu trackingu a fit môže miešať "
                    + "polohy z mapy, ktorá sa medzitým prekreslila. Run FriLens > Wire Scene.", this);
        }

        void Update()
        {
            if (!m_Enabled)
                return;

            UpdateSettle();
            TrackedMarker = FindMarker();

            if (State == AlignmentState.Waiting && m_AlignOnFirstSighting && TrackedMarker != null
                && LastAlignmentTime < 0f)
            {
                State = AlignmentState.Sampling;
            }

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

            if (State != AlignmentState.Sampling)
            {
                WaitingForSettle = false;
                return;
            }

            // Poses reported while the tracker is only guessing would poison the average, so
            // limited tracking contributes nothing and the burst simply waits.
            if (TrackedMarker == null || TrackedMarker.trackingState != TrackingState.Tracking)
            {
                WaitingForSettle = false;

                // Waiting is fine for a moment, but a burst left half full while the marker is
                // out of view is a trap: when it comes back the average would mix poses from
                // before and after — possibly across a relocalisation, from a different distance
                // and angle — and produce an alignment that looks measured and is not. Old
                // samples are dropped rather than continued.
                if (Time.time - m_LastSampleTime > m_SampleGapTimeoutSeconds)
                {
                    if (m_Positions.Count > 0)
                    {
                        Debug.LogWarning($"{nameof(MarkerAlignment)}: dropped {m_Positions.Count} samples, "
                            + $"the marker was out of view for more than {m_SampleGapTimeoutSeconds:F0} s.", this);
                        m_Positions.Clear();
                        m_Rotations.Clear();
                        m_LastSampleTime = Time.time;
                    }
                    else
                    {
                        // Nothing was ever collected, so this is a re-anchor pressed with no
                        // marker in front of the camera. Standing in "sampling 0/30" for ever
                        // reads as work in progress; going back to waiting is the truth.
                        State = AlignmentState.Waiting;
                    }
                }

                return;
            }

            // Which marker the burst belongs to is settled by its first sample. A second marker
            // coming into view part way through would otherwise be averaged in with the first,
            // and the result would be a pose somewhere between two places on the wall.
            var imageName = TrackedMarker.referenceImage.name;

            // Neustálená značka do burstu nepatrí ani jednou snímkou: jej póza je síce pokojná,
            // ale môže byť celá posunutá, a priemer z nej by to len spevnil. Burst počká a časovač
            // výpadku sa drží, inak by Re-anchor po dvoch sekundách čakania potichu zrušil.
            if (!IsSettled(imageName))
            {
                WaitingForSettle = true;
                m_Positions.Clear();
                m_Rotations.Clear();
                m_LastSampleTime = Time.time;
                return;
            }

            WaitingForSettle = false;

            if (m_Positions.Count > 0 && imageName != m_BurstImageName)
            {
                Debug.LogWarning($"{nameof(MarkerAlignment)}: '{imageName}' came into view while "
                    + $"sampling '{m_BurstImageName}'. Starting again on the new one rather than "
                    + "averaging the two.", this);
                m_Positions.Clear();
                m_Rotations.Clear();
            }

            m_BurstImageName = imageName;

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
            m_BurstImageName = "";
            m_LastSampleTime = Time.time;
            State = AlignmentState.Sampling;
        }

        /// <summary>
        /// Restricts alignment to one marker, or to any of them when given an empty name.
        /// </summary>
        public void SetTarget(string imageName)
        {
            var wanted = imageName ?? "";
            if (wanted == m_TargetImageName)
                return;

            m_TargetImageName = wanted;

            // Samples already in hand came from the marker that was wanted a moment ago. Keeping
            // them would apply the old marker's pose under the new one's name.
            if (State == AlignmentState.Sampling)
                Realign();
        }

        /// <summary>
        /// Steps the target through the surveyed markers and back to "any", and returns the new
        /// one. One button rather than one per marker: the marker list is a serialized field
        /// that grows as markers get surveyed, and a fixed row of buttons would have to be kept
        /// in step with it by hand.
        /// </summary>
        public string CycleTarget()
        {
            if (m_Markers == null || m_Markers.Length == 0)
                return m_TargetImageName;

            var index = -1;
            for (var i = 0; i < m_Markers.Length; i++)
                if (m_Markers[i].imageName == m_TargetImageName)
                    index = i;

            var next = index + 1;
            SetTarget(next >= m_Markers.Length ? "" : m_Markers[next].imageName);
            return m_TargetImageName;
        }

        /// <summary>
        /// Picks the tracked image that has a surveyed anchor. An image the library knows but
        /// nobody has measured is worse than none: it would align the overlay to a guess.
        ///
        /// An image ARCore has seen once stays in <c>trackables</c> for the rest of the session
        /// with its state dropped to Limited, so taking the first match would pin the HUD to a
        /// marker left behind at the other end of the room while the one actually in front of
        /// the camera is ignored. A marker being tracked outranks one merely remembered.
        ///
        /// Keď sú sledované dve naraz, vyhrá tá, ktorej observácia je najstaršia alebo chýba.
        /// Do 0.3.0-alpha vyhrávala prvá v <c>trackables</c>, teda vždy tá istá: burst sa robí
        /// z jednej značky, takže druhá sa do zásoby observácií nikdy nedostala a fit z dvoch
        /// značiek pri cieli <c>any</c> nemohol nastať, hoci boli obe v zábere. Rozpracovaný
        /// burst si svoju značku drží, inak by ho výber po každej snímke reštartoval.
        /// </summary>
        ARTrackedImage FindMarker()
        {
            ARTrackedImage remembered = null;
            ARTrackedImage stalest = null;
            var stalestTime = float.PositiveInfinity;

            foreach (var image in m_TrackedImageManager.trackables)
            {
                if (AnchorFor(image) == null)
                    continue;

                var name = image.referenceImage.name;
                if (m_TargetImageName.Length > 0 && name != m_TargetImageName)
                    continue;

                if (image.trackingState != TrackingState.Tracking)
                {
                    remembered ??= image;
                    continue;
                }

                if (State == AlignmentState.Sampling && m_Positions.Count > 0 && name == m_BurstImageName)
                    return image;

                var seenAt = m_Observations.TryGet(name, out var observation)
                    ? observation.time
                    : float.NegativeInfinity;

                if (stalest == null || seenAt < stalestTime)
                {
                    stalest = image;
                    stalestTime = seenAt;
                }
            }

            return stalest != null ? stalest : remembered;
        }

        /// <summary>
        /// Čo ARCore práve hlási o každej zameranej značke, napr. <c>M1 Tracking M2 Limited</c>.
        /// Ide do logu pri zarovnaní, lebo „boli obe v zábere?" sa inak z CSV zistiť nedá —
        /// a práve od toho závisí, či je <c>1 značka</c> zamietnutý fit alebo nevidená značka.
        /// </summary>
        public string SeenImagesSummary()
        {
            if (m_TrackedImageManager == null)
                return "";

            var summary = new System.Text.StringBuilder();
            foreach (var image in m_TrackedImageManager.trackables)
            {
                if (AnchorFor(image) == null)
                    continue;
                if (summary.Length > 0)
                    summary.Append(' ');
                summary.Append(ShortName(image.referenceImage.name)).Append(' ').Append(image.trackingState);
            }

            return summary.Length > 0 ? summary.ToString() : "-";
        }

        /// <summary><c>frilens-M1</c> → <c>M1</c>. Plné meno je v logu len šum.</summary>
        public static string ShortName(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return "";

            var dash = imageName.LastIndexOf('-');
            return dash >= 0 && dash < imageName.Length - 1 ? imageName[(dash + 1)..] : imageName;
        }

        Transform AnchorFor(ARTrackedImage image)
        {
            if (image == null)
                return null;

            foreach (var marker in m_Markers)
                if (marker.anchor != null && marker.imageName == image.referenceImage.name)
                    return marker.anchor;

            return null;
        }

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

        /// <summary>
        /// Či sa fit dá brať vážne, alebo je postavený na hrubo zle prečítanej polohe.
        ///
        /// Rozptyl v rámci burstu takú chybu neodhalí: ARCore vie tú istú nepohnutú značku
        /// ohlásiť o metre inde a hlásiť to pokojne, lebo je to sústavná chyba, nie šum. Beh
        /// 20260909-134644 má polohu jednej značky rozídenú o 5,02 m pri rozptyloch pod
        /// centimetrom.
        ///
        /// Chytí sa to až porovnaním s modelom, ktorý vzdialenosti medzi značkami pozná. Pri
        /// dvoch je to rozdiel dĺžok spojnice, pri troch a viac najväčší zvyšok; oboje sa meria
        /// voči rozpätiu značiek, lebo dvadsať centimetrov znamená niečo iné na šiestich metroch
        /// než na polmetri.
        ///
        /// Zamietnutý fit spadne na jednu značku, čo je horšie zarovnanie — ale vedome horšie,
        /// nie tiché a ľubovoľne zlé.
        /// </summary>
        bool FitLooksSane(AlignmentSolver.Result fit)
        {
            if (fit.modelSpanMeters <= 0f)
                return false;

            var error = fit.markerCount == 2
                ? Mathf.Abs(fit.baselineErrorMeters)
                : fit.worstResidualMeters;

            var allowed = m_MaxFitErrorFraction * fit.modelSpanMeters;
            if (error <= allowed)
                return true;

            RejectedFitErrorMeters = error;
            RejectedFitAllowedMeters = allowed;

            Debug.LogWarning($"{nameof(MarkerAlignment)}: fit z {fit.markerCount} značiek zamietnutý "
                + $"— chyba {error:F2} m na rozpätí {fit.modelSpanMeters:F2} m je nad prahom "
                + $"{allowed:F2} m. Niektorá značka je ohlásená hrubo zle; zarovnávam z jednej.", this);
            return false;
        }

        /// <summary>
        /// Po strate trackingu je poloha každej uloženej značky odhad z mapy, ktorá sa medzitým
        /// mohla prekresliť. Zahodiť ich je lacnejšie než fit cez pomiešané súradnice.
        /// </summary>
        void OnTrackingLost(NotTrackingReason reason)
        {
            m_Observations.Clear();
            m_Settle.Clear();
        }

        /// <summary>
        /// Sleduje, ako dlho a z akých smerov je ktorá zameraná značka v stave Tracking.
        ///
        /// Ustálená je, keď je sledovaná aspoň <see cref="m_SettleMinSeconds"/> a smer pohľadu
        /// kamery na ňu sa od prvého zbadania zmenil aspoň o <see cref="m_SettleMinSweepDegrees"/>.
        /// Samotné čakanie nestačí: M2 v behu 134724 hlásila zlú polohu pokojne niekoľko sekúnd
        /// a ARCore ju opravil, až keď sa telefón pohol do strany. Približovanie k značke paralaxu
        /// nedá, lebo ide po tom istom lúči — preto uhol, nie prejdená dráha.
        ///
        /// Zabúda sa pri strate trackingu a pri relokalizačnom skoku, z rovnakého dôvodu ako
        /// observácie: po nich je každá póza z inej mapy.
        /// </summary>
        void UpdateSettle()
        {
            var segment = m_Travel != null ? m_Travel.JumpGeneration : 0;
            if (segment != m_SettleSegment)
            {
                m_SettleSegment = segment;
                m_Settle.Clear();
            }

            var camera = m_Camera != null ? m_Camera
                : Camera.main != null ? Camera.main.transform : null;
            if (camera == null)
                return;

            foreach (var image in m_TrackedImageManager.trackables)
            {
                if (image.trackingState != TrackingState.Tracking || AnchorFor(image) == null)
                    continue;

                var direction = image.transform.position - camera.position;
                if (direction.sqrMagnitude < 1e-6f)
                    continue;
                direction.Normalize();

                var name = image.referenceImage.name;
                if (!m_Settle.TryGetValue(name, out var state))
                {
                    m_Settle[name] = new SettleState { since = Time.time, firstDirection = direction };
                    continue;
                }

                if (state.settled)
                    continue;

                state.sweepDegrees = Mathf.Max(state.sweepDegrees,
                    Vector3.Angle(state.firstDirection, direction));

                if (Time.time - state.since >= m_SettleMinSeconds
                    && state.sweepDegrees >= m_SettleMinSweepDegrees)
                {
                    state.settled = true;
                    MarkerSettled?.Invoke(name, Time.time - state.since, state.sweepDegrees);
                }

                m_Settle[name] = state;
            }
        }

        void OnDestroy()
        {
            if (m_Continuity != null)
                m_Continuity.Lost -= OnTrackingLost;
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
            var anchor = AnchorFor(TrackedMarker);
            if (anchor == null)
            {
                Debug.LogWarning($"{nameof(MarkerAlignment)}: the marker stopped being tracked "
                    + "before the burst was applied. Dropping it rather than guessing.", this);
                m_Positions.Clear();
                m_Rotations.Clear();
                State = AlignmentState.Waiting;
                return;
            }

            var anchorLocalPosition = m_AlignmentRoot.InverseTransformPoint(anchor.position);
            var anchorLocalRotation = Quaternion.Inverse(m_AlignmentRoot.rotation) * anchor.rotation;

            // Burst dobehol celý v stave Tracking, takže je to platné pozorovanie bez ohľadu na
            // to, či sa z neho hneď bude riešiť. O prijatí rozhodne rozptyl polohy.
            m_Observations.MaxSpreadMeters = m_MaxPositionSpreadMeters;
            LastBurstAccepted = m_Observations.Offer(m_BurstImageName, position, SampleSpreadMeters,
                m_Travel != null ? m_Travel.JumpGeneration : 0, Time.time);

            if (!LastBurstAccepted)
                Debug.LogWarning($"{nameof(MarkerAlignment)}: burst na '{ShortName(m_BurstImageName)}' "
                    + $"neprijatý do fitu — rozptyl {SampleSpreadMeters * 100f:F1} cm je nad prahom "
                    + $"{m_MaxPositionSpreadMeters * 100f:F1} cm.", this);

            var segment = m_Travel != null ? m_Travel.JumpGeneration : 0;
            var usable = m_Observations.Current(segment, Time.time, m_ObservationMaxAgeSeconds);

            RejectedFitErrorMeters = -1f;
            var inputs = new System.Text.StringBuilder();

            var pairs = new List<AlignmentSolver.Correspondence>();
            foreach (var observation in usable)
            {
                var surveyed = AnchorNamed(observation.imageName);
                if (surveyed == null)
                    continue;

                // Cieľ obmedzuje aj fit, nie len burst. Inak by `target M1` minútu po pohľade
                // na M2 dal fit z oboch a Test F by nemeral jednu značku, ako tvrdí protokol.
                if (m_TargetImageName.Length > 0 && observation.imageName != m_TargetImageName)
                    continue;

                if (inputs.Length > 0)
                    inputs.Append(" + ");
                inputs.Append(ShortName(observation.imageName)).Append(' ')
                    .Append((Time.time - observation.time).ToString("F1", System.Globalization.CultureInfo.InvariantCulture))
                    .Append("s ")
                    .Append((observation.spreadMeters * 100f).ToString("F1", System.Globalization.CultureInfo.InvariantCulture))
                    .Append("cm");

                pairs.Add(new AlignmentSolver.Correspondence
                {
                    imageName = observation.imageName,
                    modelPosition = m_AlignmentRoot.InverseTransformPoint(surveyed.position),
                    measuredPosition = observation.measuredPosition,
                });
            }

            FitInputs = inputs.Length > 0 ? inputs.ToString() : "-";

            Vector3 rootPosition;
            Quaternion rootRotation;

            if (AlignmentSolver.TrySolve(pairs, out var fit) && FitLooksSane(fit))
            {
                // Fit má len kurz, takže je vzpriamený z konštrukcie a gravitácia sa naň
                // neaplikuje — nie je čo stavať.
                rootPosition = fit.rootPose.position;
                rootRotation = fit.rootPose.rotation;

                FitMarkerCount = fit.markerCount;

                // Zvyšok existuje od dvoch značiek vyššie: dve dávajú štyri vodorovné rovnice pre
                // tri neznáme, takže sústava je preurčená a rozdiel dĺžok sa medzi ne rozdelí.
                FitWorstResidualMeters = fit.worstResidualMeters;
                FitWorstResidualImage = fit.worstResidualImage;
                FitBaselineErrorMeters = fit.baselineErrorMeters;
                LevelledDegrees = 0f;
            }
            else
            {
                // Jedna značka: kurz sa z jednej polohy určiť nedá, tak sa vezme z jej natočenia
                // ako pred ADR 010 a postaví sa gravitáciou.
                SolveRootPose(position, rotation, anchorLocalPosition, anchorLocalRotation,
                    out rootPosition, out rootRotation);

                LevelledDegrees = m_LevelWithGravity
                    ? LevelRootPose(position, anchorLocalPosition, ref rootPosition, ref rootRotation)
                    : 0f;

                FitMarkerCount = 1;
                FitWorstResidualMeters = -1f;
                FitWorstResidualImage = "";
                FitBaselineErrorMeters = 0f;
            }

            LastMeasuredPose = new Pose(position, rotation);
            LastRootPose = new Pose(rootPosition, rootRotation);

            // Veľkosť opravy je drift nazbieraný od minulého fitu, odčítaný ako číslo namiesto
            // odhadu okom. V meracom režime je to skok, ktorý si niekto vyžiadal; v navigačnom
            // je to samotný výsledok merania.
            LastCorrectionMeters = LastAlignmentTime < 0f
                ? 0f
                : Vector3.Distance(m_AlignmentRoot.position, rootPosition);

            // Through the anchor when there is one. A pose written straight into the transform is
            // correct for exactly as long as ARCore's idea of the world does not change, and the
            // whole point of the marker is to survive the moments when it does.
            if (m_Anchored != null)
                m_Anchored.PlaceAt(new Pose(rootPosition, rootRotation));
            else
                m_AlignmentRoot.SetPositionAndRotation(rootPosition, rootRotation);

            LastAlignmentTime = Time.time;
            State = AlignmentState.Aligned;
            AlignedImageName = m_BurstImageName;

            Debug.Log($"{nameof(MarkerAlignment)}: aligned on '{m_BurstImageName}' from "
                + $"{m_Positions.Count} samples, spread {SampleSpreadMeters * 100f:F1} cm / "
                + $"{SampleSpreadDegrees:F2} deg.", this);

            m_Positions.Clear();
            m_Rotations.Clear();

            Aligned?.Invoke();
        }

        void WarnIfAnchorLooksUnset()
        {
            if (m_WarnedAboutUnsetAnchor)
                return;

            foreach (var marker in m_Markers)
            {
                if (marker.anchor == null) continue;
                if (marker.anchor.localPosition != Vector3.zero
                    || marker.anchor.localRotation != Quaternion.identity)
                    continue;

                m_WarnedAboutUnsetAnchor = true;
                Debug.LogWarning($"{nameof(MarkerAlignment)}: '{marker.anchor.name}' is still at the "
                    + "origin with no rotation. The overlay will land somewhere meaningless until "
                    + "the marker's surveyed pose is entered.", this);
            }
        }

        /// <summary>
        /// Stands the solved root upright and puts the anchor back on the measured position.
        /// Returns how far it had to turn it, in degrees.
        ///
        /// A tracked image gives its position to a fraction of a centimetre and its rotation to
        /// whole degrees: the out-of-plane tilt of a flat target is the badly conditioned part of
        /// the estimate, and it does not average away because it is a bias, not noise. One run in
        /// the break room reported the marker's own "up" between 2.6° and 5.7° off vertical, and
        /// the two markers on that one flat wall disagreed about the wall's normal by 8.0°. Both
        /// are impossible: the paper hangs on a vertical wall and the wall has one normal.
        ///
        /// The tilt turns the whole overlay about an axis lying in the wall, which is why the wall
        /// carrying the markers looks right while everything at an angle to it climbs or sinks —
        /// 4.5° is 0.78 m at ten metres.
        ///
        /// Gravity is the cure and it is already in hand. ARCore's session space is gravity
        /// aligned from the IMU, its pitch and roll are good to a fraction of a degree and do not
        /// drift, and the model is built upright. So the marker is believed about heading and
        /// position and disbelieved about tilt.
        ///
        /// The turn is the shortest one that brings the root's own up onto the world's, so no
        /// heading is invented while it happens. Position is then re-derived rather than kept:
        /// the anchor sits metres away from the root inside the model, so turning the root would
        /// swing it off the marker. Re-deriving pins the anchor back on the measured position,
        /// which is the half of the measurement worth trusting.
        /// </summary>
        static float LevelRootPose(Vector3 measuredPosition, Vector3 anchorLocalPosition,
            ref Vector3 rootPosition, ref Quaternion rootRotation)
        {
            var up = rootRotation * Vector3.up;
            var upright = Quaternion.FromToRotation(up, Vector3.up);

            rootRotation = upright * rootRotation;
            rootPosition = measuredPosition - rootRotation * anchorLocalPosition;

            return Vector3.Angle(up, Vector3.up);
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
