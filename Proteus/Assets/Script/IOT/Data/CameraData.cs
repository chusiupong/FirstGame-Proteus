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
        public ActionType detectedAction;   // Which action is detected
        public float confidence;            // Confidence score (0-1)
        public bool isPoseCorrect;          // Is the pose correct?
        public float timestamp;

        public CameraData(ActionType action = ActionType.None, float confidence = 0f)
        {
            this.detectedAction = action;
            this.confidence = Mathf.Clamp01(confidence);
            this.isPoseCorrect = confidence > 0.7f;  // 70% threshold
            this.timestamp = Time.time;
        }

        public bool IsValidAction()
        {
            return detectedAction != ActionType.None && isPoseCorrect;
        }

        public override string ToString()
        {
            return $"Camera [{detectedAction} Confidence:{confidence:P0} Valid:{isPoseCorrect}]";
        }
    }
}
