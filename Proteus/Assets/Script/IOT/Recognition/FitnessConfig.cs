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

        // ==================== ROUND TIMING ====================
        
        /// <summary>
        /// Action timeout in seconds
        /// If no action is performed within this time during a round, it's a MISS
        /// </summary>
        public float ActionTimeout = 5f;

        // ==================== MOTOR POWER CONTROL ====================
        
        public bool UseESP32Motor = true;  // Whether to use true data

        public bool IsMotorPowerOn = false; // Current motor power state, updated by FitnessManager

        /// <summary>
        /// Auto power on motor when a round starts
        /// </summary>
        public bool AutoPowerOnMotor = true;

        /// <summary>
        /// Auto power off motor when a round ends
        /// </summary>
        public bool AutoPowerOffMotor = true;

        /// <summary>
        /// Which motors to control (0=Motor1, 1=Motor2, 2=Both)
        /// </summary>
        public byte MotorControlTarget = 2;

        /// <summary>
        /// ESP32 serial port name for motor/IMU stream, e.g. COM3.
        /// </summary>
        public string Esp32PortName = "COM3";

        /// <summary>
        /// ESP32 serial baud rate.
        /// </summary>
        public int Esp32BaudRate = 115200;

        /// <summary>
        /// Camera bridge serial port name, e.g. COM7.
        /// Must be different from ESP32 port if both run on the same PC.
        /// </summary>
        public string CameraPortName = "COM7";

        /// <summary>
        /// Camera bridge serial baud rate.
        /// </summary>
        public int CameraBaudRate = 115200;

        // ==================== IMU ESP32 ====================
        public bool UseEsp32IMU = true;  // Whether to read IMU data from ESP32 stream

        // ==================== camera ====================
        public bool UseCameraBridge = true;  // Whether to read camera data from separate serial bridge

        // ==================== IOT protection and arrangement ====================

        public float SensorTimeoutSeconds = 3f;

        // ==================== QUALITY THRESHOLDS ====================
        
        public float QualityThresholdS = 90f;
        public float QualityThresholdA = 75f;
        public float QualityThresholdB = 60f;
        public float QualityThresholdC = 40f;
    }
}
