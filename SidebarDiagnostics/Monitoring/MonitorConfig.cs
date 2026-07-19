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
    [Serializable]
    public enum MonitorType : byte
    {
        CPU,
        RAM,
        GPU,
        HD,
        Network
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MonitorConfig : PropertyChangedBase, ICloneable
    {

        public MonitorConfig Clone()
        {
            MonitorConfig _clone = (MonitorConfig)MemberwiseClone();
            _clone.Hardware = _clone.Hardware.Select(h => h.Clone()).ToArray();
            _clone.Metrics = _clone.Metrics.Select(m => m.Clone()).ToArray();
            _clone.Params = _clone.Params.Select(p => p.Clone()).ToArray();

            if (_clone.HardwareOC != null)
            {
                _clone.HardwareOC = new ObservableCollection<HardwareConfig>(_clone.HardwareOC.Select(h => h.Clone()));
            }

            return _clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private MonitorType _type;

        [JsonProperty]
        public MonitorType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private bool _enabled;

        [JsonProperty]
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        private byte _order;

        [JsonProperty]
        public byte Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }

        private HardwareConfig[] _hardware;

        [JsonProperty]
        public HardwareConfig[] Hardware
        {
            get => _hardware;
            set => SetProperty(ref _hardware, value);
        }

        private ObservableCollection<HardwareConfig> _hardwareOC;

        public ObservableCollection<HardwareConfig> HardwareOC
        {
            get => _hardwareOC;
            set => SetProperty(ref _hardwareOC, value);
        }

        private MetricConfig[] _metrics;

        [JsonProperty]
        public MetricConfig[] Metrics
        {
            get => _metrics;
            set => SetProperty(ref _metrics, value);
        }

        private ConfigParam[] _params;

        [JsonProperty]
        public ConfigParam[] Params
        {
            get => _params;
            set => SetProperty(ref _params, value);
        }

        public string Name
        {
            get
            {
                return Type.GetDescription();
            }
        }

        public static MonitorConfig[] CheckConfig(MonitorConfig[] config)
        {
            MonitorConfig[] _default = Default;

            if (config == null)
            {
                return _default;
            }

            config = (
                from def in _default
                join rec in config on def.Type equals rec.Type into merged
                from newrec in merged.DefaultIfEmpty(def)
                select newrec
                ).ToArray();

            foreach (MonitorConfig _record in config)
            {
                MonitorConfig _defaultRecord = _default.Single(d => d.Type == _record.Type);

                if (_record.Hardware == null)
                {
                    _record.Hardware = _defaultRecord.Hardware;
                }

                if (_record.Metrics == null)
                {
                    _record.Metrics = _defaultRecord.Metrics;
                }
                else
                {
                    _record.Metrics = (
                        from def in _defaultRecord.Metrics
                        join metric in _record.Metrics on def.Key equals metric.Key into merged
                        from newmetric in merged.DefaultIfEmpty(def)
                        select newmetric
                        ).ToArray();
                }

                if (_record.Params == null)
                {
                    _record.Params = _defaultRecord.Params;
                }
                else
                {
                    _record.Params = (
                        from def in _defaultRecord.Params
                        join param in _record.Params on def.Key equals param.Key into merged
                        from newparam in merged.DefaultIfEmpty(def)
                        select newparam
                        ).ToArray();
                }
            }

            return config;
        }

        public static MonitorConfig[] Default
        {
            get
            {
                return new MonitorConfig[5]
                {
                    new MonitorConfig()
                    {
                        Type = MonitorType.CPU,
                        Enabled = true,
                        Order = 5,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[6]
                        {
                            new MetricConfig(MetricKey.CPUClock, true),
                            new MetricConfig(MetricKey.CPUTemp, true),
                            new MetricConfig(MetricKey.CPUVoltage, true),
                            new MetricConfig(MetricKey.CPUFan, true),
                            new MetricConfig(MetricKey.CPULoad, true),
                            new MetricConfig(MetricKey.CPUCoreLoad, true)
                        },
                        Params = new ConfigParam[6]
                        {
                            ConfigParam.Defaults.HardwareNames,
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.AllCoreClocks,
                            ConfigParam.Defaults.UseGHz,
                            ConfigParam.Defaults.UseFahrenheit,
                            ConfigParam.Defaults.TempAlert
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.RAM,
                        Enabled = true,
                        Order = 4,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[5]
                        {
                            new MetricConfig(MetricKey.RAMClock, true),
                            new MetricConfig(MetricKey.RAMVoltage, true),
                            new MetricConfig(MetricKey.RAMLoad, true),
                            new MetricConfig(MetricKey.RAMUsed, true),
                            new MetricConfig(MetricKey.RAMFree, true)
                        },
                        Params = new ConfigParam[2]
                        {
                            ConfigParam.Defaults.NoHardwareNames,
                            ConfigParam.Defaults.RoundAll
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.GPU,
                        Enabled = true,
                        Order = 3,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[7]
                        {
                            new MetricConfig(MetricKey.GPUCoreClock, true),
                            new MetricConfig(MetricKey.GPUVRAMClock, true),
                            new MetricConfig(MetricKey.GPUCoreLoad, true),
                            new MetricConfig(MetricKey.GPUVRAMLoad, true),
                            new MetricConfig(MetricKey.GPUVoltage, true),
                            new MetricConfig(MetricKey.GPUTemp, true),
                            new MetricConfig(MetricKey.GPUFan, true)
                        },
                        Params = new ConfigParam[5]
                        {
                            ConfigParam.Defaults.HardwareNames,
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.UseGHz,
                            ConfigParam.Defaults.UseFahrenheit,
                            ConfigParam.Defaults.TempAlert
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.HD,
                        Enabled = true,
                        Order = 2,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[6]
                        {
                            new MetricConfig(MetricKey.DriveLoadBar, true),
                            new MetricConfig(MetricKey.DriveLoad, true),
                            new MetricConfig(MetricKey.DriveUsed, true),
                            new MetricConfig(MetricKey.DriveFree, true),
                            new MetricConfig(MetricKey.DriveRead, true),
                            new MetricConfig(MetricKey.DriveWrite, true)
                        },
                        Params = new ConfigParam[2]
                        {
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.UsedSpaceAlert
                        }
                    },
                    new MonitorConfig()
                    {
                        Type = MonitorType.Network,
                        Enabled = true,
                        Order = 1,
                        Hardware = new HardwareConfig[0],
                        Metrics = new MetricConfig[4]
                        {
                            new MetricConfig(MetricKey.NetworkIP, true),
                            new MetricConfig(MetricKey.NetworkExtIP, false),
                            new MetricConfig(MetricKey.NetworkIn, true),
                            new MetricConfig(MetricKey.NetworkOut, true)
                        },
                        Params = new ConfigParam[5]
                        {
                            ConfigParam.Defaults.HardwareNames,
                            ConfigParam.Defaults.RoundAll,
                            ConfigParam.Defaults.UseBytes,
                            ConfigParam.Defaults.BandwidthInAlert,
                            ConfigParam.Defaults.BandwidthOutAlert
                        }
                    }
                };
            }
        }
    }
}
