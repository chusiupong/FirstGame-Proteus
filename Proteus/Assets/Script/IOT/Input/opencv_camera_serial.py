import cv2
import mediapipe as mp
import numpy as np
import serial
import time
import warnings

warnings.filterwarnings("ignore", category=UserWarning)

mp_drawing = mp.solutions.drawing_utils
mp_pose = mp.solutions.pose

CAM_WIDTH, CAM_HEIGHT = 640, 480
COUNTDOWN = 2
FPS_LIMIT = 15

# Pose thresholds
BOW_ARM_STRAIGHT = 120
DRAW_ARM_BENT = 100

# Serial config: must match FitnessConfig.CameraPortName / CameraBaudRate
# Use a dedicated camera COM port (not the same COM as ESP32 motor/IMU).
SERIAL_PORT = "COM7"
SERIAL_BAUD = 115200
SERIAL_WRITE_INTERVAL = 0.03  # ~33Hz

state = {
    "active": False,
    "is_drawing": False,
    "last_send": 0.0,
}


def get_elbow_angle(landmarks, is_left):
    sh_idx = mp_pose.PoseLandmark.LEFT_SHOULDER if is_left else mp_pose.PoseLandmark.RIGHT_SHOULDER
    el_idx = mp_pose.PoseLandmark.LEFT_ELBOW if is_left else mp_pose.PoseLandmark.RIGHT_ELBOW
    wr_idx = mp_pose.PoseLandmark.LEFT_WRIST if is_left else mp_pose.PoseLandmark.RIGHT_WRIST

    sh = np.array([landmarks.landmark[sh_idx].x, landmarks.landmark[sh_idx].y])
    el = np.array([landmarks.landmark[el_idx].x, landmarks.landmark[el_idx].y])
    wr = np.array([landmarks.landmark[wr_idx].x, landmarks.landmark[wr_idx].y])

    v1 = sh - el
    v2 = wr - el
    cos_ang = np.dot(v1, v2) / (np.linalg.norm(v1) * np.linalg.norm(v2) + 1e-6)
    return np.degrees(np.arccos(np.clip(cos_ang, -1, 1)))


def compute_confidence(bow_angle, draw_angle):
    draw_pose = bow_angle > BOW_ARM_STRAIGHT and draw_angle < DRAW_ARM_BENT
    if not draw_pose:
        return 0.0

    # Simple confidence shaping around thresholds.
    bow_part = min(1.0, max(0.0, (bow_angle - BOW_ARM_STRAIGHT) / 40.0))
    draw_part = min(1.0, max(0.0, (DRAW_ARM_BENT - draw_angle) / 40.0))
    confidence = 0.5 + 0.5 * (0.6 * bow_part + 0.4 * draw_part)
    return max(0.0, min(1.0, confidence))


def send_camera(serial_conn, confidence):
    line = f"CAMERA,{confidence:.3f}\n"
    serial_conn.write(line.encode("ascii"))


def main():
    print("[CAM] Starting OpenCV camera serial bridge")
    print(f"[CAM] Serial -> {SERIAL_PORT} @ {SERIAL_BAUD}")

    ser = serial.Serial(SERIAL_PORT, SERIAL_BAUD, timeout=0.01, write_timeout=0.05)
    cap = cv2.VideoCapture(0)
    cap.set(3, CAM_WIDTH)
    cap.set(4, CAM_HEIGHT)
    cap.set(cv2.CAP_PROP_FPS, FPS_LIMIT)

    pose = mp_pose.Pose(model_complexity=0, min_detection_confidence=0.4)
    start = time.time()

    try:
        while True:
            ret, frame = cap.read()
            if not ret:
                break

            frame = cv2.flip(frame, 1)
            rem = max(0, COUNTDOWN - int(time.time() - start))
            if rem == 0:
                state["active"] = True

            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            res = pose.process(rgb)

            bow_angle, draw_angle = 0.0, 0.0
            confidence = 0.0

            if res.pose_landmarks:
                mp_drawing.draw_landmarks(frame, res.pose_landmarks, mp_pose.POSE_CONNECTIONS)
                bow_angle = get_elbow_angle(res.pose_landmarks, is_left=False)
                draw_angle = get_elbow_angle(res.pose_landmarks, is_left=True)
                if state["active"]:
                    confidence = compute_confidence(bow_angle, draw_angle)

            now = time.time()
            if now - state["last_send"] >= SERIAL_WRITE_INTERVAL:
                send_camera(ser, confidence)
                state["last_send"] = now

            status = "DRAW" if confidence > 0.7 else "NEUTRAL"
            cv2.rectangle(frame, (0, 0), (CAM_WIDTH, 100), (0, 0, 0), -1)
            cv2.putText(
                frame,
                f"Bow:{bow_angle:.0f} Draw:{draw_angle:.0f} Conf:{confidence:.2f} {status}",
                (10, 60),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.9,
                (0, 255, 255),
                2,
            )

            cv2.imshow("Camera -> Serial (Q to quit)", frame)
            if cv2.waitKey(1) & 0xFF == ord("q"):
                break

    finally:
        cap.release()
        cv2.destroyAllWindows()
        pose.close()
        ser.close()
        print("[CAM] Exit clean")


if __name__ == "__main__":
    main()
