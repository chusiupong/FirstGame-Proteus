using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Calculates muscle training gains based on action type and quality
    /// Different actions train different muscle groups
    /// </summary>
    public class MuscleCalculator
    {
        // Base muscle gains per action (at 100% quality)
        private readonly MuscleData bowDrawMuscles = new MuscleData(
            deltoid: 8f,      // High rear deltoid engagement
            trapezius: 10f,   // Primary: scapular retraction
            latissimus: 12f,  // Primary: pulling motion
            rhomboid: 6f,     // Secondary: scapular stability
            biceps: 7f        // Secondary: arm flexion
        );

        private readonly MuscleData facePullMuscles = new MuscleData(
            deltoid: 10f,     // Primary: rear deltoid
            trapezius: 12f,   // Primary: scapular retraction
            latissimus: 5f,   // Secondary: back engagement
            rhomboid: 9f,     // Primary: rhomboid activation
            biceps: 4f        // Secondary: arm pull
        );

        /// <summary>
        /// Calculate muscle training gains for an action
        /// </summary>
        public MuscleData CalculateMuscleGains(ActionType action, float quality)
        {
            // Get base muscle distribution for this action
            MuscleData baseMuscles = GetBaseMusclesForAction(action);

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
        /// Calculate experience points gained from training
        /// Base EXP = total muscle gains × 2
        /// </summary>
        public float CalculateExpGain(MuscleData muscleGains, float quality)
        {
            float baseExp = muscleGains.GetTotal() * 2f;

            // Quality bonus: S rank gets 50% more EXP
            float qualityBonus = 1f;
            if (quality >= 90f) qualityBonus = 1.5f;
            else if (quality >= 75f) qualityBonus = 1.25f;
            else if (quality >= 60f) qualityBonus = 1.1f;

            return baseExp * qualityBonus;
        }

        /// <summary>
        /// Get base muscle distribution for an action type
        /// </summary>
        private MuscleData GetBaseMusclesForAction(ActionType action)
        {
            switch (action)
            {
                case ActionType.BowDraw:
                    return bowDrawMuscles;
                
                case ActionType.FacePull:
                    return facePullMuscles;
                
                default:
                    return new MuscleData();
            }
        }

        /// <summary>
        /// Get primary muscle groups for an action (for UI display)
        /// </summary>
        public string GetPrimaryMuscles(ActionType action)
        {
            switch (action)
            {
                case ActionType.BowDraw:
                    return "Latissimus, Trapezius, Deltoid";
                
                case ActionType.FacePull:
                    return "Trapezius, Deltoid, Rhomboid";
                
                default:
                    return "None";
            }
        }
    }
}
