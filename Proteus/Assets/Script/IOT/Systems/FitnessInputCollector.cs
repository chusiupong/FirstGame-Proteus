using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Input collection layer.
    /// Owns camera/motor adapters and exposes raw sensor data.
    /// </summary>
    public class FitnessInputCollector
    {
        private readonly ICameraInput cameraInput;
        private readonly IMotorInput motorInput;
        private bool motorPowered;

        public bool MotorPowered => motorPowered;

        public FitnessInputCollector(bool useMockData)
        {
            if (useMockData)
            {
                cameraInput = new MockCameraInput();
                motorInput = new MockMotorInput();
                Debug.Log("[IOT][Input] Using MOCK camera/motor inputs");
            }
            else
            {
                // TODO: Replace with real hardware adapters.
                Debug.LogWarning("[IOT][Input] Real hardware adapters not implemented, fallback to mock");
                cameraInput = new MockCameraInput();
                motorInput = new MockMotorInput();
            }
        }

        public void Initialize()
        {
            cameraInput.Initialize();
            motorInput.Initialize();
        }

        public void Shutdown()
        {
            cameraInput.Shutdown();
            motorInput.Shutdown();
        }

        public void ReadRawData(out CameraData cameraData, out MotorData motorData)
        {
            cameraData = cameraInput.GetCameraData();
            motorData = motorInput.GetMotorData();
        }

        public bool IsActionDetected(CameraData cameraData)
        {
            return cameraData.IsValidAction();
        }

        public void ResetMotorState()
        {
            motorInput.Reset();
        }

        public bool PowerOnMotor(byte motorTarget)
        {
            if (motorPowered)
                return false;

            var powerOnPacket = MotorCommandPacket.CreatePowerOnPacket(motorTarget);
            motorInput.SendCommand(powerOnPacket);

            var workModePacket = MotorCommandPacket.CreateWorkModePacket(motorTarget, 0x00);
            motorInput.SendCommand(workModePacket);

            motorPowered = true;
            return true;
        }

        public bool PowerOffMotor(byte motorTarget)
        {
            if (!motorPowered)
                return false;

            var powerOffPacket = MotorCommandPacket.CreatePowerOffPacket(motorTarget);
            motorInput.SendCommand(powerOffPacket);

            motorPowered = false;
            return true;
        }
    }
}