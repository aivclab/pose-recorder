#if UNITY_2019_2_OR_NEWER
using UnityEngine;
#else
using UnityEngine.Experimental;
#endif

namespace UnityEngine.AdaptivePerformance
{
    internal static class EnumExt
    {
        public static bool HasFlag(this Provider.Feature value, Provider.Feature flags)
        {
            uint a = System.Convert.ToUInt32(value);
            uint b = System.Convert.ToUInt32(flags);
            return (a & b) == b;
        }
    }

    internal class AdaptivePerformanceManager
        : MonoBehaviour
        , IAdaptivePerformance
        , IThermalStatus, IPerformanceStatus, IDevicePerformanceControl, IDevelopmentSettings
    {
        public event ThermalEventHandler ThermalEvent;
        public event PerformanceBottleneckChangeHandler PerformanceBottleneckChangeEvent;
        public event PerformanceLevelChangeHandler PerformanceLevelChangeEvent;

        private Provider.AdaptivePerformanceSubsystem m_Subsystem = null;

        private bool m_JustResumed = false;

        private int m_RequestedCpuLevel = Constants.UnknownPerformanceLevel;
        private int m_RequestedGpuLevel = Constants.UnknownPerformanceLevel;
        private bool m_NewUserPerformanceLevelRequest = false;

        private ThermalMetrics m_ThermalMetrics = new ThermalMetrics
        {
            WarningLevel = WarningLevel.NoWarning,
            TemperatureLevel = -1.0f,
            TemperatureTrend = 0.0f,
        };

        public ThermalMetrics ThermalMetrics { get { return m_ThermalMetrics; } }

        private PerformanceMetrics m_PerformanceMetrics = new PerformanceMetrics
        {
            CurrentCpuLevel = Constants.UnknownPerformanceLevel,
            CurrentGpuLevel = Constants.UnknownPerformanceLevel,
            PerformanceBottleneck = PerformanceBottleneck.Unknown
        };

        public PerformanceMetrics PerformanceMetrics { get { return m_PerformanceMetrics; } }

        private FrameTiming m_FrameTiming = new FrameTiming
        {
            CurrentFrameTime = -1.0f,
            AverageFrameTime = -1.0f,
            CurrentGpuFrameTime = -1.0f,
            AverageGpuFrameTime = -1.0f,
            CurrentCpuFrameTime = -1.0f,
            AverageCpuFrameTime = -1.0f
        };

        public FrameTiming FrameTiming { get { return m_FrameTiming; } }

        public bool Logging
        {
            get { return APLog.enabled; }
            set { APLog.enabled = value; }
        }

        public int LoggingFrequencyInFrames { get; set; }
   
        public bool Active { get { return m_Subsystem != null; } } 

        public int MaxCpuPerformanceLevel { get { return m_DevicePerfControl != null ? m_DevicePerfControl.MaxCpuPerformanceLevel : Constants.UnknownPerformanceLevel; } }
  
        public int MaxGpuPerformanceLevel { get { return m_DevicePerfControl != null ? m_DevicePerfControl.MaxGpuPerformanceLevel : Constants.UnknownPerformanceLevel; } }

        public bool AutomaticPerformanceControl { get; set; }

        public PerformanceControlMode PerformanceControlMode { get { return m_DevicePerfControl != null ? m_DevicePerfControl.PerformanceControlMode : PerformanceControlMode.System; } }

        public int CpuLevel
        {
            get { return m_RequestedCpuLevel; }
            set
            {
                m_RequestedCpuLevel = value;
                m_NewUserPerformanceLevelRequest = true;
            }
        }

        public int GpuLevel
        {
            get { return m_RequestedGpuLevel; }
            set
            {
                m_RequestedGpuLevel = value;
                m_NewUserPerformanceLevelRequest = true;
            }
        }

        public IDevelopmentSettings DevelopmentSettings { get { return this; } }
        public IThermalStatus ThermalStatus { get { return this; } }
        public IPerformanceStatus PerformanceStatus { get { return this; } }
        public IDevicePerformanceControl DevicePerformanceControl { get { return this; } }

        DevicePerformanceControlImpl m_DevicePerfControl;
        AutoPerformanceLevelController m_AutoPerformanceLevelController;
        CpuTimeProvider m_CpuFrameTimeProvider;
        Provider.IApplicationLifecycle m_AppLifecycle;
        TemperatureTrend m_TemperatureTrend;

        AdaptivePerformanceManager()
        {
            AutomaticPerformanceControl = StartupSettings.AutomaticPerformanceControl;
        }

        private bool InitializeSubsystem(Provider.AdaptivePerformanceSubsystem subsystem)
        {
            if (subsystem == null)
                return false;

            subsystem.Start();

            if (subsystem.initialized)
            {
                m_Subsystem = subsystem;

                APLog.Debug("version={0}", m_Subsystem.Version);

                return true;
            }
            else
            {
                subsystem.Destroy();

                return false;
            }
        }

        public void Start()
        {
            APLog.enabled = StartupSettings.Logging;
            LoggingFrequencyInFrames = StartupSettings.StatsLoggingFrequencyInFrames;
            if (!StartupSettings.Enable)
                return;

            if (InitializeSubsystem(StartupSettings.PreferredSubsystem))
            {
                m_Subsystem = StartupSettings.PreferredSubsystem;
            }
            else
            {
                System.Collections.Generic.List<Provider.AdaptivePerformanceSubsystemDescriptor> perfDescriptors = Provider.AdaptivePerformanceSubsystemRegistry.GetRegisteredDescriptors();
                if(perfDescriptors.Count == 0)
                {
                    APLog.Debug("No Subsystems found and initialized.");
                } 
                foreach (var perfDesc in perfDescriptors)
                {
                    var subsystem = perfDesc.Create();
                    if (InitializeSubsystem(subsystem))
                    {
                        m_Subsystem = subsystem;
                        break;
                    }
                }
            }

            if (m_Subsystem != null)
            {

                m_DevicePerfControl = new DevicePerformanceControlImpl(m_Subsystem.PerformanceLevelControl);
                m_AutoPerformanceLevelController = new AutoPerformanceLevelController(m_DevicePerfControl, PerformanceStatus, ThermalStatus);
      
                m_AppLifecycle = m_Subsystem.ApplicationLifecycle;

                if (!m_Subsystem.Capabilities.HasFlag(Provider.Feature.CpuFrameTime))
                {
                    m_CpuFrameTimeProvider = new CpuTimeProvider();
                    StartCoroutine(InvokeEndOfFrame());
                }

                m_TemperatureTrend = new TemperatureTrend(m_Subsystem.Capabilities.HasFlag(Provider.Feature.TemperatureTrend));

                if (m_RequestedCpuLevel == Constants.UnknownPerformanceLevel)
                    m_RequestedCpuLevel = m_DevicePerfControl.MaxCpuPerformanceLevel;

                if (m_RequestedGpuLevel == Constants.UnknownPerformanceLevel)
                    m_RequestedGpuLevel = m_DevicePerfControl.MaxGpuPerformanceLevel;

                if (m_Subsystem.PerformanceLevelControl == null)
                    m_DevicePerfControl.PerformanceControlMode = PerformanceControlMode.System;
                else if (AutomaticPerformanceControl)
                    m_DevicePerfControl.PerformanceControlMode = PerformanceControlMode.Automatic;
                else
                    m_DevicePerfControl.PerformanceControlMode = PerformanceControlMode.Manual;

                ThermalEvent += (ThermalMetrics thermalMetrics) => LogThermalEvent(thermalMetrics);
                PerformanceBottleneckChangeEvent += (PerformanceBottleneckChangeEventArgs ev) => LogBottleneckEvent(ev);
                PerformanceLevelChangeEvent += (PerformanceLevelChangeEventArgs ev) => LogPerformanceLevelEvent(ev);

                UpdateSubsystem();
            }
        }

        private void LogThermalEvent(ThermalMetrics ev)
        {
            APLog.Debug("[thermal event] temperature level: {0}, warning level: {1}, thermal trend: {2}", ev.TemperatureLevel, ev.WarningLevel, ev.TemperatureTrend);
        }

        private void LogBottleneckEvent(PerformanceBottleneckChangeEventArgs ev)
        {
            APLog.Debug("[perf event] bottleneck: {0}", ev.PerformanceBottleneck);
        }

        private static string ToStringWithSign(int x)
        {
            return x.ToString("+#;-#;0");
        }

        private void LogPerformanceLevelEvent(PerformanceLevelChangeEventArgs ev)
        {
            APLog.Debug("[perf level change] cpu: {0}({1}) gpu: {2}({3})", ev.CpuLevel, ToStringWithSign(ev.CpuLevelDelta), ev.GpuLevel, ToStringWithSign(ev.GpuLevelDelta));
        }

        private void AddNonNegativeValue(RunningAverage runningAverage, float value)
        {
            if (value >= 0.0f && value < 1.0f) // don't add frames that took longer than 1s
                runningAverage.AddValue(value);
        }

        private WaitForEndOfFrame m_WaitForEndOfFrame = new WaitForEndOfFrame();

        private System.Collections.IEnumerator InvokeEndOfFrame()
        {
            while (true)
            {
                yield return m_WaitForEndOfFrame;
                if (m_CpuFrameTimeProvider != null)
                    m_CpuFrameTimeProvider.EndOfFrame();
            }
        }

        public void LateUpdate()
        {
            // m_RenderThreadCpuTime uses native plugin event to get CPU time of render thread.
            // We don't want to do this at end of frame because it might introduce an unnecessary sync when GraphicsJobs are used.
            // Alternative would be to use Vulkan native plugin API to configure the event.
            if (m_CpuFrameTimeProvider != null)
                m_CpuFrameTimeProvider.LateUpdate();
        }

        public void Update()
        {
            if (m_Subsystem == null)
                return;      

            UpdateSubsystem();

            if (APLog.enabled && LoggingFrequencyInFrames > 0)
            {
                m_FrameCount++;
                if (m_FrameCount % LoggingFrequencyInFrames == 0)
                {
                    APLog.Debug(m_Subsystem.Stats);
                    APLog.Debug("Performance level CPU={0}/{1} GPU={2}/{3} warn={4}({5}) mode={6}", m_PerformanceMetrics.CurrentCpuLevel, MaxCpuPerformanceLevel,
                        m_PerformanceMetrics.CurrentGpuLevel, MaxGpuPerformanceLevel, m_ThermalMetrics.WarningLevel, (int)m_ThermalMetrics.WarningLevel, m_DevicePerfControl.PerformanceControlMode);
                    APLog.Debug("Average GPU frametime = {0} ms (Current = {1} ms)", m_FrameTiming.AverageGpuFrameTime * 1000.0f, m_FrameTiming.CurrentGpuFrameTime * 1000.0f);
                    APLog.Debug("Average CPU frametime = {0} ms (Current = {1} ms)", m_FrameTiming.AverageCpuFrameTime * 1000.0f, m_FrameTiming.CurrentCpuFrameTime * 1000.0f);
                    APLog.Debug("Average frametime = {0} ms (Current = {1} ms)", m_FrameTiming.AverageFrameTime * 1000.0f, m_FrameTiming.CurrentFrameTime * 1000.0f);
                    APLog.Debug("Bottleneck {0}", m_PerformanceMetrics.PerformanceBottleneck);
                    APLog.Debug("FPS = {0}", 1.0f / m_FrameTiming.AverageFrameTime);
                }
            }
        }

        private void UpdateSubsystem()
        {
            Provider.PerformanceDataRecord updateResult = m_Subsystem.Update();

            m_PerformanceMetrics.CurrentCpuLevel = updateResult.CpuPerformanceLevel;
            m_PerformanceMetrics.CurrentGpuLevel = updateResult.GpuPerformanceLevel;
            m_ThermalMetrics.WarningLevel = updateResult.WarningLevel;
            m_ThermalMetrics.TemperatureLevel = updateResult.TemperatureLevel;

            if (!m_JustResumed)
            {
                // Update overall frame time
                m_OverallFrameTime.AddValue(m_Subsystem.Capabilities.HasFlag(Provider.Feature.OverallFrameTime) ? updateResult.OverallFrameTime : Time.unscaledDeltaTime);
                AddNonNegativeValue(m_GpuFrameTime, updateResult.GpuFrameTime);
                AddNonNegativeValue(m_CpuFrameTime, m_CpuFrameTimeProvider != null ? m_CpuFrameTimeProvider.CpuFrameTime : updateResult.CpuFrameTime);
                m_TemperatureTrend.Update(updateResult.TemperatureTrend, updateResult.TemperatureLevel, updateResult.ChangeFlags.HasFlag(Provider.Feature.TemperatureLevel), Time.time);
               
            }
            else
            {
                m_TemperatureTrend.Reset(updateResult.TemperatureTrend, updateResult.TemperatureLevel, Time.time);
                m_JustResumed = false;
            }

            m_ThermalMetrics.TemperatureTrend = m_TemperatureTrend.ThermalTrend;

            // Update frame timing info and calculate performance bottleneck
            m_FrameTiming.AverageFrameTime = m_OverallFrameTime.GetAverage();
            m_FrameTiming.CurrentFrameTime = m_OverallFrameTime.GetMostRecentValue();
            m_FrameTiming.AverageGpuFrameTime = m_GpuFrameTime.GetAverage();
            m_FrameTiming.CurrentGpuFrameTime = m_GpuFrameTime.GetMostRecentValue();
            m_FrameTiming.AverageCpuFrameTime = m_CpuFrameTime.GetAverage();
            m_FrameTiming.CurrentCpuFrameTime = m_CpuFrameTime.GetMostRecentValue();

            float targerFrameRate = EffectiveTargetFrameRate();
            float targetFrameTime = -1.0f;
            if (targerFrameRate > 0)
                targetFrameTime = 1.0f / targerFrameRate;

            if (m_OverallFrameTime.GetNumValues() == m_OverallFrameTime.GetSampleWindowSize() &&
                m_GpuFrameTime.GetNumValues() == m_GpuFrameTime.GetSampleWindowSize())
            {
                PerformanceBottleneck bottleneck = BottleneckUtil.DetermineBottleneck(m_PerformanceMetrics.PerformanceBottleneck, m_FrameTiming.AverageCpuFrameTime,
                    m_FrameTiming.AverageGpuFrameTime, m_FrameTiming.AverageFrameTime, targetFrameTime);

                if (bottleneck != m_PerformanceMetrics.PerformanceBottleneck)
                {
                    m_PerformanceMetrics.PerformanceBottleneck = bottleneck;
                    var args = new PerformanceBottleneckChangeEventArgs();
                    args.PerformanceBottleneck = bottleneck;

                    if (PerformanceBottleneckChangeEvent != null)
                        PerformanceBottleneckChangeEvent.Invoke(args);
                }
            }


            if (updateResult.ChangeFlags.HasFlag(Provider.Feature.WarningLevel) ||
                updateResult.ChangeFlags.HasFlag(Provider.Feature.TemperatureLevel) ||
                updateResult.ChangeFlags.HasFlag(Provider.Feature.TemperatureTrend))
            {
                if (ThermalEvent != null)
                {
                    ThermalEvent.Invoke(m_ThermalMetrics);
                }
            }

            // Update PerformanceControlMode
            if (updateResult.ChangeFlags.HasFlag(Provider.Feature.PerformanceLevelControl))
            {
                if (updateResult.PerformanceLevelControlAvailable)
                {
                    if (AutomaticPerformanceControl)
                        m_DevicePerfControl.PerformanceControlMode = PerformanceControlMode.Automatic;
                    else
                        m_DevicePerfControl.PerformanceControlMode = PerformanceControlMode.Manual;
                }
                else
                {
                    m_DevicePerfControl.PerformanceControlMode = PerformanceControlMode.System;
                }
            }

            // Apply performance levels according to PerformanceControlMode
            m_AutoPerformanceLevelController.TargetFrameTime = targetFrameTime;
            m_AutoPerformanceLevelController.Enabled = (m_DevicePerfControl.PerformanceControlMode == PerformanceControlMode.Automatic);

            PerformanceLevelChangeEventArgs levelChangeEventArgs = new PerformanceLevelChangeEventArgs();
            if (m_DevicePerfControl.PerformanceControlMode != PerformanceControlMode.System)
            {
                if (m_AutoPerformanceLevelController.Enabled)
                {
                    if (m_NewUserPerformanceLevelRequest)
                    {
                        m_AutoPerformanceLevelController.Override(m_RequestedCpuLevel, m_RequestedGpuLevel);
                        levelChangeEventArgs.ManualOverride = true;
                    }

                    m_AutoPerformanceLevelController.Update();
                }
                else
                {
                    m_DevicePerfControl.CpuLevel = m_RequestedCpuLevel;
                    m_DevicePerfControl.GpuLevel = m_RequestedGpuLevel;
                }
            }

            if (m_DevicePerfControl.Update(out levelChangeEventArgs) && PerformanceLevelChangeEvent != null)
                PerformanceLevelChangeEvent.Invoke(levelChangeEventArgs);

            m_PerformanceMetrics.CurrentCpuLevel = m_DevicePerfControl.CurrentCpuLevel;
            m_PerformanceMetrics.CurrentGpuLevel = m_DevicePerfControl.CurrentGpuLevel;

            m_NewUserPerformanceLevelRequest = false;
        }

        private static float AndroidDefaultFrameRate()
        {
            // see https://docs.unity3d.com/ScriptReference/Application-targetFrameRate.html 
            return 30.0f;
        }

        public static float EffectiveTargetFrameRate()
        {
            int vsyncCount = QualitySettings.vSyncCount;
            if (vsyncCount == 0)
            {
                var targetFrameRate = Application.targetFrameRate;
                if (targetFrameRate >= 0)
                    return targetFrameRate;
#if UNITY_EDITOR
                switch(UnityEditor.EditorUserBuildSettings.activeBuildTarget)
                {
                    default:
                        return -1.0f;
                    case UnityEditor.BuildTarget.Android:
                        return AndroidDefaultFrameRate();
                }
#elif UNITY_ANDROID
                return AndroidDefaultFrameRate();
#else
                return -1.0f;
#endif
            }

            float displayRefreshRate = 60.0f;

#if !UNITY_EDITOR
            int refreshRate = Screen.currentResolution.refreshRate;
            if (refreshRate > 0)
                displayRefreshRate = (float)refreshRate;
#endif

            return displayRefreshRate / vsyncCount;
        }

        public void OnDestroy()
        {
            if (m_Subsystem != null)
                m_Subsystem.Destroy();
        }

        public void OnApplicationPause(bool pause)
        {
            if (m_Subsystem != null)
            {
                if (pause)
                {
                    if (m_AppLifecycle != null)
                        m_AppLifecycle.ApplicationPause();
                    m_OverallFrameTime.Reset();
                    m_GpuFrameTime.Reset();
                    m_CpuFrameTime.Reset();
                    if (m_CpuFrameTimeProvider != null)
                        m_CpuFrameTimeProvider.Reset();
                }
                else
                {
                    m_ThermalMetrics.WarningLevel = WarningLevel.NoWarning;
                    if (m_AppLifecycle != null)
                        m_AppLifecycle.ApplicationResume();
                    m_JustResumed = true;
                }
            }
        }

        private int m_FrameCount = 0;

        private RunningAverage m_OverallFrameTime = new RunningAverage();   // In seconds
        private RunningAverage m_GpuFrameTime = new RunningAverage();   // In seconds
        private RunningAverage m_CpuFrameTime = new RunningAverage();   // In seconds
    }
}
