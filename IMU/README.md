# ESP32 + ICM42688P Wireless IMU Demo

This example sends IMU data from an ESP32 to a computer over UDP.

It tries these modes in order:

- Connect to your normal WiFi first
- If WiFi fails, automatically start an ESP32 hotspot

Packet format:

```text
IMU,ts,ax,ay,az,gx,gy,gz\n
```

Example:

```text
IMU,1234,0.012300,-0.004500,0.998100,0.120000,-0.080000,0.030000
```

## Wiring

- `ICM42688P SDA` -> `ESP32 GPIO21`
- `ICM42688P SCL` -> `ESP32 GPIO22`
- `ICM42688P VCC` -> `ESP32 3V3`
- `ICM42688P GND` -> `ESP32 GND`

If your board uses different I2C pins, update them in the sketch.

## Arduino libraries

Install:

- `SparkFun ICM-42688-P Arduino Library`

## How to use

1. Open `esp32_icm42688_udp/esp32_icm42688_udp.ino`
2. Fill in:
   - Optional: `TEST_MODE = true` for hardware diagnosis
   - `WIFI_SSID`
   - `WIFI_PASS`
   - `PC_IP`
   - Optional: `AP_SSID` / `AP_PASS`
3. Upload to ESP32
4. On the computer, run:

```bash
python udp_receiver.py
```

## Mode 1: Normal WiFi

- ESP32 connects to `WIFI_SSID`
- UDP is sent to `PC_IP:5005`
- Your computer and ESP32 must be on the same LAN

## Mode 2: Fallback Hotspot

If the ESP32 cannot connect to WiFi within about 10 seconds, it will create a hotspot:

- SSID: `ESP32_IMU_AP`
- Password: `12345678`

Then:

1. Connect your computer to this hotspot
2. Run `python udp_receiver.py`
3. The ESP32 will send UDP to the hotspot broadcast address automatically

In this fallback mode, you usually do not need to change `udp_receiver.py`.

## Test Mode

Set:

```cpp
constexpr bool TEST_MODE = true;
```

Then the sketch will:

- Scan the I2C bus at startup
- Print all detected I2C addresses to the serial monitor
- Try to initialize the ICM42688
- Keep printing heartbeat and diagnosis messages instead of sending UDP

This is useful when the IMU powers on but still fails to initialize.

## Notes

- `ts` uses `millis()`, so the unit is milliseconds.
- In hotspot mode, the computer connects directly to the ESP32 AP.
- If the IMU address is `0x69`, change `IMU_I2C_ADDR`.
