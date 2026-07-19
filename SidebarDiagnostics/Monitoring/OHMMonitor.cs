using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Windows.Media;
using LibreHardwareMonitor.Hardware;
using Newtonsoft.Json;
using SidebarDiagnostics.Framework;
using System.Threading.Tasks;

namespace SidebarDiagnostics.Monitoring
{
    public class OHMMonitor : BaseMonitor
    {
        public OHMMonitor(MonitorType type, string id, string name, IHardware hardware, IHardware board, MetricConfig[] metrics, ConfigParam[] parameters) : base(id, name, parameters.GetValue<bool>(ParamKey.HardwareNames))
        {
            _hardware = hardware;

            UpdateHardware();

            switch (type)
            {
                case MonitorType.CPU:
                    InitCPU(
                        board,
                        metrics,
                        parameters.GetValue<bool>(ParamKey.RoundAll),
                        parameters.GetValue<bool>(ParamKey.AllCoreClocks),
                        parameters.GetValue<bool>(ParamKey.UseGHz),
                        parameters.GetValue<bool>(ParamKey.UseFahrenheit),
                        parameters.GetValue<int>(ParamKey.TempAlert)
                        );
                    break;

                case MonitorType.RAM:
                    InitRAM(
                        board,
                        metrics,
                        parameters.GetValue<bool>(ParamKey.RoundAll)
                        );
                    break;

                case MonitorType.GPU:
                    InitGPU(
                        metrics,
                        parameters.GetValue<bool>(ParamKey.RoundAll),
                        parameters.GetValue<bool>(ParamKey.UseGHz),
                        parameters.GetValue<bool>(ParamKey.UseFahrenheit),
                        parameters.GetValue<int>(ParamKey.TempAlert)
                        );
                    break;

                default:
                    throw new ArgumentException("Invalid MonitorType.");
            }
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    _hardware = null;
                }

                _disposed = true;
            }
        }

        ~OHMMonitor()
        {
            Dispose(false);
        }

        public static iMonitor[] GetInstances(HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters, MonitorType type, IHardware board, IHardware[] hardware)
        {
            return (
                from hw in hardware
                join c in hardwareConfig on hw.Identifier.ToString() equals c.ID into merged
                from n in merged.DefaultIfEmpty(new HardwareConfig() { ID = hw.Identifier.ToString(), Name = hw.Name, ActualName = hw.Name }).Select(n => { if (n.ActualName != hw.Name) { n.Name = n.ActualName = hw.Name; } return n; })
                where n.Enabled
                orderby n.Order descending, n.Name ascending
                select new OHMMonitor(type, n.ID, n.Name ?? n.ActualName, hw, board, metrics, parameters)
                ).ToArray();
        }

        public override void Update()
        {
            UpdateHardware();

            base.Update();
        }

        private void UpdateHardware()
        {
            _hardware.Update();
        }

        private void InitCPU(IHardware board, MetricConfig[] metrics, bool roundAll, bool allCoreClocks, bool useGHz, bool useFahrenheit, double tempAlert)
        {
            List<OHMMetric> _sensorList = new List<OHMMetric>();

            if (metrics.IsEnabled(MetricKey.CPUClock))
            {
                Regex regex = new Regex(@"^.*(CPU|Core).*#(\d+)$");

                var coreClocks = _hardware.Sensors
                    .Where(s => s.SensorType == SensorType.Clock)
                    .Select(s => new
                    {
                        Match = regex.Match(s.Name),
                        Sensor = s
                    })
                    .Where(s => s.Match.Success)
                    .Select(s => new
                    {
                        Index = int.Parse(s.Match.Groups[2].Value),
                        s.Sensor
                    })
                    .OrderBy(s => s.Index)
                    .ToList();

                if (coreClocks.Count > 0)
                {
                    if (allCoreClocks)
                    {
                        foreach (var coreClock in coreClocks)
                        {
                            _sensorList.Add(new OHMMetric(coreClock.Sensor, MetricKey.CPUClock, DataType.MHz, string.Format("{0} {1}", Resources.CPUCoreClockLabel, coreClock.Index - 1), (useGHz ? false : true), 0, (useGHz ? MHzToGHz.Instance : null)));
                        }
                    }
                    else
                    {
                        ISensor firstClock = coreClocks
                            .Select(s => s.Sensor)
                            .FirstOrDefault();

                        _sensorList.Add(new OHMMetric(firstClock, MetricKey.CPUClock, DataType.MHz, null, (useGHz ? false : true), 0, (useGHz ? MHzToGHz.Instance : null)));
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUVoltage))
            {
                ISensor _voltage = null;

                if (board != null)
                {
                    _voltage = board.Sensors.Where(s => s.SensorType == SensorType.Voltage && s.Name.Contains("CPU")).FirstOrDefault();
                }

                if (_voltage == null)
                {
                    _voltage = _hardware.Sensors.Where(s => s.SensorType == SensorType.Voltage).FirstOrDefault();
                }

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.CPUVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUTemp))
            {
                ISensor _tempSensor = null;

                _tempSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Name.Contains("CCDs Max (Tdie)")).FirstOrDefault(); // Check for AMD core chiplet dies (CCDs)

                if (board != null && _tempSensor == null)
                {
                    _tempSensor = board.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Name.Contains("CPU")).FirstOrDefault();
                }

                if (_tempSensor == null)
                {
                    _tempSensor =
                        _hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && (s.Name == "CPU Package" || s.Name.Contains("Tdie"))).FirstOrDefault() ??
                        _hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature).FirstOrDefault();
                }

                if (_tempSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_tempSensor, MetricKey.CPUTemp, DataType.Celcius, null, roundAll, tempAlert, (useFahrenheit ? CelciusToFahrenheit.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.CPUFan))
            {
                ISensor _fanSensor = null;

                if (board != null)
                {
                    _fanSensor = board.Sensors.Where(s => new SensorType[2] { SensorType.Fan, SensorType.Control }.Contains(s.SensorType) && s.Name.Contains("CPU")).FirstOrDefault();
                }

                if (_fanSensor == null)
                {
                    _fanSensor = _hardware.Sensors.Where(s => new SensorType[2] { SensorType.Fan, SensorType.Control }.Contains(s.SensorType)).FirstOrDefault();
                }

                if (_fanSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_fanSensor, MetricKey.CPUFan, DataType.RPM, null, roundAll));
                }
            }

            bool _loadEnabled = metrics.IsEnabled(MetricKey.CPULoad);
            bool _coreLoadEnabled = metrics.IsEnabled(MetricKey.CPUCoreLoad);

            if (_loadEnabled || _coreLoadEnabled)
            {
                ISensor[] _loadSensors = _hardware.Sensors.Where(s => s.SensorType == SensorType.Load).ToArray();

                if (_loadSensors.Length > 0)
                {
                    if (_loadEnabled)
                    {
                        ISensor _totalCPU = _loadSensors.Where(s => s.Index == 0).FirstOrDefault();

                        if (_totalCPU != null)
                        {
                            _sensorList.Add(new OHMMetric(_totalCPU, MetricKey.CPULoad, DataType.Percent, null, roundAll));
                        }
                    }

                    if (_coreLoadEnabled)
                    {
                        for (int i = 1; i <= _loadSensors.Max(s => s.Index); i++)
                        {
                            ISensor _coreLoad = _loadSensors.Where(s => s.Index == i).FirstOrDefault();

                            if (_coreLoad != null)
                            {
                                _sensorList.Add(new OHMMetric(_coreLoad, MetricKey.CPUCoreLoad, DataType.Percent, string.Format("{0} {1}", Resources.CPUCoreLoadLabel, i - 1), roundAll));
                            }
                        }
                    }
                }
            }

            Metrics = _sensorList.ToArray();
        }

        public void InitRAM(IHardware board, MetricConfig[] metrics, bool roundAll)
        {
            List<OHMMetric> _sensorList = new List<OHMMetric>();

            if (metrics.IsEnabled(MetricKey.RAMClock))
            {
                ISensor _ramClock = _hardware.Sensors.Where(s => s.SensorType == SensorType.Clock).FirstOrDefault();

                if (_ramClock != null)
                {
                    _sensorList.Add(new OHMMetric(_ramClock, MetricKey.RAMClock, DataType.MHz, null, true));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMVoltage))
            {
                ISensor _voltage = null;

                if (board != null)
                {
                    _voltage = board.Sensors.Where(s => s.SensorType == SensorType.Voltage && s.Name.Contains("RAM")).FirstOrDefault();
                }

                if (_voltage == null)
                {
                    _voltage = _hardware.Sensors.Where(s => s.SensorType == SensorType.Voltage).FirstOrDefault();
                }

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.RAMVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMLoad))
            {
                ISensor _loadSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 0).FirstOrDefault();

                if (_loadSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_loadSensor, MetricKey.RAMLoad, DataType.Percent, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMUsed))
            {
                ISensor _usedSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Data && s.Index == 0).FirstOrDefault();

                if (_usedSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_usedSensor, MetricKey.RAMUsed, DataType.Gigabyte, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.RAMFree))
            {
                ISensor _freeSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Data && s.Index == 1).FirstOrDefault();

                if (_freeSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_freeSensor, MetricKey.RAMFree, DataType.Gigabyte, null, roundAll));
                }
            }

            Metrics = _sensorList.ToArray();
        }

        public void InitGPU(MetricConfig[] metrics, bool roundAll, bool useGHz, bool useFahrenheit, double tempAlert)
        {
            List<iMetric> _sensorList = new List<iMetric>();

            if (metrics.IsEnabled(MetricKey.GPUCoreClock))
            {
                ISensor _coreClock = _hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Name.Contains("Core")).FirstOrDefault();

                if (_coreClock != null)
                {
                    _sensorList.Add(new OHMMetric(_coreClock, MetricKey.GPUCoreClock, DataType.MHz, null, (useGHz ? false : true), 0, (useGHz ? MHzToGHz.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVRAMClock))
            {
                ISensor _vramClock = _hardware.Sensors.Where(s => s.SensorType == SensorType.Clock && s.Name.Contains("Memory")).FirstOrDefault();

                if (_vramClock != null)
                {
                    _sensorList.Add(new OHMMetric(_vramClock, MetricKey.GPUVRAMClock, DataType.MHz, null, (useGHz ? false : true), 0, (useGHz ? MHzToGHz.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUCoreLoad))
            {
                ISensor _coreLoad = _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Name.Contains("Core")).FirstOrDefault() ??
                    _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 0).FirstOrDefault();

                if (_coreLoad != null)
                {
                    _sensorList.Add(new OHMMetric(_coreLoad, MetricKey.GPUCoreLoad, DataType.Percent, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVRAMLoad))
            {
                ISensor _memoryUsed = _hardware.Sensors.Where(s => (s.SensorType == SensorType.Data || s.SensorType == SensorType.SmallData) && s.Name == "GPU Memory Used").FirstOrDefault();
                ISensor _memoryTotal = _hardware.Sensors.Where(s => (s.SensorType == SensorType.Data || s.SensorType == SensorType.SmallData) && s.Name == "GPU Memory Total").FirstOrDefault();

                if (_memoryUsed != null && _memoryTotal != null)
                {
                    _sensorList.Add(new GPUVRAMMLoadMetric(_memoryUsed, _memoryTotal, MetricKey.GPUVRAMLoad, DataType.Percent, null, roundAll));
                }
                else
                {
                    ISensor _vramLoad = _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Name.Contains("Memory")).FirstOrDefault() ??
                        _hardware.Sensors.Where(s => s.SensorType == SensorType.Load && s.Index == 1).FirstOrDefault();

                    if (_vramLoad != null)
                    {
                        _sensorList.Add(new OHMMetric(_vramLoad, MetricKey.GPUVRAMLoad, DataType.Percent, null, roundAll));
                    }
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUVoltage))
            {
                ISensor _voltage = _hardware.Sensors.Where(s => s.SensorType == SensorType.Voltage && s.Index == 0).FirstOrDefault();

                if (_voltage != null)
                {
                    _sensorList.Add(new OHMMetric(_voltage, MetricKey.GPUVoltage, DataType.Voltage, null, roundAll));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUTemp))
            {
                ISensor _tempSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Index == 0).FirstOrDefault();

                if (_tempSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_tempSensor, MetricKey.GPUTemp, DataType.Celcius, null, roundAll, tempAlert, (useFahrenheit ? CelciusToFahrenheit.Instance : null)));
                }
            }

            if (metrics.IsEnabled(MetricKey.GPUFan))
            {
                ISensor _fanSensor = _hardware.Sensors.Where(s => s.SensorType == SensorType.Control).OrderBy(s => s.Index).FirstOrDefault();

                if (_fanSensor != null)
                {
                    _sensorList.Add(new OHMMetric(_fanSensor, MetricKey.GPUFan, DataType.Percent));
                }
            }

            Metrics = _sensorList.ToArray();
        }

        private IHardware _hardware;

        private bool _disposed { get; set; } = false;
    }
}
