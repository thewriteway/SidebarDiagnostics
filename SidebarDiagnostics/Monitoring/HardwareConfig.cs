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
    public class HardwareConfig : PropertyChangedBase, ICloneable
    {

        public HardwareConfig Clone()
        {
            return (HardwareConfig)MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        private string _id;

        [JsonProperty]
        public string ID
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _name;

        [JsonProperty]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _actualName;

        [JsonProperty]
        public string ActualName
        {
            get => _actualName;
            set => SetProperty(ref _actualName, value);
        }

        private bool _enabled = true;

        [JsonProperty]
        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        private byte _order = 0;

        [JsonProperty]
        public byte Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }
    }
}
