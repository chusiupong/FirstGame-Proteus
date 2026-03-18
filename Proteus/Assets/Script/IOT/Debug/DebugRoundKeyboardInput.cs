using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Debug-only keyboard adapter.
    /// Keeps test keybindings outside formal IoT service classes.
    /// T = RoundStart, Y = RoundEnd, SPACE = TryResolveCurrentAction.
    /// </summary>
    public class DebugRoundKeyboardInput : MonoBehaviour
    {
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

            Debug.Log("[IOT][Debug] Keyboard adapter enabled: T=RoundStart | Y=RoundEnd | SPACE=TryResolveCurrentAction");
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                fitnessManager.RoundStart();
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                fitnessManager.RoundEnd();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                bool resolved = fitnessManager.TryResolveCurrentAction();
                if (!resolved && !fitnessManager.IsActionDetected())
                {
                    Debug.Log("[IOT][Debug] No camera action detected. Hold Q then press SPACE.");
                }
            }
        }
    }
}
