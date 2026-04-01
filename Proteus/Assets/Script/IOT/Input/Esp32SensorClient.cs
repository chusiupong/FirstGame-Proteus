using System;
using System.Globalization;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using System.IO.Ports;
#endif

using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Shared ESP32 serial client for motor + IMU data and motor commands.
    /// Incoming line protocol (V1):
    /// MOTOR,force
    /// IMU,ts_ms,ax,ay,az,gx,gy,gz
    /// CAMERA,confidence
    /// </summary>
    public class Esp32SensorClient
    {
        private readonly string portName;
        private readonly int baudRate;

    #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        private SerialPort serialPort;
    #endif
        private bool initialized;
        private bool warnedUnsupportedPlatform;
        private CameraData latestCamera = new CameraData();
        private MotorData latestMotor = new MotorData();
        private IMUData latestImu = new IMUData();

        private uint lastImuTimestampMs;
        private float lastMotorUpdateTime;
        private float lastCameraUpdateTime;
        private float lastImuUpdateTime;
        private float lastMotionTime;

        public Esp32SensorClient(string portName, int baudRate)
        {
            this.portName = portName;
            this.baudRate = baudRate;
        }

        public bool IsConnected
        {
            get
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                return serialPort != null && serialPort.IsOpen;
#else
                return false;
#endif
            }
        }

        public void Initialize()
        {
            if (initialized)
                return;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            try
            {
                serialPort = new SerialPort(portName, baudRate)
                {
                    ReadTimeout = 5,
                    WriteTimeout = 100,
                    NewLine = "\n",
                    DtrEnable = true, // Crucial for Mac/ESP32 compatibility
                    RtsEnable = true  // Crucial for Mac/ESP32 compatibility
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
#else
            initialized = false;
            if (!warnedUnsupportedPlatform)
            {
                warnedUnsupportedPlatform = true;
                Debug.LogWarning("[IOT][ESP32] Serial input is only supported on Windows/macOS Editor/Standalone. Using fallback values.");
            }
#endif
        }

        public void Shutdown()
        {
            ForceDisconnect();
        }

        private void ForceDisconnect()
        {
            initialized = false;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
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
#endif
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

        public bool IsMotorActive(float timeoutSeconds)
        {
            PollIncoming();
            return Time.time - lastMotorUpdateTime <= timeoutSeconds;
        }

        public bool IsCameraActive(float timeoutSeconds)
        {
            PollIncoming();
            return Time.time - lastCameraUpdateTime <= timeoutSeconds;
        }

        public bool IsImuActive(float timeoutSeconds)
        {
            PollIncoming();
            return Time.time - lastImuUpdateTime <= timeoutSeconds;
        }

        public bool IsMotionActive(float timeoutSeconds)
        {
            PollIncoming();
            return Time.time - lastMotionTime <= timeoutSeconds;
        }

        public uint LastImuTimestampMs => lastImuTimestampMs;

        public void ResetRoundState()
        {
            latestCamera = new CameraData(0f);
            latestMotor = new MotorData(0f);
            latestImu = new IMUData();

            lastImuTimestampMs = 0;
            lastMotorUpdateTime = 0f;
            lastCameraUpdateTime = 0f;
            lastImuUpdateTime = 0f;
            lastMotionTime = 0f;
        }

        public void ResetMotorEstimate()
        {
            latestMotor = new MotorData(0f);
            lastMotorUpdateTime = 0f;
        }

        public void SendMotorCommand(MotorCommandPacket packet)
        {
            if (!IsConnected)
                return;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            try
            {
                byte[] bytes = packet.GetBytes();
                serialPort.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IOT][ESP32] Send command failed (Device disconnected?): {ex.Message}");
                ForceDisconnect();
            }
#endif
        }

        private void PollIncoming()
        {
            if (!initialized || !IsConnected)
                return;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
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
            catch (Exception ex) // Catch IOException, InvalidOperationException etc.
            {
                Debug.LogWarning($"[IOT][ESP32] Read failed (Device forcefully disconnected?): {ex.Message}");
                ForceDisconnect();
            }
#endif
        }

        private void ApplyIncomingLine(string line)
        {
            string[] parts = line.Split(',');
            if (parts.Length == 0)
                return;

            string kind = parts[0].Trim().ToUpperInvariant();
            if (kind == "MOTOR" && parts.Length >= 2)
            {
                ParseMotor(parts);
                return;
            }

            if ((kind == "CAMERA" || kind == "CAM") && parts.Length >= 2)
            {
                ParseCamera(parts);
                return;
            }

            if (kind == "IMU")
            {
                ParseImu(parts);
            }
        }

        private void ParseMotor(string[] parts)
        {
            if (!TryParse(parts[1], out float force))
                return;

            latestMotor = new MotorData(force);
            lastMotorUpdateTime = Time.time;
            lastMotionTime = Time.time;
        }

        private void ParseCamera(string[] parts)
        {
            if (!TryParse(parts[1], out float confidence))
                return;

            latestCamera = new CameraData(confidence);
            lastCameraUpdateTime = Time.time;
            lastMotionTime = Time.time;
        }

        private void ParseImu(string[] parts)
        {
            // V1 required: IMU,ts_ms,ax,ay,az,gx,gy,gz
            if (parts.Length < 8)
                return;

            if (!uint.TryParse(parts[1], out uint tsMs))
                return;

            if (TryParse(parts[2], out float ax) &&
                TryParse(parts[3], out float ay) &&
                TryParse(parts[4], out float az) &&
                TryParse(parts[5], out float gx) &&
                TryParse(parts[6], out float gy) &&
                TryParse(parts[7], out float gz))
            {
                latestImu = new IMUData(new Vector3(ax, ay, az), new Vector3(gx, gy, gz));
                lastImuTimestampMs = tsMs;
                lastImuUpdateTime = Time.time;
                lastMotionTime = Time.time;
            }
        }

        private static bool TryParse(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
