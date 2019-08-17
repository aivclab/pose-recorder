using System;

namespace UnityEngine.AdaptivePerformance
{
    public delegate void ThermalEventHandler(ThermalMetrics thermalMetrics);

    public struct ThermalMetrics
    {
        /// <summary>
        /// Current thermal warning level.
        /// </summary>
        public WarningLevel WarningLevel { get; set; }

        /// <summary>
        /// Current normalized temperature level in the range of [0, 1]. 
        /// A value of 0 means standard operation temperature and the device is not in a throttling state.
        /// A value of 1 means that the maximum temperature of the device is reached and the device is going into or is already in throttling state.
        /// </summary>
        /// <value>Value in the range [0, 1].</value>
        public float TemperatureLevel { get; set; }

        /// <summary>
        /// Current normalized temperature trend in the range of [-1, 1].
        /// A value of 1 describes a rapid increase in temperature.
        /// A value of 0 describes a constant temperature.
        /// A value of -1 describes a rapid decrease in temperature.
        /// Please note that it takes at least 10s until the temperature trend may reflect any changes.
        /// </summary>
        /// <value>Value in the range [-1, 1].</value>
        public float TemperatureTrend { get; set; }
    }

    public interface IThermalStatus
    {
        /// <summary>
        /// The latest thermal metrics available.
        /// </summary>
        /// <value>The latest thermal metrics</value>
        ThermalMetrics ThermalMetrics { get; }

        /// <summary>
        /// Subscribe to thermal events which Adaptive Performance sends when the thermal state of the device changes.
        /// </summary>
        event ThermalEventHandler ThermalEvent;
    }

    public enum WarningLevel
    {
        /// <summary>
        /// No warning is the normal warning level during standard thermal state. 
        /// </summary>
        NoWarning,

        /// <summary>
        /// If throttling is imminent the application should perform adjustments to avoid thermal throttling.
        /// </summary>
        ThrottlingImminent,

        /// <summary>
        /// If the application is in the throttling state it should make adjustments to go back to normal temperature levels.
        /// </summary>
        Throttling,
    }
}
