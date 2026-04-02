using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class SimpleMQTT : MonoBehaviour
{
    private bool move = false;

    void Start()
    {
        new Thread(() =>
        {
            try
            {
                TcpClient client = new TcpClient("broker.emqx.io", 1883);
                NetworkStream stream = client.GetStream();

                byte[] connect = new byte[] { 0x10, 0x0C, 0x00, 0x04, 0x4D,0x51,0x54,0x54, 4, 0, 0, 60, 0, 0 };
                stream.Write(connect, 0, connect.Length);

                byte[] sub = new byte[] { 0x82, 8, 0,1, 0,4, 117,110,105,116,121,47,116,101,115,116, 0 };
                stream.Write(sub, 0, sub.Length);

                byte[] buffer = new byte[128];
                while (true)
                {
                    int r = stream.Read(buffer, 0, buffer.Length);
                    if (r > 0) move = true;
                }
            }
            catch {}
        }).Start();
    }

    void Update()
    {
        if (move)
        {
            transform.Rotate(0, 20 * Time.deltaTime, 0);
            transform.Translate(0, 0, Time.deltaTime * 2);
        }
    }
}