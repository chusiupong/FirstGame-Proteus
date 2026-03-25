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
        private readonly IIMUInput imuInput;
        private readonly Esp32SensorClient esp32Client;
        private bool motorPowered;

        public bool MotorPowered => motorPowered;

        public FitnessInputCollector(bool useMockData, FitnessConfig config)
        {
            if (useMockData)
            {
                cameraInput = new MockCameraInput();
                motorInput = new MockMotorInput();
                imuInput = new MockIMUData();
                Debug.Log("[IOT][Input] Using MOCK camera/motor/imu inputs");
            }
            else
            {
                esp32Client = new Esp32SensorClient(config.Esp32PortName, config.Esp32BaudRate);

                cameraInput = new TeammateCameraInput(config.CameraPortName, config.CameraBaudRate);
                motorInput = new Esp32MotorInput(esp32Client);
                imuInput = new Esp32IMUInput(esp32Client);

                Debug.Log($"[IOT][Input] Using REAL motor/imu via ESP32 {config.Esp32PortName}@{config.Esp32BaudRate}, camera adapter pending integration");
            }
        }

        public void Initialize()
        {
            cameraInput.Initialize();
            motorInput.Initialize();
            imuInput.Initialize();
        }

        public void Shutdown()
        {
            cameraInput.Shutdown();
            motorInput.Shutdown();
            imuInput.Shutdown();
        }

        public SensorFrame ReadSensorFrame()
        {
            CameraData cameraData = cameraInput.GetCameraData();
            MotorData motorData = motorInput.GetMotorData();
            IMUData imuData = imuInput.GetIMUData();
            return new SensorFrame(cameraData, motorData, imuData);
        }

        public void ReadRawData(out CameraData cameraData, out MotorData motorData)
        {
            SensorFrame frame = ReadSensorFrame();
            cameraData = frame.camera;
            motorData = frame.motor;
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