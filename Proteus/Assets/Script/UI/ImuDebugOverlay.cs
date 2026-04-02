using UnityEngine;
using FitnessGame.IOT;

/// <summary>
/// Runtime IMU overlay for fast prototyping.
/// This is intentionally presentation-only so it can be replaced by richer visuals later.
/// </summary>
public class ImuDebugOverlay : MonoBehaviour
{
    [Header("Source")]
    public FitnessManager fitnessManager;

    [Header("Layout")]
    public bool showOverlay = true;
    public Vector2 anchor = new Vector2(20f, 20f);
    public int fontSize = 18;
    public int precision = 3;

    [Header("Style")]
    public Color textColor = Color.white;
    public Color panelColor = new Color(0f, 0f, 0f, 0.55f);

    [Header("Refresh")]
    public float refreshInterval = 0.05f;
    public float staleThresholdSeconds = 1.0f;

    private CameraData latestCamera;
    private MotorData latestMotor;
    private IMUData latestImu;
    private float motor1Speed;
    private float motor1Distance;
    private int motor1PullCount;
    private bool hasMotor1Telemetry;
    private float nextRefreshTime;

    private string cachedText = "";
    private GUIStyle labelStyle;
    private Texture2D panelTexture;

    private void Start()
    {
        if (fitnessManager == null)
            fitnessManager = FitnessManager.Instance;

        panelTexture = new Texture2D(1, 1);
        panelTexture.SetPixel(0, 0, panelColor);
        panelTexture.Apply();
    }

    private void Update()
    {
        if (!showOverlay)
            return;

        if (fitnessManager == null)
            fitnessManager = FitnessManager.Instance;

        if (fitnessManager == null)
            return;

        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.01f, refreshInterval);

        fitnessManager.GetLatestRawInputs(out latestCamera, out latestMotor, out latestImu);
        hasMotor1Telemetry = fitnessManager.TryGetMotor1Telemetry(out motor1Speed, out motor1Distance, out motor1PullCount);
        cachedText = BuildDisplayText();
    }

    private void OnGUI()
    {
        if (!showOverlay || string.IsNullOrEmpty(cachedText))
            return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                richText = false,
                wordWrap = false
            };
        }

        labelStyle.fontSize = fontSize;
        labelStyle.normal.textColor = textColor;

        Vector2 size = labelStyle.CalcSize(new GUIContent(cachedText));
        Rect rect = new Rect(anchor.x, anchor.y, size.x + 24f, size.y + 18f);

        Color oldColor = GUI.color;
        GUI.color = panelColor;
        GUI.DrawTexture(rect, panelTexture);
        GUI.color = oldColor;

        GUI.Label(new Rect(rect.x + 12f, rect.y + 9f, rect.width - 20f, rect.height - 16f), cachedText, labelStyle);
    }

    private string BuildDisplayText()
    {
        int p = Mathf.Clamp(precision, 0, 6);
        float age = Time.time - latestImu.timestamp;
        bool isActive = age <= Mathf.Max(0.05f, staleThresholdSeconds);

        Vector3 a = latestImu.acceleration;
        Vector3 g = latestImu.gyroscope;

        string state = fitnessManager.CurrentState.ToString();
        string status = isActive ? "ACTIVE" : "STALE";

        return
            "IMU (UDP)\\n" +
            $"State: {state}  RoundActive: {fitnessManager.RoundActive}\\n" +
            $"Stream: {status}  Age: {age * 1000f:F0} ms\\n" +
            $"Acc: X={a.x.ToString($"F{p}")}  Y={a.y.ToString($"F{p}")}  Z={a.z.ToString($"F{p}")}  |M|={a.magnitude.ToString($"F{p}")}\\n" +
            $"Gyro: X={g.x.ToString($"F{p}")}  Y={g.y.ToString($"F{p}")}  Z={g.z.ToString($"F{p}")}  |M|={g.magnitude.ToString($"F{p}")}\\n" +
            $"Motor Force: {latestMotor.force.ToString($"F1")}  Camera Conf: {latestCamera.confidence.ToString("F2")}\n" +
            (hasMotor1Telemetry
                ? $"Motor1 Speed:{motor1Speed:F1}cm/s  Dist:{motor1Distance:F1}cm  Count:{motor1PullCount}"
                : "Motor1 Telemetry: N/A (not using STM32 motor)");
    }

    private void OnDestroy()
    {
        if (panelTexture != null)
            Destroy(panelTexture);
    }
}
