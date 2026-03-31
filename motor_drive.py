import serial
import time


# =========================
# Basic protocol constants
# =========================
PORT = "COM18"  # Change to your serial port, for example COM10 / COM6
BAUDRATE = 57600
TIMEOUT = 0.2

FRAME_LEN = 32
STATUS_FRAME_LEN = 40
VERSION_FRAME_LEN = 7

FRAME_HEAD_0 = 0x64
FRAME_END_0 = 0x0D
FRAME_END_1 = 0x0A

# Motor select
MOTOR_0 = 0x00

# Commands
CMD_GET_STATUS = 0x00
CMD_SET_MOTOR = 0x01
CMD_ENABLE_DISABLE = 0x02
CMD_CALIB_MODE = 0x03
CMD_CALIB_MOTOR1 = 0x04
CMD_CALIB_MOTOR2 = 0x05
CMD_SET_COMPENSATION = 0x06
CMD_RELEASE_PROTECT = 0xA0
CMD_BALANCE_FUNC = 0xA2
CMD_RESET_FAULT = 0xF0
CMD_GET_VERSION = 0xF1
CMD_DEVICE_REBOOT = 0xFF

# Modes
MODE_STANDARD = 0x00
MODE_SPRING = 0x02
MODE_ROWING = 0x03
MODE_BALANCE = 0x04
MODE_ISOKINETIC = 0x06
MODE_CUSTOM_ELASTIC = 0x08
MODE_RESET_START = 0xFC
MODE_SOFT_RESET = 0xFF


def crc8_maxim(data: bytes) -> int:
    """CRC-8/MAXIM, reflected poly 0x8C."""
    crc = 0x00
    for byte in data:
        crc ^= byte
        for _ in range(8):
            if crc & 0x01:
                crc = (crc >> 1) ^ 0x8C
            else:
                crc >>= 1
    return crc & 0xFF


def u16_to_hi_lo(value: int) -> tuple[int, int]:
    if not (0 <= value <= 0xFFFF):
        raise ValueError(f"value out of range: {value}")
    return (value >> 8) & 0xFF, value & 0xFF


def i16_to_hi_lo(value: int) -> tuple[int, int]:
    if not (-0x8000 <= value <= 0x7FFF):
        raise ValueError(f"signed 16-bit value out of range: {value}")
    raw = value & 0xFFFF
    return (raw >> 8) & 0xFF, raw & 0xFF


def hi_lo_to_u16(hi: int, lo: int) -> int:
    return ((hi & 0xFF) << 8) | (lo & 0xFF)


def hex_str(data: bytes) -> str:
    return " ".join(f"{b:02X}" for b in data)


def parse_version_number(raw_value: int) -> str:
    date_code = raw_value >> 12
    hw_sw = raw_value & 0xFFF
    return f"date_code={date_code}, hw={hw_sw >> 8}.{(hw_sw >> 4) & 0xF}, sw={hw_sw & 0xF}"


class ProtocolError(Exception):
    pass


class DeviceRejectedError(ProtocolError):
    pass


class MotorDriver:
    def __init__(self, port: str, baudrate: int = BAUDRATE, timeout: float = TIMEOUT):
        self.ser = serial.Serial(port=port, baudrate=baudrate, timeout=timeout)
        self.ser.reset_input_buffer()
        self.ser.reset_output_buffer()

    def close(self) -> None:
        if self.ser and self.ser.is_open:
            self.ser.close()

    def listen_only(self, duration_s: float = 10.0) -> list[bytes]:
        self.ser.reset_input_buffer()
        deadline = time.time() + duration_s
        chunks: list[bytes] = []
        print(f"Listening on {self.ser.port} for {duration_s:.1f}s ...")
        while time.time() < deadline:
            waiting = self.ser.in_waiting
            if waiting > 0:
                chunk = self.ser.read(waiting)
                if chunk:
                    chunks.append(chunk)
                    print(f"RX ({len(chunk)} bytes): {hex_str(chunk)}")
            else:
                time.sleep(0.02)
        if not chunks:
            print("RX: <no data during listen window>")
        return chunks

    def _build_frame(self, motor_sel: int, cmd: int) -> bytearray:
        frame = bytearray([0x00] * FRAME_LEN)
        frame[0] = FRAME_HEAD_0
        frame[1] = motor_sel
        frame[2] = cmd
        frame[30] = FRAME_END_0
        frame[31] = FRAME_END_1
        return frame

    def _finalize_frame(self, frame: bytearray) -> bytes:
        if len(frame) != FRAME_LEN:
            raise ValueError("frame length must be 32")
        frame[29] = crc8_maxim(bytes(frame[:29]))
        return bytes(frame)

    def _read_exact(self, expected_len: int, wait_s: float) -> bytes:
        deadline = time.time() + max(wait_s, self.ser.timeout or 0.0) + 0.5
        data = bytearray()
        while len(data) < expected_len and time.time() < deadline:
            chunk = self.ser.read(expected_len - len(data))
            if chunk:
                data.extend(chunk)
            else:
                time.sleep(0.01)
        return bytes(data)

    def _validate_status_frame(self, frame: bytes) -> None:
        if frame == b"\xFF":
            raise DeviceRejectedError("device returned 0xFF, command rejected or no valid status frame available")
        if len(frame) != STATUS_FRAME_LEN:
            raise ProtocolError(f"status frame length mismatch: expected 40, got {len(frame)}")
        if frame[0] != FRAME_HEAD_0 or frame[1] != 0x02:
            raise ProtocolError(f"invalid status frame header: {hex_str(frame[:2])}")
        if frame[38] != FRAME_END_0 or frame[39] != FRAME_END_1:
            raise ProtocolError(f"invalid status frame tail: {hex_str(frame[-2:])}")
        expected_crc = crc8_maxim(frame[:37])
        if frame[37] != expected_crc:
            raise ProtocolError(
                f"status CRC mismatch: expected {expected_crc:02X}, got {frame[37]:02X}"
            )

    def _parse_status_frame(self, frame: bytes) -> dict:
        self._validate_status_frame(frame)
        return {
            "mode": frame[2],
            "motor1_force": hi_lo_to_u16(frame[3], frame[4]),
            "motor1_speed_cm_s": hi_lo_to_u16(frame[5], frame[6]),
            "motor1_distance_cm": hi_lo_to_u16(frame[7], frame[8]),
            "motor1_count": hi_lo_to_u16(frame[9], frame[10]),
            "motor1_train_state": frame[11],
            "motor1_single_time_s_x100": hi_lo_to_u16(frame[12], frame[13]),
            "motor2_force": hi_lo_to_u16(frame[14], frame[15]),
            "motor2_speed_cm_s": hi_lo_to_u16(frame[16], frame[17]),
            "motor2_distance_cm": hi_lo_to_u16(frame[18], frame[19]),
            "motor2_count": hi_lo_to_u16(frame[20], frame[21]),
            "motor2_train_state": frame[22],
            "motor2_single_time_s_x100": hi_lo_to_u16(frame[23], frame[24]),
            "motor1_fault": frame[25],
            "motor2_fault": frame[26],
            "motor1_state": frame[27],
            "motor2_state": frame[28],
            "motor1_alarm": frame[29],
            "motor2_alarm": frame[30],
            "motor1_temp_c": frame[31],
            "motor2_temp_c": frame[32],
            "driver_temp_c": frame[33],
            "bus_voltage_raw": hi_lo_to_u16(frame[34], frame[35]),
            "fan_state": frame[36],
            "crc": frame[37],
        }

    def _parse_version_frame(self, frame: bytes) -> dict:
        if frame == b"\xFF":
            raise DeviceRejectedError("device returned 0xFF, version query rejected")
        if len(frame) != VERSION_FRAME_LEN:
            raise ProtocolError(f"version frame length mismatch: expected 7, got {len(frame)}")
        if frame[0] != 0x02 or frame[1] != FRAME_HEAD_0:
            raise ProtocolError(f"invalid version frame header: {hex_str(frame[:2])}")

        version_raw = (frame[2] << 24) | (frame[3] << 16) | (frame[4] << 8) | frame[5]
        crc_ok = crc8_maxim(frame[:6]) == frame[6]

        return {
            "version_raw": version_raw,
            "version_hex": f"0x{version_raw:08X}",
            "version_note": parse_version_number(version_raw),
            "crc": frame[6],
            "crc_ok": crc_ok,
        }

    def send_frame(
        self,
        frame: bytearray,
        read_response: bool = True,
        wait_s: float = 0.1,
        expected_len: int | None = None,
    ) -> bytes:
        packet = self._finalize_frame(frame)
        self.ser.reset_input_buffer()
        print(f"\nTX ({len(packet)} bytes): {hex_str(packet)}")
        self.ser.write(packet)
        self.ser.flush()
        time.sleep(wait_s)

        if not read_response:
            return b""

        if expected_len is None:
            resp = self.ser.read(128)
        else:
            resp = self._read_exact(expected_len, wait_s)

        if resp:
            print(f"RX ({len(resp)} bytes): {hex_str(resp)}")
        else:
            print("RX: <no response>")
        return resp

    def enable_motor0(self) -> bytes:
        frame = self._build_frame(MOTOR_0, CMD_ENABLE_DISABLE)
        frame[3] = 0xAA
        return self.send_frame(frame, read_response=False)

    def disable_motor0(self) -> bytes:
        frame = self._build_frame(MOTOR_0, CMD_ENABLE_DISABLE)
        frame[3] = 0x55
        return self.send_frame(frame, read_response=False)

    def get_status_motor0(self) -> dict:
        frame = self._build_frame(MOTOR_0, CMD_GET_STATUS)
        resp = self.send_frame(frame, expected_len=STATUS_FRAME_LEN)
        if not resp:
            raise ProtocolError("no status response received")
        return self._parse_status_frame(resp)

    def get_version(self) -> dict:
        frame = self._build_frame(MOTOR_0, CMD_GET_VERSION)
        resp = self.send_frame(frame, expected_len=VERSION_FRAME_LEN)
        if not resp:
            raise ProtocolError("no version response received")
        return self._parse_version_frame(resp)

    def reboot_device(self) -> bytes:
        frame = self._build_frame(MOTOR_0, CMD_DEVICE_REBOOT)
        return self.send_frame(frame, read_response=False, wait_s=0.3)

    def set_standard_mode(
        self,
        pull_value: int,
        return_value: int,
        clear_count: int = 0,
        slow_change: int = 0,
        slow_coeff: int = 25,
    ) -> bytes:
        frame = self._build_frame(MOTOR_0, CMD_SET_MOTOR)
        frame[3] = MODE_STANDARD

        ret_hi, ret_lo = u16_to_hi_lo(return_value)
        pull_hi, pull_lo = u16_to_hi_lo(pull_value)
        frame[4] = ret_hi
        frame[5] = ret_lo
        frame[6] = pull_hi
        frame[7] = pull_lo
        frame[11] = clear_count & 0xFF
        frame[12] = slow_change & 0xFF
        frame[13] = slow_coeff & 0xFF
        return self.send_frame(frame, read_response=False)

    def set_spring_mode(
        self,
        base_return_value: int,
        max_pull_value: int,
        spring_distance_cm: int = 50,
        clear_count: int = 0,
        slow_change: int = 0,
        slow_coeff: int = 25,
    ) -> bytes:
        if not (10 <= spring_distance_cm <= 255):
            raise ValueError("spring_distance_cm must be 10~255")

        frame = self._build_frame(MOTOR_0, CMD_SET_MOTOR)
        frame[3] = MODE_SPRING

        ret_hi, ret_lo = u16_to_hi_lo(base_return_value)
        pull_hi, pull_lo = u16_to_hi_lo(max_pull_value)
        frame[4] = ret_hi
        frame[5] = ret_lo
        frame[6] = pull_hi
        frame[7] = pull_lo
        frame[8] = 0x00
        frame[9] = spring_distance_cm
        frame[11] = clear_count & 0xFF
        frame[12] = slow_change & 0xFF
        frame[13] = slow_coeff & 0xFF
        return self.send_frame(frame, read_response=False)

    def set_rowing_mode(
        self,
        return_value: int,
        clear_count: int = 0,
        slow_change: int = 0,
        slow_coeff: int = 25,
    ) -> bytes:
        frame = self._build_frame(MOTOR_0, CMD_SET_MOTOR)
        frame[3] = MODE_ROWING

        ret_hi, ret_lo = u16_to_hi_lo(return_value)
        frame[4] = ret_hi
        frame[5] = ret_lo
        frame[11] = clear_count & 0xFF
        frame[12] = slow_change & 0xFF
        frame[13] = slow_coeff & 0xFF
        return self.send_frame(frame, read_response=False)

    def set_compensation(self, return_comp: int, pull_comp: int) -> bytes:
        frame = self._build_frame(MOTOR_0, CMD_SET_COMPENSATION)
        ret_hi, ret_lo = i16_to_hi_lo(return_comp)
        pull_hi, pull_lo = i16_to_hi_lo(pull_comp)
        frame[4] = ret_hi
        frame[5] = ret_lo
        frame[6] = pull_hi
        frame[7] = pull_lo
        return self.send_frame(frame, read_response=False)

    def reset_start_point(self) -> bytes:
        frame = self._build_frame(MOTOR_0, CMD_SET_MOTOR)
        frame[3] = MODE_RESET_START
        return self.send_frame(frame, read_response=False)

    def soft_reset_from_mode(self) -> bytes:
        frame = self._build_frame(MOTOR_0, CMD_SET_MOTOR)
        frame[3] = MODE_SOFT_RESET
        return self.send_frame(frame, read_response=False, wait_s=0.3)


def print_menu() -> None:
    print("\n========== MOTOR DEBUG MENU ==========")
    print("1. Enable motor0")
    print("2. Disable motor0")
    print("3. Get status")
    print("4. Get version")
    print("5. Set STANDARD mode (small force)")
    print("6. Set SPRING mode (small force)")
    print("7. Set ROWING mode (small force)")
    print("8. Reset start point")
    print("9. Reboot device")
    print("10. Set compensation (+100 / +100)")
    print("11. Listen only for 10 seconds")
    print("0. Exit")
    print("======================================")


def main() -> None:
    driver = None
    try:
        driver = MotorDriver(PORT)
        print(f"Opened serial: {PORT} @ {BAUDRATE}")

        while True:
            print_menu()
            choice = input("Select: ").strip()

            if choice == "1":
                driver.enable_motor0()

            elif choice == "2":
                driver.disable_motor0()

            elif choice == "3":
                status = driver.get_status_motor0()
                print("Parsed status:")
                for key, value in status.items():
                    print(f"  {key}: {value}")

            elif choice == "4":
                version = driver.get_version()
                print("Parsed version:")
                for key, value in version.items():
                    print(f"  {key}: {value}")

            elif choice == "5":
                driver.set_standard_mode(
                    pull_value=300,
                    return_value=300,
                    clear_count=1,
                    slow_change=0,
                    slow_coeff=25,
                )

            elif choice == "6":
                driver.set_spring_mode(
                    base_return_value=200,
                    max_pull_value=500,
                    spring_distance_cm=40,
                    clear_count=1,
                    slow_change=0,
                    slow_coeff=25,
                )

            elif choice == "7":
                driver.set_rowing_mode(
                    return_value=400,
                    clear_count=1,
                    slow_change=0,
                    slow_coeff=25,
                )

            elif choice == "8":
                driver.reset_start_point()

            elif choice == "9":
                driver.reboot_device()

            elif choice == "10":
                driver.set_compensation(return_comp=100, pull_comp=100)

            elif choice == "11":
                driver.listen_only(duration_s=10.0)

            elif choice == "0":
                print("Bye.")
                break

            else:
                print("Invalid choice.")

    except (serial.SerialException, ProtocolError, ValueError) as e:
        print(f"Error: {e}")
    except KeyboardInterrupt:
        print("\nInterrupted by user.")
    finally:
        if driver is not None:
            driver.close()


if __name__ == "__main__":
    main()
