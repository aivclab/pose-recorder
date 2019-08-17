using System;
using UnityEngine.Scripting;

namespace UnityEngine.AdaptivePerformance.Provider
{
    [Preserve]
    public class TestAdaptivePerformanceSubsystem : AdaptivePerformanceSubsystem, IDevicePerformanceLevelControl
    {
        public Feature ChangeFlags
        {
            get { return updateResult.ChangeFlags; }
            set { updateResult.ChangeFlags = value; }
        }

        public float TemperatureLevel
        {
            get { return updateResult.TemperatureLevel; }
            set
            {
                updateResult.TemperatureLevel = value;
                updateResult.ChangeFlags |= Provider.Feature.TemperatureLevel;
            }
        }

        public float TemperatureTrend
        {
            get { return updateResult.TemperatureTrend; }
            set
            {
                updateResult.TemperatureTrend = value;
                updateResult.ChangeFlags |= Provider.Feature.TemperatureTrend;
            }
        }

        public WarningLevel WarningLevel
        {
            get { return updateResult.WarningLevel; }
            set
            {
                updateResult.WarningLevel = value;
                updateResult.ChangeFlags |= Provider.Feature.WarningLevel;
            }
        }

        public int CpuPerformanceLevel
        {
            get { return updateResult.CpuPerformanceLevel; }
            set
            {
                updateResult.CpuPerformanceLevel = value;
                updateResult.ChangeFlags |= Provider.Feature.CpuPerformanceLevel;
            }
        }

        public int GpuPerformanceLevel
        {
            get { return updateResult.GpuPerformanceLevel; }
            set
            {
                updateResult.GpuPerformanceLevel = value;
                updateResult.ChangeFlags |= Provider.Feature.GpuPerformanceLevel;
            }
        }

        public float NextGpuFrameTime
        {
            get { return updateResult.GpuFrameTime; }
            set
            {
                updateResult.GpuFrameTime = value;
                updateResult.ChangeFlags |= Provider.Feature.GpuFrameTime;
            }
        }
        public float NextCpuFrameTime
        {
            get { return updateResult.CpuFrameTime; }
            set
            {
                updateResult.CpuFrameTime = value;
                updateResult.ChangeFlags |= Provider.Feature.CpuFrameTime;
            }
        }

        public float NextOverallFrameTime
        {
            get { return updateResult.OverallFrameTime; }
            set
            {
                updateResult.OverallFrameTime = value;
                updateResult.ChangeFlags |= Provider.Feature.OverallFrameTime;
            }
        }

        public bool AcceptsPerformanceLevel
        {
            get { return updateResult.PerformanceLevelControlAvailable; }
            set
            {
                updateResult.PerformanceLevelControlAvailable = value;
                updateResult.ChangeFlags |= Provider.Feature.PerformanceLevelControl;
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0);
            }
        }
 
        public static TestAdaptivePerformanceSubsystem Initialize()
        {
            var desc = RegisterDescriptor();
            if (desc == null)
                return null;
            var subsystem = desc.Create();
            return subsystem as TestAdaptivePerformanceSubsystem;
        }

        public int MaxCpuPerformanceLevel { get { return 4; } }
        public int MaxGpuPerformanceLevel { get { return 2; } }

        public Feature InitCapabilities
        {
            set { Capabilities = value; }
        }

        public TestAdaptivePerformanceSubsystem()
        {
            Capabilities = Feature.CpuPerformanceLevel | Feature.GpuPerformanceLevel | Feature.PerformanceLevelControl |
                Feature.TemperatureLevel | Feature.WarningLevel | Feature.TemperatureTrend | Feature.CpuFrameTime | Feature.GpuFrameTime | Feature.OverallFrameTime;
            updateResult.PerformanceLevelControlAvailable = true;
        }

        override public void Start()
        {
            initialized = true;
        }

        override public void Stop()
        {
        }

        override public void Destroy()
        {
        }

        private PerformanceDataRecord updateResult;

        override public PerformanceDataRecord Update()
        {
            updateResult.ChangeFlags &= Capabilities;
            var result = updateResult;
            updateResult.ChangeFlags = Feature.None;
            return result;
        }

        public override IApplicationLifecycle ApplicationLifecycle { get { return null; } }
        public override IDevicePerformanceLevelControl PerformanceLevelControl { get { return this; } }

        public bool SetPerformanceLevel(int cpuLevel, int gpuLevel)
        {
            if (!AcceptsPerformanceLevel)
            {
                CpuPerformanceLevel = AdaptivePerformance.Constants.UnknownPerformanceLevel;
                GpuPerformanceLevel = AdaptivePerformance.Constants.UnknownPerformanceLevel;
                return false;
            }

            return cpuLevel >= 0 && gpuLevel >= 0 && cpuLevel <= MaxCpuPerformanceLevel && gpuLevel <= MaxGpuPerformanceLevel;
        }

        static AdaptivePerformanceSubsystemDescriptor RegisterDescriptor()
        {
            return AdaptivePerformanceSubsystemDescriptor.RegisterDescriptor(new AdaptivePerformanceSubsystemDescriptor.Cinfo
            {
                id = "TestAdaptivePerformanceSubsystem",
                subsystemImplementationType = typeof(TestAdaptivePerformanceSubsystem)
            });
        }
    }
}
