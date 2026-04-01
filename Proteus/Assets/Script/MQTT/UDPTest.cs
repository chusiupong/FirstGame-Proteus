using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPTest : MonoBehaviour
{
    private bool move = false;

    void Start()
    {
        new Thread(() => {
            UdpClient udp = new UdpClient(8888);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

            // WAIT FOR DATA FROM PYTHON
            byte[] data = udp.Receive(ref ep);
            move = true;
        }).Start();
    }

    void Update()
    {
        // ONLY MOVES WHEN IT RECEIVES DATA!!!
        if (move)
        {
            transform.Rotate(0, 50 * Time.deltaTime, 0);
        }
    }
}