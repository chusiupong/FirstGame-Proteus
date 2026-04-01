using System;
using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Muscle training data for 5 muscle groups
    /// </summary>
    [Serializable]
    public class MuscleData
    {
        [Header("Muscle Groups")]
        public float deltoid;
        public float trapezius;
        public float latissimus;
        public float rhomboid;
        public float biceps;

        public MuscleData()
        {
            deltoid = 0f;
            trapezius = 0f;
            latissimus = 0f;
            rhomboid = 0f;
            biceps = 0f;
        }

        public MuscleData(float deltoid, float trapezius, float latissimus, float rhomboid, float biceps)
        {
            this.deltoid = deltoid;
            this.trapezius = trapezius;
            this.latissimus = latissimus;
            this.rhomboid = rhomboid;
            this.biceps = biceps;
        }

        /// <summary>
        /// Add another MuscleData to this one
        /// </summary>
        public void Add(MuscleData other)
        {
            deltoid += other.deltoid;
            trapezius += other.trapezius;
            latissimus += other.latissimus;
            rhomboid += other.rhomboid;
            biceps += other.biceps;
        }

        /// <summary>
        /// Get total muscle training value
        /// </summary>
        public float GetTotal()
        {
            return deltoid + trapezius + latissimus + rhomboid + biceps;
        }

        public override string ToString()
        {
            return $"Muscles [D:{deltoid:F1} T:{trapezius:F1} L:{latissimus:F1} R:{rhomboid:F1} B:{biceps:F1}]";
        }
    }
}
