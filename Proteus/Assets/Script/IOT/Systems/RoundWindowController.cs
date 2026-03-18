using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Round window layer.
    /// Handles round active state, timeout clock, and action cooldown timing.
    /// </summary>
    public class RoundWindowController
    {
        private bool roundActive;
        private float roundStartTime;
        private float lastActionTime;

        public bool RoundActive => roundActive;

        public void RoundStart(float now)
        {
            roundActive = true;
            roundStartTime = now;
            lastActionTime = 0f;
        }

        public void RoundEnd()
        {
            roundActive = false;
            roundStartTime = 0f;
            lastActionTime = 0f;
        }

        public void MarkActionResolved(float now)
        {
            lastActionTime = now;
        }

        public bool IsActionReady(float now, float actionCooldown)
        {
            return now - lastActionTime >= actionCooldown;
        }

        public float GetRemainingTime(float now, float actionTimeout)
        {
            if (!roundActive)
                return 0f;

            float reference = lastActionTime > 0f ? lastActionTime : roundStartTime;
            float elapsed = now - reference;
            return Mathf.Max(0f, actionTimeout - elapsed);
        }

        public bool TryConsumeTimeout(float now, float actionTimeout)
        {
            if (!roundActive)
                return false;

            float reference = lastActionTime > 0f ? lastActionTime : roundStartTime;
            float elapsed = now - reference;
            if (elapsed < actionTimeout)
                return false;

            // Reset timeout baseline so next timeout starts from now.
            lastActionTime = now;
            return true;
        }
    }
}