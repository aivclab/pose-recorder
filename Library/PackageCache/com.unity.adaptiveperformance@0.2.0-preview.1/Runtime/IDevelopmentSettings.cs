using System;

namespace UnityEngine.AdaptivePerformance
{
    public interface IDevelopmentSettings
    {
        /// <summary>
        /// Returns true if logging was enabled in StartupSettings.
        /// </summary>
        bool Logging { get; set; }

        /// <summary>
        /// Adjust the frequency in frames at which the application logs frame statistics to the console.
        /// This is only relevant when logging is enabled.
        /// </summary>
        int LoggingFrequencyInFrames { get; set; }
    }
}
