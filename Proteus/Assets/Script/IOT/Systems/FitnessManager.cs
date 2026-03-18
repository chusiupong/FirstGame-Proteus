using System;
using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Orchestrates the IoT pipeline for one player round:
    /// input capture -> action resolution -> round state/timing.
    /// </summary>
    public class FitnessManager : MonoBehaviour
    {
        public event Action<ActionData> OnActionResolved;

        // Singleton instance
        public static FitnessManager Instance { get; private set; }

        private FitnessConfig config;
        private FitnessInputCollector inputCollector;
        private RoundWindowController roundWindow;
        private ActionResolutionService resolutionService;

        [Header("Player Data")]
        public PlayerFitnessData playerData;

        [Header("Settings")]
        public bool useMockData = true;  // Use mock data for testing
        public float actionCooldown = 0.5f;  // Minimum time between actions

        [Header("Round State")]
        private ActionData lastResolvedAction;

        void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Initialize()
        {
            Debug.Log("[IOT] FitnessManager initializing...");

            config = new FitnessConfig();
            inputCollector = new FitnessInputCollector(useMockData);
            roundWindow = new RoundWindowController();
            resolutionService = new ActionResolutionService(config);

            inputCollector.Initialize();

            // Initialize or load player data
            playerData = new PlayerFitnessData();
            playerData.experienceToNextLevel = resolutionService.CalculateExpForLevel(playerData.level);
            // TODO: Load from saved data

            lastResolvedAction = new ActionData();

            Debug.Log("[IOT] FitnessManager ready");
            Debug.Log("[IOT] Awaiting round control and action confirmation from external caller");
        }

        void Update()
        {
            // Check for round timeout
            CheckRoundTimeout();
        }

        /// <summary>
        /// Check if action timeout has been exceeded inside an active round.
        /// </summary>
        void CheckRoundTimeout()
        {
            if (roundWindow.TryConsumeTimeout(Time.time, config.ActionTimeout))
            {
                TriggerMiss();
            }
        }

        /// <summary>
        /// Trigger a MISS event (timeout)
        /// </summary>
        void TriggerMiss()
        {
            Debug.LogWarning($"[IOT][Round] MISS: no valid action within {config.ActionTimeout:F1}s");
            Debug.Log($"[IOT][Round] Timer reset. Next timeout: {config.ActionTimeout:F1}s");

            // TODO: Trigger miss penalty in game (e.g., take damage, lose combo)
        }

        /// <summary>
        /// Camera-only action detection check.
        /// </summary>
        public bool IsActionDetected()
        {
            inputCollector.ReadRawData(out CameraData cameraData, out _);
            return resolutionService.IsActionDetected(cameraData);
        }

        /// <summary>
        /// Try resolving one action from current sensor data.
        /// Returns true when an action is successfully resolved.
        /// </summary>
        public bool TryResolveCurrentAction()
        {
            if (!roundWindow.IsActionReady(Time.time, actionCooldown))
                return false;

            inputCollector.ReadRawData(out CameraData cameraData, out MotorData motorData);

            if (!resolutionService.IsActionDetected(cameraData))
                return false;

            if (!roundWindow.RoundActive)
            {
                Debug.LogWarning("[IOT][Round] Action ignored: round is not active. Call RoundStart first.");
                return false;
            }

            ResolveAction(cameraData, motorData);
            roundWindow.MarkActionResolved(Time.time);

            // Reset motor state to prevent force accumulation across actions
            inputCollector.ResetMotorState();

            // Restart round timeout for next action
            Debug.Log($"[IOT][Round] Action resolved. Next timeout: {config.ActionTimeout:F1}s");
            return true;
        }

        /// <summary>
        /// Resolve one valid action and publish the result.
        /// </summary>
        void ResolveAction(CameraData camera, MotorData motor)
        {
            lastResolvedAction = resolutionService.Resolve(camera, motor, playerData);

            OnActionResolved?.Invoke(lastResolvedAction);

            // ========== LOGGING ==========
            string grade = resolutionService.GetQualityGrade(lastResolvedAction.qualityScore);
            Debug.Log($"[IOT][Action] BowDraw resolved. Grade: {grade} | " +
                      $"Quality: {lastResolvedAction.qualityScore:F1}% | Attack: {lastResolvedAction.attackPower:F1} | EXP: +{lastResolvedAction.expGain:F1}");
            Debug.Log($"[IOT][Action] Muscles trained: {lastResolvedAction.muscleGain}");
            Debug.Log($"[IOT][Player] Level: {playerData.level} | EXP: {playerData.experience:F0}/{playerData.experienceToNextLevel:F0}");

            // TODO: Trigger game events (attack animation, damage calculation, etc.)
        }

        /// <summary>
        /// Get player's level-based attack bonus
        /// </summary>
        public int AttackBonus => resolutionService.GetAttackBonus(playerData);

        /// <summary>
        /// Start one player round window.
        /// Enables timeout and optionally powers on the motor.
        /// </summary>
        public void RoundStart()
        {
            roundWindow.RoundStart(Time.time);
            
            // Auto power on motor if enabled
            if (config.AutoPowerOnMotor)
            {
                PowerOnMotor();
            }
            
            Debug.Log($"[IOT][Round] START. Timeout: {config.ActionTimeout:F1}s");
        }

        /// <summary>
        /// End current player round window.
        /// Disables timeout and optionally powers off the motor.
        /// </summary>
        public void RoundEnd()
        {
            roundWindow.RoundEnd();
            
            // Auto power off motor if enabled
            if (config.AutoPowerOffMotor)
            {
                PowerOffMotor();
            }
            
            Debug.Log("[IOT][Round] END");
        }

        /// <summary>
        /// Send power-on command to motor and switch to work mode.
        /// </summary>
        public void PowerOnMotor()
        {
            if (inputCollector.PowerOnMotor(config.MotorControlTarget))
            {
                Debug.Log("[IOT][Motor] Power ON + WorkMode configured");
            }
            else
            {
                Debug.Log("[IOT][Motor] Power already ON");
            }
        }

        /// <summary>
        /// Send power-off command to motor.
        /// </summary>
        public void PowerOffMotor()
        {
            if (inputCollector.PowerOffMotor(config.MotorControlTarget))
            {
                Debug.Log("[IOT][Motor] Power OFF");
            }
            else
            {
                Debug.Log("[IOT][Motor] Power already OFF");
            }
        }

        /// <summary>
        /// Returns current motor power state tracked by FitnessManager.
        /// </summary>
        public bool IsMotorPowered()
        {
            return inputCollector.MotorPowered;
        }

        public float RemainingTime
        {
            get
            {
                return roundWindow.GetRemainingTime(Time.time, config.ActionTimeout);
            }
        }

        public bool RoundActive => roundWindow.RoundActive;

        /// <summary>
        /// Check if action is ready (cooldown finished)
        /// </summary>
        public bool IsActionReady()
        {
            return roundWindow.IsActionReady(Time.time, actionCooldown);
        }

        void OnDestroy()
        {
            // Cleanup
            if (inputCollector != null)
                inputCollector.Shutdown();
            
            // TODO: Save player data
        }

        // === Public API for other systems ===

        public PlayerFitnessData PlayerData => playerData;

        /// <summary>
        /// Force save player data
        /// </summary>
        public void SavePlayerData()
        {
            // TODO: Implement data persistence
            Debug.Log("[IOT][Player] Data saved");
        }
    }
}
