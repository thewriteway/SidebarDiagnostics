using System;
using LibreHardwareMonitor.Hardware;

namespace SidebarDiagnostics.Monitoring
{
    /// <summary>
    /// GPU power draw in watts for NVIDIA adapters. Reads NVML first (same source
    /// as nvidia-smi) and falls back to a LibreHardwareMonitor power sensor when
    /// NVML is unavailable.
    /// </summary>
    public class NvidiaPowerMetric : BaseMetric
    {
        public NvidiaPowerMetric(int adapterIndex, ISensor fallbackSensor, MetricKey key, DataType dataType, string label = null, bool round = false, double alertValue = 0, iConverter converter = null) : base(key, dataType, label, round, alertValue, converter)
        {
            _adapterIndex = adapterIndex;
            _fallbackSensor = fallbackSensor;
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
                    _fallbackSensor = null;
                }

                _disposed = true;
            }
        }

        ~NvidiaPowerMetric()
        {
            Dispose(false);
        }

        public override void Update()
        {
            double _watts;

            if (Nvml.TryGetPowerUsage(_adapterIndex, out _watts))
            {
                Update(_watts);
                return;
            }

            if (_fallbackSensor != null && _fallbackSensor.Value.HasValue)
            {
                Update(_fallbackSensor.Value.Value);
                return;
            }

            Text = "No Value";
        }

        private int _adapterIndex { get; set; }

        private ISensor _fallbackSensor { get; set; }

        private bool _disposed { get; set; } = false;
    }
}
