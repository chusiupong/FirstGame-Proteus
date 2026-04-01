using System;
using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Combined action data: type + quality
    /// </summary>
    [Serializable]
    public class ActionData
    {
        public float qualityScore;      // 0-100, combined from camera and motor
        public float attackPower;       // Calculated attack power for this action
        public MuscleData muscleGain;   // Muscle training from this action
        public float expGain;           // Experience points gained

        public ActionData()
        {
            qualityScore = 0f;
            attackPower = 0f;
            muscleGain = new MuscleData();
            expGain = 0f;
        }

        public bool IsValid()
        {
            return qualityScore > 0f;
        }

        public override string ToString()
        {
            return $"Action [BowDraw Quality:{qualityScore:F1}% Attack:{attackPower:F1} EXP:{expGain:F1}]";
        }
    }
}
