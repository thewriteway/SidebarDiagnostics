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
    [JsonObject(MemberSerialization.OptIn)]
    public class ConfigParam : PropertyChangedBase, ICloneable
    {

        public ConfigParam Clone()
        {
            return (ConfigParam)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private ParamKey _key;

        [JsonProperty]
        public ParamKey Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        private object _value;

        [JsonProperty]
        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                // JSON deserializes whole numbers as long; the rest of the app expects int.
                if (value is long)
                {
                    _value = Convert.ToInt32(value);
                }
                else
                {
                    _value = value;
                }

                NotifyPropertyChanged(nameof(Value));
            }
        }

        public Type Type
        {
            get
            {
                return Value.GetType();
            }
        }

        public string TypeString
        {
            get
            {
                return Type.ToString();
            }
        }

        public string Name
        {
            get
            {
                switch (Key)
                {
                    case ParamKey.HardwareNames:
                        return Resources.SettingsShowHardwareNames;

                    case ParamKey.UseFahrenheit:
                        return Resources.SettingsUseFahrenheit;

                    case ParamKey.AllCoreClocks:
                        return Resources.SettingsAllCoreClocks;

                    case ParamKey.CoreLoads:
                        return Resources.SettingsCoreLoads;

                    case ParamKey.TempAlert:
                        return Resources.SettingsTemperatureAlert;

                    case ParamKey.DriveDetails:
                        return Resources.SettingsShowDriveDetails;

                    case ParamKey.UsedSpaceAlert:
                        return Resources.SettingsUsedSpaceAlert;

                    case ParamKey.BandwidthInAlert:
                        return Resources.SettingsBandwidthInAlert;

                    case ParamKey.BandwidthOutAlert:
                        return Resources.SettingsBandwidthOutAlert;

                    case ParamKey.UseBytes:
                        return Resources.SettingsUseBytesPerSecond;

                    case ParamKey.RoundAll:
                        return Resources.SettingsRoundAllDecimals;

                    case ParamKey.DriveSpace:
                        return Resources.SettingsShowDriveSpace;

                    case ParamKey.DriveIO:
                        return Resources.SettingsShowDriveIO;

                    case ParamKey.UseGHz:
                        return Resources.SettingsUseGHz;

                    default:
                        return "Unknown";
                }
            }
        }

        public string Tooltip
        {
            get
            {
                switch (Key)
                {
                    case ParamKey.HardwareNames:
                        return Resources.SettingsShowHardwareNamesTooltip;

                    case ParamKey.UseFahrenheit:
                        return Resources.SettingsUseFahrenheitTooltip;

                    case ParamKey.AllCoreClocks:
                        return Resources.SettingsAllCoreClocksTooltip;

                    case ParamKey.CoreLoads:
                        return Resources.SettingsCoreLoadsTooltip;

                    case ParamKey.TempAlert:
                        return Resources.SettingsTemperatureAlertTooltip;

                    case ParamKey.DriveDetails:
                        return Resources.SettingsDriveDetailsTooltip;

                    case ParamKey.UsedSpaceAlert:
                        return Resources.SettingsUsedSpaceAlertTooltip;

                    case ParamKey.BandwidthInAlert:
                        return Resources.SettingsBandwidthInAlertTooltip;

                    case ParamKey.BandwidthOutAlert:
                        return Resources.SettingsBandwidthOutAlertTooltip;

                    case ParamKey.UseBytes:
                        return Resources.SettingsUseBytesPerSecondTooltip;

                    case ParamKey.RoundAll:
                        return Resources.SettingsRoundAllDecimalsTooltip;

                    case ParamKey.DriveSpace:
                        return Resources.SettingsShowDriveSpaceTooltip;

                    case ParamKey.DriveIO:
                        return Resources.SettingsShowDriveIOTooltip;

                    case ParamKey.UseGHz:
                        return Resources.SettingsUseGHzTooltip;

                    default:
                        return "Unknown";
                }
            }
        }

        public static class Defaults
        {
            public static ConfigParam HardwareNames
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.HardwareNames, Value = true };
                }
            }

            public static ConfigParam NoHardwareNames
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.HardwareNames, Value = false };
                }
            }

            public static ConfigParam UseFahrenheit
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseFahrenheit, Value = false };
                }
            }

            public static ConfigParam AllCoreClocks
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.AllCoreClocks, Value = false };
                }
            }

            public static ConfigParam CoreLoads
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.CoreLoads, Value = true };
                }
            }

            public static ConfigParam TempAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.TempAlert, Value = 0 };
                }
            }

            public static ConfigParam DriveDetails
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveDetails, Value = false };
                }
            }

            public static ConfigParam UsedSpaceAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UsedSpaceAlert, Value = 0 };
                }
            }

            public static ConfigParam BandwidthInAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.BandwidthInAlert, Value = 0 };
                }
            }

            public static ConfigParam BandwidthOutAlert
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.BandwidthOutAlert, Value = 0 };
                }
            }

            public static ConfigParam UseBytes
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseBytes, Value = false };
                }
            }

            public static ConfigParam RoundAll
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.RoundAll, Value = false };
                }
            }

            public static ConfigParam ShowDriveSpace
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveSpace, Value = true };
                }
            }

            public static ConfigParam ShowDriveIO
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.DriveIO, Value = true };
                }
            }

            public static ConfigParam UseGHz
            {
                get
                {
                    return new ConfigParam() { Key = ParamKey.UseGHz, Value = false };
                }
            }
        }
    }

    [Serializable]
    public enum ParamKey : byte
    {
        HardwareNames,
        UseFahrenheit,
        AllCoreClocks,
        CoreLoads,
        TempAlert,
        DriveDetails,
        UsedSpaceAlert,
        BandwidthInAlert,
        BandwidthOutAlert,
        UseBytes,
        RoundAll,
        DriveSpace,
        DriveIO,
        UseGHz
    }

    public enum DataType : byte
    {
        Dynamic,
        Bit,
        Kilobit,
        Megabit,
        Gigabit,
        Byte,
        Kilobyte,
        Megabyte,
        Gigabyte,
        bps,
        kbps,
        Mbps,
        Gbps,
        Bps,
        kBps,
        MBps,
        GBps,
        MHz,
        GHz,
        Voltage,
        Percent,
        RPM,
        Celcius,
        Fahrenheit,
        IP
    }
}
