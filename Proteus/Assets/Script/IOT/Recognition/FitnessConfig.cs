using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Centralized configuration for all fitness calculations
    /// Adjust parameters here to tune the game balance
    /// </summary>
    public class FitnessConfig
    {
        // ==================== MUSCLE CONFIGURATION ====================
        
        /// <summary>
        /// Base muscle gains for Bow Draw action (at 100% quality)
        /// </summary>
        public MuscleData BowDrawMuscles = new MuscleData(
            deltoid: 8f,      // Rear deltoid engagement
            trapezius: 10f,   // Scapular retraction
            latissimus: 12f,  // Primary pulling muscle
            rhomboid: 6f,     // Scapular stability
            biceps: 7f        // Arm flexion
        );

        /// <summary>
        /// Base muscle gains for Face Pull action (at 100% quality)
        /// </summary>
        public MuscleData FacePullMuscles = new MuscleData(
            deltoid: 10f,     // Primary rear deltoid focus
            trapezius: 12f,   // Primary upper back
            latissimus: 5f,   // Secondary back engagement
            rhomboid: 9f,     // Primary rhomboid activation
            biceps: 4f        // Secondary arm pull
        );

        // ==================== EXPERIENCE CONFIGURATION ====================
        
        /// <summary>
        /// Multiplier to convert total muscle gains to base experience
        /// Formula: baseExp = totalMuscleGains × expMultiplier
        /// </summary>
        public float ExpMultiplier = 2f;

        /// <summary>
        /// Experience bonus for quality grades
        /// </summary>
        public float ExpBonusS = 1.5f;   // S grade (90+): +50% exp
        public float ExpBonusA = 1.25f;  // A grade (75+): +25% exp
        public float ExpBonusB = 1.1f;   // B grade (60+): +10% exp
        public float ExpBonusC = 1.0f;   // C grade and below: no bonus

        // ==================== LEVEL CONFIGURATION ====================
        
        /// <summary>
        /// Base experience required for level 2
        /// </summary>
        public float LevelExpBase = 100f;

        /// <summary>
        /// Exponent for level progression curve
        /// Formula: expRequired = LevelExpBase × (level ^ LevelExpExponent)
        /// 1.0 = Linear, 1.5 = Moderate curve, 2.0 = Steep curve
        /// </summary>
        public float LevelExpExponent = 1.5f;

        /// <summary>
        /// Attack bonus per level
        /// </summary>
        public int AttackBonusPerLevel = 2;

        // ==================== QUALITY CONFIGURATION ====================
        
        /// <summary>
        /// Minimum force multiplier (when force = 0)
        /// </summary>
        public float MinForceMultiplier = 0.5f;

        /// <summary>
        /// Maximum force multiplier (when force = 100)
        /// Final quality = pose_score × force_multiplier
        /// Range: [MinForceMultiplier, MaxForceMultiplier]
        /// </summary>
        public float MaxForceMultiplier = 1.5f;

        /// <summary>
        /// Base attack damage for quality calculations
        /// </summary>
        public int BaseAttackDamage = 10;

        // ==================== COMBAT TIMING ====================
        
        /// <summary>
        /// Action timeout in seconds
        /// If no action is performed within this time during combat, it's a MISS
        /// </summary>
        public float ActionTimeout = 5f;

        // ==================== QUALITY THRESHOLDS ====================
        
        public float QualityThresholdS = 90f;
        public float QualityThresholdA = 75f;
        public float QualityThresholdB = 60f;
        public float QualityThresholdC = 40f;
    }
}
