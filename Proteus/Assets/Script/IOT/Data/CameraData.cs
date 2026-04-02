using System;
using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Camera pose detection data
    /// </summary>
    [Serializable]
    public class CameraData
    {
        public float confidence;            // Confidence score (0-1)
        public float timestamp;

        public CameraData(float confidence = 0f)
        {
            this.confidence = Mathf.Clamp01(confidence);
            this.timestamp = Time.time;
        }

        public bool IsValidAction()
        {
            return confidence > 0.7f;  // 70% threshold
        }

        public override string ToString()
        {
            return $"Camera [Confidence:{confidence:P0} Valid:{IsValidAction()}]";
        }
    }
}
