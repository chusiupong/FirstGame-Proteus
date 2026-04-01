using System.IO.Ports;
using UnityEngine;

public class IMUCameraController : MonoBehaviour
{
    // ==========================
    // SERIAL SETTINGS
    // ==========================
    [Header("Serial Port")]
    public string portName = "/dev/cu.usbmodem5B5F1260651";
    public int baudRate = 115200;

    // ==========================
    // CAMERA CONTROL
    // ==========================
    [Header("Camera Control")]
    public Camera mainCamera;
    public float yawSensitivity = 2f;
    public float pitchSensitivity = 2f;
    public float smoothSpeed = 10f;
    public float pitchLimit = 85f;

    // ==========================
    // ARROW SHOOTING
    // ==========================
    [Header("Arrow Shooting")]
    public GameObject arrowPrefab;
    public float arrowForce = 800f;
    public KeyCode shootKey = KeyCode.A;

    private SerialPort serial;
    private float ax, ay, az;
    private float gx, gy, gz;

    private float yaw;
    private float pitch;
    private Quaternion targetRot;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        InitializeSerialPort();
        yaw = mainCamera.transform.eulerAngles.y;
        pitch = mainCamera.transform.eulerAngles.x;
    }

    void Update()
    {
        ReadIMU();
        UpdateCamera();
        HandleShoot();
    }

    void InitializeSerialPort()
    {
        try
        {
            serial = new SerialPort(portName, baudRate)
            {
                ReadTimeout = 50,
                DtrEnable = true,
                RtsEnable = true
            };
            serial.Open();
            Debug.Log("✅ IMU Connected!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Serial Error: " + e.Message);
        }
    }

    void ReadIMU()
    {
        if (serial == null || !serial.IsOpen) return;

        try
        {
            string data = serial.ReadLine().Trim();
            if (data.StartsWith("IMU")) ParseData(data);
        }
        catch { }
    }

    void ParseData(string data)
    {
        string[] v = data.Split(',');
        if (v.Length < 8) return;

        float.TryParse(v[2], out ax);
        float.TryParse(v[3], out ay);
        float.TryParse(v[4], out az);
        float.TryParse(v[5], out gx);
        float.TryParse(v[6], out gy);
        float.TryParse(v[7], out gz);

        // INVERTED AXES (PERFECT FOR YOUR CONTROLS)
        yaw -= gz * yawSensitivity * Time.deltaTime;
        pitch += gy * pitchSensitivity * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);
        targetRot = Quaternion.Euler(pitch, yaw, 0);
    }

    void UpdateCamera()
    {
        mainCamera.transform.rotation = Quaternion.Lerp(
            mainCamera.transform.rotation,
            targetRot,
            smoothSpeed * Time.deltaTime
        );
    }

    void HandleShoot()
    {
        if (Input.GetKeyDown(shootKey) && arrowPrefab != null)
        {
            Vector3 spawn = mainCamera.transform.position + mainCamera.transform.forward * 0.5f;
            GameObject arrow = Instantiate(arrowPrefab, spawn, mainCamera.transform.rotation);

            Rigidbody rb = arrow.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.AddForce(mainCamera.transform.forward * arrowForce);
            }
        }
    }

    void OnApplicationQuit()
    {
        if (serial != null && serial.IsOpen) serial.Close();
    }
}