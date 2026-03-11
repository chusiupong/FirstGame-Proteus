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
        /// Note: Level up logic is now handled by LevelCalculator
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

            // Note: CheckLevelUp is now handled externally by LevelCalculator
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
        /// Get experience progress percentage (0-1)
        /// </summary>
        public float GetExpProgress()
        {
            return experience / experienceToNextLevel;
        }
    }
}
