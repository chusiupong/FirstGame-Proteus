using UnityEngine;

public class BattleTrigger : MonoBehaviour
{
    private bool battleStarted = false;

    void OnTriggerEnter(Collider other)
    {
        // When player touches enemy → START BATTLE
        if (other.CompareTag("Player") && !battleStarted)
        {
            battleStarted = true;
            FindFirstObjectByType<BattleSystem>().StartBattle(GetComponent<EnemyStats>());
        }
    }
}