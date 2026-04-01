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

        /// <summary>
        /// Reset motor state (force and duration)
        /// Called after each action to prevent duration accumulation
        /// </summary>
        void Reset();

        /// <summary>
        /// Send command packet to motor
        /// Used for power control and mode switching
        /// </summary>
        void SendCommand(MotorCommandPacket packet);
    }
}
