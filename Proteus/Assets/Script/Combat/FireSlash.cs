using UnityEngine;

public class FireSlash : MonoBehaviour
{
    public float flySpeed = 12f;
    public float lifetime = 2f;
    public int damage = 1;

    // ADD DELAY HERE (0.3f = 300ms)
    private float delay = 0.3f;
    private bool canMove = false;

    void Start()
    {
        transform.Rotate(0, 0, 0);
        Destroy(gameObject, lifetime);

        // Start moving AFTER delay
        Invoke(nameof(StartMoving), delay);
    }

    void Update()
    {
        // Only fly when delay is done
        if (canMove)
            transform.Translate(Vector3.forward * flySpeed * Time.deltaTime);
    }

    void StartMoving()
    {
        canMove = true;
    }

 
}