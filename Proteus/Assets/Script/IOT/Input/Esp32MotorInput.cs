namespace FitnessGame.IOT
{
    /// <summary>
    /// Real motor adapter backed by ESP32 serial client.
    /// </summary>
    public class Esp32MotorInput : IMotorInput
    {
        private readonly Esp32SensorClient client;

        public Esp32MotorInput(Esp32SensorClient client)
        {
            this.client = client;
        }

        public MotorData GetMotorData()
        {
            return client.GetLatestMotorData();
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

        public void Reset()
        {
            client.ResetMotorEstimate();
        }

        public void SendCommand(MotorCommandPacket packet)
        {
            client.SendMotorCommand(packet);
        }
    }
}