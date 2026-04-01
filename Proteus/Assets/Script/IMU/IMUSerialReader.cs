using System.IO.Ports;
using UnityEngine;

public class IMUSerialReader : MonoBehaviour
{
    [Header("Serial Settings")]
    public string portName = "/dev/cu.usbmodem5B5F1260651";      // CHANGE TO YOUR PORT
    public int baudRate = 115200;        // MUST MATCH ESP32

    [Header("Control Settings")]
    public GameObject targetObject;
    public float sensitivity = 0.1f;

    private SerialPort serial;
    private float ax, ay, az;
    private float gx, gy, gz;

    void Start()
    {
        try
        {
            serial = new SerialPort(portName, baudRate)
            {
                ReadTimeout = 10
            };
            serial.Open();
            Debug.Log("Connected to ESP32 IMU!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Connection failed: " + e.Message);
        }
    }

    void Update()
    {
        if (serial == null || !serial.IsOpen)
            return;

        try
        {
            string data = serial.ReadLine().Trim();

            if (data.StartsWith("IMU"))
            {
                ParseData(data);
                ControlObject();
            }
        }
        catch
        {
            // Ignore empty reads
        }
    }

    void ParseData(string data)
    {
        string[] values = data.Split(',');

        if (values.Length < 8) return;

        float.TryParse(values[2], out ax);
        float.TryParse(values[3], out ay);
        float.TryParse(values[4], out az);
        float.TryParse(values[5], out gx);
        float.TryParse(values[6], out gy);
        float.TryParse(values[7], out gz);
    }

    void ControlObject()
    {
        // Tilt control (most natural for IMU)
        float pitch = Mathf.Atan2(ay, az) * Mathf.Rad2Deg;
        float roll = Mathf.Atan2(-ax, Mathf.Sqrt(ay * ay + az * az)) * Mathf.Rad2Deg;

        targetObject.transform.rotation = Quaternion.Euler(pitch, 0, roll);
    }

    void OnApplicationQuit()
    {
        if (serial != null && serial.IsOpen)
            serial.Close();
    }
}