# pose-recorder
Application part of 3D pose-recording and replay using Unity ARFoundation
Be aware that without the pose detection ZMQ server this project is useless.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See deployment for notes on how to deploy the project on a live system.

### Prerequisites

What things you need to install the software and how to install them

 * [NetMQ](https://netmq.readthedocs.io/en/latest/#installation) is included in project but may need [ZMQ](https://zeromq.org/download/) installed.
 * ARFoundation 2.2 (enable preview packages in Unity package manager)
 * Unity3D 2019.2 (it will likely run on 2019.0 but has not been tested)
 * Other stuff I'm probably forgetting

### Installing

A step by step series of examples that tell you how to get a development env running

 1. Git clone or unzip the repo in your preferred Unity projects folder

```
cd /path/to/folder
git clone https://github.com/ChaiKnight/pose-recorder.git
```

 2. Open Unity Hub and Add new project, navigating to your folder and selecting it.

 3. Try and build the project on your phone. If you can press the record button, this part of the project is working.

## Deployment

Deploying this project currently requires a ZMQ server. More info to follow. 

## Authors

* **Oliver G. Hjermitslev** - [ChaiKnight](https://github.com/ChaiKnight)

See also the list of [contributors](https://github.com/ChaiKnight/pose-recorder/contributors) who participated in this project


# Human pose estimation service
The server-side part of the project. Runs a ZMQ service, which takes an image and returns a list of pose estimates.

## Requirements
Software:
* Python 3.6+
* Pytorch 1.2.0 (*Older versions may work*)
* Torchvision 0.4.0
* pyzmq
* Pillow
* python-json
* numpy
* CUDA

Hardware:
* A GPU with CUDA support.

## Running the service
```bash
python pose-service.py <address of the machine>:<port>
```
Replace the address and port, e.g. with `127.0.0.1:8000` (localhost)

## How it works
#####  ZMQ service
The script starts a ZMQ REPLY service on the port specified. When this service receives a request, containing an image (e.g. a JPG or PNG), it runs the pose-estimation network and returns a JSON string, containing estimated poses.
##### Inference network
The inference system uses a pretrained network from torchvision (`torchvision.models.detection.keypointrcnn_resnet50_fpn`). Some of the parameters have been altered in order to decrease runtime significantly and run at 30-40 FPS on a RTX 2080 GPU.
