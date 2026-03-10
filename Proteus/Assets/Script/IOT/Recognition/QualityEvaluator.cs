using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Evaluates action quality based on camera pose and motor force
    /// Quality Score = Pose Correctness × Force Factor
    /// </summary>
    public class QualityEvaluator
    {
        /// <summary>
        /// Evaluate action quality from camera and motor data
        /// Returns a score from 0-100
        /// </summary>
        public float EvaluateQuality(CameraData camera, MotorData motor)
        {
            if (!camera.IsValidAction() || !motor.IsActive())
                return 0f;

            // Base score from camera confidence (0-1 → 0-100)
            float poseScore = camera.confidence * 100f;

            // Force factor (0-100 force → 0.5-1.5 multiplier)
            // Minimum 50% even with low force, maximum 150% with high force
            float forceFactor = 0.5f + (motor.force / 100f);

            // Duration bonus: reward sustained effort (max 20% bonus)
            float durationBonus = Mathf.Min(motor.duration * 5f, 20f);

            // Final quality = base × force factor + duration bonus
            float quality = (poseScore * forceFactor) + durationBonus;

            // Clamp to 0-100
            return Mathf.Clamp(quality, 0f, 100f);
        }

        /// <summary>
        /// Get quality grade (S/A/B/C/F)
        /// </summary>
        public string GetQualityGrade(float quality)
        {
            if (quality >= 90f) return "S";
            if (quality >= 75f) return "A";
            if (quality >= 60f) return "B";
            if (quality >= 40f) return "C";
            return "F";
        }

        /// <summary>
        /// Calculate attack power based on quality
        /// Higher quality = stronger attack
        /// </summary>
        public float CalculateAttackPower(float quality, int baseAttack = 10)
        {
            // Quality 0-100 → 0.5x to 2.0x multiplier
            float multiplier = 0.5f + (quality / 100f) * 1.5f;
            return baseAttack * multiplier;
        }
    }
}
