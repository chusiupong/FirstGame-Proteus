using FitnessGame.IOT;
using UnityEngine;

public class IotActionToPlayerBridge : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement playerMovement;
    public IMUCameraController imuCameraController;

    [Header("Behavior")]
    public bool autoStartGameOnFirstAction = true;
    public bool autoStartIotRoundOnTurnStart = true;
    public bool triggerCameraShootOnActionResolved = false;

    private FitnessManager subscribedManager;
    private PlayerMovement subscribedPlayer;

    void Awake()
    {
        if (playerMovement == null)
        {
#if UNITY_2023_1_OR_NEWER
            playerMovement = FindFirstObjectByType<PlayerMovement>();
#else
            playerMovement = FindObjectOfType<PlayerMovement>();
#endif
        }

        if (imuCameraController == null)
        {
    #if UNITY_2023_1_OR_NEWER
            imuCameraController = FindFirstObjectByType<IMUCameraController>();
    #else
            imuCameraController = FindObjectOfType<IMUCameraController>();
    #endif
        }
    }

    void OnEnable()
    {
        SubscribePlayerTurn();
        TrySubscribe();
    }

    void Update()
    {
        // Handles delayed FitnessManager creation order in scene.
        if (subscribedManager == null)
            TrySubscribe();

        if (subscribedPlayer == null)
            SubscribePlayerTurn();
    }

    void OnDisable()
    {
        UnsubscribePlayerTurn();
        Unsubscribe();
    }

    void OnDestroy()
    {
        UnsubscribePlayerTurn();
        Unsubscribe();
    }

    private void SubscribePlayerTurn()
    {
        if (playerMovement == null)
            return;

        if (subscribedPlayer == playerMovement)
            return;

        UnsubscribePlayerTurn();
        subscribedPlayer = playerMovement;
        subscribedPlayer.OnTurnStarted += HandleTurnStarted;
    }

    private void UnsubscribePlayerTurn()
    {
        if (subscribedPlayer == null)
            return;

        subscribedPlayer.OnTurnStarted -= HandleTurnStarted;
        subscribedPlayer = null;
    }

    private void HandleTurnStarted()
    {
        if (!autoStartIotRoundOnTurnStart)
            return;

        if (subscribedManager == null)
            TrySubscribe();

        if (subscribedManager != null && subscribedManager.CurrentState == ActionState.Idle)
            subscribedManager.RoundStart();
    }

    private void TrySubscribe()
    {
        FitnessManager manager = FitnessManager.Instance;
        if (manager == null)
            return;

        if (subscribedManager == manager)
            return;

        Unsubscribe();
        subscribedManager = manager;
        subscribedManager.OnActionResolved += HandleActionResolved;
    }

    private void Unsubscribe()
    {
        if (subscribedManager == null)
            return;

        subscribedManager.OnActionResolved -= HandleActionResolved;
        subscribedManager = null;
    }

    private void HandleActionResolved(ActionData action)
    {
        if (playerMovement == null)
            return;

        if (autoStartGameOnFirstAction)
            playerMovement.TriggerExternalStartGame();

        if (action == null)
            return;

        if (triggerCameraShootOnActionResolved && imuCameraController != null)
            imuCameraController.TriggerExternalShoot();

        playerMovement.TriggerExternalByQuality(action.qualityScore);
    }
}
