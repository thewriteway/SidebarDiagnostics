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
    public class DriveMonitor : BaseMonitor
    {
        private const string CATEGORYNAME = "LogicalDisk";

        private const string FREEMB = "Free Megabytes";
        private const string PERCENTFREE = "% Free Space";
        private const string BYTESREADPERSECOND = "Disk Read Bytes/sec";
        private const string BYTESWRITEPERSECOND = "Disk Write Bytes/sec";

        public DriveMonitor(string id, string name, MetricConfig[] metrics, bool roundAll = false, double usedSpaceAlert = 0) : base(id, name, true)
        {
            _loadEnabled = metrics.IsEnabled(MetricKey.DriveLoad);

            bool _loadBarEnabled = metrics.IsEnabled(MetricKey.DriveLoadBar);
            bool _usedEnabled = metrics.IsEnabled(MetricKey.DriveUsed);
            bool _freeEnabled = metrics.IsEnabled(MetricKey.DriveFree);
            bool _readEnabled = metrics.IsEnabled(MetricKey.DriveRead);
            bool _writeEnabled = metrics.IsEnabled(MetricKey.DriveWrite);

            if (_loadBarEnabled)
            {
                if (metrics.Count(m => m.Enabled) == 1 && new Regex("^[A-Z]:$").IsMatch(name))
                {
                    Status = State.LoadBarInline;
                }
                else
                {
                    Status = State.LoadBarStacked;
                }
            }
            else
            {
                Status = State.NoLoadBar;
            }

            if (_loadBarEnabled || _loadEnabled || _usedEnabled || _freeEnabled)
            {
                _counterFreeMB = new PerformanceCounter(CATEGORYNAME, FREEMB, id);
                _counterFreePercent = new PerformanceCounter(CATEGORYNAME, PERCENTFREE, id);
            }

            List<iMetric> _metrics = new List<iMetric>();

            if (_loadBarEnabled || _loadEnabled)
            {
                LoadMetric = new BaseMetric(MetricKey.DriveLoad, DataType.Percent, null, roundAll, usedSpaceAlert);
                _metrics.Add(LoadMetric);
            }

            if (_usedEnabled)
            {
                UsedMetric = new BaseMetric(MetricKey.DriveUsed, DataType.Gigabyte, null, roundAll);
                _metrics.Add(UsedMetric);
            }

            if (_freeEnabled)
            {
                FreeMetric = new BaseMetric(MetricKey.DriveFree, DataType.Gigabyte, null, roundAll);
                _metrics.Add(FreeMetric);
            }

            if (_readEnabled)
            {
                _metrics.Add(new PCMetric(new PerformanceCounter(CATEGORYNAME, BYTESREADPERSECOND, id), MetricKey.DriveRead, DataType.kBps, null, roundAll, 0, BytesPerSecondConverter.Instance));
            }

            if (_writeEnabled)
            {
                _metrics.Add(new PCMetric(new PerformanceCounter(CATEGORYNAME, BYTESWRITEPERSECOND, id), MetricKey.DriveWrite, DataType.kBps, null, roundAll, 0, BytesPerSecondConverter.Instance));
            }

            Metrics = _metrics.ToArray();
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
                    if (_loadMetric != null)
                    {
                        _loadMetric.Dispose();
                        _loadMetric = null;
                    }

                    if (_usedMetric != null)
                    {
                        _usedMetric.Dispose();
                        _usedMetric = null;
                    }

                    if (_freeMetric != null)
                    {
                        _freeMetric.Dispose();
                        _freeMetric = null;
                    }

                    if (_counterFreeMB != null)
                    {
                        _counterFreeMB.Dispose();
                        _counterFreeMB = null;
                    }

                    if (_counterFreePercent != null)
                    {
                        _counterFreePercent.Dispose();
                        _counterFreePercent = null;
                    }
                }

                _disposed = true;
            }
        }

        ~DriveMonitor()
        {
            Dispose(false);
        }

        public static IEnumerable<HardwareConfig> GetHardware()
        {
            string[] _instances;

            try
            {
                _instances = new PerformanceCounterCategory(CATEGORYNAME).GetInstanceNames();
            }
            catch (InvalidOperationException)
            {
                _instances = new string[0];

                App.ShowPerformanceCounterError();
            }

            Regex _regex = new Regex("^[A-Z]:$");

            return _instances.Where(n => _regex.IsMatch(n)).OrderBy(d => d[0]).Select(h => new HardwareConfig() { ID = h, Name = h, ActualName = h });
        }

        public static iMonitor[] GetInstances(HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters)
        {
            bool _roundAll = parameters.GetValue<bool>(ParamKey.RoundAll);
            int _usedSpaceAlert = parameters.GetValue<int>(ParamKey.UsedSpaceAlert);

            return (
                from hw in GetHardware()
                join c in hardwareConfig on hw.ID equals c.ID into merged
                from n in merged.DefaultIfEmpty(hw).Select(n => { n.ActualName = hw.Name; return n; })
                where n.Enabled
                orderby n.Order descending, n.Name ascending
                select new DriveMonitor(n.ID, n.Name ?? n.ActualName, metrics, _roundAll, _usedSpaceAlert)
                ).ToArray();
        }

        public override void Update()
        {
            if (_counterFreeMB != null && _counterFreePercent != null)
            {
                double _freeGB;
                double _freePercent;

                try
                {
                    _freeGB = _counterFreeMB.NextValue() / 1024d;
                    _freePercent = _counterFreePercent.NextValue();
                }
                catch (InvalidOperationException)
                {
                    // The counter instance no longer exists (drive removed).
                    return;
                }

                double _usedPercent = 100d - _freePercent;

                double _totalGB = _freeGB / (_freePercent / 100d);
                double _usedGB = _totalGB - _freeGB;

                if (LoadMetric != null)
                {
                    LoadMetric.Update(_usedPercent);
                }

                if (UsedMetric != null)
                {
                    UsedMetric.Update(_usedGB);
                }

                if (FreeMetric != null)
                {
                    FreeMetric.Update(_freeGB);
                }
            }

            base.Update();
        }

        private State _status;

        public State Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        private iMetric _loadMetric;

        public iMetric LoadMetric
        {
            get => _loadMetric;
            private set => SetProperty(ref _loadMetric, value);
        }

        private iMetric _usedMetric;

        public iMetric UsedMetric
        {
            get => _usedMetric;
            private set => SetProperty(ref _usedMetric, value);
        }

        private iMetric _freeMetric;

        public iMetric FreeMetric
        {
            get => _freeMetric;
            private set => SetProperty(ref _freeMetric, value);
        }

        public iMetric[] DriveMetrics
        {
            get
            {
                if (_loadEnabled)
                {
                    return Metrics;
                }
                else
                {
                    return Metrics.Where(m => m.Key != MetricKey.DriveLoad).ToArray();
                }
            }
        }

        private PerformanceCounter _counterFreeMB { get; set; }

        private PerformanceCounter _counterFreePercent { get; set; }

        private bool _loadEnabled { get; set; }

        private bool _disposed { get; set; } = false;

        public enum State : byte
        {
            NoLoadBar,
            LoadBarInline,
            LoadBarStacked
        }
    }
}
