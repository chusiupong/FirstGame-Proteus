using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class IoTReceiver : MonoBehaviour
{
    // ONLY move when we receive MQTT data
    private bool receivedData = false;

    void Start()
    {
        new Thread(ReceiveThread).Start();
    }

    void ReceiveThread()
    {
        try
        {
            // Public MQTT server (works worldwide)
            TcpClient client = new TcpClient("broker.emqx.io", 1883);
            NetworkStream stream = client.GetStream();

            // MQTT Connect
            byte[] connect = new byte[] {
                0x10, 0x0C, 0x00, 0x04, 0x4D,0x51,0x54,0x54,
                0x04, 0x00, 0x00, 60, 0, 0
            };
            stream.Write(connect, 0, connect.Length);

            // Subscribe to topic: unity/iot
            byte[] sub = new byte[] {
                0x82, 0x09, 0x00, 0x01,
                0x00, 0x04, 117,110,105,116,121,47,105,111,116, 0x00
            };
            stream.Write(sub, 0, sub.Length);

            byte[] buffer = new byte[256];
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0 && buffer[0] == 0x30)
                {
                    // DATA RECEIVED FROM YOUR IoT DEVICE!
                    receivedData = true;
                }
            }
        }
        catch
        {
            Debug.LogError("MQTT Error");
        }
    }

    void Update()
    {
        // ⬇️ IMPORTANT: ONLY moves if data is received
        if (receivedData)
        {
            transform.Rotate(0, 30 * Time.deltaTime, 0);
            transform.Translate(0, 0, Time.deltaTime * 3);
        }
    }
}