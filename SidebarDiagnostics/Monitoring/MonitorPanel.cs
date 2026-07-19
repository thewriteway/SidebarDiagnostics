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
    public class MonitorPanel : PropertyChangedBase, IDisposable
    {
        public MonitorPanel(string title, string iconData, params iMonitor[] monitors)
        {
            IconPath = Geometry.Parse(iconData);
            Title = title;

            Monitors = monitors;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (iMonitor _monitor in Monitors)
                    {
                        _monitor.Dispose();
                    }

                    _monitors = null;
                    _iconPath = null;
                }

                _disposed = true;
            }
        }

        ~MonitorPanel()
        {
            Dispose(false);
        }

        private Geometry _iconPath;

        public Geometry IconPath
        {
            get => _iconPath;
            private set => SetProperty(ref _iconPath, value);
        }

        private string _title;

        public string Title
        {
            get => _title;
            private set => SetProperty(ref _title, value);
        }

        private iMonitor[] _monitors;

        public iMonitor[] Monitors
        {
            get => _monitors;
            private set => SetProperty(ref _monitors, value);
        }

        private bool _disposed { get; set; } = false;
    }
}
