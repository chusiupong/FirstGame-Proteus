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

        [Header("Processing Systems")]
        private FitnessConfig config;
        private QualityEvaluator qualityEvaluator;
        private MuscleCalculator muscleCalculator;
        private ExperienceCalculator experienceCalculator;
        private LevelCalculator levelCalculator;

        [Header("Player Data")]
        public PlayerFitnessData playerData;

        [Header("Settings")]
        public bool useMockData = true;  // Use mock data for testing
        public float actionCooldown = 0.5f;  // Minimum time between actions

        [Header("Combat State")]
        private bool inCombat = false;  // Is player currently in combat?
        private float combatStartTime = 0f;  // When did combat start?
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

            // Initialize configuration
            config = new FitnessConfig();

            // Initialize processing systems
            qualityEvaluator = new QualityEvaluator(config);
            muscleCalculator = new MuscleCalculator(config);
            experienceCalculator = new ExperienceCalculator(config);
            levelCalculator = new LevelCalculator(config);

            // Initialize or load player data
            playerData = new PlayerFitnessData();
            playerData.experienceToNextLevel = levelCalculator.CalculateExpForLevel(playerData.level);
            // TODO: Load from saved data

            currentAction = new ActionData();

            Debug.Log("✅ Fitness Manager Ready!");
            Debug.Log("🎮 Controls: Q/E=Action | 1-5=Force | SPACE=Execute | T=Start Combat | Y=End Combat");
        }

        void Update()
        {
            // Check for combat timeout
            CheckCombatTimeout();
            
            // Check for player actions every frame
            ProcessInput();
        }

        /// <summary>
        /// Check if action timeout has been exceeded
        /// Triggers MISS if player fails to act in time
        /// </summary>
        void CheckCombatTimeout()
        {
            if (!inCombat)
                return;

            float timeSinceLastAction = Time.time - (lastActionTime > 0 ? lastActionTime : combatStartTime);
            
            if (timeSinceLastAction >= config.ActionTimeout)
            {
                // MISS! Player took too long
                TriggerMiss();
            }
        }

        /// <summary>
        /// Trigger a MISS event (timeout)
        /// </summary>
        void TriggerMiss()
        {
            Debug.LogWarning($"❌ MISS! No action within {config.ActionTimeout}s timeout!");
            
            // Continue combat but reset timer for next action
            lastActionTime = Time.time;  // Reset timer to give player another chance
            
            Debug.Log($"⏱️ Next action timeout: {config.ActionTimeout}s - Keep fighting!");
            
            // TODO: Trigger miss penalty in game (e.g., take damage, lose combo)
        }

        /// <summary>
        /// Process input from camera and motor sensors
        /// User must press SPACE to confirm action execution
        /// </summary>
        void ProcessInput()
        {
            // Test key: T to start combat (for testing timeout)
            if (Input.GetKeyDown(KeyCode.T) && !inCombat)
            {
                StartCombat();
            }
            
            // Test key: Y to end combat
            if (Input.GetKeyDown(KeyCode.Y) && inCombat)
            {
                EndCombat();
            }
            
            // Cooldown check
            if (Time.time - lastActionTime < actionCooldown)
                return;

            // Only execute action when SPACE is pressed (confirmation)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Get data from both input sources
                CameraData cameraData = cameraInput.GetCameraData();
                MotorData motorData = motorInput.GetMotorData();

                // Check if a valid action is ready
                if (cameraData.IsValidAction() && motorData.IsActive())
                {
                    // Check if in combat (only allow attacks during combat)
                    if (!inCombat)
                    {
                        Debug.LogWarning("⚠️ Not in combat! Press T to start combat first.");
                        return;
                    }
                    
                    // Process the action
                    ProcessAction(cameraData, motorData);
                    lastActionTime = Time.time;
                    
                    // ✅ Reset motor state to prevent duration accumulation
                    motorInput.Reset();
                    
                    // Restart combat timer for next action
                    Debug.Log($"⏱️ Next action timeout: {config.ActionTimeout}s");
                }
                else
                {
                    Debug.LogWarning("⚠️ Action failed: Hold Q/E for action + 1-5 for force, then press SPACE");
                }
            }
        }

        /// <summary>
        /// Process a detected action
        /// Uses all calculators from Processing layer
        /// </summary>
        void ProcessAction(CameraData camera, MotorData motor)
        {
            // ========== QUALITY EVALUATION ==========
            float quality = qualityEvaluator.EvaluateQuality(camera, motor);

            // ========== MUSCLE CALCULATION ==========
            MuscleData muscleGains = muscleCalculator.CalculateMuscleGains(camera.detectedAction, quality);

            // ========== EXPERIENCE CALCULATION ==========
            float expGain = experienceCalculator.CalculateExpGain(muscleGains, quality);

            // ========== ATTACK POWER CALCULATION ==========
            float attackPower = qualityEvaluator.CalculateAttackPower(quality);

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

            // ========== LEVEL UP CHECK ==========
            int levelsGained = levelCalculator.ProcessLevelUp(playerData);

            // ========== LOGGING ==========
            string grade = experienceCalculator.GetQualityGrade(quality);
            Debug.Log($"💥 {camera.detectedAction} performed! Grade: {grade} | " +
                      $"Quality: {quality:F1}% | Attack: {attackPower:F1} | EXP: +{expGain:F1}");
            Debug.Log($"🏋️ Muscles trained: {muscleGains}");
            Debug.Log($"📊 Level: {playerData.level} | EXP: {playerData.experience:F0}/{playerData.experienceToNextLevel:F0}");

            // TODO: Trigger game events (attack animation, damage calculation, etc.)
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
            return levelCalculator.GetTotalAttackBonus(playerData);
        }

        /// <summary>
        /// Start combat encounter (called by combat system)
        /// Begins the action timeout countdown
        /// </summary>
        public void StartCombat()
        {
            inCombat = true;
            combatStartTime = Time.time;
            lastActionTime = 0f;  // Reset last action time
            
            Debug.Log($"⚔️ Combat Started! Perform action within {config.ActionTimeout}s or MISS!");
        }

        /// <summary>
        /// End combat encounter (called by combat system)
        /// </summary>
        public void EndCombat()
        {
            inCombat = false;
            lastActionTime = 0f;
            
            Debug.Log("✌️ Combat Ended!");
        }

        /// <summary>
        /// Get remaining time before timeout (for UI display)
        /// </summary>
        public float GetRemainingTime()
        {
            if (!inCombat)
                return 0f;
                
            float timeSinceLastAction = Time.time - (lastActionTime > 0 ? lastActionTime : combatStartTime);
            return Mathf.Max(0f, config.ActionTimeout - timeSinceLastAction);
        }

        /// <summary>
        /// Check if currently in combat
        /// </summary>
        public bool IsInCombat()
        {
            return inCombat;
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
