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
    public class NetworkMonitor : BaseMonitor
    {
        private const string CATEGORYNAME = "Network Interface";

        private const string BYTESRECEIVEDPERSECOND = "Bytes Received/sec";
        private const string BYTESSENTPERSECOND = "Bytes Sent/sec";

        public NetworkMonitor(string id, string name, MetricConfig[] metrics, bool showName = true, bool roundAll = false, bool useBytes = false, double bandwidthInAlert = 0, double bandwidthOutAlert = 0) : base(id, name, showName)
        {
            iConverter _converter;

            if (useBytes)
            {
                _converter = BytesPerSecondConverter.Instance;
            }
            else
            {
                _converter = BitsPerSecondConverter.Instance;
            }

            List<iMetric> _metrics = new List<iMetric>();

            if (metrics.IsEnabled(MetricKey.NetworkIP))
            {
                string _ipAddress = GetAdapterIPAddress(name);

                if (!string.IsNullOrEmpty(_ipAddress))
                {
                    _metrics.Add(new IPMetric(_ipAddress, MetricKey.NetworkIP, DataType.IP));
                }
            }

            if (metrics.IsEnabled(MetricKey.NetworkExtIP))
            {
                // Filled in asynchronously by GetInstances once the lookup completes.
                _metrics.Add(new IPMetric("...", MetricKey.NetworkExtIP, DataType.IP));
            }

            if (metrics.IsEnabled(MetricKey.NetworkIn))
            {
                _metrics.Add(new PCMetric(new PerformanceCounter(CATEGORYNAME, BYTESRECEIVEDPERSECOND, id), MetricKey.NetworkIn, DataType.kbps, null, roundAll, bandwidthInAlert, _converter));
            }

            if (metrics.IsEnabled(MetricKey.NetworkOut))
            {
                _metrics.Add(new PCMetric(new PerformanceCounter(CATEGORYNAME, BYTESSENTPERSECOND, id), MetricKey.NetworkOut, DataType.kbps, null, roundAll, bandwidthOutAlert, _converter));
            }

            Metrics = _metrics.ToArray();
        }

        ~NetworkMonitor()
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

            Regex _regex = new Regex(@"^isatap.*$");

            return _instances.Where(i => !_regex.IsMatch(i)).OrderBy(h => h).Select(h => new HardwareConfig() { ID = h, Name = h, ActualName = h });
        }

        public static iMonitor[] GetInstances(HardwareConfig[] hardwareConfig, MetricConfig[] metrics, ConfigParam[] parameters)
        {
            bool _showName = parameters.GetValue<bool>(ParamKey.HardwareNames);
            bool _roundAll = parameters.GetValue<bool>(ParamKey.RoundAll);
            bool _useBytes = parameters.GetValue<bool>(ParamKey.UseBytes);
            int _bandwidthInAlert = parameters.GetValue<int>(ParamKey.BandwidthInAlert);
            int _bandwidthOutAlert = parameters.GetValue<int>(ParamKey.BandwidthOutAlert);

            NetworkMonitor[] _instances = (
                from hw in GetHardware()
                join c in hardwareConfig on hw.ID equals c.ID into merged
                from n in merged.DefaultIfEmpty(hw).Select(n => { n.ActualName = hw.Name; return n; })
                where n.Enabled
                orderby n.Order descending, n.Name ascending
                select new NetworkMonitor(n.ID, n.Name ?? n.ActualName, metrics, _showName, _roundAll, _useBytes, _bandwidthInAlert, _bandwidthOutAlert)
                ).ToArray();

            if (metrics.IsEnabled(MetricKey.NetworkExtIP))
            {
                IPMetric[] _extIPMetrics = _instances.SelectMany(i => i.Metrics).OfType<IPMetric>().Where(m => m.Key == MetricKey.NetworkExtIP).ToArray();

                if (_extIPMetrics.Length > 0)
                {
                    // Resolve in the background so a slow lookup can't stall startup or reload.
                    GetExternalIPAddressAsync().ContinueWith(t =>
                    {
                        string _ip = t.Status == TaskStatus.RanToCompletion && !string.IsNullOrEmpty(t.Result) ? t.Result : "-";

                        App _app = App.Current;

                        if (_app == null)
                        {
                            return;
                        }

                        _app.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            foreach (IPMetric _metric in _extIPMetrics)
                            {
                                _metric.SetText(_ip);
                            }
                        }));
                    });
                }
            }

            return _instances;
        }

        private static string GetAdapterIPAddress(string name)
        {
            //Here we need to match the apdapter returned by the network interface to the
            //adapter represented by this instance of the class.

            string configuredName = Regex.Replace(name, @"[^\w\d\s]", "");

            foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces())
            {
                //Strange pattern matching as the Performance Monitor routines which provide the ID and Names
                //instantiating this class return different values for the devices than the NetworkInterface calls used here.
                //For example Performance Monitor routines return Intel[R] where as NetworkInterface returns Intel(R) causing the
                //strings not to match.  So to get around this, use Regex to strip off the special characters and just compare the string values.
                //Also, in some cases the values for Description match the Performance Monitor calls, and 
                //in others the Name is what matches.  It's a little weird, but this will pick up all 4 network adapters on 
                //my test machine correctly.

                string interfaceDesc = Regex.Replace(netif.Description, @"[^\w\d\s]", "");
                string interfaceName = Regex.Replace(netif.Name, @"[^\w\d\s]", "");

                if (interfaceDesc == configuredName || interfaceName == configuredName)
                {
                    IPInterfaceProperties properties = netif.GetIPProperties();

                    foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return unicast.Address.ToString();
                        }
                    }
                }
            }

            return null;
        }

        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static async Task<string> GetExternalIPAddressAsync()
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, Constants.URLs.IPIFY);
                var res = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead)
                                    .ConfigureAwait(false);
                res.EnsureSuccessStatusCode();

                var ip = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                return ip.Trim();
            }
            catch (HttpRequestException)
            {
                return "";
            }
            catch (TaskCanceledException) // timeout or cancellation
            {
                return "";
            }
        }
    }
}
