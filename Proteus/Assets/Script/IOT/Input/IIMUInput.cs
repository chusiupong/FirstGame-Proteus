namespace FitnessGame.IOT
{
    /// <summary>
    /// IMU input interface for motion sensing.
    /// </summary>
    public interface IIMUInput
    {
        IMUData GetIMUData();
        bool IsConnected();
        void Initialize();
        void Shutdown();
    }
}