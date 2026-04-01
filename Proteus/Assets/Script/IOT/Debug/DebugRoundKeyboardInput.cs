using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Debug-only keyboard adapter.
    /// Keeps test keybindings outside formal IoT service classes.
    /// T = RoundStart, Y = RoundEnd,
    /// </summary>
    public class DebugRoundKeyboardInput : MonoBehaviour
    {
        [Header("Keys")]
        public KeyCode startRoundKey = KeyCode.T;
        public KeyCode endRoundKey = KeyCode.Y;
        public KeyCode toggleRoundKey = KeyCode.R;

        [Header("Demo")]
        public bool autoLoopRounds = false;
        public float autoLoopDelaySeconds = 1.0f;

        private FitnessManager fitnessManager;

        void Start()
        {
            fitnessManager = FitnessManager.Instance;
            if (fitnessManager == null)
            {
                Debug.LogWarning("[IOT][Debug] FitnessManager not found. Debug keyboard adapter disabled.");
                enabled = false;
                return;
            }

            fitnessManager.OnActionResolved += OnActionResolved;
            Debug.Log($"[IOT][Debug] Keyboard adapter enabled: {startRoundKey}=RoundStart | {endRoundKey}=RoundEnd | {toggleRoundKey}=Toggle Round | AutoLoop={autoLoopRounds}");
        }

        void Update()
        {
            if (Input.GetKeyDown(startRoundKey))
            {
                fitnessManager.RoundStart();
            }

            if (Input.GetKeyDown(endRoundKey))
            {
                fitnessManager.RoundEnd();
            }

            if (Input.GetKeyDown(toggleRoundKey))
            {
                if (fitnessManager.CurrentState == ActionState.Idle)
                {
                    fitnessManager.RoundStart();
                }
                else
                {
                    fitnessManager.RoundEnd();
                }
            }
        }

        private void OnActionResolved(ActionData _)
        {
            if (!autoLoopRounds)
                return;

            CancelInvoke(nameof(TryStartNextRound));
            Invoke(nameof(TryStartNextRound), Mathf.Max(0f, autoLoopDelaySeconds));
        }

        private void TryStartNextRound()
        {
            if (fitnessManager == null)
                return;

            if (fitnessManager.CurrentState == ActionState.Idle)
            {
                fitnessManager.RoundStart();
            }
        }

        void OnDestroy()
        {
            CancelInvoke(nameof(TryStartNextRound));

            if (fitnessManager != null)
            {
                fitnessManager.OnActionResolved -= OnActionResolved;
            }
        }
    }
}
