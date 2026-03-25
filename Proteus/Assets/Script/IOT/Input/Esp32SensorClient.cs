using System;
using System.Globalization;
using System.IO.Ports;
using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Shared ESP32 serial client for motor + IMU data and motor commands.
    /// Incoming line protocol is intentionally simple and can be replaced later:
    /// MOTOR,force
    /// IMU,ax,ay,az,gx,gy,gz
    /// </summary>
    public class Esp32SensorClient
    {
        private readonly string portName;
        private readonly int baudRate;

        private SerialPort serialPort;
        private bool initialized;
        private CameraData latestCamera = new CameraData();
        private MotorData latestMotor = new MotorData();
        private IMUData latestImu = new IMUData();

        public Esp32SensorClient(string portName, int baudRate)
        {
            this.portName = portName;
            this.baudRate = baudRate;
        }

        public bool IsConnected => serialPort != null && serialPort.IsOpen;

        public void Initialize()
        {
            if (initialized)
                return;

            try
            {
                serialPort = new SerialPort(portName, baudRate)
                {
                    ReadTimeout = 5,
                    WriteTimeout = 100,
                    NewLine = "\n"
                };
                serialPort.Open();
                initialized = true;
                Debug.Log($"[IOT][ESP32] Connected: {portName} @ {baudRate}");
            }
            catch (Exception ex)
            {
                initialized = false;
                Debug.LogWarning($"[IOT][ESP32] Connect failed ({portName}): {ex.Message}. Fallback values will be used.");
            }
        }

        public void Shutdown()
        {
            initialized = false;
            if (serialPort == null)
                return;

            try
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IOT][ESP32] Close failed: {ex.Message}");
            }
            finally
            {
                serialPort.Dispose();
                serialPort = null;
            }
        }

        public MotorData GetLatestMotorData()
        {
            PollIncoming();
            return latestMotor;
        }

        public CameraData GetLatestCameraData()
        {
            PollIncoming();
            return latestCamera;
        }

        public IMUData GetLatestImuData()
        {
            PollIncoming();
            return latestImu;
        }

        public void ResetMotorEstimate()
        {
            latestMotor = new MotorData(0f);
        }

        public void SendMotorCommand(MotorCommandPacket packet)
        {
            if (!IsConnected)
                return;

            try
            {
                byte[] bytes = packet.GetBytes();
                serialPort.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IOT][ESP32] Send command failed: {ex.Message}");
            }
        }

        private void PollIncoming()
        {
            if (!initialized || !IsConnected)
                return;

            try
            {
                while (serialPort.BytesToRead > 0)
                {
                    string line = serialPort.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        ApplyIncomingLine(line.Trim());
                    }
                }
            }
            catch (TimeoutException)
            {
                // Ignore timeout; keep last valid data.
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IOT][ESP32] Read failed: {ex.Message}");
            }
        }

        private void ApplyIncomingLine(string line)
        {
            // Temporary protocol adapter before teammate finalizes protocol.
            // Replace this parser only; upper layers remain unchanged.
            string[] parts = line.Split(',');
            if (parts.Length == 0)
                return;

            string kind = parts[0].Trim().ToUpperInvariant();
            if (kind == "MOTOR" && parts.Length >= 2)
            {
                if (TryParse(parts[1], out float force))
                {
                    latestMotor = new MotorData(force);
                }
                return;
            }

            if ((kind == "CAMERA" || kind == "CAM") && parts.Length >= 2)
            {
                if (TryParse(parts[1], out float confidence))
                {
                    latestCamera = new CameraData(confidence);
                }
                return;
            }

            if (kind == "IMU" && parts.Length >= 7)
            {
                if (TryParse(parts[1], out float ax) &&
                    TryParse(parts[2], out float ay) &&
                    TryParse(parts[3], out float az) &&
                    TryParse(parts[4], out float gx) &&
                    TryParse(parts[5], out float gy) &&
                    TryParse(parts[6], out float gz))
                {
                    latestImu = new IMUData(new Vector3(ax, ay, az), new Vector3(gx, gy, gz));
                }
            }
        }

        private static bool TryParse(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}