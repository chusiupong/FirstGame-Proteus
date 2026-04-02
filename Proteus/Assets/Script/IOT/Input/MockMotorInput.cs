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

        public void Initialize()
        {
            isInitialized = true;
            Debug.Log("💪 Mock Motor Initialized - Press 1-5 for force level");
            Debug.Log("   1=Weak(20) | 2=Light(40) | 3=Medium(60) | 4=Strong(80) | 5=Max(100)");
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
                return new MotorData(0f);

            // Keyboard simulation:
            // Press 1-5 = Set force level (20/40/60/80/100)
            // Force level determines action quality
            
            float force = 0f;
            
            if (Input.GetKey(KeyCode.Alpha1) || Input.GetKey(KeyCode.Keypad1))
                force = 20f;  // Level 1: Weak
            else if (Input.GetKey(KeyCode.Alpha2) || Input.GetKey(KeyCode.Keypad2))
                force = 40f;  // Level 2: Light
            else if (Input.GetKey(KeyCode.Alpha3) || Input.GetKey(KeyCode.Keypad3))
                force = 60f;  // Level 3: Medium
            else if (Input.GetKey(KeyCode.Alpha4) || Input.GetKey(KeyCode.Keypad4))
                force = 80f;  // Level 4: Strong
            else if (Input.GetKey(KeyCode.Alpha5) || Input.GetKey(KeyCode.Keypad5))
                force = 100f; // Level 5: Maximum
            
            return new MotorData(force);
        }

        /// <summary>
        /// Reset motor state (no-op now since we don't track state)
        /// </summary>
        public void Reset()
        {
            // No state to reset anymore
        }

        /// <summary>
        /// Send command to motor (mock - just log)
        /// </summary>
        public void SendCommand(MotorCommandPacket packet)
        {
            Debug.Log($"💪 [MOCK] Motor command sent: {packet.ToString()}");
        }
    }
}
