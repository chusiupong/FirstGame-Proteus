namespace FitnessGame.IOT
{
    /// <summary>
    /// Camera input interface for pose detection
    /// </summary>
    public interface ICameraInput
    {
        /// <summary>
        /// Get current camera detection data
        /// </summary>
        CameraData GetCameraData();

        /// <summary>
        /// Check if camera is connected and working
        /// </summary>
        bool IsConnected();

        /// <summary>
        /// Initialize camera system
        /// </summary>
        void Initialize();

        /// <summary>
        /// Shutdown camera system
        /// </summary>
        void Shutdown();
    }
}
