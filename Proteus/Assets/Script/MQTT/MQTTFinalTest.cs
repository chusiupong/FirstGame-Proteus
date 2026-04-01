using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class MQTTFinalTest : MonoBehaviour
{
    private bool shouldMove = false;

    void Start()
    {
        new Thread(() => {
            try
            {
                TcpClient client = new TcpClient("127.0.0.1", 1883);
                NetworkStream stream = client.GetStream();

                // ✅ FIXED: Valid MQTT Connect Packet with ClientID
                byte[] connect = new byte[] {
                    0x10, 18, 0, 4, 77,81,84,84,      // MQTT header
                    4, 2, 0, 60,                     // Protocol + keepalive
                    0, 6, 85,110,105,116,121,80       // ClientID: "UnityPlayer"
                };
                stream.Write(connect, 0, connect.Length);

                // Subscribe to unity/test
                byte[] sub = new byte[] {
                    0x82, 9, 0, 1, 0, 4, 117,110,105,116,47,116,101,115,116, 0
                };
                stream.Write(sub, 0, sub.Length);

                byte[] buffer = new byte[256];
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read > 0 && buffer[0] == 0x30)
                    {
                        shouldMove = true;
                        Debug.Log("✅ DATA RECEIVED!");
                    }
                }
            }
            catch
            {
                Debug.LogError("MQTT Error");
            }
        }).Start();
    }

    void Update()
    {
        if (shouldMove)
        {
            transform.Rotate(0, 30 * Time.deltaTime, 0);
        }
    }
}