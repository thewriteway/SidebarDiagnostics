using System;
using System.ComponentModel;
using System.Windows.Threading;
using SidebarDiagnostics.Monitoring;
using SidebarDiagnostics.Utilities;
using SidebarDiagnostics.Framework;

namespace SidebarDiagnostics.Models
{
    public class SidebarModel : PropertyChangedBase, IDisposable
    {
        public SidebarModel()
        {
            InitMachineName();
            InitClock();
            InitMonitors();
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
                    DisposeClock();
                    DisposeMonitors();
                }

                _disposed = true;
            }
        }

        ~SidebarModel()
        {
            Dispose(false);
        }

        public void Start()
        {
            StartClock();
            StartMonitors();
        }

        public void Reload()
        {
            DisposeMonitors();
            InitMonitors();
            StartMonitors();
        }

        public void Pause()
        {
            PauseClock();
            PauseMonitors();
        }

        public void Resume()
        {
            ResumeClock();
            ResumeMonitors();
        }

        private void InitMachineName()
        {
            ShowMachineName = Framework.Settings.Instance.ShowMachineName;

            MachineName = Environment.MachineName;
        }

        private void InitClock()
        {
            ShowClock = Framework.Settings.Instance.ShowClock;

            if (!ShowClock)
            {
                return;
            }

            ShowDate = !Framework.Settings.Instance.DateSetting.Equals(Framework.DateSetting.Disabled);

            UpdateClock();
        }

        private void InitMonitors()
        {
            MonitorManager = new MonitorManager(Framework.Settings.Instance.MonitorConfig);
            MonitorManager.Update();
        }

        private void StartClock()
        {
            if (!ShowClock)
            {
                return;
            }

            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += new EventHandler(ClockTimer_Tick);
            _clockTimer.Start();
        }

        private void StartMonitors()
        {
            lock (_monitorLock)
            {
                _monitorPaused = false;

                // Poll on a background timer so slow sensor reads never stall the UI.
                // WPF marshals the resulting property-change notifications itself.
                _monitorTimer = new System.Timers.Timer(Framework.Settings.Instance.PollingInterval) { AutoReset = false };
                _monitorTimer.Elapsed += MonitorTimer_Elapsed;
                _monitorTimer.Start();
            }
        }

        private void UpdateClock()
        {
            DateTime _now = DateTime.Now;

            Time = _now.ToString(Framework.Settings.Instance.Clock24HR ? "H:mm:ss" : "h:mm:ss tt", Culture.CultureInfo);

            if (ShowDate)
            {
                Date = _now.ToString(Framework.Settings.Instance.DateSetting.Format, Culture.CultureInfo);
            }
        }

        private void PauseClock()
        {
            if (_clockTimer != null)
            {
                _clockTimer.Stop();
            }
        }

        private void PauseMonitors()
        {
            lock (_monitorLock)
            {
                _monitorPaused = true;

                if (_monitorTimer != null)
                {
                    _monitorTimer.Stop();
                }
            }
        }

        private void ResumeClock()
        {
            if (_clockTimer != null)
            {
                _clockTimer.Start();
            }
        }

        private void ResumeMonitors()
        {
            lock (_monitorLock)
            {
                _monitorPaused = false;

                if (_monitorTimer != null)
                {
                    _monitorTimer.Start();
                }
            }
        }

        private void DisposeClock()
        {
            if (_clockTimer != null)
            {
                _clockTimer.Stop();
                _clockTimer = null;
            }
        }

        private void DisposeMonitors()
        {
            lock (_monitorLock)
            {
                if (_monitorTimer != null)
                {
                    _monitorTimer.Stop();
                    _monitorTimer.Dispose();
                    _monitorTimer = null;
                }

                if (MonitorManager != null)
                {
                    MonitorManager.Dispose();
                    _monitorManager = null;
                }
            }
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }

        private void MonitorTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // AutoReset is off: the timer is restarted only after a pass completes,
            // so updates can never overlap no matter how slow the sensors are.
            lock (_monitorLock)
            {
                if (_monitorTimer == null || MonitorManager == null)
                {
                    return;
                }

                MonitorManager.Update();

                if (!_monitorPaused)
                {
                    _monitorTimer.Start();
                }
            }
        }

        private bool _ready = false;

        public bool Ready
        {
            get => _ready;
            set => SetProperty(ref _ready, value);
        }

        private bool _showMachineName;

        public bool ShowMachineName
        {
            get => _showMachineName;
            set => SetProperty(ref _showMachineName, value);
        }

        private string _machineName;

        public string MachineName
        {
            get => _machineName;
            set => SetProperty(ref _machineName, value);
        }

        private bool _showClock;

        public bool ShowClock
        {
            get => _showClock;
            set => SetProperty(ref _showClock, value);
        }

        private string _time;

        public string Time
        {
            get => _time;
            set => SetProperty(ref _time, value);
        }

        private bool _showDate;

        public bool ShowDate
        {
            get => _showDate;
            set => SetProperty(ref _showDate, value);
        }

        private string _date;

        public string Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private MonitorManager _monitorManager;

        public MonitorManager MonitorManager
        {
            get => _monitorManager;
            set => SetProperty(ref _monitorManager, value);
        }

        private DispatcherTimer _clockTimer { get; set; }

        private System.Timers.Timer _monitorTimer { get; set; }

        private readonly object _monitorLock = new object();

        private bool _monitorPaused { get; set; }

        private bool _disposed { get; set; } = false;
    }
}
