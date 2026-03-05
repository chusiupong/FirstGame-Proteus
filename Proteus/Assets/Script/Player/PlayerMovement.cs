using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float stepDistance = 8f;
    public float moveSpeed = 8f;

    [Header("Bow Attack")]
    public GameObject mainBow;
    public GameObject magicProjectile;
    public Transform firePoint;

    [Header("Timing")]
    public float vfxDelay = 0.3f;
    public float fullAttackAnimLength = 1.2f;

    private Animator anim;
    private bool isMoving;
    private bool canAttack = true;
    private Vector3 targetPos;

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (mainBow != null) mainBow.SetActive(false);
    }

    void Update()
    {
        // MOVEMENT — ALWAYS WORKS
        if (Input.GetKeyDown(KeyCode.Space) && !isMoving)
            StartMoveForward();

        // ATTACK — ONLY WHEN NOT MOVING
        if (Input.GetKeyDown(KeyCode.F) && !isMoving && canAttack)
            Attack();

        // Movement logic
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
                isMoving = false;
        }

        anim.SetBool("IsMoving", isMoving);
    }

    void Attack()
    {
        canAttack = false;
        mainBow.SetActive(true);

        // ONLY play these two WHEN ATTACKING
        anim.Play("DrawBow", 0, 0);
        anim.Play("BowAnimation", 1, 0);

        Invoke(nameof(SpawnVFX), vfxDelay);
        Invoke(nameof(EndAttack), fullAttackAnimLength);
    }

    void SpawnVFX()
    {
        if (magicProjectile && firePoint)
            Instantiate(magicProjectile, firePoint.position, transform.rotation);
    }

    void EndAttack()
    {
        mainBow.SetActive(false);

        // ✅ FIX THE LAYER CONFLICT:
        // Only reset BASE layer (0) — DO NOT TOUCH BowLayer (1)
        anim.Play("Idle", 0);
        
        // STOP bow layer clean — no Idle conflict
        anim.StopPlayback();

        canAttack = true;
    }

    public void StartMoveForward()
    {
        targetPos = transform.position + transform.forward * stepDistance;
        isMoving = true;
    }
}