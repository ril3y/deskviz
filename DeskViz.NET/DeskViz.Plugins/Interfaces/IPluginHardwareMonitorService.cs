using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeskViz.Plugins.Interfaces
{
    public interface IPluginHardwareMonitorService
    {
        bool IsInitialized { get; }

        void Update();

        /// <summary>
        /// Asynchronously updates all hardware sensor values on a background thread.
        /// Use this from UI code to avoid blocking the UI thread.
        /// </summary>
        Task UpdateAsync();

        // CPU Monitoring
        string GetCpuName();
        float GetOverallCpuUsage();
        List<float> GetCpuCoreUsage();
        float GetCpuPackageTemperature();
        float GetCpuClockSpeed();
        float GetCpuPowerUsage();

        // RAM Monitoring
        float GetRamUsagePercentage();
        (float TotalMB, float AvailableMB) GetRamInfo();
        float GetUsedRamMB();

        // GPU Monitoring
        int GetGpuCount();
        List<(int Index, string Name, string Type)> GetAvailableGpus();
        string GetGpuName(int gpuIndex = 0);
        float GetGpuUsage(int gpuIndex = 0);
        float GetGpuTemperature(int gpuIndex = 0);
        (float UsedMB, float TotalMB, float UsagePercent) GetGpuMemoryInfo(int gpuIndex = 0);
        float GetGpuClockSpeed(int gpuIndex = 0);
        float GetGpuPowerUsage(int gpuIndex = 0);

        // Drive Monitoring
        List<DriveInfo> GetDriveInfo();
        float GetDriveTemperature(string driveName);
    }

    public record DriveInfo(
        string Name,
        string Label,
        long TotalBytes,
        long UsedBytes,
        float UsagePercent,
        float TemperatureCelsius
    );
}