using System;
using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// One aligned sensor snapshot used by the IoT pipeline.
    /// </summary>
    [Serializable]
    public class SensorFrame
    {
        public CameraData camera;
        public MotorData motor;
        public IMUData imu;
        public float timestamp;

        public SensorFrame(CameraData camera, MotorData motor, IMUData imu)
        {
            this.camera = camera ?? new CameraData();
            this.motor = motor ?? new MotorData();
            this.imu = imu ?? new IMUData();
            timestamp = Time.time;
        }
    }
}