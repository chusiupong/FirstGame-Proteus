using System;
using UnityEngine;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Motor command codes (Table 2)
    /// </summary>
    public enum MotorCommand : byte
    {
        GetStatus = 0x00,           // 获取状态
        SetMode1 = 0x01,            // 设置模式1 - 主要使用这个
        SerialEnable = 0x02,        // 串行使能/失能
        CalibrationMode = 0x03,     // 电机校准模式
        CalibrateMotor1 = 0x04,     // 校准电机1力量
        CalibrateMotor2 = 0x05,     // 校准电机2力量
        SetMotorCache = 0x06,       // 设置电机缓值
        RestoreSettings = 0xA0,     // 恢复功能设置
        SetPowerOff = 0xA2,         // 设置关机功能
        QueryFault = 0xF0,          // 查询故障码
        GetVersion = 0xF1,          // 获取版本
        SetUserID = 0xFF            // 设置用户ID
    }

    /// <summary>
    /// Motor control command packet (32 bytes)
    /// Based on Direct Drive Tech motor protocol
    /// </summary>
    public class MotorCommandPacket
    {
        // Packet is 32 bytes total
        private byte[] packet = new byte[32];

        public MotorCommandPacket()
        {
            // Initialize fixed values
            packet[0] = 0x64;   // Fixed header
            packet[30] = 0x0D;  // End flag 1
            packet[31] = 0x0A;  // End flag 2
        }

        /// <summary>
        /// Set which motor to control
        /// </summary>
        /// <param name="motorId">0=Motor1, 1=Motor2, 2=Both</param>
        public void SetMotor(byte motorId)
        {
            packet[1] = motorId;
        }

        /// <summary>
        /// Set command code (refer to Table 2 in manual)
        /// </summary>
        public void SetCommandCode(byte code)
        {
            packet[2] = code;
        }

        /// <summary>
        /// Set command code using enum
        /// </summary>
        public void SetCommand(MotorCommand command)
        {
            packet[2] = (byte)command;
        }

        /// <summary>
        /// Set mode/status (refer to Table 3 in manual)
        /// </summary>
        public void SetMode(byte mode)
        {
            packet[3] = mode;
        }

        /// <summary>
        /// Set resistance force (base force)
        /// </summary>
        public void SetResistanceForce(byte force)
        {
            packet[4] = force;
            packet[5] = force;
        }

        /// <summary>
        /// Set pull force (maximum force)
        /// </summary>
        public void SetPullForce(byte force)
        {
            packet[7] = force;
        }

        /// <summary>
        /// Set speed coefficient (0-10 for constant speed mode)
        /// </summary>
        public void SetSpeedCoefficient(byte coeff)
        {
            packet[8] = coeff;
        }

        /// <summary>
        /// Set spring distance (10-255 cm for spring mode)
        /// </summary>
        public void SetSpringDistance(byte distance)
        {
            packet[9] = distance;
        }

        /// <summary>
        /// Clear motor pull count
        /// </summary>
        /// <param name="clearFlag">1=Motor1, 2=Motor2, 3=Both</param>
        public void ClearPullCount(byte clearFlag)
        {
            packet[11] = clearFlag;
        }

        /// <summary>
        /// Set slow speed mode
        /// </summary>
        /// <param name="enabled">true=enable slow speed</param>
        /// <param name="coefficient">1-250, higher=faster change</param>
        public void SetSlowSpeed(bool enabled, byte coefficient = 25)
        {
            packet[12] = (byte)(enabled ? 1 : 0);
            packet[13] = coefficient;
        }

        /// <summary>
        /// Calculate and set CRC checksum
        /// </summary>
        private void CalculateCRC()
        {
            // TODO: Implement actual CRC calculation based on motor manual
            // For now, simple sum checksum
            byte crc = 0;
            for (int i = 0; i < 29; i++)
            {
                crc += packet[i];
            }
            packet[29] = crc;
        }

        /// <summary>
        /// Get the final packet bytes ready to send
        /// </summary>
        public byte[] GetBytes()
        {
            CalculateCRC();
            return packet;
        }

        /// <summary>
        /// Create a simple test packet (motor 1, all zeros)
        /// </summary>
        public static MotorCommandPacket CreateTestPacket()
        {
            var packet = new MotorCommandPacket();
            packet.SetMotor(0);  // Motor 1
            packet.SetCommandCode(0x00);
            packet.SetMode(0x00);
            return packet;
        }

        /// <summary>
        /// Create a standard work mode packet
        /// Uses SetMode1 command - TODO: specify exact mode later
        /// </summary>
        public static MotorCommandPacket CreateWorkModePacket(byte motorId = 0, byte mode = 0x00)
        {
            var packet = new MotorCommandPacket();
            packet.SetMotor(motorId);
            packet.SetCommand(MotorCommand.SetMode1);  // 使用设置模式1指令
            packet.SetMode(mode);  // 具体模式待定
            return packet;
        }

        /// <summary>
        /// Create power ON packet (enable motor)
        /// Command: 0x02, Mode: 0xAA
        /// </summary>
        public static MotorCommandPacket CreatePowerOnPacket(byte motorId = 2)
        {
            var packet = new MotorCommandPacket();
            packet.SetMotor(motorId);  // Default: both motors
            packet.SetCommand(MotorCommand.SerialEnable);  // 0x02
            packet.SetMode(0xAA);  // enable
            return packet;
        }

        /// <summary>
        /// Create power OFF packet (disable motor)
        /// Command: 0x02, Mode: 0x55
        /// </summary>
        public static MotorCommandPacket CreatePowerOffPacket(byte motorId = 2)
        {
            var packet = new MotorCommandPacket();
            packet.SetMotor(motorId);  // Default: both motors
            packet.SetCommand(MotorCommand.SerialEnable);  // 0x02
            packet.SetMode(0x55);  // disable
            return packet;
        }

        /// <summary>
        /// Debug string representation
        /// </summary>
        public override string ToString()
        {
            string cmdName = Enum.IsDefined(typeof(MotorCommand), packet[2]) 
                ? ((MotorCommand)packet[2]).ToString() 
                : $"0x{packet[2]:X2}";
            return $"MotorPacket [Motor:{packet[1]} Cmd:{cmdName} Mode:0x{packet[3]:X2}]";
        }
    }
}
