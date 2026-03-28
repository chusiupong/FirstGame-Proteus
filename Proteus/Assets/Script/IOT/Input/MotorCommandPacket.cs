using System;

namespace FitnessGame.IOT
{
    /// <summary>
    /// Motor control command packet (32 bytes)
    /// Simplified byte protocol based on standard spec
    /// </summary>
    public class MotorCommandPacket
    {
        private byte[] packet = new byte[32];

        private MotorCommandPacket()
        {
            packet[0] = 0x64;   // Fixed header DATA[0]
            packet[30] = 0x0D;  // End flag 1
            packet[31] = 0x0A;  // End flag 2
        }

        /// <summary>
        /// Create power ON packet (enable motor)
        /// DATA[1]=0x02, DATA[3]=0xAA
        /// </summary>
        public static MotorCommandPacket CreatePowerOnPacket()
        {
            var cmd = new MotorCommandPacket();
            cmd.packet[1] = 0x02; // Target: Both motors (0x02)
            cmd.packet[3] = 0xAA; // Status: Enable
            return cmd;
        }

        /// <summary>
        /// Create power OFF packet (disable motor)
        /// DATA[1]=0x02, DATA[3]=0x55
        /// </summary>
        public static MotorCommandPacket CreatePowerOffPacket()
        {
            var cmd = new MotorCommandPacket();
            cmd.packet[1] = 0x02; // Target: Both motors (0x02)
            cmd.packet[3] = 0x55; // Status: Disable
            return cmd;
        }

        /// <summary>
        /// Create Spring Mode config packet
        /// </summary>
        public static MotorCommandPacket CreateSpringModePacket(byte force, byte distance)
        {
            var cmd = new MotorCommandPacket();
            cmd.packet[1] = 0x02; // Target: Both motors
            cmd.packet[2] = 0x01; // Command code
            cmd.packet[3] = 0x02; // Mode code: Spring
            
            // Single-motor demo mode: Motor1 active, Motor2 disabled.
            cmd.packet[4] = force; // Motor1 base return force
            cmd.packet[5] = 0; // Motor2
            cmd.packet[6] = 100;   // Motor1 max pull force limit (100 decimal = 0x64)
            cmd.packet[7] = 0;     // Motor2 max pull force disabled

            cmd.packet[9] = distance; // Spring length
            
            return cmd;
        }
        public static MotorCommandPacket CreateSetForcePacket(byte force)
        {
            var cmd = new MotorCommandPacket();
            cmd.packet[1] = 0x02; // Target: Both motors
            cmd.packet[2] = 0x03; // Command code
            
            // Single-motor demo mode: Motor1 active, Motor2 disabled.
            cmd.packet[4] = force; // Motor1 base return force
            cmd.packet[5] = 0; // Motor2
            
            return cmd;
        }
        private void CalculateCRC()
        {
            byte crc = 0;
            for (int i = 0; i < 29; i++)
            {
                crc += packet[i];
            }
            packet[29] = crc;
        }

        public byte[] GetBytes()
        {
            CalculateCRC();
            return packet;
        }
    }
}
