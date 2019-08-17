**_Adaptive Performance Guide_**

# About Adaptive Performance

Use *Adaptive Performance* to get feedback about the thermal state of your mobile device and react appropriately. As an example, use the API provided to create applications that react to the thermal trend and events of the device. This ensures constant frame rates over a longer period of time while avoiding thermal throttling, even before throttling happens.

This version of *Adaptive Performance* supports the following subsystems:

* [Samsung (Android)](https://docs.unity3d.com/Packages/com.unity.adaptiveperformance.samsung.android@latest/index.html)

At least one subsystem is required to use Adaptive Performance. 

# Installing Adaptive Performance

This package is automatically installed as dependency if you install a Adaptive Performance subsystem. Please see the list of subsystems in the *Adaptive Performance* about section and see install instruction in the subsystem documentation. 

# Using Adaptive Performance

Once the Adaptive Performance is installed in your Unity project a GameObject that implements `IAdaptivePerformance` is automatically created at runtime.
You can access the instance via `UnityEngine.AdaptivePerformance.Holder.Instance`.

You can check if your device is supported with the `Instance.Active` property.
To get detailed information during runtime, enable debug logging with the `UnityEngine.AdaptivePerformance.StartupSettings.Logging` flag:

```
static class AdaptivePerformanceConfig
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Setup()
	{
		UnityEngine.AdaptivePerformance.StartupSettings.Logging = true;
    }
}
```

## Performance Status

Adaptive Performance tracks serveral performance metrics and updates them every frame.
These metrics can be accesses through the property `Instance.PerformanceStatus`.

### Frame timing

Adaptive Performance always tracks the average GPU, CPU and overall frame times and updates them every frame.
The latest timing data can be accessed through the property `PerformanceStatus.FrameTiming`.

The overall frame time is the delta between frames. It can be used to calculate the current framerate of the application.
The CPU time only includes the time the CPU is actually executing Unity's main thread and the render thread.
It does not include the time when Unity might be blocked by the operating system or when Unity needs to wait for the GPU to catch up with rendering.
The GPU time is the time the GPU is actively processing data to render a frame. It does not include the time when the GPU has to wait for Unity to provide data to render.

### Performance bottleneck

Adaptive Performance uses the currently configured target framerate (see `UnityEngine.Application.targetFrameRate` and `QualitySettings`) and the information provided by `FrameTiming` to calculate what is limiting the frame rate of the application.
If the application is not hitting the desired target framerate then it may be bound by either CPU or GPU processing.
To get notified whenever the current performance bottleneck of the application changes you can subscribe to the event `PerformanceStatus.PerformanceBottleneckChangeEvent`.

You can use the information about the current performance bottleneck to make targeted adjustments to the game content at runtime.
For example in a GPU bound application lowering the rendering resolution often improves the framerate significantly, while the same change may not make a big different in a purely CPU bound application.

## Device thermal state feedback

The Adaptive Performance API gives you access to the current thermal warning level of the device (`Instance.ThermalStatus.ThermalMetrics.WarningLevel`) and a more detailed temperature level (`Instance.ThermalStatus.ThermalMetrics.TemperatureLevel`).
The application can use those values as feedback to make modifications and avoid getting throttled by the operating system.

The following example shows the implementation of Unity component that adjusts the global LOD bias based on Adaptive Performance feedback:

```
using UnityEngine;  
using UnityEngine.AdaptivePerformance;  
  
public class AdaptiveLOD : MonoBehaviour  
{  
    private IAdaptivePerformance ap = null;  
  
    void Start() {  
        ap = Holder.Instance;  
  		if (!ap.Active)  
            return;  
  
        QualitySettings.lodBias = 1.0f;  
        ap.ThermalStatus.ThermalEvent += OnThermalEvent;  
    }  
  
    void OnThermalEvent(ThermalMetrics ev) {  
        switch (ev.WarningLevel) {  
            case WarningLevel.NoWarning:  
                QualitySettings.lodBias = 1;  
                break;  
            case WarningLevel.ThrottlingImminent:
				if (ev.temperatureLevel > 0.8f)
					QualitySettings.lodBias = 0.75f;
				else
					QualitySettings.lodBias = 1.0f;
                break;  
            case WarningLevel.Throttling:  
                QualitySettings.lodBias = 0.5f;  
                break;  
        }  
    }  
} 
```

## Configuring CPU and GPU performance levels

The CPU and GPU of a mobile device make up for a very large part of the power utilization of a mobile device, especially when running a game.
Typically, the operating system decides which clock speeds are used for the CPU and GPU.

CPU cores and GPUs are less efficient when run at their maximum clock speed. Running at high clock speeds overheats the mobile device easily and the operating system throttles the frequency of the CPU and GPU to cool down the device.

By default Adaptive Performance automatically configures CPU and GPU performance levels bases on the current performance bottleneck.

Alternatively you can switch to manual mode by setting `Instance.DevicePerformanceControl.AutomaticPerformanceControl` to `true`.
You can check the current mode with the help of the property `Instance.DevicePerformanceControl.PerformanceControlMode`.
In `Manual` mode you can use the properties `Instance.DevicePerformanceControl.CpuLevel` and `Instance.DevicePerformanceControl.GpuLevel` to optimize CPU and GPU performance.

The application can configure those properties based on the thermal feedback and frame time data provided by the Adaptive Performance API and the application's special knowledge about the current performance requirements:
- did the application reach the target frame rate in the previous frames?
- is the application in an in-game scene, a loading-screen or in a menu?
- are device temperatures rising?
- is the device close to thermal throttling?
- is the device GPU or CPU bound?

Please note that changing GPU and GPU levels only has an effect as long as the device is not in thermal throttling state (`Instance.WarningLevel` equals `PerformanceWarningLevel.Throttling`).
In some situations the device may take control over CPU and GPU levels. In this case the value of `Instance.DevicePerformanceControl.PerformanceControlMode`
is `PerformanceControlMode.System`.

For following example show how to configure performance levels based on the current type of scene:

```
public void EnterMenu()
{   
    if (!ap.Active)  
        return;   
  
    var ctrl = ap.DevicePerformanceControl;
  
    // Set low CPU and GPU level in menu  
    ctrl.CpuLevel = 0;  
    ctrl.GpuLevel = 0;
    // Set low target FPS  
    Application.targetFrameRate = 15;  
}  
  
public void ExitMenu()
{   
    var ctrl = ap.DevicePerformanceControl;
    // Set higher CPU and GPU level when going back into the game  
    ctrl.cpuLevel = ctrl.MaxCpuPerformanceLevel;  
    ctrl.gpuLevel = ctrl.MaxGpuPerformanceLevel;  
} 
```

# Technical details
## Requirements

This version of Adaptive Performance is compatible with the following versions of the Unity Editor:

* 2018.3 and later (2019.1 and later recommended)

At least one Adaptive Performance subsystem is required to use Adaptive Performance. Please read more about the Adaptive Performance subsystem support in the About section.

## Document revision history
This section includes the revision history of the document. The revision history tracks when a document is created, edited, and updated. If you create or update a document, you must add a new row describing the revision.  The Documentation Team also uses this table to track when a document is edited and its editing level. An example is provided:
 
|Date|Reason|
|---|---|
|Jun 17, 2019|Work in progress for 1.0 release.|
|Mar 14, 2019|Document created. Work in progress for initial release.|
