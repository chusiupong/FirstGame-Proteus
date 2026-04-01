using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class MQTTUnity : MonoBehaviour
{
    private float turn = 0;
    private bool go = false;

    void Start()
    {
        new Thread(() =>
        {
            try
            {
                // PUBLIC SERVER (works anywhere)
                TcpClient client = new TcpClient("broker.emqx.io", 1883);
                NetworkStream stream = client.GetStream();

                byte[] connect = new byte[] {
                    0x10, 0x0C, 0x00, 0x04, 0x4D,0x51,0x54,0x54,
                    0x04, 0x02, 0x00, 60, 0x00, 0x02, 0xAA,0xBB
                };
                stream.Write(connect, 0, connect.Length);

                byte[] sub = new byte[] {
                    0x82, 19, 0,1, 0,14,
                    117,110,105,116,121,47,103,97,109,101,47,105,110,112,117,116, 0
                };
                stream.Write(sub, 0, sub.Length);

                byte[] buffer = new byte[1024];
                while (true)
                {
                    int r = stream.Read(buffer, 0, buffer.Length);
                    if (r > 0 && buffer[0] == 0x30)
                    {
                        string json = Encoding.UTF8.GetString(buffer);
                        if (json.Contains("imu")) turn = 2f;
                        if (json.Contains("motor")) go = true;
                    }
                }
            }
            catch {}
        }).Start();
    }

    void Update()
    {
        transform.Rotate(0, turn, 0);
        if (go) transform.Translate(0, 0, Time.deltaTime * 4f);
    }
}