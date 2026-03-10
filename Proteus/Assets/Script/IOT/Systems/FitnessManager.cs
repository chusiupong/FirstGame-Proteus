using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Main fitness system manager - coordinates all IoT components
    /// Singleton pattern for easy access from game code
    /// </summary>
    public class FitnessManager : MonoBehaviour
    {
        // Singleton instance
        public static FitnessManager Instance { get; private set; }

        [Header("Input Systems")]
        private ICameraInput cameraInput;
        private IMotorInput motorInput;

        [Header("Processing Systems - DISABLED FOR NOW")]
        // private QualityEvaluator qualityEvaluator;  // ⏸️ 暂时不用，等硬件来了再启用
        // private MuscleCalculator muscleCalculator;  // ⏸️ 暂时不用，等硬件来了再启用

        [Header("Player Data")]
        public PlayerFitnessData playerData;

        [Header("Settings")]
        public bool useMockData = true;  // Use mock data for testing
        public float actionCooldown = 0.5f;  // Minimum time between actions

        [Header("Simple Test Values (Keyboard Mode)")]
        public float simpleExpGain = 50f;  // 每次动作固定给50经验
        public float simpleAttackPower = 15f;  // 每次攻击固定15伤害

        private float lastActionTime = 0f;
        private ActionData currentAction;

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
            Debug.Log("🎮 Fitness Manager Initializing...");

            // Initialize input systems
            if (useMockData)
            {
                cameraInput = new MockCameraInput();
                motorInput = new MockMotorInput();
                Debug.Log("📝 Using MOCK data for testing");
            }
            else
            {
                // TODO: Initialize real hardware when available
                Debug.LogWarning("⚠️ Real hardware not implemented yet");
                cameraInput = new MockCameraInput();
                motorInput = new MockMotorInput();
            }

            cameraInput.Initialize();
            motorInput.Initialize();

            // Initialize processing systems - DISABLED FOR NOW
            // qualityEvaluator = new QualityEvaluator();  // ⏸️ 暂时不用
            // muscleCalculator = new MuscleCalculator();  // ⏸️ 暂时不用

            // Initialize or load player data
            playerData = new PlayerFitnessData();
            // TODO: Load from saved data

            currentAction = new ActionData();

            Debug.Log("✅ Fitness Manager Ready!");
        }

        void Update()
        {
            // Check for player actions every frame
            ProcessInput();
        }

        /// <summary>
        /// Process input from camera and motor sensors
        /// </summary>
        void ProcessInput()
        {
            // Cooldown check
            if (Time.time - lastActionTime < actionCooldown)
                return;

            // Get data from both input sources
            CameraData cameraData = cameraInput.GetCameraData();
            MotorData motorData = motorInput.GetMotorData();

            // Check if a valid action is detected
            if (cameraData.IsValidAction() && motorData.IsActive())
            {
                // Process the action
                ProcessAction(cameraData, motorData);
                lastActionTime = Time.time;
            }
        }

        /// <summary>
        /// Process a detected action
        /// SIMPLIFIED VERSION - Using fixed values for keyboard testing
        /// </summary>
        void ProcessAction(CameraData camera, MotorData motor)
        {
            // ========== SIMPLIFIED VERSION (Keyboard Testing) ==========
            // 暂时用固定值，不做复杂计算
            
            // Fixed quality for now
            float quality = 75f;  // 假设质量都是75分
            
            // Simple muscle gains based on action type
            MuscleData muscleGains = GetSimpleMuscleGains(camera.detectedAction);
            
            // Fixed experience
            float expGain = simpleExpGain;
            
            // Fixed attack power
            float attackPower = simpleAttackPower;

            // Create action data
            currentAction = new ActionData
            {
                actionType = camera.detectedAction,
                qualityScore = quality,
                attackPower = attackPower,
                muscleGain = muscleGains,
                expGain = expGain
            };

            // Apply to player data
            playerData.AddTraining(muscleGains, expGain);

            // Log the action (simplified)
            Debug.Log($"💥 {camera.detectedAction} performed! | " +
                      $"Attack: {attackPower:F1} | EXP: +{expGain:F1}");
            Debug.Log($"🏋️ Muscles trained: {muscleGains}");
            Debug.Log($"📊 Level: {playerData.level} | EXP: {playerData.experience:F0}/{playerData.experienceToNextLevel:F0}");

            // TODO: Trigger game events (attack animation, damage calculation, etc.)
        }

        /// <summary>
        /// Get simple fixed muscle gains for testing
        /// 简单的固定肌肉增长值（用于测试）
        /// </summary>
        MuscleData GetSimpleMuscleGains(ActionType action)
        {
            switch (action)
            {
                case ActionType.BowDraw:
                    // 拉弓：主要练背和手臂
                    return new MuscleData(
                        deltoid: 8f,
                        trapezius: 10f,
                        latissimus: 12f,
                        rhomboid: 6f,
                        biceps: 7f
                    );
                
                case ActionType.FacePull:
                    // Face Pull：主要练肩和上背
                    return new MuscleData(
                        deltoid: 10f,
                        trapezius: 12f,
                        latissimus: 5f,
                        rhomboid: 9f,
                        biceps: 4f
                    );
                
                default:
                    return new MuscleData();
            }
        }

        /// <summary>
        /// Get the last performed action data (for combat system)
        /// </summary>
        public ActionData GetLastAction()
        {
            return currentAction;
        }

        /// <summary>
        /// Get attack power for current action
        /// </summary>
        public float GetCurrentAttackPower()
        {
            return currentAction.attackPower;
        }

        /// <summary>
        /// Get player's level-based attack bonus
        /// </summary>
        public int GetPlayerAttackBonus()
        {
            return playerData.GetAttackBonus();
        }

        /// <summary>
        /// Check if action is ready (cooldown finished)
        /// </summary>
        public bool IsActionReady()
        {
            return Time.time - lastActionTime >= actionCooldown;
        }

        void OnDestroy()
        {
            // Cleanup
            if (cameraInput != null)
                cameraInput.Shutdown();
            if (motorInput != null)
                motorInput.Shutdown();
            
            // TODO: Save player data
        }

        // === Public API for other systems ===

        /// <summary>
        /// Get current player fitness data
        /// </summary>
        public PlayerFitnessData GetPlayerData()
        {
            return playerData;
        }

        /// <summary>
        /// Force save player data
        /// </summary>
        public void SavePlayerData()
        {
            // TODO: Implement data persistence
            Debug.Log("💾 Player data saved");
        }
    }
}
