import socket
import time

# Send a message to Unity LOCALLY
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

print("Waiting 2 seconds...")
time.sleep(2)

# Send to Unity
sock.sendto(b"START", ("127.0.0.1", 8888))
print("✅ SENT SUCCESS")
