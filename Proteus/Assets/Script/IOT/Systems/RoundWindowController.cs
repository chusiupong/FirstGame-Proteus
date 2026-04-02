using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Round window layer.
    /// Handles round active state and timeout clock string.
    /// </summary>
    public class RoundWindowController
    {
        private bool roundActive;
        private float roundStartTime;

        public bool RoundActive => roundActive;

        public void RoundStart(float now)
        {
            roundActive = true;
            roundStartTime = now;
        }

        public void RoundEnd()
        {
            roundActive = false;
            roundStartTime = 0f;
        }

        public float GetRemainingTime(float now, float actionTimeout)
        {
            if (!roundActive)
                return 0f;

            float elapsed = now - roundStartTime;
            return Mathf.Max(0f, actionTimeout - elapsed);
        }

        public bool TryConsumeTimeout(float now, float actionTimeout)
        {
            if (!roundActive)
                return false;

            float elapsed = now - roundStartTime;
            return elapsed >= actionTimeout;
        }
    }
}
