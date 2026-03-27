using System;
using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// IMU sensor data for fitness game
    /// </summary>
    [Serializable]
    public class IMUData
    {
        public Vector3 acceleration;   // Acceleration in m/s²
        public Vector3 gyroscope;      // Angular velocity in °/s
        public float timestamp;        // When this data was captured

        public IMUData()
        {
            acceleration = Vector3.zero;
            gyroscope = Vector3.zero;
            timestamp = Time.time;
        }

        public IMUData(Vector3 acceleration, Vector3 gyroscope)
        {
            this.acceleration = acceleration;
            this.gyroscope = gyroscope;
            this.timestamp = Time.time;
        }

        public override string ToString()
        {
            return $"IMU [Acc:{acceleration} Gyro:{gyroscope}]";
        }

    }
}