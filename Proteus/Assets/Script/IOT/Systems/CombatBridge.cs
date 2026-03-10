using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Bridge between fitness system and combat system
    /// Translates fitness actions into game attacks
    /// </summary>
    public class CombatBridge : MonoBehaviour
    {
        [Header("Combat Settings")]
        public int baseAttackDamage = 10;
        public float damageMultiplierPerLevel = 0.1f;

        private FitnessManager fitnessManager;
        private BattleSystem battleSystem;

        void Start()
        {
            fitnessManager = FitnessManager.Instance;
            battleSystem = FindFirstObjectByType<BattleSystem>();

            if (fitnessManager == null)
            {
                Debug.LogError("❌ FitnessManager not found! Make sure it's in the scene.");
            }

            if (battleSystem == null)
            {
                Debug.LogWarning("⚠️ BattleSystem not found. Combat integration disabled.");
            }
        }

        void Update()
        {
            // Listen for fitness actions and convert to combat actions
            if (fitnessManager != null && battleSystem != null)
            {
                CheckForCombatAction();
            }
        }

        /// <summary>
        /// Check if a fitness action should trigger a combat action
        /// </summary>
        void CheckForCombatAction()
        {
            ActionData lastAction = fitnessManager.GetLastAction();

            // If a valid action was just performed
            if (lastAction.IsValid())
            {
                ExecuteCombatAction(lastAction);
            }
        }

        /// <summary>
        /// Execute combat action based on fitness action
        /// </summary>
        void ExecuteCombatAction(ActionData action)
        {
            // Calculate total damage
            int damage = CalculateDamage(action);

            // Get action type for animation/VFX
            switch (action.actionType)
            {
                case ActionType.BowDraw:
                    TriggerBowAttack(damage);
                    break;

                case ActionType.FacePull:
                    TriggerFacePullAttack(damage);
                    break;
            }

            Debug.Log($"⚔️ Combat Action: {action.actionType} | Damage: {damage}");
        }

        /// <summary>
        /// Calculate total damage from fitness action
        /// Damage = Base × Quality Factor × Level Bonus
        /// </summary>
        int CalculateDamage(ActionData action)
        {
            // Base damage from action quality
            float qualityDamage = action.attackPower;

            // Level bonus from player fitness data
            int levelBonus = fitnessManager.GetPlayerAttackBonus();

            // Total damage
            int totalDamage = Mathf.RoundToInt(qualityDamage) + levelBonus;

            return Mathf.Max(totalDamage, 1);  // At least 1 damage
        }

        /// <summary>
        /// Trigger bow attack in game
        /// </summary>
        void TriggerBowAttack(int damage)
        {
            // TODO: Connect to your teammate's PlayerMovement.Attack() system
            // For now, just apply damage to battle system
            if (battleSystem != null)
            {
                battleSystem.playerAttackDamage = damage;
                // Could trigger: FindFirstObjectByType<PlayerMovement>().Attack();
            }

            Debug.Log($"🏹 Bow Attack! Damage: {damage}");
        }

        /// <summary>
        /// Trigger face pull attack in game
        /// </summary>
        void TriggerFacePullAttack(int damage)
        {
            // TODO: This will be a different attack animation/skill
            // Work with teammate to implement second attack type
            if (battleSystem != null)
            {
                battleSystem.playerAttackDamage = damage;
            }

            Debug.Log($"💪 Face Pull Attack! Damage: {damage}");
        }

        /// <summary>
        /// Public method for game code to check if attack is ready
        /// </summary>
        public bool CanAttack()
        {
            return fitnessManager != null && fitnessManager.IsActionReady();
        }

        /// <summary>
        /// Get current attack damage preview (before performing action)
        /// </summary>
        public int GetEstimatedDamage()
        {
            if (fitnessManager == null)
                return baseAttackDamage;

            int levelBonus = fitnessManager.GetPlayerAttackBonus();
            return baseAttackDamage + levelBonus;
        }
    }
}
