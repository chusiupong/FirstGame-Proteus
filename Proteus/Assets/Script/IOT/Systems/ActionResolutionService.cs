namespace FitnessGame.IOT
{
    /// <summary>
    /// Action resolution layer.
    /// Calculates quality/exp/attack and outputs ActionData.
    /// </summary>
    public class ActionResolutionService
    {
        private readonly QualityEvaluator qualityEvaluator;
        private readonly MuscleCalculator muscleCalculator;
        private readonly ExperienceCalculator experienceCalculator;
        private readonly LevelCalculator levelCalculator;

        public ActionResolutionService(FitnessConfig config)
        {
            qualityEvaluator = new QualityEvaluator(config);
            muscleCalculator = new MuscleCalculator(config);
            experienceCalculator = new ExperienceCalculator(config);
            levelCalculator = new LevelCalculator(config);
        }

        public bool IsActionDetected(CameraData cameraData)
        {
            return qualityEvaluator.IsActionDetected(cameraData);
        }

        public ActionData Resolve(CameraData cameraData, MotorData motorData, PlayerFitnessData playerData)
        {
            float quality = qualityEvaluator.EvaluateActionQuality(cameraData, motorData);
            MuscleData muscleGain = muscleCalculator.CalculateMuscleGains(quality);
            float expGain = experienceCalculator.CalculateExpGain(muscleGain, quality);
            float attackPower = qualityEvaluator.CalculateAttackPower(quality);

            var action = new ActionData
            {
                qualityScore = quality,
                attackPower = attackPower,
                muscleGain = muscleGain,
                expGain = expGain
            };

            playerData.AddTraining(muscleGain, expGain);
            levelCalculator.ProcessLevelUp(playerData);

            return action;
        }

        public string GetQualityGrade(float quality)
        {
            return experienceCalculator.GetQualityGrade(quality);
        }

        public int GetAttackBonus(PlayerFitnessData playerData)
        {
            return levelCalculator.GetTotalAttackBonus(playerData);
        }

        public float CalculateExpForLevel(int level)
        {
            return levelCalculator.CalculateExpForLevel(level);
        }
    }
}