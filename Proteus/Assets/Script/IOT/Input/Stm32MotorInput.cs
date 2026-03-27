using System;
using System.Collections.Generic;
using System.Globalization;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using System.IO.Ports;
#endif

using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Real motor adapter backed by STM32 serial bridge.
    /// Outgoing command frame is 32 bytes (MotorCommandPacket).
    /// Incoming status frame is 40 bytes and this adapter currently
    /// reads motor1 fields only: force/speed/distance/pullCount.
    /// </summary>
    public class Stm32MotorInput : IMotorInput
    {
        private const int StatusFrameLength = 40;
        private const byte FrameHeader = 0x64;
        private const byte EndFlag1 = 0x0D;
        private const byte EndFlag2 = 0x0A;

        private readonly string portName;
        private readonly int baudRate;
        private readonly float staleSeconds;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        private SerialPort serialPort;
#endif
        private bool initialized;
        private bool warnedUnsupportedPlatform;
        private MotorData latestMotor = new MotorData();
        private readonly List<byte> receiveBuffer = new List<byte>(256);
        private float lastReceiveTime;

        public float Motor1SpeedCmPerSec { get; private set; }
        public float Motor1DistanceCm { get; private set; }
        public int Motor1PullCount { get; private set; }

        public Stm32MotorInput(string portName, int baudRate, float staleSeconds)
        {
            this.portName = portName;
            this.baudRate = baudRate;
            this.staleSeconds = Mathf.Max(0.05f, staleSeconds);
        }

        public MotorData GetMotorData()
        {
            PollIncoming();

            if (!IsConnected())
                return new MotorData(0f);

            return latestMotor;
        }

        public bool IsConnected()
        {
            if (!initialized)
                return false;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            if (serialPort == null || !serialPort.IsOpen)
                return false;
#endif
            return Time.time - lastReceiveTime <= staleSeconds;
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
                    NewLine = "\n"
                };
                serialPort.Open();
                initialized = true;
                lastReceiveTime = 0f;
                Debug.Log($"[IOT][STM32-Motor] Connected: {portName} @ {baudRate}");
            }
            catch (Exception ex)
            {
                initialized = false;
                Debug.LogWarning($"[IOT][STM32-Motor] Connect failed ({portName}): {ex.Message}");
            }
#else
            initialized = false;
            if (!warnedUnsupportedPlatform)
            {
                warnedUnsupportedPlatform = true;
                Debug.LogWarning("[IOT][STM32-Motor] Serial input is only supported on Windows/macOS Editor/Standalone.");
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
                Debug.LogWarning($"[IOT][STM32-Motor] Close failed: {ex.Message}");
            }
            finally
            {
                serialPort.Dispose();
                serialPort = null;
            }
#endif
        }

        public void Reset()
        {
            latestMotor = new MotorData(0f);
            Motor1SpeedCmPerSec = 0f;
            Motor1DistanceCm = 0f;
            Motor1PullCount = 0;
            lastReceiveTime = 0f;
            receiveBuffer.Clear();
        }

        public void SendCommand(MotorCommandPacket packet)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            if (!initialized || serialPort == null || !serialPort.IsOpen)
                return;

            try
            {
                byte[] bytes = packet.GetBytes();
                serialPort.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IOT][STM32-Motor] Send failed: {ex.Message}");
            }
#endif
        }

        private void PollIncoming()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            if (!initialized || serialPort == null || !serialPort.IsOpen)
                return;

            try
            {
                int available = serialPort.BytesToRead;
                if (available <= 0)
                    return;

                byte[] chunk = new byte[available];
                int read = serialPort.Read(chunk, 0, chunk.Length);
                if (read <= 0)
                    return;

                for (int i = 0; i < read; i++)
                    receiveBuffer.Add(chunk[i]);

                ParseStatusFrames();
            }
            catch (TimeoutException)
            {
                // Keep latest valid value.
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IOT][STM32-Motor] Read failed: {ex.Message}");
            }
#endif
        }

        private void ParseStatusFrames()
        {
            while (receiveBuffer.Count >= StatusFrameLength)
            {
                int headerIndex = receiveBuffer.IndexOf(FrameHeader);
                if (headerIndex < 0)
                {
                    receiveBuffer.Clear();
                    return;
                }

                if (headerIndex > 0)
                    receiveBuffer.RemoveRange(0, headerIndex);

                if (receiveBuffer.Count < StatusFrameLength)
                    return;

                if (receiveBuffer[38] != EndFlag1 || receiveBuffer[39] != EndFlag2)
                {
                    // Shift by one byte and continue searching for next valid frame.
                    receiveBuffer.RemoveAt(0);
                    continue;
                }

                byte[] frame = receiveBuffer.GetRange(0, StatusFrameLength).ToArray();
                receiveBuffer.RemoveRange(0, StatusFrameLength);
                ApplyStatusFrame(frame);
            }
        }

        private void ApplyStatusFrame(byte[] frame)
        {
            // Demo scope: read motor1 only.
            // DATA[4]  = force (kg, 1 byte in current protocol usage)
            // DATA[5:6]= speed (cm/s, big-endian uint16)
            // DATA[7:8]= distance (cm, big-endian uint16)
            // DATA[9:10]= pull count (big-endian uint16)
            float forceKg = frame[4];
            int speedRaw = (frame[5] << 8) | frame[6];
            int distanceRaw = (frame[7] << 8) | frame[8];
            int countRaw = (frame[9] << 8) | frame[10];

            latestMotor = new MotorData(forceKg);
            Motor1SpeedCmPerSec = speedRaw;
            Motor1DistanceCm = distanceRaw;
            Motor1PullCount = countRaw;
            lastReceiveTime = Time.time;
        }
    }
}
