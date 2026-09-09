using UnityEngine;

namespace FriLens
{
    /// <summary>
    /// Odhaduje, nakoľko sa dá prekryvu ešte veriť.
    ///
    /// Aplikácia dnes ukazuje prekryv rovnako sebavedome sekundu po zarovnaní aj po 46 metroch
    /// chôdze a relokalizačnom skoku 4,8 m. Pritom o oboje vie — <see cref="CameraTravel"/>
    /// obe čísla ráta a pri každom zarovnaní ich nuluje, takže sú to rovno „od zarovnania".
    /// Toto z nich spraví jedno.
    ///
    /// Prekryv sa nikdy neskrýva. Zmiznutie by v teréne vyzeralo ako pád aplikácie a mlčky by
    /// zobralo možnosť pozrieť sa, ako veľmi je vedľa — čo je pri meraní tá zaujímavá informácia.
    /// </summary>
    public class AlignmentConfidence : MonoBehaviour
    {
        [SerializeField] MarkerAlignment m_Alignment;
        [SerializeField] CameraTravel m_Travel;

        [Tooltip("Metre prejdené od zarovnania, po ktorých už prekryv nemá dôveru. Baseline "
            + "merania dávajú drift rádovo jednotky percent dráhy, takže tridsať metrov je "
            + "zhruba meter vedľa.")]
        [SerializeField] float m_DistrustAfterMeters = 30f;

        [Tooltip("Relokalizačný skok posunie celú mapu pod prekryvom naraz. Jeden stačí na to, "
            + "aby zarovnanie prestalo platiť.")]
        [SerializeField] int m_DistrustAfterJumps = 1;

        /// <summary>1 tesne po zarovnaní, 0 keď mu už netreba veriť.</summary>
        public float Trust { get; private set; }

        /// <summary>Prečo dôvera klesla, na jeden riadok do HUD-u. Prázdne, keď je plná.</summary>
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

            // Obe počítadlá sa pri zarovnaní nulujú, takže sa neporovnávajú s ničím ďalším.
            var byDistance = m_DistrustAfterMeters <= 0f
                ? 1f
                : Mathf.Clamp01(1f - walked / m_DistrustAfterMeters);

            // Skok nie je postupné zhoršovanie, je to jednorazový posun celej mapy. Preto nuluje
            // dôveru naraz a nie úmerne.
            var byJumps = jumps >= m_DistrustAfterJumps ? 0f : 1f;

            Trust = Mathf.Min(byDistance, byJumps);

            if (jumps >= m_DistrustAfterJumps)
                Reason = jumps + "x skok mapy";
            else if (byDistance < 1f)
                Reason = walked.ToString("F0") + " m od zarovnania";
            else
                Reason = "";
        }
    }
}
