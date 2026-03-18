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
        public Direction GetMovementDirection()
        {
            if (acceleration.magnitude < 0.1f) return Direction.None; // No significant movement

            // Determine primary movement direction based on acceleration
            if (Mathf.Abs(acceleration.x) > Mathf.Abs(acceleration.y) && Mathf.Abs(acceleration.x) > Mathf.Abs(acceleration.z))
                return acceleration.x > 0 ? Direction.Right : Direction.Left;
            else if (Mathf.Abs(acceleration.y) > Mathf.Abs(acceleration.x) && Mathf.Abs(acceleration.y) > Mathf.Abs(acceleration.z))
                return acceleration.y > 0 ? Direction.Up : Direction.Down;
            else
                return acceleration.z > 0 ? Direction.Forward : Direction.Backward;
        }


        public override string ToString()
        {
            return $"IMU [Acc:{acceleration} Gyro:{gyroscope}]";
        }

    }
}