using System;

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// Constants used by Adaptive Performance.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The minimum temperature level.
        /// See <see cref="ThermalMetrics.TemperatureLevel"/>.
        /// </summary>
        /// <value>0.0</value>
        public const float MinTemperatureLevel = 0.0f;

        /// <summary>
        /// The maximum temperature level.
        /// See <see cref="ThermalMetrics.TemperatureLevel"/>.
        /// </summary>
        /// <value>1.0</value>
        public const float MaxTemperatureLevel = 1.0f;

        /// <summary>
        /// The minimum CPU level.
        /// Used by <see cref="IDevicePerformanceControl.CpuLevel"/> and <see cref="PerformanceMetrics.CurrentCpuLevel"/>.
        /// </summary>
        /// <value>0</value>
        public const int MinCpuPerformanceLevel = 0;

        /// <summary>
        /// The minimum GPU level.
        /// Used by <see cref="IDevicePerformanceControl.GpuLevel"/> and <see cref="PerformanceMetrics.CurrentGpuLevel"/>.
        /// </summary>
        /// <value>0</value>
        public const int MinGpuPerformanceLevel = 0;

        /// <summary>
        /// UnknownPerformanceLevel is the value of <see cref="IDevicePerformanceControl.GpuLevel"/>, <see cref="PerformanceMetrics.CurrentGpuLevel"/>,
        /// <see cref="IDevicePerformanceControl.CpuLevel"/>, and <see cref="PerformanceMetrics.CurrentCpuLevel"/> if the current performance level is unknown.
        /// This may happen when AdaptivePerformance is not supported or when the device is in throttling state (see <see cref="WarningLevel.Throttling"/>). 
        /// </summary>
        /// <value>-1</value>
        public const int UnknownPerformanceLevel = -1;

        /// <summary>
        /// The number of past frames that are considered to calculate average frame times
        /// </summary>
        /// <value>100</value>
        public const int DefaultAverageFrameCount = 100;
    }

    /// <summary>
    /// The main interface to access Adaptive Performance.
    /// None of the properties in this interface change after startup.
    /// This means the references returned by the properties may be cached by the user.
    /// </summary>
    public interface IAdaptivePerformance
    {
        /// <summary>
        /// Returns `true` if Adaptive Performance was initialized successfully otherwise `false`.
        /// This means that Adaptive Performance is enabled in StartupSettings and the application runs on a device that supports Adaptive Performance.
        /// </summary>
        /// <value>`true` when Adaptive Performance is available, `false` otherwise</value>
        bool Active { get; }

        /// <summary>
        /// Access thermal status information of the device.
        /// </summary>
        /// <value>Interface to access thermal status information of the device</value>
        IThermalStatus ThermalStatus { get; }

        /// <summary>
        /// Access performance status information of the device and your application.
        /// </summary>
        /// <value>Interface to access performance status information of the device and your application</value>
        IPerformanceStatus PerformanceStatus { get; }

        /// <summary>
        /// Control CPU and GPU performance of the device.
        /// </summary>
        /// <value>Interface to control CPU and GPU performance levels of the device</value>
        IDevicePerformanceControl DevicePerformanceControl { get; }

        /// <summary>
        /// Access to development (logging) settings.
        /// </summary>
        /// <value>Interface to control CPU and GPU performance levels of the device</value>
        IDevelopmentSettings DevelopmentSettings { get; }
    }

    /// <summary>
    /// Global access to the default AdaptivePerformance interface
    /// </summary>
    public static class Holder
    {
        static public IAdaptivePerformance Instance { get; internal set; }
    }

    /// <summary>
    /// Changes to the startup settings are only respected when those are made before Adaptive Performance starts, for instance, from a method with the attribute [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]).
    /// </summary>
    public static class StartupSettings
    {
        /// <summary>
        /// Default Settings are applied. 
        /// </summary>
        static StartupSettings()
        {
            Logging = false;
            StatsLoggingFrequencyInFrames = 50;
            Enable = true;
            PreferredSubsystem = null;
            AutomaticPerformanceControl = true;
        }

        /// <summary>
        ///  Control debug logging.
        ///  This setting only affects development builds. In release builds all logging is disabled.
        ///  This setting can also be controlled after startup using <see cref="IDevelopmentSettings.Logging"/>.
        ///  Logging is disabled by default.
        /// </summary>
        /// <value>`true` to enable debug logging, `false` to disable it (default: `false`)</value>
        static public bool Logging { get; set; }

        /// <summary>
        /// Adjust the frequency in frames at which the application logs frame statistics to the console.
        /// This is only relevant when logging is enabled. See <see cref="Logging"/>.
        /// This setting can also be controlled after startup using <see cref="IDevelopmentSettings.StatsLoggingFrequencyInFrames"/>.
        /// </summary>
        /// <value>Logging frequency in frames (default: 50)</value>
        static public int StatsLoggingFrequencyInFrames { get; set; }

        /// <summary>
        ///  Enable Adaptive Performance.
        /// </summary>
        /// <value>`true` to enable debug Adaptive Performance, `false` to disable it (default: `true`)</value>
        static public bool Enable { get; set; }

        /// <summary>
        /// The Initial value of <see cref="IDevicePerformanceControl.AutomaticPerformanceControl"/>.
        /// </summary>
        static public bool AutomaticPerformanceControl { get; set; }

        /// <summary>
        /// You can use this property to override the automatic selection of an Adaptive Performance subsystem.
        /// You should use this primarily for testing.
        /// </summary>
        static public Provider.AdaptivePerformanceSubsystem PreferredSubsystem { get; set; }
    }
}
