using System;
using System.Collections.Generic;
using UnityEngine;

namespace FitnessGame.IOT
{
    public enum ActionState
    {
        Idle,             // Round inactive
        WaitingForAction, // Round started, waiting for player action (limited by timeout)
        Recording         // Action detected, recording sensor trajectory
    }

    /// <summary>
    /// Orchestrates the IoT pipeline for one player round:
    /// timeout check -> action state machine -> action resolution.
    /// </summary>
    public class FitnessManager : MonoBehaviour
    {
        public event Action<ActionData> OnActionResolved;

        public static FitnessManager Instance { get; private set; }

        private FitnessConfig config;
        private FitnessInputCollector inputCollector;
        private RoundWindowController roundWindow;
        private ActionResolutionService resolutionService;

        [Header("Player Data")]
        public PlayerFitnessData playerData;

        [Header("Settings")]
        public bool useMockData = true;

        [Header("State Machine")]
        public ActionState CurrentState = ActionState.Idle;

        // Trajectory recording containers
        private List<MotorData> motorTrajectory = new List<MotorData>();
        private List<IMUData> imuTrajectory = new List<IMUData>();
        private CameraData startCameraFrame;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Initialize()
        {
            config = new FitnessConfig();
            inputCollector = new FitnessInputCollector(useMockData, config);
            roundWindow = new RoundWindowController();
            resolutionService = new ActionResolutionService(config);

            inputCollector.Initialize();
            playerData = new PlayerFitnessData();
            playerData.experienceToNextLevel = resolutionService.CalculateExpForLevel(playerData.level);

            // ONLY ONCE: Turn on the motor when the application/manager initializes
            if (config.AutoPowerOnMotor)
            {
                inputCollector.PowerOnMotor(config.MotorControlTarget);
            }

            Debug.Log("[IOT] FitnessManager ready - Motor mapped to application lifecycle");
        }

        public void RoundStart()
        {
            roundWindow.RoundStart(Time.time);
            CurrentState = ActionState.WaitingForAction;
            inputCollector.ResetRoundState();

            motorTrajectory.Clear();
            imuTrajectory.Clear();

            Debug.Log($"[IOT][Round] START. Waiting for action (Timeout: {config.ActionTimeout:F1}s)");
        }

        public void RoundEnd()
        {
            roundWindow.RoundEnd();
            CurrentState = ActionState.Idle;
            inputCollector.ResetRoundState();

            Debug.Log("[IOT][Round] END.");
        }

        void Update()
        {
            if (CurrentState == ActionState.Idle) return;

            // Global highest priority: Round timeout and anti-stuck mechanism
            if (roundWindow.TryConsumeTimeout(Time.time, config.ActionTimeout))
            {
                if (CurrentState == ActionState.WaitingForAction)
                {
                    // Timeout reached before any action started
                    TriggerMiss();
                }
                else if (CurrentState == ActionState.Recording)
                {
                    // Timeout reached while recording (stuck or taking too long), force resolve
                    Debug.LogWarning("[IOT] Timeout reached while recording! Forcing resolution.");
                    ResolveAndEndAction();
                }
                return;
            }

            inputCollector.ReadRawInputs(out CameraData camera, out MotorData motor, out IMUData imu);

            switch (CurrentState)
            {
                case ActionState.WaitingForAction:
                    UpdateWaitingState(camera, motor, imu);
                    break;
                case ActionState.Recording:
                    UpdateRecordingState(camera, motor, imu);
                    break;
            }
        }

        private void UpdateWaitingState(CameraData camera, MotorData motor, IMUData imu)
        {
            // Trigger conditions: Pose detected by camera OR motor force spike (> 2f)
            if (resolutionService.IsActionDetected(camera) || motor.force > 2f)
            {
                Debug.Log("[IOT] Action Started! Transitioning to Recording.");
                CurrentState = ActionState.Recording;
                startCameraFrame = camera;
                motorTrajectory.Clear();
                imuTrajectory.Clear();
                motorTrajectory.Add(motor);
                imuTrajectory.Add(imu);
            }
        }

        private void UpdateRecordingState(CameraData camera, MotorData motor, IMUData imu)
        {
            motorTrajectory.Add(motor);
            imuTrajectory.Add(imu);

            // End conditions: Pose lost AND force dropped back
            bool actionFinished = !resolutionService.IsActionDetected(camera) && motor.force < 1f;

            if (actionFinished)
            {
                Debug.Log($"[IOT] Action Finished (Frames: {motorTrajectory.Count}). Resolving immediately.");
                ResolveAndEndAction();
            }
        }

        private void ResolveAndEndAction()
        {
            // Temporary: Extract peak motor force since ResolutionService doesn't support List yet
            MotorData peakMotor = GetPeakMotor(motorTrajectory);
            ActionData result = resolutionService.Resolve(startCameraFrame, peakMotor, imuTrajectory[0], playerData);

            OnActionResolved?.Invoke(result);

            string grade = resolutionService.GetQualityGrade(result.qualityScore);
            Debug.Log($"[IOT][Action] Resolved! Grade: {grade} | Attack: {result.attackPower:F1} | Peak Force: {peakMotor.force:F1}");

            // Action finished, end round immediately and wait for the next RoundStart call
            RoundEnd();
        }

        private void TriggerMiss()
        {
            Debug.LogWarning($"[IOT][Round] MISS: Timeout exceeded before action started.");

            ActionData missAction = new ActionData { qualityScore = 0, attackPower = 0, expGain = 0 };
            OnActionResolved?.Invoke(missAction);

            RoundEnd();
        }

        private MotorData GetPeakMotor(List<MotorData> trajectory)
        {
            MotorData peak = new MotorData(0);
            foreach (var m in trajectory) {
                if (m.force > peak.force) peak = m;
            }
            return peak;
        }

        public int AttackBonus => resolutionService.GetAttackBonus(playerData);
        public PlayerFitnessData PlayerData => playerData;
        public float RemainingTime => roundWindow.GetRemainingTime(Time.time, config.ActionTimeout);
        public bool RoundActive => roundWindow.RoundActive;

        void OnDestroy()
        {
            // ONLY ONCE: Turn off the motor when playing stops
            if (config.AutoPowerOffMotor && inputCollector != null)
            {
                inputCollector.PowerOffMotor(config.MotorControlTarget);
            }
            
            if (inputCollector != null) inputCollector.Shutdown();
        }
    }
}
