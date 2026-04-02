using System;
using System.Globalization;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using System.IO.Ports;
#endif

using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Camera adapter reserved for teammate-provided camera code.
    /// Keep all third-party camera integration inside this class only.
    /// </summary>
    public class TeammateCameraInput : ICameraInput
    {
        private readonly string portName;
        private readonly int baudRate;

    #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        private SerialPort serialPort;
    #endif
        private CameraData latestCamera = new CameraData();
        private bool initialized;
        private bool warnedUnsupportedPlatform;

        public TeammateCameraInput(string portName, int baudRate)
        {
            this.portName = portName;
            this.baudRate = baudRate;
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
                    NewLine = "\n"
                };
                serialPort.Open();
                initialized = true;
                Debug.Log($"[IOT][Camera] Serial connected: {portName} @ {baudRate}");
            }
            catch (Exception ex)
            {
                initialized = false;
                Debug.LogWarning($"[IOT][Camera] Serial connect failed ({portName}): {ex.Message}");
            }
#else
            initialized = false;
            if (!warnedUnsupportedPlatform)
            {
                warnedUnsupportedPlatform = true;
                Debug.LogWarning("[IOT][Camera] Serial input is only supported on Windows/macOS Editor/Standalone. Using fallback values.");
            }
#endif
        }

        public void Shutdown()
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
                Debug.LogWarning($"[IOT][Camera] Serial close failed: {ex.Message}");
            }
            finally
            {
                serialPort.Dispose();
                serialPort = null;
            }
#endif
        }

        public bool IsConnected()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            return initialized && serialPort != null && serialPort.IsOpen;
#else
            return false;
#endif
        }

        public CameraData GetCameraData()
        {
            if (!initialized)
                return new CameraData();

            PollIncoming();

            return latestCamera;
        }

        private void PollIncoming()
        {
            if (!IsConnected())
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
                // Keep latest valid value.
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IOT][Camera] Serial read failed: {ex.Message}");
            }
#endif
        }

        private void ApplyIncomingLine(string line)
        {
            // Expected camera line format:
            // CAMERA,0.85 or CAM,0.85
            string[] parts = line.Split(',');
            if (parts.Length < 2)
                return;

            string kind = parts[0].Trim().ToUpperInvariant();
            if (kind != "CAMERA" && kind != "CAM")
                return;

            if (float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float confidence))
            {
                latestCamera = new CameraData(confidence);
            }
        }
    }
}