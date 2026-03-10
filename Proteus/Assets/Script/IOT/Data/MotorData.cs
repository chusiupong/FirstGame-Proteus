using System;
using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Motor sensor raw data
    /// </summary>
    [Serializable]
    public class MotorData
    {
        public float force;        // Force intensity (0-100)
        public float duration;     // How long the force is applied (seconds)
        public float timestamp;    // When this data was captured

        public MotorData(float force = 0f, float duration = 0f)
        {
            this.force = Mathf.Clamp(force, 0f, 100f);
            this.duration = duration;
            this.timestamp = Time.time;
        }

        public bool IsActive()
        {
            return force > 5f;  // Threshold for detecting active movement
        }

        public override string ToString()
        {
            return $"Motor [Force:{force:F1} Duration:{duration:F2}s]";
        }
    }
}
