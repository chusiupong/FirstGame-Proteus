using UnityEngine;

// Magic projectile that flies forward and damages enemies
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 1;
    public float lifetime = 3f;

    void Start()
    {
        // Destroy bullet after 3 seconds to avoid garbage
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Fly forward
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    // When hit something
    void OnTriggerEnter(Collider other)
    {
        // If hit enemy → deal damage
        if (other.CompareTag("Enemy"))
        {
            EnemyStats enemy = other.GetComponent<EnemyStats>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // Destroy bullet when hit
            Destroy(gameObject);
        }
    }
}