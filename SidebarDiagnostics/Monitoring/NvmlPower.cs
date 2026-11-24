using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SidebarDiagnostics.Monitoring
{
    /// <summary>
    /// Minimal NVML wrapper used to read the current NVIDIA GPU power draw in watts.
    ///
    /// LibreHardwareMonitor 0.9.6 exposes Nvidia power through the NVAPI power
    /// topology as Load-typed sensors ("GPU Power" / "GPU Board Power"), and on
    /// some boards (observed on an RTX 5070 Ti) those values are far below the
    /// wattage reported by nvidia-smi / NVML. NVML is the same source nvidia-smi
    /// uses, so it is queried directly and only used when it actually works.
    /// </summary>
    internal static class Nvml
    {
        private const string DLL = "nvml.dll";

        [DllImport(DLL, EntryPoint = "nvmlInit_v2")]
        private static extern int Init();

        [DllImport(DLL, EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
        private static extern int GetHandleByIndex(uint index, out IntPtr device);

        [DllImport(DLL, EntryPoint = "nvmlDeviceGetPowerUsage")]
        private static extern int GetPowerUsage(IntPtr device, out uint milliwatts);

        public static bool TryGetPowerUsage(int index, out double watts)
        {
            watts = 0;

            lock (_sync)
            {
                if (!_initialized)
                {
                    if (_initFailed)
                    {
                        return false;
                    }

                    try
                    {
                        _initFailed = Init() != 0;
                    }
                    catch (Exception)
                    {
                        // nvml.dll is missing or unreadable (no NVIDIA driver).
                        _initFailed = true;
                    }

                    _initialized = true;

                    if (_initFailed)
                    {
                        return false;
                    }
                }

                if (!_devices.TryGetValue(index, out IntPtr _device))
                {
                    if (GetHandleByIndex((uint)index, out _device) != 0)
                    {
                        return false;
                    }

                    _devices[index] = _device;
                }

                if (GetPowerUsage(_device, out uint _milliwatts) != 0)
                {
                    return false;
                }

                watts = _milliwatts / 1000d;
                return true;
            }
        }

        private static readonly object _sync = new object();
        private static readonly Dictionary<int, IntPtr> _devices = new Dictionary<int, IntPtr>();
        private static bool _initialized;
        private static bool _initFailed;
    }
}
