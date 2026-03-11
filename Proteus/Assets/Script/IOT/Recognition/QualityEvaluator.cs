using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Evaluates action quality based on camera pose and motor force
    /// Quality Score = Pose Correctness × Force Factor
    /// </summary>
    public class QualityEvaluator
    {
        private FitnessConfig config;

        public QualityEvaluator(FitnessConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Evaluate action quality from camera and motor data
        /// Returns a score from 0-100
        /// Quality = Camera Confidence × Force Multiplier
        /// </summary>
        public float EvaluateQuality(CameraData camera, MotorData motor)
        {
            if (!camera.IsValidAction() || !motor.IsActive())
                return 0f;

            // Base score from camera confidence (0-1 → 0-100)
            float poseScore = camera.confidence * 100f;

            // Force multiplier: 0.5x to 1.5x based on force (0-100)
            // Formula: 0.5 + (force/100) * 1.0 = range [0.5, 1.5]
            float forceMultiplier = config.MinForceMultiplier + (motor.force / 100f) * (config.MaxForceMultiplier - config.MinForceMultiplier);

            // Final quality = pose score × force multiplier
            float quality = poseScore * forceMultiplier;

            // Clamp to 0-100
            return Mathf.Clamp(quality, 0f, 100f);
        }

        /// <summary>
        /// Calculate attack power based on quality
        /// Higher quality = stronger attack
        /// </summary>
        public float CalculateAttackPower(float quality)
        {
            // Quality 0-100 → 0.5x to 2.0x multiplier
            float multiplier = 0.5f + (quality / 100f) * 1.5f;
            return config.BaseAttackDamage * multiplier;
        }
    }
}
