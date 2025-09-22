using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using LibreHardwareMonitor.Hardware;
using System.Diagnostics;

namespace DeskViz.Core.Services
{
    /// <summary>
    /// Service for retrieving hardware information using LibreHardwareMonitor.
    /// </summary>
    public class LibreHardwareMonitorService : IHardwareMonitorService, IDisposable
    {
        private readonly Computer _computer;
        private bool _isInitialized = false;
        private readonly object _initLock = new object();

        /// <summary>
        /// Gets a value indicating whether the service was initialized successfully.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        public LibreHardwareMonitorService()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsControllerEnabled = true,
                IsNetworkEnabled = false,
                IsStorageEnabled = true,
                IsPsuEnabled = false
            };

            try
            {
                _computer.Open();
                _isInitialized = true;
                // Debug.WriteLine("LibreHardwareMonitorService initialized successfully.");
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                Debug.WriteLine($"*** Failed to initialize LibreHardwareMonitor: {ex.Message}");
                Debug.WriteLine($"*** Service methods will return default values.");
                // Consider showing a user-friendly message here if critical
                // System.Windows.MessageBox.Show($"Hardware monitoring failed to start: {ex.Message}", "Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        // Helper method to find the CPU hardware (can be cached later if needed)
        private IHardware? GetCpuHardware()
        {
            if (!_isInitialized) return null;
            return _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        }

        /// <summary>
        /// Updates the hardware monitoring data
        /// </summary>
        public void Update()
        {
            if (!_isInitialized) return;

            lock (_initLock)
            {
                try
                {
                    // This forces sensors to actually read fresh data from hardware
                    _computer.Hardware.ToList().ForEach(hardware => 
                    {
                        hardware.Update();
                        // Ensure subhardware is also updated
                        foreach (var subHardware in hardware.SubHardware)
                        {
                            subHardware.Update();
                        }
                    });
                }
                catch (Exception ex)
                {
                    // Log the error but continue
                    System.Diagnostics.Debug.WriteLine($"Error updating hardware: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gets the CPU name.
        /// </summary>
        public string GetCpuName()
        {
            if (!_isInitialized) return "Hardware Init Failed";
            // Note: LibreHardwareMonitor might need an update cycle before Name is fully populated
            // Calling Accept here might be necessary in some cases, but usually Name is static.
            // _computer.Accept(new UpdateVisitor()); 
            var cpu = GetCpuHardware();
            return cpu?.Name ?? "Unknown CPU";
        }

        /// <summary>
        /// Gets the total CPU usage as a percentage.
        /// </summary>
        public float GetOverallCpuUsage()
        {
            if (!_isInitialized) return 0f;
            var cpu = GetCpuHardware();
            if (cpu == null) return 0f;

            var totalLoadSensor = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "CPU Total");
            return totalLoadSensor?.Value ?? 0f;
        }

        /// <summary>
        /// Gets CPU usage for each core as a percentage.
        /// </summary>
        public List<float> GetCpuCoreUsage()
        {
            if (!_isInitialized) return new List<float>();
            var cpu = GetCpuHardware();
            if (cpu == null) return new List<float>();

            var coreLoadSensors = cpu.Sensors
                .Where(s => s.SensorType == SensorType.Load && s.Name.StartsWith("CPU Core #"))
                .OrderBy(s => {
                    // Safely parse the core number after '#'
                    if (int.TryParse(s.Name.AsSpan(s.Name.LastIndexOf('#') + 1), out int coreIndex)) 
                    {
                         return coreIndex;
                    }
                    return -1; // Handle cases where parsing fails (shouldn't happen with LHM default names)
                 })
                .Select(s => s.Value ?? 0f)
                .ToList();

            return coreLoadSensors;
        }

        // Helper method to find the Memory hardware
        private IHardware? GetMemoryHardware()
        {
            if (!_isInitialized) return null;
            return _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
        }

        /// <summary>
        /// Gets the RAM usage percentage.
        /// </summary>
        public float GetRamUsagePercentage()
        {
            if (!_isInitialized) return 0f;
            var memory = GetMemoryHardware();
            if (memory == null) return 0f;

            // Look for a general Load sensor for memory percentage
            var usagePercentSensor = memory.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "Memory");
            if (usagePercentSensor?.Value != null) return usagePercentSensor.Value.Value;

            // Fallback: Calculate from Used/Total if Load sensor not found
            var usedMemorySensor = memory.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Used");
            var totalMemorySensor = memory.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Total"); // Note: LHM might not have 'Total' sensor, check GetRamInfo alternative

            if (usedMemorySensor?.Value != null && totalMemorySensor?.Value != null && totalMemorySensor.Value > 0)
            {
                // Assuming values are in GiB, the ratio is the same regardless of unit
                return (usedMemorySensor.Value.Value / totalMemorySensor.Value.Value) * 100f;
            }
            
             // Last fallback: Calculate using Available/Total from GetRamInfo (less direct)
            var (totalMb, availableMb) = GetRamInfo();
            if (totalMb > 0)
            {
                return ((totalMb - availableMb) / totalMb) * 100f;
            }

            return 0f; // Could not determine usage
        }

        /// <summary>
        /// Gets the total and available RAM in megabytes (MB).
        /// </summary>
        /// <returns>A tuple containing (Total RAM in MB, Available RAM in MB).</returns>
        public (float Total, float Available) GetRamInfo()
        {
            if (!_isInitialized) return (0f, 0f);
            var memory = GetMemoryHardware();
            if (memory == null) return (0f, 0f);

            // LHM typically provides 'Memory Used' and 'Memory Available' in GiB
            var usedMemorySensor = memory.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Used");
            var availableMemorySensor = memory.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Available");

            float usedGb = usedMemorySensor?.Value ?? 0f;
            float availableGb = availableMemorySensor?.Value ?? 0f;
            
            // Total = Used + Available (convert GiB to MB)
            float totalMb = (usedGb + availableGb) * 1024f;
            float availableMb = availableGb * 1024f;
            
            return (totalMb, availableMb);
        }

        public float GetUsedRam()
        {
            if (!_isInitialized) return 0f;

            var memory = GetMemoryHardware();
            if (memory == null) return 0f;

            // Find the sensor for used RAM in GiB
            var usedRamSensor = memory.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Used");
            return usedRamSensor?.Value ?? 0f;
        }

        /// <summary>
        /// Gets the CPU package temperature in Celsius.
        /// Returns float.NaN if the sensor is not found.
        /// </summary>
        public float GetCpuPackageTemperature()
        {
            if (!_isInitialized) return float.NaN;
            var cpu = GetCpuHardware();
            if (cpu == null) return float.NaN;

            // Prioritize "CPU Package" sensor
            var packageTempSensor = cpu.Sensors.FirstOrDefault(s => 
                s.SensorType == SensorType.Temperature && 
                s.Name.Equals("CPU Package", StringComparison.OrdinalIgnoreCase));

            if (packageTempSensor?.Value != null) return packageTempSensor.Value.Value;
            
            // Fallback: Look for any sensor named containing "CPU Temperature" or "Core (Tctl/Tdie)" as last resort
            var fallbackTempSensor = cpu.Sensors.FirstOrDefault(s =>
                s.SensorType == SensorType.Temperature &&
                (s.Name.Contains("CPU Temperature", StringComparison.OrdinalIgnoreCase) || 
                 s.Name.Contains("Core (Tctl/Tdie)", StringComparison.OrdinalIgnoreCase))); // Common on AMD
            
            return fallbackTempSensor?.Value ?? float.NaN; // Return NaN if no suitable sensor found
        }

        /// <summary>
        /// Gets the current CPU clock speed in MHz (average across all cores).
        /// </summary>
        /// <returns>CPU clock speed in MHz or 0 if not available</returns>
        public float GetCpuClockSpeed()
        {
            // Force an update to get fresh data
            Update();
            
            if (!_isInitialized) return 0f;
            var cpu = GetCpuHardware();
            if (cpu == null) return 0f;

            // Look for the CPU Core # Clock sensors and average them
            var clockSensors = cpu.Sensors
                .Where(s => s.SensorType == SensorType.Clock && s.Name.StartsWith("CPU Core #"))
                .ToList();

            if (clockSensors.Count > 0)
            {
                return clockSensors.Average(s => s.Value ?? 0);
            }

            // Fallback to "CPU Package" clock sensor if available
            var packageClockSensor = cpu.Sensors.FirstOrDefault(s => 
                s.SensorType == SensorType.Clock && 
                s.Name.Equals("CPU Package", StringComparison.OrdinalIgnoreCase));

            return packageClockSensor?.Value ?? 0f;
        }

        /// <summary>
        /// Gets the current CPU power usage in watts.
        /// </summary>
        /// <returns>CPU power usage in watts or 0 if not available</returns>
        public float GetCpuPowerUsage()
        {
            // Force an update to get fresh data
            Update();
            
            if (!_isInitialized) return 0f;
            var cpu = GetCpuHardware();
            if (cpu == null) return 0f;

            // Try to find power sensors, prioritizing CPU Package
            var powerSensor = cpu.Sensors.FirstOrDefault(s => 
                s.SensorType == SensorType.Power && 
                s.Name.Equals("CPU Package", StringComparison.OrdinalIgnoreCase));

            if (powerSensor?.Value != null) return powerSensor.Value.Value;

            // Fallback: Try other power sensors
            var fallbackSensor = cpu.Sensors.FirstOrDefault(s => 
                s.SensorType == SensorType.Power && 
                s.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase));

            return fallbackSensor?.Value ?? 0f;
        }

        // Helper method to find all GPU hardware
        private List<IHardware> GetAllGpuHardware()
        {
            if (!_isInitialized) return new List<IHardware>();
            return _computer.Hardware.Where(h => 
                h.HardwareType == HardwareType.GpuNvidia || 
                h.HardwareType == HardwareType.GpuAmd ||
                h.HardwareType == HardwareType.GpuIntel).ToList();
        }

        // Helper method to find the GPU hardware by index
        private IHardware? GetGpuHardware(int gpuIndex = 0)
        {
            var gpus = GetAllGpuHardware();
            return gpuIndex >= 0 && gpuIndex < gpus.Count ? gpus[gpuIndex] : null;
        }

        /// <summary>
        /// Gets the GPU name for the first available GPU.
        /// </summary>
        public string GetGpuName()
        {
            return GetGpuName(0);
        }

        /// <summary>
        /// Gets the GPU name for the specified GPU index.
        /// </summary>
        public string GetGpuName(int gpuIndex)
        {
            if (!_isInitialized) return "Hardware Init Failed";
            var gpu = GetGpuHardware(gpuIndex);
            return gpu?.Name ?? "No GPU Detected";
        }

        /// <summary>
        /// Gets the GPU usage as a percentage for the first available GPU.
        /// </summary>
        public float GetGpuUsage()
        {
            return GetGpuUsage(0);
        }

        /// <summary>
        /// Gets the GPU usage as a percentage for the specified GPU index.
        /// </summary>
        public float GetGpuUsage(int gpuIndex)
        {
            if (!_isInitialized) return 0f;
            var gpu = GetGpuHardware(gpuIndex);
            if (gpu == null) return 0f;

            // Look for GPU Core load sensor
            var coreLoadSensor = gpu.Sensors.FirstOrDefault(s => 
                s.SensorType == SensorType.Load && 
                s.Name.Equals("GPU Core", StringComparison.OrdinalIgnoreCase));
            
            return coreLoadSensor?.Value ?? 0f;
        }

        /// <summary>
        /// Gets the GPU temperature in Celsius for the first available GPU.
        /// </summary>
        public float GetGpuTemperature()
        {
            return GetGpuTemperature(0);
        }

        /// <summary>
        /// Gets the GPU temperature in Celsius for the specified GPU index.
        /// </summary>
        public float GetGpuTemperature(int gpuIndex)
        {
            if (!_isInitialized) return float.NaN;
            var gpu = GetGpuHardware(gpuIndex);
            if (gpu == null) return float.NaN;

            // Try multiple temperature sensor names that different GPUs might use
            var tempSensorNames = new[] { "GPU Core", "GPU Temperature", "Temperature", "Hot Spot", "GPU" };
            
            foreach (var sensorName in tempSensorNames)
            {
                var tempSensor = gpu.Sensors.FirstOrDefault(s => 
                    s.SensorType == SensorType.Temperature && 
                    s.Name.Contains(sensorName, StringComparison.OrdinalIgnoreCase));
                
                if (tempSensor?.Value != null)
                {
                    return tempSensor.Value.Value;
                }
            }
            
            return float.NaN;
        }

        /// <summary>
        /// Gets the GPU memory usage information for the first available GPU.
        /// </summary>
        /// <returns>A tuple containing (Used memory in MB, Total memory in MB, Usage percentage)</returns>
        public (float UsedMB, float TotalMB, float UsagePercent) GetGpuMemoryInfo()
        {
            return GetGpuMemoryInfo(0);
        }

        /// <summary>
        /// Gets the GPU memory usage information for the specified GPU index.
        /// </summary>
        /// <returns>A tuple containing (Used memory in MB, Total memory in MB, Usage percentage)</returns>
        public (float UsedMB, float TotalMB, float UsagePercent) GetGpuMemoryInfo(int gpuIndex)
        {
            if (!_isInitialized) return (0f, 0f, 0f);
            var gpu = GetGpuHardware(gpuIndex);
            if (gpu == null) return (0f, 0f, 0f);

            // Look for memory usage sensors
            var memoryUsedSensor = gpu.Sensors.FirstOrDefault(s => 
                s.SensorType == SensorType.SmallData && 
                s.Name.Equals("GPU Memory Used", StringComparison.OrdinalIgnoreCase));
            
            var memoryTotalSensor = gpu.Sensors.FirstOrDefault(s => 
                s.SensorType == SensorType.SmallData && 
                s.Name.Equals("GPU Memory Total", StringComparison.OrdinalIgnoreCase));
            
            var memoryLoadSensor = gpu.Sensors.FirstOrDefault(s => 
                s.SensorType == SensorType.Load && 
                s.Name.Equals("GPU Memory", StringComparison.OrdinalIgnoreCase));

            float usedMB = memoryUsedSensor?.Value ?? 0f;
            float totalMB = memoryTotalSensor?.Value ?? 0f;
            float usagePercent = memoryLoadSensor?.Value ?? 0f;

            // If we don't have the percentage but have used/total, calculate it
            if (usagePercent == 0 && totalMB > 0 && usedMB > 0)
            {
                usagePercent = (usedMB / totalMB) * 100f;
            }

            return (usedMB, totalMB, usagePercent);
        }

        /// <summary>
        /// Gets the GPU clock speed in MHz for the first available GPU.
        /// </summary>
        public float GetGpuClockSpeed()
        {
            return GetGpuClockSpeed(0);
        }

        /// <summary>
        /// Gets the GPU clock speed in MHz for the specified GPU index.
        /// </summary>
        public float GetGpuClockSpeed(int gpuIndex)
        {
            if (!_isInitialized) return 0f;
            var gpu = GetGpuHardware(gpuIndex);
            if (gpu == null) return 0f;

            // Look for GPU Core clock sensor
            var clockSensor = gpu.Sensors.FirstOrDefault(s => 
                s.SensorType == SensorType.Clock && 
                s.Name.Equals("GPU Core", StringComparison.OrdinalIgnoreCase));
            
            return clockSensor?.Value ?? 0f;
        }

        /// <summary>
        /// Gets the GPU power usage in watts for the first available GPU.
        /// </summary>
        public float GetGpuPowerUsage()
        {
            return GetGpuPowerUsage(0);
        }

        /// <summary>
        /// Gets the GPU power usage in watts for the specified GPU index.
        /// </summary>
        public float GetGpuPowerUsage(int gpuIndex)
        {
            if (!_isInitialized) return 0f;
            var gpu = GetGpuHardware(gpuIndex);
            if (gpu == null) return 0f;

            // Look for GPU power sensor
            var powerSensor = gpu.Sensors.FirstOrDefault(s => 
                s.SensorType == SensorType.Power && 
                (s.Name.Equals("GPU Power", StringComparison.OrdinalIgnoreCase) ||
                 s.Name.Equals("GPU Package", StringComparison.OrdinalIgnoreCase)));
            
            return powerSensor?.Value ?? 0f;
        }

        /// <summary>
        /// Gets the number of available GPUs.
        /// </summary>
        public int GetGpuCount()
        {
            return GetAllGpuHardware().Count;
        }

        /// <summary>
        /// Gets a list of available GPUs with their index, name, and type.
        /// </summary>
        public List<(int Index, string Name, string Type)> GetAvailableGpus()
        {
            var gpus = GetAllGpuHardware();
            var result = new List<(int, string, string)>();

            for (int i = 0; i < gpus.Count; i++)
            {
                var gpu = gpus[i];
                var type = gpu.HardwareType switch
                {
                    HardwareType.GpuNvidia => "NVIDIA",
                    HardwareType.GpuAmd => "AMD",
                    HardwareType.GpuIntel => "Intel",
                    _ => "Unknown"
                };
                result.Add((i, gpu.Name, type));
            }

            return result;
        }

        #region Drive Monitoring Methods

        /// <summary>
        /// Gets information for all available drives including usage percentages
        /// </summary>
        public List<(string Name, string Label, long TotalBytes, long UsedBytes, float UsagePercent)> GetDriveInfo()
        {
            var drives = new List<(string, string, long, long, float)>();

            if (!_isInitialized)
            {
                return drives;
            }

            try
            {
                // Get drive info using System.IO.DriveInfo for space information
                foreach (var drive in System.IO.DriveInfo.GetDrives())
                {
                    try
                    {
                        if (drive.IsReady && drive.DriveType == System.IO.DriveType.Fixed)
                        {
                            var totalBytes = drive.TotalSize;
                            var availableBytes = drive.AvailableFreeSpace;
                            var usedBytes = totalBytes - availableBytes;
                            var usagePercent = totalBytes > 0 ? (float)(usedBytes * 100.0 / totalBytes) : 0f;

                            var label = string.IsNullOrEmpty(drive.VolumeLabel) ? drive.Name : drive.VolumeLabel;
                            drives.Add((drive.Name, label, totalBytes, usedBytes, usagePercent));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error reading drive {drive.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting drive info: {ex.Message}");
            }

            return drives;
        }

        /// <summary>
        /// Gets the temperature of a specific drive by name
        /// </summary>
        public float GetDriveTemperature(string driveName)
        {
            if (!_isInitialized)
            {
                return float.NaN;
            }

            try
            {
                // Get available storage device temperatures
                var storageTemperatures = GetStorageTemperatures();

                // For logical drives like C:, D:, etc., we'll use a round-robin approach
                // to distribute temperatures from available storage devices
                if (storageTemperatures.Any())
                {
                    // Simple mapping: use the drive letter to pick a storage device
                    var driveIndex = driveName.Length > 0 ? driveName[0] - 'A' : 0;
                    var temperatureIndex = driveIndex % storageTemperatures.Count;

                    return storageTemperatures[temperatureIndex];
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting drive temperature for {driveName}: {ex.Message}");
            }

            return float.NaN;
        }

        /// <summary>
        /// Gets all available storage device temperatures
        /// </summary>
        public List<float> GetStorageTemperatures()
        {
            var temperatures = new List<float>();

            if (!_isInitialized)
            {
                return temperatures;
            }

            try
            {
                var storageDevices = _computer.Hardware
                    .Where(h => h.HardwareType == HardwareType.Storage)
                    .ToList();

                foreach (var device in storageDevices)
                {
                    var tempSensor = device.Sensors
                        .FirstOrDefault(s => s.SensorType == SensorType.Temperature &&
                                           s.Value.HasValue &&
                                           s.Value.Value > 0);

                    if (tempSensor != null)
                    {
                        temperatures.Add(tempSensor.Value.Value);
                        Debug.WriteLine($"Storage device {device.Name}: {tempSensor.Value.Value}°C");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting storage temperatures: {ex.Message}");
            }

            return temperatures;
        }

        #endregion

        public void Dispose()
        {
            if (_isInitialized)
            {
                _computer?.Close();
            }
            GC.SuppressFinalize(this);
        }
    }

    // Helper visitor class to update sensor values
    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}
