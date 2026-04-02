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

   
   
}