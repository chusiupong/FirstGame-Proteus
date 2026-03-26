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
        private readonly FitnessConfig config;
        private readonly float sensorTimeoutSeconds;
        private bool motorPowered;

        public bool MotorPowered => motorPowered;

        public FitnessInputCollector(bool useMockData, FitnessConfig config)
        {
            this.config = config;
            sensorTimeoutSeconds = Mathf.Max(0.1f, config.SensorTimeoutSeconds);

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

            // Hardware Motor lifecycle initialization at application start rather than round start
            if (!esp32Client?.IsConnected ?? true) 
            {
                // Safety check: skip if client is mock or not yet fully ready, 
                // but since Initialize runs first, let's keep the logic simple.
            }
            
            if (config.AutoPowerOnMotor)
            {
                PowerOnMotor(config.MotorControlTarget);
            }
        }

        public void Shutdown()
        {
            if (config.AutoPowerOffMotor)
            {
                PowerOffMotor(config.MotorControlTarget);
            }

            cameraInput.Shutdown();
            motorInput.Shutdown();
            imuInput.Shutdown();
        }

        public void ReadRawInputs(out CameraData cameraData, out MotorData motorData, out IMUData imuData)
        {
            if (esp32Client == null)
            {
                cameraData = cameraInput.GetCameraData();
                motorData = motorInput.GetMotorData();
                imuData = imuInput.GetIMUData();
                return;
            }

            cameraData = cameraInput.GetCameraData();
            if (!esp32Client.IsCameraActive(sensorTimeoutSeconds))
                cameraData = new CameraData(0f);

            motorData = motorInput.GetMotorData();
            if (!esp32Client.IsMotorActive(sensorTimeoutSeconds))
                motorData = new MotorData(0f);

            imuData = imuInput.GetIMUData();
            if (!esp32Client.IsImuActive(sensorTimeoutSeconds))
                imuData = new IMUData();
        }

        public void ReadRawData(out CameraData cameraData, out MotorData motorData)
        {
            ReadRawInputs(out cameraData, out motorData, out _);
        }

        public void ResetRoundState()
        {
            esp32Client?.ResetRoundState();
            ResetMotorState();
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

            var powerOnPacket = MotorCommandPacket.CreatePowerOnPacket();
            motorInput.SendCommand(powerOnPacket);

            var configPacket = MotorCommandPacket.CreateSpringModePacket(force: 50, distance: 150);
            motorInput.SendCommand(configPacket);

            motorPowered = true;
            Debug.Log("[IOT][Motor] Power ON + SpringMode configured via Initialization");
            return true;
        }

        public bool PowerOffMotor(byte motorTarget)
        {
            if (!motorPowered)
                return false;

            var powerOffPacket = MotorCommandPacket.CreatePowerOffPacket();
            motorInput.SendCommand(powerOffPacket);

            motorPowered = false;
            Debug.Log("[IOT][Motor] Power OFF via Shutdown");
            return true;
        }
    }
}
