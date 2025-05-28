using System.Collections.Generic;

namespace DeskViz.Core.Services
{
    public interface IHardwareMonitorService
    {
        bool IsInitialized { get; }
        string GetCpuName();
        float GetOverallCpuUsage();
        List<float> GetCpuCoreUsage();
        float GetRamUsagePercentage();
        (float Total, float Available) GetRamInfo(); // Returns total/available in MB
        float GetUsedRam();
        float GetCpuPackageTemperature(); // Added method
        
        /// <summary>
        /// Gets the current CPU clock speed in MHz (average across all cores).
        /// </summary>
        /// <returns>CPU clock speed in MHz</returns>
        float GetCpuClockSpeed();
        
        /// <summary>
        /// Gets the current CPU power usage in watts.
        /// </summary>
        /// <returns>CPU power usage in watts</returns>
        float GetCpuPowerUsage();
        
        /// <summary>
        /// Updates all hardware sensor values. Call this once before retrieving multiple sensor readings.
        /// </summary>
        void Update();
        
        // GPU monitoring methods
        string GetGpuName();
        string GetGpuName(int gpuIndex);
        float GetGpuUsage();
        float GetGpuUsage(int gpuIndex);
        float GetGpuTemperature();
        float GetGpuTemperature(int gpuIndex);
        (float UsedMB, float TotalMB, float UsagePercent) GetGpuMemoryInfo();
        (float UsedMB, float TotalMB, float UsagePercent) GetGpuMemoryInfo(int gpuIndex);
        float GetGpuClockSpeed();
        float GetGpuClockSpeed(int gpuIndex);
        float GetGpuPowerUsage();
        float GetGpuPowerUsage(int gpuIndex);
        
        // GPU enumeration
        int GetGpuCount();
        List<(int Index, string Name, string Type)> GetAvailableGpus();
    }
}
