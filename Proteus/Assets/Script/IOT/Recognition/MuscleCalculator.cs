using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Calculates muscle training gains based on action type and quality
    /// Different actions train different muscle groups
    /// </summary>
    public class MuscleCalculator
    {
        private FitnessConfig config;

        public MuscleCalculator(FitnessConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Calculate muscle training gains for BowDraw action
        /// </summary>
        public MuscleData CalculateMuscleGains(float quality)
        {
            // Get base muscle distribution for bow draw
            MuscleData baseMuscles = config.BowDrawMuscles;

            // Scale by quality (0-100% quality = 0-100% gains)
            float qualityFactor = quality / 100f;

            return new MuscleData(
                deltoid: baseMuscles.deltoid * qualityFactor,
                trapezius: baseMuscles.trapezius * qualityFactor,
                latissimus: baseMuscles.latissimus * qualityFactor,
                rhomboid: baseMuscles.rhomboid * qualityFactor,
                biceps: baseMuscles.biceps * qualityFactor
            );
        }

        /// <summary>
        /// Get primary muscle groups for UI display
        /// </summary>
        public string GetPrimaryMuscles()
        {
            return "Latissimus, Trapezius, Deltoid";
        }
    }
}
