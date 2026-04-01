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

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (mainBow != null) mainBow.SetActive(false);
        if (shootingCircleIcon != null) shootingCircleIcon.SetActive(false);
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

        currentTime -= Time.deltaTime;
        timerBar.rectTransform.localScale = new Vector3(Mathf.Clamp01(currentTime / turnDuration), 1, 1);

        if (enableKeyboardInput && Input.GetKeyDown(KeyCode.F)) SetIntensity(1, color1Star);
        if (enableKeyboardInput && Input.GetKeyDown(KeyCode.G)) SetIntensity(2, color2Star);
        if (enableKeyboardInput && Input.GetKeyDown(KeyCode.H)) SetIntensity(3, color3Star);

        intensityBar.rectTransform.localScale = Vector3.Lerp(
            intensityBar.rectTransform.localScale,
            new Vector3(targetFill, 1, 1),
            smoothSpeed * Time.deltaTime
        );

        intensityBar.color = Color.Lerp(intensityBar.color, targetColor, smoothSpeed * Time.deltaTime);

        if (currentTime <= 0)
            FinalizeTurn();
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
            Instantiate(magicProjectile, firePoint.position, firePoint.rotation);

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
        currentTime = turnDuration;
        currentIntensity = 0;
        targetFill = 0f;
        targetColor = color1Star;

        timerBar.rectTransform.localScale = Vector3.one;
        intensityBar.rectTransform.localScale = new Vector3(0f, 1, 1);
        intensityBar.color = color1Star;

        if (shootingCircleIcon != null)
            shootingCircleIcon.SetActive(false);

        if (gameStarted)
            OnTurnStarted?.Invoke();
    }
}