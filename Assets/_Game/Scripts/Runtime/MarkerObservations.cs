using System.Collections.Generic;
using UnityEngine;

namespace FriLens
{
    /// <summary>
    /// Drží poslednú prijatú observáciu každej značky a zahadzuje tie, ktoré prestali platiť.
    ///
    /// Zámerne bez <see cref="MonoBehaviour"/> a bez <c>Time.time</c>: čas aj číslo úseku
    /// trackingu sa podávajú zvonku, takže sa celá trieda dá overiť bez scény a bez telefónu
    /// cez FriLens &gt; Verify Alignment Solver.
    ///
    /// Najdôležitejšie je zahadzovanie pri relokalizačnom skoku. Pozorovania spred a spoza
    /// skoku sú v dvoch rôznych mapách a fit cez ne dá pózu, ktorá nepatrí ani jednej — beh
    /// 134644 mal po 45,9 m chôdze skok 4,818 m a poloha tej istej nepohnutej značky sa
    /// v session space rozišla až o 5,02 m.
    /// </summary>
    public class MarkerObservations
    {
        public struct Observation
        {
            public string imageName;
            public Vector3 measuredPosition;
            public float spreadMeters;
            public int segment;
            public float time;
        }

        /// <summary>
        /// Nad týmto rozptylom polohy sa burst neprijme. Namerané býva 0,1 – 1,2 cm, takže
        /// dva centimetre prepustia poctivé zarovnanie a zastavia meranie šumu.
        /// </summary>
        public float MaxSpreadMeters { get; set; } = 0.02f;

        readonly Dictionary<string, Observation> m_ByImage = new();

        /// <summary>Koľko značiek je práve v zásobe, bez ohľadu na platnosť.</summary>
        public int Count => m_ByImage.Count;

        /// <summary>
        /// Ponúkne observáciu. Vráti, či sa prijala. Prijatá prepíše staršiu tej istej značky:
        /// dve pozorovania jednej značky nie sú dva body pre fit, je to jeden bod dvakrát.
        /// </summary>
        public bool Offer(string imageName, Vector3 measuredPosition, float spreadMeters,
            int segment, float atTime)
        {
            if (string.IsNullOrEmpty(imageName))
                return false;

            if (spreadMeters > MaxSpreadMeters)
                return false;

            m_ByImage[imageName] = new Observation
            {
                imageName = imageName,
                measuredPosition = measuredPosition,
                spreadMeters = spreadMeters,
                segment = segment,
                time = atTime,
            };
            return true;
        }

        /// <summary>
        /// Observácie, ktoré sa smú použiť: z aktuálneho úseku trackingu a nie staršie než
        /// <paramref name="maxAgeSeconds"/>.
        ///
        /// Vracia vlastný zoznam pri každom volaní. Zdieľaná vyrovnávacia pamäť by ušetrila
        /// alokáciu, ale metóda beží pri dokončení burstu, nie po snímkoch, takže niet čo
        /// šetriť — a dva výsledky držané naraz by sa ticho prepísali. Presne o to sa porezala
        /// prvá kontrola, ktorá ju použila.
        /// </summary>
        public List<Observation> Current(int segment, float now, float maxAgeSeconds)
        {
            var usable = new List<Observation>();
            foreach (var observation in m_ByImage.Values)
            {
                if (observation.segment != segment)
                    continue;
                if (now - observation.time > maxAgeSeconds)
                    continue;
                usable.Add(observation);
            }
            return usable;
        }

        /// <summary>
        /// Zabudne všetko. Volá sa pri strate trackingu: potom je poloha každej uloženej značky
        /// odhad z mapy, ktorá sa medzitým mohla prekresliť.
        /// </summary>
        public void Clear() => m_ByImage.Clear();
    }
}
