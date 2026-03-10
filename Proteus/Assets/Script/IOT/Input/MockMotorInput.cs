using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Mock motor input using keyboard for testing
    /// Used before real motor sensor integration
    /// </summary>
    public class MockMotorInput : IMotorInput
    {
        private bool isInitialized = false;
        private float currentForce = 0f;
        private float forceStartTime = 0f;

        public void Initialize()
        {
            isInitialized = true;
            Debug.Log("💪 Mock Motor Initialized - Hold SPACE to apply force");
        }

        public void Shutdown()
        {
            isInitialized = false;
            Debug.Log("💪 Mock Motor Shutdown");
        }

        public bool IsConnected()
        {
            return isInitialized;
        }

        public MotorData GetMotorData()
        {
            if (!isInitialized)
                return new MotorData(0f, 0f);

            // Keyboard simulation:
            // Hold SPACE = Apply force
            // Force gradually increases while holding
            
            if (Input.GetKey(KeyCode.Space))
            {
                // First frame of pressing
                if (currentForce < 5f)
                {
                    forceStartTime = Time.time;
                    currentForce = 20f;
                }
                else
                {
                    // Gradually increase force (simulate pulling harder)
                    currentForce = Mathf.Min(currentForce + Time.deltaTime * 50f, 100f);
                }

                float duration = Time.time - forceStartTime;
                return new MotorData(currentForce, duration);
            }
            else
            {
                // Reset when released
                currentForce = 0f;
                return new MotorData(0f, 0f);
            }
        }
    }
}
