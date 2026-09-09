using System.Collections.Generic;
using UnityEngine;

namespace FriLens
{
    /// <summary>
    /// Spočíta pózu modelu z polôh značiek. Natočenie značiek sa nepoužije vôbec — dôvody
    /// v ADR 010.
    ///
    /// Hľadá tuhú transformáciu so štyrmi stupňami voľnosti: posun XYZ a kurz okolo zvislice.
    /// Sklon sa nefituje, takže výsledok je vzpriamený z konštrukcie a gravitácia sa naň
    /// nemusí aplikovať dodatočne.
    ///
    /// Mierka sa nefituje zámerne. Keby áno, model by sa natiahol tak, aby rozdiel medzi
    /// nameranou a modelovou vzdialenosťou značiek pohltil — a ten rozdiel je práve to, čo
    /// projekt meria. Vracia sa preto ako <see cref="Result.baselineErrorMeters"/>.
    ///
    /// Čistá matematika bez scény, aby sa dala overiť bez telefónu cez
    /// FriLens &gt; Verify Alignment Solver.
    /// </summary>
    public static class AlignmentSolver
    {
        /// <summary>Jedna značka: kde ju čaká model a kde ju ohlásil tracker.</summary>
        public struct Correspondence
        {
            /// <summary>Meno v knižnici referenčných obrázkov, kvôli hláseniu zvyškov.</summary>
            public string imageName;

            /// <summary>Poloha kotvy v súradniciach modelu, teda lokálne voči rootu.</summary>
            public Vector3 modelPosition;

            /// <summary>Poloha nameraná trackerom, v session space.</summary>
            public Vector3 measuredPosition;
        }

        public struct Result
        {
            /// <summary>Póza, ktorú má dostať AlignmentRoot.</summary>
            public Pose rootPose;

            /// <summary>Z koľkých značiek fit vznikol.</summary>
            public int markerCount;

            /// <summary>Najväčší zvyšok cez všetky značky, v metroch.</summary>
            public float worstResidualMeters;

            /// <summary>Meno značky, na ktorej ten najväčší zvyšok je.</summary>
            public string worstResidualImage;

            /// <summary>
            /// Pri presne dvoch značkách rozdiel nameranej a modelovej dĺžky ich spojnice,
            /// kladný keď je nameraná dlhšia. Inak 0 — pri troch a viac to jedno číslo
            /// neexistuje a hovoria za to zvyšky.
            /// </summary>
            public float baselineErrorMeters;

            /// <summary>
            /// Najväčšia vzdialenosť medzi dvomi značkami v súradniciach modelu. Je to mierka,
            /// voči ktorej má zmysel posudzovať zvyšky: dvadsať centimetrov na šesťmetrovej
            /// základni je iné číslo než dvadsať centimetrov na polmetrovej.
            /// </summary>
            public float modelSpanMeters;
        }

        /// <summary>
        /// Vráti false, keď je značiek menej než dve. Z jednej polohy sa kurz určiť nedá
        /// a hádať ho je horšie než to priznať; volajúci má v tom prípade použiť
        /// <see cref="MarkerAlignment.SolveRootPose"/> s natočením tej jednej značky.
        /// </summary>
        public static bool TrySolve(IReadOnlyList<Correspondence> pairs, out Result result)
        {
            result = default;
            if (pairs == null || pairs.Count < 2)
                return false;

            var modelCentre = Vector3.zero;
            var measuredCentre = Vector3.zero;
            foreach (var pair in pairs)
            {
                modelCentre += pair.modelPosition;
                measuredCentre += pair.measuredPosition;
            }
            modelCentre /= pairs.Count;
            measuredCentre /= pairs.Count;

            // Kurz z vodorovných zložiek vycentrovaných bodov. Otočenie okolo Y v Unity mapuje
            // (x, z) na (x cos + z sin, -x sin + z cos), z čoho maximalizovaný člen vyjde
            // C cos + S sin a optimum je atan2(S, C). Znamienko overuje YawSignMatchesUnity.
            float s = 0f;
            float c = 0f;
            foreach (var pair in pairs)
            {
                var a = pair.modelPosition - modelCentre;
                var o = pair.measuredPosition - measuredCentre;
                s += o.x * a.z - o.z * a.x;
                c += a.x * o.x + a.z * o.z;
            }

            // Všetky značky v jednom bode: kurz je nedourčený. Nula je aspoň predvídateľná.
            var yaw = (Mathf.Abs(s) < 1e-9f && Mathf.Abs(c) < 1e-9f)
                ? 0f
                : Mathf.Atan2(s, c) * Mathf.Rad2Deg;

            var rotation = Quaternion.Euler(0f, yaw, 0f);
            var position = measuredCentre - rotation * modelCentre;

            result.rootPose = new Pose(position, rotation);
            result.markerCount = pairs.Count;

            foreach (var pair in pairs)
            {
                var landed = position + rotation * pair.modelPosition;
                var residual = Vector3.Distance(landed, pair.measuredPosition);
                if (residual > result.worstResidualMeters)
                {
                    result.worstResidualMeters = residual;
                    result.worstResidualImage = pair.imageName;
                }
            }

            for (var i = 0; i < pairs.Count; i++)
            for (var j = i + 1; j < pairs.Count; j++)
            {
                var span = Vector3.Distance(pairs[i].modelPosition, pairs[j].modelPosition);
                if (span > result.modelSpanMeters)
                    result.modelSpanMeters = span;
            }

            if (pairs.Count == 2)
            {
                var measuredSpan = Vector3.Distance(pairs[0].measuredPosition, pairs[1].measuredPosition);
                result.baselineErrorMeters = measuredSpan - result.modelSpanMeters;
            }

            return true;
        }
    }
}
