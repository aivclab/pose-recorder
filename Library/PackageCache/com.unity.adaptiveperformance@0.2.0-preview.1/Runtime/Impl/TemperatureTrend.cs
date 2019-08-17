using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.AdaptivePerformance
{
    internal class TemperatureTrend
    {
        private float m_OldTemperatureLevel;
        private float m_OldTemperatureTimestamp;

        private bool m_UseProviderTrend;

        public TemperatureTrend(bool useProviderTrend)
        {
            m_UseProviderTrend = useProviderTrend;
        }

        public void Reset(float temperatureTrendFromProvider, float tempLevel, float timestamp)
        {
            if (m_UseProviderTrend)
            {
                ThermalTrend = temperatureTrendFromProvider;
                return;
            }

            ThermalTrend = 0.0f;
            m_OldTemperatureLevel = tempLevel;
            m_OldTemperatureTimestamp = timestamp;
        }

        public float ThermalTrend { get; private set; }

        public void Update(float temperatureTrendFromProvider, float newTemperatureLevel, bool changed, float newTemperatureTimestamp)
        {
            if (m_UseProviderTrend)
            {
                ThermalTrend = temperatureTrendFromProvider;
                return;
            }
 
            float oldTemperatureLevel = m_OldTemperatureLevel;
            float oldTemperatureTimestamp = m_OldTemperatureTimestamp;
     
            if (changed)
            {
                float temperatureLevelDiff = (newTemperatureLevel - oldTemperatureLevel);

                if (temperatureLevelDiff < 0.0f)
                {
                    if (temperatureLevelDiff < -0.2f)
                    {
                        ThermalTrend = -1.0f;
                    }
                    else
                    {
                        ThermalTrend = -0.5f;
                    }
                }
                else
                {
                    if (temperatureLevelDiff > 0.2f)
                    {
                        ThermalTrend = 1.0f;
                    }
                    else if (ThermalTrend > 0.0f && newTemperatureTimestamp - oldTemperatureTimestamp < 1.0f * 60.0f)
                    {
                        // multiple increases within 1 minute -> rapid increase
                        ThermalTrend = 0.8f;
                    }
                    else
                    {
                        ThermalTrend = 0.5f;
                    }
                }
            }
            else
            {
                if (ThermalTrend != 0.0f)
                {
                    if (newTemperatureLevel - oldTemperatureTimestamp > 5.0f * 60.0f)
                    {
                        // no change within 5 minutes -> constant temperature
                        ThermalTrend = 0.0f;
                    }
                }
            }
        }
    }
}
