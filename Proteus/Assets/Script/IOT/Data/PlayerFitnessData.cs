using System;
using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Player's accumulated fitness data and progression
    /// </summary>
    [Serializable]
    public class PlayerFitnessData
    {
        [Header("Level & Experience")]
        public int level = 1;
        public float experience = 0f;
        public float experienceToNextLevel = 100f;

        [Header("Accumulated Muscle Training")]
        public MuscleData totalMuscles = new MuscleData();

        [Header("Statistics")]
        public int totalActionsPerformed = 0;
        public float totalTrainingTime = 0f;  // in seconds

        [Header("Session Data")]
        public MuscleData sessionMuscles = new MuscleData();  // Current session only
        public float sessionTime = 0f;

        /// <summary>
        /// Add training result from one action
        /// </summary>
        public void AddTraining(MuscleData muscles, float expGain)
        {
            // Add to total
            totalMuscles.Add(muscles);
            sessionMuscles.Add(muscles);

            // Add experience
            experience += expGain;

            // Increment action count
            totalActionsPerformed++;

            // Check for level up
            CheckLevelUp();
        }

        /// <summary>
        /// Check if player should level up
        /// </summary>
        private void CheckLevelUp()
        {
            while (experience >= experienceToNextLevel)
            {
                experience -= experienceToNextLevel;
                level++;
                experienceToNextLevel = CalculateExpForNextLevel(level);
                Debug.Log($"🎉 LEVEL UP! Now Level {level}");
            }
        }

        /// <summary>
        /// Calculate experience required for next level
        /// Simple formula: 100 * level^1.5
        /// </summary>
        private float CalculateExpForNextLevel(int currentLevel)
        {
            return 100f * Mathf.Pow(currentLevel, 1.5f);
        }

        /// <summary>
        /// Start a new training session
        /// </summary>
        public void StartSession()
        {
            sessionMuscles = new MuscleData();
            sessionTime = 0f;
        }

        /// <summary>
        /// Get attack power bonus based on level and muscle strength
        /// </summary>
        public int GetAttackBonus()
        {
            // Level gives flat bonus
            int levelBonus = (level - 1) * 2;

            // Muscle strength gives additional bonus
            // Focus on pulling muscles for attack power
            float muscleBonus = (totalMuscles.latissimus + totalMuscles.biceps) / 50f;

            return levelBonus + Mathf.FloorToInt(muscleBonus);
        }

        /// <summary>
        /// Get experience progress percentage (0-1)
        /// </summary>
        public float GetExpProgress()
        {
            return experience / experienceToNextLevel;
        }
    }
}
