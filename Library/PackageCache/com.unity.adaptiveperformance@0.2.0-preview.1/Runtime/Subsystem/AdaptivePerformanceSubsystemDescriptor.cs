using System;
using UnityEngine;
using UnityEngine.Scripting;
using System.Collections.Generic;

#if UNITY_2018_3_OR_NEWER
    [assembly: AlwaysLinkAssembly]
#endif

namespace UnityEngine.AdaptivePerformance.Provider
{

#if UNITY_2018_3_OR_NEWER
    #if UNITY_2019_2_OR_NEWER
        using AdaptivePerformanceSubsystemDescriptorBase = UnityEngine.SubsystemDescriptor<AdaptivePerformanceSubsystem>;
    #else
        using AdaptivePerformanceSubsystemDescriptorBase = UnityEngine.Experimental.SubsystemDescriptor<AdaptivePerformanceSubsystem>;
        using SubsystemManager = UnityEngine.Experimental.SubsystemManager;
    #endif

    [Preserve]
    internal static class AdaptivePerformanceSubsystemRegistry
    {
        public static AdaptivePerformanceSubsystemDescriptor RegisterDescriptor(AdaptivePerformanceSubsystemDescriptor.Cinfo cinfo)
        {
            var desc = new AdaptivePerformanceSubsystemDescriptor(cinfo);
            if (SubsystemRegistration.CreateDescriptor(desc))
            {
                return desc;
            }
            else
            {
                var registeredDescriptors = GetRegisteredDescriptors();
                foreach (var d in registeredDescriptors)
                {
                    if (d.subsystemImplementationType == cinfo.subsystemImplementationType)
                        return d;
                }
            }
            return null;
        }

        public static List<AdaptivePerformanceSubsystemDescriptor> GetRegisteredDescriptors()
        {
            var perfDescriptors = new List<AdaptivePerformanceSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors<AdaptivePerformanceSubsystemDescriptor>(perfDescriptors);
            return perfDescriptors;
        }
    }

#else

    [Preserve]
    public class AdaptivePerformanceSubsystemDescriptorBase
    {
        public AdaptivePerformanceSubsystem Create()
        {
            return Activator.CreateInstance(subsystemImplementationType) as AdaptivePerformanceSubsystem;
        }

        public string id { get; set; }
        public Type subsystemImplementationType { get; set; }
    }


    [Preserve]
    internal static class AdaptivePerformanceSubsystemRegistry
    {
        private static List<AdaptivePerformanceSubsystemDescriptor> SubsystemDescriptors = new List<AdaptivePerformanceSubsystemDescriptor>();

        public static AdaptivePerformanceSubsystemDescriptor RegisterDescriptor(AdaptivePerformanceSubsystemDescriptor.Cinfo cinfo)
        {
            foreach (var d in SubsystemDescriptors)
            {
                if (d.subsystemImplementationType == cinfo.subsystemImplementationType)
                    return d;
            }

            var desc = new AdaptivePerformanceSubsystemDescriptor(cinfo);
            SubsystemDescriptors.Add(desc);
            return desc;
        }

        public static List<AdaptivePerformanceSubsystemDescriptor> GetRegisteredDescriptors()
        {
            return SubsystemDescriptors;
        }
    }

#endif

    [Preserve]
    public sealed class AdaptivePerformanceSubsystemDescriptor : AdaptivePerformanceSubsystemDescriptorBase
    {
        public struct Cinfo
        {
            public string id { get; set; }
            public Type subsystemImplementationType { get; set; }
        }

        public AdaptivePerformanceSubsystemDescriptor(Cinfo cinfo)
        {
            id = cinfo.id;
            subsystemImplementationType = cinfo.subsystemImplementationType;
        }

        public static AdaptivePerformanceSubsystemDescriptor RegisterDescriptor(Cinfo cinfo)
        {
            return AdaptivePerformanceSubsystemRegistry.RegisterDescriptor(cinfo);
        }
    }
}
