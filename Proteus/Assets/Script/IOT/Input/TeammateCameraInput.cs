using System;
using System.Globalization;
using System.IO.Ports;
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

        private SerialPort serialPort;
        private CameraData latestCamera = new CameraData();
        private bool initialized;

        public TeammateCameraInput(string portName, int baudRate)
        {
            this.portName = portName;
            this.baudRate = baudRate;
        }

        public void Initialize()
        {
            if (initialized)
                return;

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
                Debug.LogWarning($"[IOT][Camera] Serial close failed: {ex.Message}");
            }
            finally
            {
                serialPort.Dispose();
                serialPort = null;
            }
        }

        public bool IsConnected()
        {
            return initialized && serialPort != null && serialPort.IsOpen;
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