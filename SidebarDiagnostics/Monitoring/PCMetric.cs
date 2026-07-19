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
    public class PCMetric : BaseMetric
    {
        public PCMetric(PerformanceCounter counter, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            _counter = counter;
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
                    if (_counter != null)
                    {
                        _counter.Dispose();
                        _counter = null;
                    }
                }

                _disposed = true;
            }
        }

        ~PCMetric()
        {
            Dispose(false);
        }

        public override void Update()
        {
            try
            {
                Update(_counter.NextValue());
            }
            catch (InvalidOperationException)
            {
                // The counter instance no longer exists (drive/adapter removed).
            }
        }

        private PerformanceCounter _counter { get; set; }

        private bool _disposed { get; set; } = false;
    }
}
