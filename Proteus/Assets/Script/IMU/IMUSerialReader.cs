using System.IO.Ports;
using UnityEngine;
using FitnessGame.IOT;

public class IMUCameraController : MonoBehaviour
{
    public enum GyroAxis
    {
        X,
        Y,
        Z
    }

    [Header("Input Source")]
    public bool useIotImuInput = true;
    public bool allowSerialFallback = true;

    [Header("Debug")]
    public bool autoAttachImuOverlay = true;
    public bool logMissingIotImu = true;
    public float missingIotImuLogInterval = 2f;

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

    [Header("Axis Mapping")]
    public GyroAxis yawAxis = GyroAxis.Z;
    public GyroAxis pitchAxis = GyroAxis.Y;
    public bool invertYaw = true;
    public bool invertPitch = false;

    [Header("IMU Tuning")]
    public bool normalizeImuValues = true;
    public float accelScale = 1f;
    public float gyroScale = 131f;
    public Vector3 gyroOffset = Vector3.zero;
    public float gyroDeadzone = 0.5f;
    public float gyroClamp = 360f;

    // ==========================
    // ARROW SHOOTING
    // ==========================
    [Header("Arrow Shooting")]
    public GameObject arrowPrefab;
    public float arrowForce = 800f;
    public KeyCode shootKey = KeyCode.A;
    public bool enableKeyboardShoot = false;

    private SerialPort serial;
    private float ax, ay, az;
    private float gx, gy, gz;

    private float yaw;
    private float pitch;
    private Quaternion targetRot;
    private float nextMissingLogTime;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (!useIotImuInput)
            InitializeSerialPort();

        if (autoAttachImuOverlay)
            EnsureImuOverlay();

        yaw = mainCamera.transform.eulerAngles.y;
        pitch = mainCamera.transform.eulerAngles.x;
        targetRot = mainCamera.transform.rotation;
    }

    void Update()
    {
        bool hasIotImu = false;
        if (useIotImuInput)
            hasIotImu = ReadIMUFromIot();

        if (!hasIotImu && (!useIotImuInput || allowSerialFallback))
            ReadIMUFromSerial();

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

    bool ReadIMUFromIot()
    {
        FitnessManager manager = FitnessManager.Instance;
        if (manager == null)
        {
            LogMissingIotImu("FitnessManager is null");
            return false;
        }

        manager.GetLatestRawInputs(out _, out _, out IMUData imu);
        if (imu == null)
        {
            LogMissingIotImu("IMUData is null");
            return false;
        }

        Vector3 gyro = imu.gyroscope;
        if (gyro.sqrMagnitude < 0.0001f)
        {
            LogMissingIotImu("IMU gyroscope is near zero (no packet yet or stale stream)");
            return false;
        }

        ax = imu.acceleration.x;
        ay = imu.acceleration.y;
        az = imu.acceleration.z;

        gx = gyro.x;
        gy = gyro.y;
        gz = gyro.z;

        PrepareImuForControl();
        ApplyRotationFromImu();
        return true;
    }

    void EnsureImuOverlay()
    {
        if (mainCamera == null)
            return;

        ImuDebugOverlay overlay = mainCamera.GetComponent<ImuDebugOverlay>();
        if (overlay == null)
            overlay = mainCamera.gameObject.AddComponent<ImuDebugOverlay>();

        if (overlay.fitnessManager == null)
            overlay.fitnessManager = FitnessManager.Instance;
    }

    void LogMissingIotImu(string reason)
    {
        if (!logMissingIotImu)
            return;

        if (Time.unscaledTime < nextMissingLogTime)
            return;

        nextMissingLogTime = Time.unscaledTime + Mathf.Max(0.2f, missingIotImuLogInterval);
        Debug.LogWarning($"[IMU][Camera] IoT IMU not available: {reason}");
    }

    void ReadIMUFromSerial()
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

        PrepareImuForControl();

        ApplyRotationFromImu();
    }

    void PrepareImuForControl()
    {
        if (normalizeImuValues)
        {
            float accDiv = Mathf.Max(0.0001f, accelScale);
            float gyroDiv = Mathf.Max(0.0001f, gyroScale);

            ax /= accDiv;
            ay /= accDiv;
            az /= accDiv;

            gx /= gyroDiv;
            gy /= gyroDiv;
            gz /= gyroDiv;
        }

        gx -= gyroOffset.x;
        gy -= gyroOffset.y;
        gz -= gyroOffset.z;

        gx = ApplyDeadzone(gx, gyroDeadzone);
        gy = ApplyDeadzone(gy, gyroDeadzone);
        gz = ApplyDeadzone(gz, gyroDeadzone);

        float clampAbs = Mathf.Max(1f, gyroClamp);
        gx = Mathf.Clamp(gx, -clampAbs, clampAbs);
        gy = Mathf.Clamp(gy, -clampAbs, clampAbs);
        gz = Mathf.Clamp(gz, -clampAbs, clampAbs);
    }

    float ApplyDeadzone(float value, float deadzone)
    {
        float dz = Mathf.Max(0f, deadzone);
        return Mathf.Abs(value) < dz ? 0f : value;
    }

    void ApplyRotationFromImu()
    {
        float yawInput = GetAxisValue(yawAxis);
        float pitchInput = GetAxisValue(pitchAxis);

        if (invertYaw)
            yawInput = -yawInput;
        if (invertPitch)
            pitchInput = -pitchInput;

        yaw += yawInput * yawSensitivity * Time.deltaTime;
        pitch += pitchInput * pitchSensitivity * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);
        targetRot = Quaternion.Euler(pitch, yaw, 0);
    }

    float GetAxisValue(GyroAxis axis)
    {
        switch (axis)
        {
            case GyroAxis.X:
                return gx;
            case GyroAxis.Y:
                return gy;
            default:
                return gz;
        }
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
        if (enableKeyboardShoot && Input.GetKeyDown(shootKey))
            TriggerExternalShoot();
    }

    public void TriggerExternalShoot()
    {
        if (arrowPrefab == null || mainCamera == null)
            return;

        Vector3 spawn = mainCamera.transform.position + mainCamera.transform.forward * 0.5f;
        GameObject arrow = Instantiate(arrowPrefab, spawn, mainCamera.transform.rotation);

        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(mainCamera.transform.forward * arrowForce);
        }
    }

    void OnApplicationQuit()
    {
        if (serial != null && serial.IsOpen) serial.Close();
    }

    void OnDestroy()
    {
        if (serial != null && serial.IsOpen) serial.Close();
    }
}