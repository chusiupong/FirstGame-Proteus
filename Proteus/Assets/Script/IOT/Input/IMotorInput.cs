namespace FitnessGame.IOT
{
    /// <summary>
    /// Motor input interface for force detection
    /// </summary>
    public interface IMotorInput
    {
        /// <summary>
        /// Get current motor sensor data
        /// </summary>
        MotorData GetMotorData();

        /// <summary>
        /// Check if motor sensor is connected
        /// </summary>
        bool IsConnected();

        /// <summary>
        /// Initialize motor sensor
        /// </summary>
        void Initialize();

        /// <summary>
        /// Shutdown motor sensor
        /// </summary>
        void Shutdown();
    }
}
