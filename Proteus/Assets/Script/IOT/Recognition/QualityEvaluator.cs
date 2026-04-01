using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Centralized action algorithm module.
    /// - Action detection: camera only
    /// - Quality scoring: camera + motor (+ optional IMU score)
    /// </summary>
    public class QualityEvaluator
    {
        private readonly FitnessConfig config;

        public QualityEvaluator(FitnessConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Detect whether an action is present using camera data only.
        /// </summary>
        public bool IsActionDetected(CameraData camera)
        {
            if (camera == null)
                return false;

            return camera.IsValidAction();
        }

        public float EvaluateActionQuality(CameraData camera, MotorData motor, IMUData imu)
        {
            float imuScore01 = EvaluateImuScore01(imu);
            return EvaluateActionQuality(camera, motor, imuScore01);
        }

        /// <summary>
        /// Evaluate action quality from camera/motor and optional IMU score.
        /// Returns a score in [0, 100].
        /// </summary>
        /// <param name="imuScore01">Optional IMU quality in [0,1]. Use 1 when unavailable.</param>
        public float EvaluateActionQuality(CameraData camera, MotorData motor, float imuScore01 = 1f)
        {
            if (!IsActionDetected(camera))
                return 0f;

            // Base score from camera confidence (0-1 → 0-100)
            float poseScore = camera.confidence * 100f;

            // Force multiplier: 0.5x to 1.5x based on force (0-100)
            // Formula: 0.5 + (force/100) * 1.0 = range [0.5, 1.5]
            float forceMultiplier = config.MinForceMultiplier + (motor.force / 100f) * (config.MaxForceMultiplier - config.MinForceMultiplier);

            // IMU factor in [0,1], currently defaulted to 1 when no IMU is integrated.
            float imuFactor = Mathf.Clamp01(imuScore01);

            // Final quality = pose score × force multiplier × imu factor
            float quality = poseScore * forceMultiplier * imuFactor;

            // Clamp to 0-100
            return Mathf.Clamp(quality, 0f, 100f);
        }

        /// <summary>
        /// Convert IMU movement quality into a normalized multiplier in [0,1].
        /// </summary>
        public float EvaluateImuScore01(IMUData imu)
        {
            if (imu == null)
                return 1f;

            float accMagnitude = imu.acceleration.magnitude;
            float gyroMagnitude = imu.gyroscope.magnitude;

            // Keep default behavior stable: no movement maps to 1f.
            if (accMagnitude < 0.01f && gyroMagnitude < 0.01f)
                return 1f;

            float accScore = Mathf.Clamp01(accMagnitude / 12f);
            float gyroScore = Mathf.Clamp01(gyroMagnitude / 250f);
            return Mathf.Clamp01(0.7f * accScore + 0.3f * gyroScore);
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
