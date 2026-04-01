using UnityEngine;

namespace FitnessGame.IOT
{
	/// <summary>
	/// Mock IMU input using keyboard for testing.
	/// </summary>
	public class MockIMUData : IIMUInput
	{
		private bool isInitialized;

		public void Initialize()
		{
			isInitialized = true;
			Debug.Log("[IOT][MockIMU] Initialized. Hold I to simulate valid IMU movement");
		}

		public void Shutdown()
		{
			isInitialized = false;
			Debug.Log("[IOT][MockIMU] Shutdown");
		}

		public bool IsConnected()
		{
			return isInitialized;
		}

		public IMUData GetIMUData()
		{
			if (!isInitialized)
				return new IMUData();

			// Keep IMU mock intentionally simple: one key means "movement present".
			bool hasMotion = Input.GetKey(KeyCode.I);
			Vector3 acceleration = hasMotion ? new Vector3(0f, 9.8f, 0f) : Vector3.zero;
			Vector3 gyroscope = hasMotion ? new Vector3(60f, 20f, 10f) : Vector3.zero;

			return new IMUData(acceleration, gyroscope);
		}
	}
}

