using System;
using System.IO;
using System.ComponentModel;
using Newtonsoft.Json;
using SidebarDiagnostics.Utilities;
using SidebarDiagnostics.Monitoring;
using SidebarDiagnostics.Windows;
using System.Globalization;

namespace SidebarDiagnostics.Framework
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Settings : PropertyChangedBase
    {
        private Settings() { }

        public void Save()
        {
            if (!Directory.Exists(Paths.LocalApp))
            {
                Directory.CreateDirectory(Paths.LocalApp);
            }

            // Write to a temp file first so a crash mid-write can never
            // destroy the previous good copy.
            string _path = Paths.SettingsFile;
            string _temp = _path + ".tmp";

            using (StreamWriter _writer = File.CreateText(_temp))
            {
                new JsonSerializer() { Formatting = Formatting.Indented }.Serialize(_writer, this);
            }

            if (File.Exists(_path))
            {
                File.Replace(_temp, _path, null);
            }
            else
            {
                File.Move(_temp, _path);
            }
        }

        public void Reload()
        {
            _instance = Load();
        }

        private static Settings Load()
        {
            Settings _return = null;

            if (File.Exists(Paths.SettingsFile))
            {
                try
                {
                    using (StreamReader _reader = File.OpenText(Paths.SettingsFile))
                    {
                        _return = (Settings)new JsonSerializer().Deserialize(_reader, typeof(Settings));
                    }
                }
                catch (JsonException)
                {
                    // A corrupted file shouldn't prevent the app from starting.
                    // Set it aside so the user can recover it, and start with defaults.
                    try
                    {
                        File.Move(Paths.SettingsFile, Paths.SettingsFile + ".bak", true);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }

                    _return = null;
                }
            }

            return _return ?? new Settings();
        }

        private string _changeLog = null;

        [JsonProperty]
        public string ChangeLog
        {
            get => _changeLog;
            set => SetProperty(ref _changeLog, value);
        }

        private bool _initialSetup = true;

        [JsonProperty]
        public bool InitialSetup
        {
            get => _initialSetup;
            set => SetProperty(ref _initialSetup, value);
        }

        private DockEdge _dockEdge = DockEdge.Right;

        [JsonProperty]
        public DockEdge DockEdge
        {
            get => _dockEdge;
            set => SetProperty(ref _dockEdge, value);
        }

        private int _screenIndex = 0;

        [JsonProperty]
        public int ScreenIndex
        {
            get => _screenIndex;
            set => SetProperty(ref _screenIndex, value);
        }

        private string _culture = Utilities.Culture.DEFAULT;

        [JsonProperty]
        public string Culture
        {
            get => _culture;
            set => SetProperty(ref _culture, value);
        }

        private bool _useAppBar = true;
        
        [JsonProperty]
        public bool UseAppBar
        {
            get => _useAppBar;
            set => SetProperty(ref _useAppBar, value);
        }

        private bool _alwaysTop = true;

        [JsonProperty]
        public bool AlwaysTop
        {
            get => _alwaysTop;
            set => SetProperty(ref _alwaysTop, value);
        }

        private bool _runAtStartup = true;

        [JsonProperty]
        public bool RunAtStartup
        {
            get => _runAtStartup;
            set => SetProperty(ref _runAtStartup, value);
        }

        private double _uiScale = 1d;

        [JsonProperty]
        public double UIScale
        {
            get => _uiScale;
            set => SetProperty(ref _uiScale, value);
        }

        private int _xOffset = 0;

        [JsonProperty]
        public int XOffset
        {
            get => _xOffset;
            set => SetProperty(ref _xOffset, value);
        }

        private int _yOffset = 0;

        [JsonProperty]
        public int YOffset
        {
            get => _yOffset;
            set => SetProperty(ref _yOffset, value);
        }

        private int _pollingInterval = 1000;

        [JsonProperty]
        public int PollingInterval
        {
            get => _pollingInterval;
            set => SetProperty(ref _pollingInterval, value);
        }

        private bool _toolbarMode = true;

        [JsonProperty]
        public bool ToolbarMode
        {
            get => _toolbarMode;
            set => SetProperty(ref _toolbarMode, value);
        }

        private bool _clickThrough = false;

        [JsonProperty]
        public bool ClickThrough
        {
            get => _clickThrough;
            set => SetProperty(ref _clickThrough, value);
        }

        private bool _showTrayIcon = true;

        [JsonProperty]
        public bool ShowTrayIcon
        {
            get => _showTrayIcon;
            set => SetProperty(ref _showTrayIcon, value);
        }

        private bool _collapseMenuBar = false;

        [JsonProperty]
        public bool CollapseMenuBar
        {
            get => _collapseMenuBar;
            set => SetProperty(ref _collapseMenuBar, value);
        }

        private bool _initiallyHidden = false;

        [JsonProperty]
        public bool InitiallyHidden
        {
            get => _initiallyHidden;
            set => SetProperty(ref _initiallyHidden, value);
        }

        private int _sidebarWidth = 180;

        [JsonProperty]
        public int SidebarWidth
        {
            get => _sidebarWidth;
            set => SetProperty(ref _sidebarWidth, value);
        }

        private bool _autoBGColor = false;

        [JsonProperty]
        public bool AutoBGColor
        {
            get => _autoBGColor;
            set => SetProperty(ref _autoBGColor, value);
        }

        private string _bgColor = "#000000";

        [JsonProperty]
        public string BGColor
        {
            get => _bgColor;
            set => SetProperty(ref _bgColor, value);
        }

        private double _bgOpacity = 0.85d;

        [JsonProperty]
        public double BGOpacity
        {
            get => _bgOpacity;
            set => SetProperty(ref _bgOpacity, value);
        }

        private TextAlign _textAlign = TextAlign.Left;

        [JsonProperty]
        public TextAlign TextAlign
        {
            get => _textAlign;
            set => SetProperty(ref _textAlign, value);
        }

        private FontSetting _fontSetting = FontSetting.x14;

        [JsonProperty]
        public FontSetting FontSetting
        {
            get => _fontSetting;
            set => SetProperty(ref _fontSetting, value);
        }

        private string _fontColor = "#FFFFFF";
        
        [JsonProperty]
        public string FontColor
        {
            get => _fontColor;
            set => SetProperty(ref _fontColor, value);
        }

        private string _alertFontColor = "#FF4136";

        [JsonProperty]
        public string AlertFontColor
        {
            get => _alertFontColor;
            set => SetProperty(ref _alertFontColor, value);
        }

        private bool _alertBlink = true;

        [JsonProperty]
        public bool AlertBlink
        {
            get => _alertBlink;
            set => SetProperty(ref _alertBlink, value);
        }

        private bool _showMachineName = false;

        [JsonProperty]
        public bool ShowMachineName
        {
            get => _showMachineName;
            set => SetProperty(ref _showMachineName, value);
        }

        private bool _showClock = true;

        [JsonProperty]
        public bool ShowClock
        {
            get => _showClock;
            set => SetProperty(ref _showClock, value);
        }

        private bool _clock24HR = false;

        [JsonProperty]
        public bool Clock24HR
        {
            get => _clock24HR;
            set => SetProperty(ref _clock24HR, value);
        }

        private DateSetting _dateSetting = DateSetting.Short;

        [JsonProperty]
        public DateSetting DateSetting
        {
            get => _dateSetting;
            set => SetProperty(ref _dateSetting, value);
        }

        private MonitorConfig[] _monitorConfig = null;

        [JsonProperty]
        public MonitorConfig[] MonitorConfig
        {
            get => _monitorConfig;
            set => SetProperty(ref _monitorConfig, value);
        }

        private Hotkey[] _hotkeys = new Hotkey[0];

        [JsonProperty]
        public Hotkey[] Hotkeys
        {
            get => _hotkeys;
            set => SetProperty(ref _hotkeys, value);
        }

        private static Settings _instance { get; set; } = null;

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }

                return _instance;
            }
        }
    }

    public enum TextAlign : byte
    {
        Left,
        Right
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class FontSetting
    {
        internal FontSetting() { }

        private FontSetting(int fontSize)
        {
            FontSize = fontSize;
        }

        public override bool Equals(object obj)
        {
            FontSetting _that = obj as FontSetting;

            if (_that == null)
            {
                return false;
            }

            return this.FontSize == _that.FontSize;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static FontSetting x10
        {
            get
            {
                return new FontSetting(10);
            }
        }

        public static FontSetting x12
        {
            get
            {
                return new FontSetting(12);
            }
        }

        public static FontSetting x14
        {
            get
            {
                return new FontSetting(14);
            }
        }

        public static FontSetting x16
        {
            get
            {
                return new FontSetting(16);
            }
        }

        public static FontSetting x18
        {
            get
            {
                return new FontSetting(18);
            }
        }

        [JsonProperty]
        public int FontSize { get; set; }

        public int TitleFontSize
        {
            get
            {
                return FontSize + 2;
            }
        }

        public int SmallFontSize
        {
            get
            {
                return FontSize - 2;
            }
        }

        public int IconSize
        {
            get
            {
                switch (FontSize)
                {
                    case 10:
                        return 18;

                    case 12:
                        return 22;

                    case 14:
                    default:
                        return 24;

                    case 16:
                        return 28;

                    case 18:
                        return 32;
                }
            }
        }

        public int BarHeight
        {
            get
            {
                return FontSize - 3;
            }
        }

        public int BarWidth
        {
            get
            {
                return BarHeight * 6;
            }
        }

        public int BarWidthWide
        {
            get
            {
                return BarHeight * 8;
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class DateSetting
    {
        internal DateSetting() { }

        private DateSetting(string format)
        {
            Format = format;
        }

        [JsonProperty]
        public string Format { get; set; }

        public string Display
        {
            get
            {
                if (string.Equals(Format, "Disabled", StringComparison.Ordinal))
                {
                    return Resources.SettingsDateFormatDisabled;
                }

                return DateTime.Today.ToString(Format, Culture.CultureInfo);
            }
        }

        public override bool Equals(object obj)
        {
            DateSetting _that = obj as DateSetting;

            if (_that == null)
            {
                return false;
            }

            return string.Equals(this.Format, _that.Format, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static readonly DateSetting Disabled = new DateSetting("Disabled");
        public static readonly DateSetting Short = new DateSetting("M");
        public static readonly DateSetting Normal = new DateSetting("d");
        public static readonly DateSetting Long = new DateSetting("D");
    }
}
