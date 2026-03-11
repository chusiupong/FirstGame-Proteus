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
        /// Get base muscle distribution for an action type from config
        /// </summary>
        private MuscleData GetBaseMusclesForAction(ActionType action)
        {
            switch (action)
            {
                case ActionType.BowDraw:
                    return config.BowDrawMuscles;
                
                case ActionType.FacePull:
                    return config.FacePullMuscles;
                
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
