using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Calculates experience points gained from training
    /// All experience-related formulas are centralized here
    /// </summary>
    public class ExperienceCalculator
    {
        private FitnessConfig config;

        public ExperienceCalculator(FitnessConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Calculate experience points from muscle gains and quality
        /// Formula: (totalMuscleGains × expMultiplier) × qualityBonus
        /// </summary>
        public float CalculateExpGain(MuscleData muscleGains, float quality)
        {
            // Base experience from muscle training
            float baseExp = muscleGains.GetTotal() * config.ExpMultiplier;

            // Quality bonus multiplier
            float qualityBonus = GetQualityBonus(quality);

            return baseExp * qualityBonus;
        }

        /// <summary>
        /// Get experience bonus multiplier based on quality
        /// </summary>
        private float GetQualityBonus(float quality)
        {
            if (quality >= config.QualityThresholdS)
                return config.ExpBonusS;
            else if (quality >= config.QualityThresholdA)
                return config.ExpBonusA;
            else if (quality >= config.QualityThresholdB)
                return config.ExpBonusB;
            else
                return config.ExpBonusC;
        }

        /// <summary>
        /// Get quality grade string for display
        /// </summary>
        public string GetQualityGrade(float quality)
        {
            if (quality >= config.QualityThresholdS) return "S";
            if (quality >= config.QualityThresholdA) return "A";
            if (quality >= config.QualityThresholdB) return "B";
            if (quality >= config.QualityThresholdC) return "C";
            return "F";
        }
    }
}
