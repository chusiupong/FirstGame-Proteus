using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// IMU input adapter that receives UDP lines from ESP32.
    /// Packet format: IMU,ts,ax,ay,az,gx,gy,gz
    /// </summary>
    public class UdpImuInput : IIMUInput
    {
        private readonly int listenPort;
        private readonly float staleSeconds;

        private UdpClient udpClient;
        private bool initialized;

        private IMUData latestImu = new IMUData();
        private float lastReceiveTime;

        public UdpImuInput(int listenPort, float staleSeconds)
        {
            this.listenPort = Mathf.Clamp(listenPort, 1, 65535);
            this.staleSeconds = Mathf.Max(0.05f, staleSeconds);
        }

        public void Initialize()
        {
            if (initialized)
                return;

            try
            {
                udpClient = new UdpClient(listenPort);
                udpClient.Client.Blocking = false;
                initialized = true;
                lastReceiveTime = 0f;
                Debug.Log($"[IOT][UDP-IMU] Listening on 0.0.0.0:{listenPort}");
            }
            catch (Exception ex)
            {
                initialized = false;
                Debug.LogWarning($"[IOT][UDP-IMU] Initialize failed on port {listenPort}: {ex.Message}");
            }
        }

        public void Shutdown()
        {
            initialized = false;

            if (udpClient == null)
                return;

            try
            {
                udpClient.Close();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IOT][UDP-IMU] Close failed: {ex.Message}");
            }
            finally
            {
                udpClient = null;
            }
        }

        public bool IsConnected()
        {
            if (!initialized)
                return false;

            return Time.time - lastReceiveTime <= staleSeconds;
        }

        public IMUData GetIMUData()
        {
            PollIncoming();

            if (!initialized)
                return new IMUData();

            if (!IsConnected())
                return new IMUData();

            return latestImu;
        }

        private void PollIncoming()
        {
            if (!initialized || udpClient == null)
                return;

            try
            {
                while (udpClient.Available > 0)
                {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] bytes = udpClient.Receive(ref remote);
                    if (bytes == null || bytes.Length == 0)
                        continue;

                    string packet = Encoding.UTF8.GetString(bytes);
                    if (string.IsNullOrWhiteSpace(packet))
                        continue;

                    string[] lines = packet.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        ApplyIncomingLine(lines[i].Trim());
                    }
                }
            }
            catch (SocketException ex)
            {
                // Non-blocking socket can report WouldBlock while no packet is ready.
                if (ex.SocketErrorCode != SocketError.WouldBlock)
                    Debug.LogWarning($"[IOT][UDP-IMU] Read failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[IOT][UDP-IMU] Poll failed: {ex.Message}");
            }
        }

        private void ApplyIncomingLine(string line)
        {
            string[] parts = line.Split(',');
            if (parts.Length < 8)
                return;

            if (!parts[0].Trim().Equals("IMU", StringComparison.OrdinalIgnoreCase))
                return;

            if (!uint.TryParse(parts[1], out _))
                return;

            if (TryParse(parts[2], out float ax) &&
                TryParse(parts[3], out float ay) &&
                TryParse(parts[4], out float az) &&
                TryParse(parts[5], out float gx) &&
                TryParse(parts[6], out float gy) &&
                TryParse(parts[7], out float gz))
            {
                latestImu = new IMUData(new Vector3(ax, ay, az), new Vector3(gx, gy, gz));
                lastReceiveTime = Time.time;
            }
        }

        private static bool TryParse(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
