using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Mock camera input using keyboard for testing
    /// Used before real camera integration
    /// </summary>
    public class MockCameraInput : ICameraInput
    {
        private bool isInitialized = false;

        public void Initialize()
        {
            isInitialized = true;
            Debug.Log("🎥 Mock Camera Initialized - Q=BowDraw | E=FacePull");
        }

        public void Shutdown()
        {
            isInitialized = false;
            Debug.Log("🎥 Mock Camera Shutdown");
        }

        public bool IsConnected()
        {
            return isInitialized;
        }

        public CameraData GetCameraData()
        {
            if (!isInitialized)
                return new CameraData();

            // Keyboard simulation:
            // Q = Bow Draw action
            // E = Face Pull action
            
            if (Input.GetKey(KeyCode.Q))
            {
                // Simulate bow draw detection
                return new CameraData(ActionType.BowDraw, 0.85f);
            }
            else if (Input.GetKey(KeyCode.E))
            {
                // Simulate face pull detection
                return new CameraData(ActionType.FacePull, 0.90f);
            }

            // No action detected
            return new CameraData(ActionType.None, 0f);
        }
    }
}
