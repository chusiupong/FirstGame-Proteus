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
        private readonly bool useMockCamera;
        private readonly bool useMockMotor;
        private readonly bool useMockImu;
        private readonly bool motorViaEsp32;
        private readonly bool imuViaEsp32;
        private bool motorPowered;

        public bool MotorPowered => motorPowered;

        public FitnessInputCollector(bool useMockCamera, bool useMockMotor, bool useMockImu, FitnessConfig config)
        {
            this.config = config;
            this.useMockCamera = useMockCamera;
            this.useMockMotor = useMockMotor;
            this.useMockImu = useMockImu;
            sensorTimeoutSeconds = Mathf.Max(0.1f, config.SensorTimeoutSeconds);

            bool useStm32Motor = !useMockMotor && config.UseStm32Motor;
            motorViaEsp32 = !useMockMotor && !useStm32Motor;

            bool useUdpImu = !useMockImu && config.UseUdpImu;
            imuViaEsp32 = !useMockImu && !useUdpImu;

            bool needEsp32Client = motorViaEsp32 || imuViaEsp32;
            if (needEsp32Client)
            {
                esp32Client = new Esp32SensorClient(config.Esp32PortName, config.Esp32BaudRate);
            }

            cameraInput = useMockCamera
                ? new MockCameraInput()
                : new TeammateCameraInput(config.CameraPortName, config.CameraBaudRate);

            motorInput = useMockMotor
                ? new MockMotorInput()
                : (useStm32Motor
                    ? new Stm32MotorInput(config.Stm32MotorPortName, config.Stm32MotorBaudRate, config.SensorTimeoutSeconds)
                    : new Esp32MotorInput(esp32Client));

            imuInput = useMockImu
                ? new MockIMUData()
                : (useUdpImu
                    ? new UdpImuInput(config.UdpImuPort, config.UdpImuStaleSeconds)
                    : new Esp32IMUInput(esp32Client));

            string imuSource = useMockImu ? "MOCK" : (useUdpImu ? "UDP" : "ESP32");
            string motorSource = useMockMotor ? "MOCK" : (useStm32Motor ? "STM32" : "ESP32");
            Debug.Log($"[IOT][Input] Source - Camera:{(useMockCamera ? "MOCK" : "REAL")} | Motor:{motorSource} | IMU:{imuSource}");
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

            motorData = motorInput.GetMotorData();
            if (motorViaEsp32 && !esp32Client.IsMotorActive(sensorTimeoutSeconds))
                motorData = new MotorData(0f);

            imuData = imuInput.GetIMUData();
            if (imuViaEsp32 && !esp32Client.IsImuActive(sensorTimeoutSeconds))
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

        public bool TryGetMotor1Telemetry(out float speedCmPerSec, out float distanceCm, out int pullCount)
        {
            if (motorInput is Stm32MotorInput stm32)
            {
                speedCmPerSec = stm32.Motor1SpeedCmPerSec;
                distanceCm = stm32.Motor1DistanceCm;
                pullCount = stm32.Motor1PullCount;
                return true;
            }

            speedCmPerSec = 0f;
            distanceCm = 0f;
            pullCount = 0;
            return false;
        }

        public bool PowerOnMotor(byte motorTarget)
        {
            if (useMockMotor)
                return false;

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
            if (useMockMotor)
                return false;

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
