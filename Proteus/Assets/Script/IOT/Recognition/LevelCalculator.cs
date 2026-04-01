using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Handles level progression calculations
    /// All level-related formulas are centralized here
    /// </summary>
    public class LevelCalculator
    {
        private FitnessConfig config;

        public LevelCalculator(FitnessConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Calculate experience required for a specific level
        /// Formula: base × (level ^ exponent)
        /// </summary>
        public float CalculateExpForLevel(int level)
        {
            return config.LevelExpBase * Mathf.Pow(level, config.LevelExpExponent);
        }

        /// <summary>
        /// Check if player should level up and process level ups
        /// Returns number of levels gained
        /// </summary>
        public int ProcessLevelUp(PlayerFitnessData playerData)
        {
            int levelsGained = 0;

            while (playerData.experience >= playerData.experienceToNextLevel)
            {
                // Subtract exp and level up
                playerData.experience -= playerData.experienceToNextLevel;
                playerData.level++;
                levelsGained++;

                // Calculate exp for next level
                playerData.experienceToNextLevel = CalculateExpForLevel(playerData.level);

                Debug.Log($"🎉 LEVEL UP! Now Level {playerData.level}");
            }

            return levelsGained;
        }

        /// <summary>
        /// Calculate attack power bonus based on level
        /// </summary>
        public int GetAttackBonus(int level)
        {
            return (level - 1) * config.AttackBonusPerLevel;
        }

        /// <summary>
        /// Calculate total attack bonus including level and muscle strength
        /// </summary>
        public int GetTotalAttackBonus(PlayerFitnessData playerData)
        {
            // Level bonus
            int levelBonus = GetAttackBonus(playerData.level);

            // Muscle strength bonus (pulling muscles contribute to attack)
            float muscleBonus = (playerData.totalMuscles.latissimus + playerData.totalMuscles.biceps) / 50f;

            return levelBonus + Mathf.FloorToInt(muscleBonus);
        }
    }
}
