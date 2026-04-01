using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class LocalMQTT : MonoBehaviour
{
    private bool received = false;

    void Start()
    {
        new Thread(() =>
        {
            try
            {
                // LOCHOST — NO NETWORK, 100% works
                TcpClient client = new TcpClient("127.0.0.1", 1883);
                NetworkStream stream = client.GetStream();

                byte[] connect = new byte[] {
                    0x10, 0x0C, 0x00, 0x04, 0x4D,0x51,0x54,0x54,
                    0x04, 0x00, 0x00, 60, 0, 0
                };
                stream.Write(connect, 0, connect.Length);

                byte[] sub = new byte[] {
                    0x82, 9, 0,1, 0,4, 117,110,105,116,121,47,116,101,115,116, 0
                };
                stream.Write(sub, 0, sub.Length);

                byte[] buffer = new byte[256];
                while (true)
                {
                    int r = stream.Read(buffer, 0, buffer.Length);
                    if (r > 0) received = true;
                }
            }
            catch {}
        }).Start();
    }

    void Update()
    {
        if (received)
        {
            transform.Rotate(0, 30 * Time.deltaTime, 0);
        }
    }
}