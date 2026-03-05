using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    private EnemyStats currentEnemy;
    private bool isPlayerTurn = true;

    public int playerAttackDamage = 1;
    public int enemyAttackDamage = 1;

    // START BATTLE
    public void StartBattle(EnemyStats enemy)
    {
        currentEnemy = enemy;
        Debug.Log("BATTLE START!");
        PlayerTurn();
    }

    // PLAYER TURN
    void PlayerTurn()
    {
        Debug.Log("Your Turn → Press SPACE to Attack");
        isPlayerTurn = true;
    }

    // ENEMY TURN
    void EnemyTurn()
    {
        isPlayerTurn = false;
        Debug.Log("Enemy Attacks!");
        currentEnemy.TakeDamage(0); // Just for turn order

        // After enemy attacks → back to player
        Invoke(nameof(PlayerTurn), 1f);
    }

    void Update()
    {
        // PRESS SPACE TO ATTACK (only on your turn)
        if (isPlayerTurn && currentEnemy != null && Input.GetKeyDown(KeyCode.Space))
        {
            AttackEnemy();
        }
    }

    // Player attack
    void AttackEnemy()
    {
        Debug.Log("You Attack!");
        currentEnemy.TakeDamage(playerAttackDamage);

        // If enemy alive → enemy turn
        if (currentEnemy.currentHealth > 0)
        {
            Invoke(nameof(EnemyTurn), 1f);
        }
    }
}