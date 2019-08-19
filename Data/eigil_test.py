import zmq
import time

 

context = zmq.Context()
socket = context.socket(zmq.REQ)
socket.connect ("tcp://10.24.11.87:8989")

 

f = open("eigil.jpg","rb")
b = f.read()

 

t0 = time.time()
for request in range (1,100):
    print("Sending request ", request,"...")
    socket.send(b)
    message = socket.recv()
    t = time.time()
    print(t-t0)
    t0 = time.time()
