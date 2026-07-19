using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using SidebarDiagnostics.Utilities;
using SidebarDiagnostics.Monitoring;
using SidebarDiagnostics.Windows;
using SidebarDiagnostics.Framework;

namespace SidebarDiagnostics.Models
{
    public class SettingsModel : PropertyChangedBase
    {
        public SettingsModel(Sidebar sidebar)
        {
            DockEdgeItems = new DockItem[2]
            {
                new DockItem() { Text = Resources.SettingsDockLeft, Value = DockEdge.Left },
                new DockItem() { Text = Resources.SettingsDockRight, Value = DockEdge.Right }
            };

            DockEdge = Framework.Settings.Instance.DockEdge;

            Monitor[] _monitors = Monitor.GetMonitors();

            ScreenItems = _monitors.Select((s, i) => new ScreenItem() { Index = i, Text = string.Format("#{0}", i + 1) }).ToArray();

            if (Framework.Settings.Instance.ScreenIndex < _monitors.Length)
            {
                ScreenIndex = Framework.Settings.Instance.ScreenIndex;
            }
            else
            {
                ScreenIndex = _monitors.Where(s => s.IsPrimary).Select((s, i) => i).Single();
            }

            CultureItems = Utilities.Culture.GetAll();
            Culture = Framework.Settings.Instance.Culture;

            UIScale = Framework.Settings.Instance.UIScale;
            XOffset = Framework.Settings.Instance.XOffset;
            YOffset = Framework.Settings.Instance.YOffset;
            PollingInterval = Framework.Settings.Instance.PollingInterval;
            UseAppBar = Framework.Settings.Instance.UseAppBar;
            AlwaysTop = Framework.Settings.Instance.AlwaysTop;
            ToolbarMode = Framework.Settings.Instance.ToolbarMode;
            ClickThrough = Framework.Settings.Instance.ClickThrough;
            ShowTrayIcon = Framework.Settings.Instance.ShowTrayIcon;
            RunAtStartup = Framework.Settings.Instance.RunAtStartup;
            SidebarWidth = Framework.Settings.Instance.SidebarWidth;
            AutoBGColor = Framework.Settings.Instance.AutoBGColor;
            BGColor = Framework.Settings.Instance.BGColor;
            BGOpacity = Framework.Settings.Instance.BGOpacity;

            TextAlignItems = new TextAlignItem[2]
            {
                new TextAlignItem() { Text = Resources.SettingsTextAlignLeft, Value = TextAlign.Left },
                new TextAlignItem() { Text = Resources.SettingsTextAlignRight, Value = TextAlign.Right }
            };

            TextAlign = Framework.Settings.Instance.TextAlign;

            FontSettingItems = new FontSetting[5]
            {
                FontSetting.x10,
                FontSetting.x12,
                FontSetting.x14,
                FontSetting.x16,
                FontSetting.x18
            };

            FontSetting = Framework.Settings.Instance.FontSetting;
            FontColor = Framework.Settings.Instance.FontColor;
            AlertFontColor = Framework.Settings.Instance.AlertFontColor;
            AlertBlink = Framework.Settings.Instance.AlertBlink;

            DateSettingItems = new DateSetting[4]
            {
                DateSetting.Disabled,
                DateSetting.Short,
                DateSetting.Normal,
                DateSetting.Long
            };

            DateSetting = Framework.Settings.Instance.DateSetting;
            CollapseMenuBar = Framework.Settings.Instance.CollapseMenuBar;
            InitiallyHidden = Framework.Settings.Instance.InitiallyHidden;
            ShowMachineName = Framework.Settings.Instance.ShowMachineName;
            ShowClock = Framework.Settings.Instance.ShowClock;
            Clock24HR = Framework.Settings.Instance.Clock24HR;

            ObservableCollection<MonitorConfig> _config = new ObservableCollection<MonitorConfig>(Framework.Settings.Instance.MonitorConfig.Select(c => c.Clone()).OrderByDescending(c => c.Order));

            if (sidebar.Ready)
            {
                foreach (MonitorConfig _record in _config)
                {
                    _record.HardwareOC = new ObservableCollection<HardwareConfig>(
                        from hw in sidebar.Model.MonitorManager.GetHardware(_record.Type)
                        join config in _record.Hardware on hw.ID equals config.ID into merged
                        from newhw in merged.DefaultIfEmpty(hw).Select(newhw => { newhw.ActualName = hw.ActualName; if (string.IsNullOrEmpty(newhw.Name)) { newhw.Name = hw.ActualName; } return newhw; })
                        orderby newhw.Order descending, newhw.Name ascending
                        select newhw
                        );
                }
            }

            MonitorConfig = _config;

            if (Framework.Settings.Instance.Hotkeys != null)
            {
                ToggleKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Toggle);
                ShowKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Show);
                HideKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Hide);
                ReloadKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Reload);
                CloseKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.Close);
                CycleEdgeKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.CycleEdge);
                CycleScreenKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.CycleScreen);
                ReserveSpaceKey = Framework.Settings.Instance.Hotkeys.FirstOrDefault(k => k.Action == Hotkey.KeyAction.ReserveSpace);
            }

            IsChanged = false;
        }

        public void Save()
        {
            if (!string.Equals(Culture, Framework.Settings.Instance.Culture, StringComparison.Ordinal))
            {
                MessageBox.Show(Resources.LanguageChangedText, Resources.LanguageChangedTitle, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
            }

            Framework.Settings.Instance.DockEdge = DockEdge;
            Framework.Settings.Instance.ScreenIndex = ScreenIndex;
            Framework.Settings.Instance.Culture = Culture;
            Framework.Settings.Instance.UIScale = UIScale;
            Framework.Settings.Instance.XOffset = XOffset;
            Framework.Settings.Instance.YOffset = YOffset;
            Framework.Settings.Instance.PollingInterval = PollingInterval;
            Framework.Settings.Instance.UseAppBar = UseAppBar;
            Framework.Settings.Instance.AlwaysTop = AlwaysTop;
            Framework.Settings.Instance.ToolbarMode = ToolbarMode;
            Framework.Settings.Instance.ClickThrough = ClickThrough;
            Framework.Settings.Instance.ShowTrayIcon = ShowTrayIcon;
            Framework.Settings.Instance.RunAtStartup = RunAtStartup;
            Framework.Settings.Instance.SidebarWidth = SidebarWidth;
            Framework.Settings.Instance.AutoBGColor = AutoBGColor;
            Framework.Settings.Instance.BGColor = BGColor;
            Framework.Settings.Instance.BGOpacity = BGOpacity;
            Framework.Settings.Instance.TextAlign = TextAlign;
            Framework.Settings.Instance.FontSetting = FontSetting;
            Framework.Settings.Instance.FontColor = FontColor;
            Framework.Settings.Instance.AlertFontColor = AlertFontColor;
            Framework.Settings.Instance.AlertBlink = AlertBlink;
            Framework.Settings.Instance.DateSetting = DateSetting;
            Framework.Settings.Instance.CollapseMenuBar = CollapseMenuBar;
            Framework.Settings.Instance.InitiallyHidden = InitiallyHidden;
            Framework.Settings.Instance.ShowMachineName = ShowMachineName;
            Framework.Settings.Instance.ShowClock = ShowClock;
            Framework.Settings.Instance.Clock24HR = Clock24HR;

            MonitorConfig[] _config = MonitorConfig.Select(c => c.Clone()).ToArray();

            for (int i = 0; i < _config.Length; i++)
            {
                // HardwareOC is only populated when the sidebar was ready as this window opened.
                if (_config[i].HardwareOC == null)
                {
                    _config[i].Order = Convert.ToByte(_config.Length - i);
                    continue;
                }

                HardwareConfig[] _hardware = _config[i].HardwareOC.ToArray();

                for (int v = 0; v < _hardware.Length; v++)
                {
                    _hardware[v].Order = Convert.ToByte(_hardware.Length - v);

                    if (string.IsNullOrEmpty(_hardware[v].Name) || string.Equals(_hardware[v].Name, _hardware[v].ActualName, StringComparison.Ordinal))
                    {
                        _hardware[v].Name = null;
                    }
                }

                _config[i].Hardware = _hardware;
                _config[i].HardwareOC = null;

                _config[i].Order = Convert.ToByte(_config.Length - i);
            }

            Framework.Settings.Instance.MonitorConfig = _config;

            List<Hotkey> _hotkeys = new List<Hotkey>();

            if (ToggleKey != null)
            {
                _hotkeys.Add(ToggleKey);
            }
            
            if (ShowKey != null)
            {
                _hotkeys.Add(ShowKey);
            }

            if (HideKey != null)
            {
                _hotkeys.Add(HideKey);
            }

            if (ReloadKey != null)
            {
                _hotkeys.Add(ReloadKey);
            }

            if (CloseKey != null)
            {
                _hotkeys.Add(CloseKey);
            }

            if (CycleEdgeKey != null)
            {
                _hotkeys.Add(CycleEdgeKey);
            }

            if (CycleScreenKey != null)
            {
                _hotkeys.Add(CycleScreenKey);
            }

            if (ReserveSpaceKey != null)
            {
                _hotkeys.Add(ReserveSpaceKey);
            }

            Framework.Settings.Instance.Hotkeys = _hotkeys.ToArray();

            Framework.Settings.Instance.Save();

            App.RefreshIcon();

            if (RunAtStartup)
            {
                Startup.EnableStartupTask();
            }
            else
            {
                Startup.DisableStartupTask();
            }

            IsChanged = false;
        }

        public override void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.NotifyPropertyChanged(propertyName);

            if (propertyName != nameof(IsChanged))
            {
                IsChanged = true;
            }
        }

        private void Child_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IsChanged = true;
        }

        private void Child_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IsChanged = true;
        }

        private bool _isChanged = false;

        public bool IsChanged
        {
            get => _isChanged;
            set => SetProperty(ref _isChanged, value);
        }

        private DockEdge _dockEdge;

        public DockEdge DockEdge
        {
            get => _dockEdge;
            set => SetProperty(ref _dockEdge, value);
        }

        private DockItem[] _dockEdgeItems;

        public DockItem[] DockEdgeItems
        {
            get => _dockEdgeItems;
            set => SetProperty(ref _dockEdgeItems, value);
        }

        private int _screenIndex;

        public int ScreenIndex
        {
            get => _screenIndex;
            set => SetProperty(ref _screenIndex, value);
        }

        private ScreenItem[] _screenItems;

        public ScreenItem[] ScreenItems
        {
            get => _screenItems;
            set => SetProperty(ref _screenItems, value);
        }

        private string _culture;

        public string Culture
        {
            get => _culture;
            set => SetProperty(ref _culture, value);
        }

        private CultureItem[] _cultureItems;

        public CultureItem[] CultureItems
        {
            get => _cultureItems;
            set => SetProperty(ref _cultureItems, value);
        }

        private double _uiScale;

        public double UIScale
        {
            get => _uiScale;
            set => SetProperty(ref _uiScale, value);
        }

        private int _xOffset;

        public int XOffset
        {
            get => _xOffset;
            set => SetProperty(ref _xOffset, value);
        }

        private int _yOffset;

        public int YOffset
        {
            get => _yOffset;
            set => SetProperty(ref _yOffset, value);
        }

        private int _pollingInterval;

        public int PollingInterval
        {
            get => _pollingInterval;
            set => SetProperty(ref _pollingInterval, value);
        }

        private bool _useAppBar;

        public bool UseAppBar
        {
            get => _useAppBar;
            set => SetProperty(ref _useAppBar, value);
        }

        private bool _alwaysTop;

        public bool AlwaysTop
        {
            get => _alwaysTop;
            set => SetProperty(ref _alwaysTop, value);
        }

        private bool _toolbarMode;
        
        public bool ToolbarMode
        {
            get => _toolbarMode;
            set => SetProperty(ref _toolbarMode, value);
        }

        private bool _clickThrough;

        public bool ClickThrough
        {
            get => _clickThrough;
            set => SetProperty(ref _clickThrough, value);
        }

        private bool _showTrayIcon;

        public bool ShowTrayIcon
        {
            get => _showTrayIcon;
            set => SetProperty(ref _showTrayIcon, value);
        }

        private bool _runAtStartup;

        public bool RunAtStartup
        {
            get => _runAtStartup;
            set => SetProperty(ref _runAtStartup, value);
        }

        private int _sidebarWidth;

        public int SidebarWidth
        {
            get => _sidebarWidth;
            set => SetProperty(ref _sidebarWidth, value);
        }

        private bool _autoBGColor;

        public bool AutoBGColor
        {
            get => _autoBGColor;
            set => SetProperty(ref _autoBGColor, value);
        }

        private string _bgColor;

        public string BGColor
        {
            get => _bgColor;
            set => SetProperty(ref _bgColor, value);
        }

        private double _bgOpacity;

        public double BGOpacity
        {
            get => _bgOpacity;
            set => SetProperty(ref _bgOpacity, value);
        }

        private TextAlign _textAlign;

        public TextAlign TextAlign
        {
            get => _textAlign;
            set => SetProperty(ref _textAlign, value);
        }

        private TextAlignItem[] _textAlignItems;

        public TextAlignItem[] TextAlignItems
        {
            get => _textAlignItems;
            set => SetProperty(ref _textAlignItems, value);
        }

        private FontSetting _fontSetting;

        public FontSetting FontSetting
        {
            get => _fontSetting;
            set => SetProperty(ref _fontSetting, value);
        }

        private FontSetting[] _fontSettingItems;

        public FontSetting[] FontSettingItems
        {
            get => _fontSettingItems;
            set => SetProperty(ref _fontSettingItems, value);
        }

        private string _fontColor;

        public string FontColor
        {
            get => _fontColor;
            set => SetProperty(ref _fontColor, value);
        }

        private string _alertFontColor;

        public string AlertFontColor
        {
            get => _alertFontColor;
            set => SetProperty(ref _alertFontColor, value);
        }

        private bool _alertBlink = true;
        
        public bool AlertBlink
        {
            get => _alertBlink;
            set => SetProperty(ref _alertBlink, value);
        }

        private DateSetting _dateSetting;

        public DateSetting DateSetting
        {
            get => _dateSetting;
            set => SetProperty(ref _dateSetting, value);
        }

        private DateSetting[] _dateSettingItems;

        public DateSetting[] DateSettingItems
        {
            get => _dateSettingItems;
            set => SetProperty(ref _dateSettingItems, value);
        }

        private bool _collapseMenuBar;

        public bool CollapseMenuBar
        {
            get => _collapseMenuBar;
            set => SetProperty(ref _collapseMenuBar, value);
        }

        private bool _initiallyHidden;
        
        public bool InitiallyHidden
        {
            get => _initiallyHidden;
            set => SetProperty(ref _initiallyHidden, value);
        }

        private bool _showMachineName = true;

        public bool ShowMachineName
        {
            get => _showMachineName;
            set => SetProperty(ref _showMachineName, value);
        }

        private bool _showClock;

        public bool ShowClock
        {
            get => _showClock;
            set => SetProperty(ref _showClock, value);
        }

        private bool _clock24HR;

        public bool Clock24HR
        {
            get => _clock24HR;
            set => SetProperty(ref _clock24HR, value);
        }

        private ObservableCollection<MonitorConfig> _monitorConfig { get; set; }

        public ObservableCollection<MonitorConfig> MonitorConfig
        {
            get
            {
                return _monitorConfig;
            }
            set
            {
                _monitorConfig = value;

                _monitorConfig.CollectionChanged += Child_CollectionChanged;

                foreach (MonitorConfig _config in _monitorConfig)
                {
                    _config.PropertyChanged += Child_PropertyChanged;

                    _config.HardwareOC.CollectionChanged += Child_CollectionChanged;

                    foreach (HardwareConfig _hardware in _config.HardwareOC)
                    {
                        _hardware.PropertyChanged += Child_PropertyChanged;
                    }

                    foreach (MetricConfig _metric in _config.Metrics)
                    {
                        _metric.PropertyChanged += Child_PropertyChanged;
                    }

                    foreach (ConfigParam _param in _config.Params)
                    {
                        _param.PropertyChanged += Child_PropertyChanged;
                    }
                }

                NotifyPropertyChanged(nameof(MonitorConfig));
            }
        }

        private Hotkey _toggleKey;

        public Hotkey ToggleKey
        {
            get => _toggleKey;
            set => SetProperty(ref _toggleKey, value);
        }

        private Hotkey _showKey;

        public Hotkey ShowKey
        {
            get => _showKey;
            set => SetProperty(ref _showKey, value);
        }

        private Hotkey _hideKey;

        public Hotkey HideKey
        {
            get => _hideKey;
            set => SetProperty(ref _hideKey, value);
        }

        private Hotkey _reloadKey;

        public Hotkey ReloadKey
        {
            get => _reloadKey;
            set => SetProperty(ref _reloadKey, value);
        }

        private Hotkey _closeKey;

        public Hotkey CloseKey
        {
            get => _closeKey;
            set => SetProperty(ref _closeKey, value);
        }

        private Hotkey _cycleEdgeKey;

        public Hotkey CycleEdgeKey
        {
            get => _cycleEdgeKey;
            set => SetProperty(ref _cycleEdgeKey, value);
        }

        private Hotkey _cycleScreenKey;

        public Hotkey CycleScreenKey
        {
            get => _cycleScreenKey;
            set => SetProperty(ref _cycleScreenKey, value);
        }

        private Hotkey _reserveSpaceKey;

        public Hotkey ReserveSpaceKey
        {
            get => _reserveSpaceKey;
            set => SetProperty(ref _reserveSpaceKey, value);
        }
    }

    public class DockItem
    {
        public DockEdge Value { get; set; }

        public string Text { get; set; }
    }

    public class ScreenItem
    {
        public int Index { get; set; }

        public string Text { get; set; }
    }

    public class TextAlignItem
    {
        public TextAlign Value { get; set; }

        public string Text { get; set; }
    }
}