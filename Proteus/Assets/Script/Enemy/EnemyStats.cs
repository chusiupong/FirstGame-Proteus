using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public int maxHealth = 3;
    public int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // Take damage from player
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Enemy defeated
    void Die()
    {
        Debug.Log("Enemy Defeated!");
        gameObject.SetActive(false); // Hide enemy
        FindFirstObjectByType<PlayerMovement>().StartMoveForward(); // Player moves forward
    }
}