namespace FitnessGame.IOT
{
    /// <summary>
    /// Real IMU adapter backed by ESP32 serial client.
    /// </summary>
    public class Esp32IMUInput : IIMUInput
    {
        private readonly Esp32SensorClient client;

        public Esp32IMUInput(Esp32SensorClient client)
        {
            this.client = client;
        }

        public IMUData GetIMUData()
        {
            return client.GetLatestImuData();
        }

        public bool IsConnected()
        {
            return client.IsConnected;
        }

        public void Initialize()
        {
            client.Initialize();
        }

        public void Shutdown()
        {
            client.Shutdown();
        }
    }
}