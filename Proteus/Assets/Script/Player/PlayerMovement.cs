using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public event Action OnTurnStarted;

    [Header("Input Source")]
    public bool enableKeyboardInput = true;
    public bool finalizeImmediatelyOnExternalTrigger = true;

    [Header("Bow Attack")]
    public GameObject mainBow;
    public GameObject magicProjectile;
    public Transform firePoint;

    [Header("Aiming")]
    public Transform aimDirectionSource;
    public GameObject handAimPreviewEffect;
    public KeyCode aimHoldKey = KeyCode.Q;
    public bool requireAimHoldForKeyboardRelease = true;

    [Header("Aim Guide")]
    public bool showAimGuideLine = true;
    public LineRenderer aimGuideLine;
    public bool autoCreateAimGuideLine = true;
    public float aimGuideMaxDistance = 30f;
    public float aimGuideLineWidth = 0.03f;
    public Color aimGuideStartColor = new Color(1f, 0.95f, 0.4f, 0.9f);
    public Color aimGuideEndColor = new Color(1f, 0.4f, 0.15f, 0.75f);
    public LayerMask aimGuideHitMask = Physics.DefaultRaycastLayers;

    [Header("Enemy")]
    public GameObject enemyPrefab;
    public float spawnDistance = 5f;
    public float enemyGroundOffset = 0.1f;

    [Header("Player Move")]
    public float moveForwardDistance = 6f;
    public float moveSpeed = 2.2f; // SLOWER → LONGER WALK

    [Header("Timer & UI")]
    public float turnDuration = 10f;
    public Image timerBar;
    public Image intensityBar;
    public GameObject shootingCircleIcon;
    public float smoothSpeed = 5f;

    [Header("Bar Colors")]
    public Color color1Star = Color.green;
    public Color color2Star = Color.blue;
    public Color color3Star = Color.yellow;

    [Header("Animation")]
    public float attackAnimLength = 1.2f;
    private const int BASE_LAYER = 0;
    private const int BOW_LAYER = 1;

    [Header("Start Menu")]
    public GameObject startMenu;

    private Animator anim;
    private bool gameStarted = false;
    private bool canAct = true;
    private float currentTime;
    private int currentIntensity = 0;
    private float targetFill;
    private Color targetColor;
    private GameObject currentEnemy;
    private bool isAimHeld;

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (mainBow != null) mainBow.SetActive(false);
        if (shootingCircleIcon != null) shootingCircleIcon.SetActive(false);
        EnsureAimGuideLine();
        SetAimGuideVisible(false);
    }

    void Start()
    {
        ResetTurn();
    }

    void Update()
    {
        if (!gameStarted)
        {
            if (enableKeyboardInput && Input.GetKeyDown(KeyCode.P))
                StartGame();
            return;
        }

        if (currentIntensity == 0 && shootingCircleIcon.activeSelf)
            shootingCircleIcon.SetActive(false);

        if (!canAct) return;

        UpdateAimHoldState();

        currentTime -= Time.deltaTime;
        timerBar.rectTransform.localScale = new Vector3(Mathf.Clamp01(currentTime / turnDuration), 1, 1);

        HandleKeyboardIntensityInput();

        intensityBar.rectTransform.localScale = Vector3.Lerp(
            intensityBar.rectTransform.localScale,
            new Vector3(targetFill, 1, 1),
            smoothSpeed * Time.deltaTime
        );

        intensityBar.color = Color.Lerp(intensityBar.color, targetColor, smoothSpeed * Time.deltaTime);

        if (currentTime <= 0)
            FinalizeTurn();
    }

    void UpdateAimHoldState()
    {
        bool shouldHoldAim = enableKeyboardInput && Input.GetKey(aimHoldKey);
        if (shouldHoldAim == isAimHeld)
        {
            if (isAimHeld)
            {
                SyncFirePointToAimDirection();
                UpdateAimGuideLine();
            }
            return;
        }

        isAimHeld = shouldHoldAim;

        if (handAimPreviewEffect != null)
            handAimPreviewEffect.SetActive(isAimHeld);

        SetAimGuideVisible(isAimHeld && showAimGuideLine);

        if (isAimHeld)
        {
            SyncFirePointToAimDirection();
            UpdateAimGuideLine();
        }
    }

    void EnsureAimGuideLine()
    {
        if (aimGuideLine != null || !autoCreateAimGuideLine)
            return;

        GameObject lineObj = new GameObject("AimGuideLine");
        lineObj.transform.SetParent(transform, false);
        aimGuideLine = lineObj.AddComponent<LineRenderer>();
        aimGuideLine.positionCount = 2;
        aimGuideLine.material = new Material(Shader.Find("Sprites/Default"));
    }

    void SetAimGuideVisible(bool visible)
    {
        if (aimGuideLine == null)
            return;

        aimGuideLine.enabled = visible;
    }

    void UpdateAimGuideLine()
    {
        if (!showAimGuideLine || aimGuideLine == null)
            return;

        Transform originTransform = firePoint != null ? firePoint : transform;
        Transform directionTransform = aimDirectionSource != null ? aimDirectionSource : originTransform;

        Vector3 start = originTransform.position;
        Vector3 direction = directionTransform.forward;
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        direction.Normalize();
        Vector3 end = start + direction * Mathf.Max(1f, aimGuideMaxDistance);

        if (Physics.Raycast(start, direction, out RaycastHit hit, Mathf.Max(1f, aimGuideMaxDistance), aimGuideHitMask, QueryTriggerInteraction.Ignore))
            end = hit.point;

        aimGuideLine.positionCount = 2;
        aimGuideLine.useWorldSpace = true;
        aimGuideLine.startWidth = aimGuideLineWidth;
        aimGuideLine.endWidth = aimGuideLineWidth * 0.6f;
        aimGuideLine.startColor = aimGuideStartColor;
        aimGuideLine.endColor = aimGuideEndColor;
        aimGuideLine.SetPosition(0, start);
        aimGuideLine.SetPosition(1, end);
    }

    void HandleKeyboardIntensityInput()
    {
        if (!enableKeyboardInput)
            return;

        HandleIntensityKey(KeyCode.Alpha1, KeyCode.F, 1, color1Star);
        HandleIntensityKey(KeyCode.Alpha2, KeyCode.G, 2, color2Star);
        HandleIntensityKey(KeyCode.Alpha3, KeyCode.H, 3, color3Star);
    }

    void HandleIntensityKey(KeyCode alphaKey, KeyCode fallbackKey, int level, Color color)
    {
        bool down = Input.GetKeyDown(alphaKey) || Input.GetKeyDown(fallbackKey);
        bool up = Input.GetKeyUp(alphaKey) || Input.GetKeyUp(fallbackKey);

        if (down)
            SetIntensity(level, color);

        if (!up)
            return;

        if (requireAimHoldForKeyboardRelease && !isAimHeld)
            return;

        if (isAimHeld)
            SyncFirePointToAimDirection();

        FinalizeTurn();
    }

    void SyncFirePointToAimDirection()
    {
        if (aimDirectionSource == null)
            return;

        if (firePoint != null)
            firePoint.rotation = Quaternion.LookRotation(aimDirectionSource.forward, Vector3.up);
    }

    public void TriggerExternalStartGame()
    {
        if (gameStarted)
            return;

        StartGame();
    }

    public void TriggerExternalByQuality(float qualityScore)
    {
        if (!gameStarted || !canAct)
            return;

        int intensity = MapQualityToIntensity(qualityScore);
        if (intensity <= 0)
            return;

        TriggerExternalIntensity(intensity);
    }

    public void TriggerExternalIntensity(int intensity)
    {
        if (!gameStarted || !canAct)
            return;

        int clamped = Mathf.Clamp(intensity, 1, 3);
        SetIntensity(clamped, GetIntensityColor(clamped));

        if (finalizeImmediatelyOnExternalTrigger)
            FinalizeTurn();
    }

    void StartGame()
    {
        gameStarted = true;
        startMenu.SetActive(false);
        SpawnNewEnemy();

        // Keep IoT round and UI timer in sync from the first visible turn.
        ResetTurn();

        if (aimDirectionSource == null && Camera.main != null)
            aimDirectionSource = Camera.main.transform;
    }

    void SetIntensity(int level, Color color)
    {
        currentIntensity = level;
        targetFill = currentIntensity / 3f;
        targetColor = color;
        if (shootingCircleIcon != null) shootingCircleIcon.SetActive(true);
    }

    int MapQualityToIntensity(float qualityScore)
    {
        if (qualityScore >= 75f) return 3;
        if (qualityScore >= 55f) return 2;
        if (qualityScore >= 35f) return 1;
        return 0;
    }

    Color GetIntensityColor(int intensity)
    {
        if (intensity == 3) return color3Star;
        if (intensity == 2) return color2Star;
        return color1Star;
    }

    void FinalizeTurn()
    {
        canAct = false;
        shootingCircleIcon.SetActive(false);

        if (currentIntensity > 0)
        {
            Debug.Log($"🔥 {currentIntensity} Star Attack!");
            FireAttack();
        }
        else
        {
            Debug.Log("✅ No Attack");
            Invoke(nameof(ResetTurn), 1f);
        }
    }

    void FireAttack()
    {
        mainBow.SetActive(true);
        anim.Play("DrawBow", BASE_LAYER, 0);
        anim.Play("BowAnimation", BOW_LAYER, 0);

        Invoke(nameof(SpawnArrow), 0.3f);
        Invoke(nameof(FinishAttack), attackAnimLength);
    }

    void SpawnArrow()
    {
        if (magicProjectile && firePoint)
        {
            Quaternion spawnRotation = firePoint.rotation;
            if (aimDirectionSource != null)
                spawnRotation = Quaternion.LookRotation(aimDirectionSource.forward, Vector3.up);

            Instantiate(magicProjectile, firePoint.position, spawnRotation);
        }

        if (currentEnemy != null)
        {
            Debug.Log("💥 Enemy Killed!");
            Destroy(currentEnemy);
        }
    }

    void FinishAttack()
    {
        mainBow.SetActive(false);
        anim.Play("Idle", BASE_LAYER, 0);
        anim.Play("Idle", BOW_LAYER, 0);
        
        StartCoroutine(MoveAndRespawn());
    }

    IEnumerator MoveAndRespawn()
    {
        // START WALKING
        anim.SetBool("IsMoving", true);

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + transform.forward * moveForwardDistance;
        
        // WALK LONGER BY USING TIME INSTEAD OF SPEED
        float totalMoveTime = 1.8f; // LONGER WALK DURATION
        float elapsedTime = 0f;

        while (elapsedTime < totalMoveTime)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / totalMoveTime);
            yield return null;
        }

        // STOP WALKING
        anim.SetBool("IsMoving", false);

        SpawnNewEnemy();
        Invoke(nameof(ResetTurn), 0.5f);
    }

    void SpawnNewEnemy()
    {
        if (enemyPrefab == null) return;

        Vector3 spawnPos = transform.position + transform.forward * spawnDistance;
        spawnPos.y = enemyGroundOffset;

        currentEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // Enemy faces player
        currentEnemy.transform.LookAt(transform);
        currentEnemy.transform.rotation = Quaternion.Euler(0, currentEnemy.transform.rotation.eulerAngles.y, 0);
    }

    void ResetTurn()
    {
        canAct = true;
        isAimHeld = false;
        currentTime = turnDuration;
        currentIntensity = 0;
        targetFill = 0f;
        targetColor = color1Star;

        timerBar.rectTransform.localScale = Vector3.one;
        intensityBar.rectTransform.localScale = new Vector3(0f, 1, 1);
        intensityBar.color = color1Star;

        if (shootingCircleIcon != null)
            shootingCircleIcon.SetActive(false);

        if (handAimPreviewEffect != null)
            handAimPreviewEffect.SetActive(false);

        SetAimGuideVisible(false);

        if (gameStarted)
            OnTurnStarted?.Invoke();
    }
}