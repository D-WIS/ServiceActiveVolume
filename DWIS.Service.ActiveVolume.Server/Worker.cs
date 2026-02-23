using DWIS.Client.ReferenceImplementation.OPCFoundation;
using DWIS.RigOS.Common.Worker;
using DWIS.Service.ActiveVolume.Model;
using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using System.Text.Json;
using System.Reflection;

namespace DWIS.Service.ActiveVolume.Server
{
    public class Worker : DWISWorker<ConfigurationForActiveVolume>
    {

        private RealtimeInputsData RealtimeInputsData { get; set; } = new RealtimeInputsData();
        private RealtimeOutputsData RealtimeOutputsData { get; set; } = new RealtimeOutputsData();
        private readonly List<RealtimeDataSample> _processLog = new();
        private DateTimeOffset? _nextDumpUtc;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

        public Worker(ILogger<IDWISWorker<ConfigurationForActiveVolume>> logger, ILogger<DWISClientOPCF>? loggerDWISClient) : base(logger, loggerDWISClient)
        {
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ConnectToBlackboard();
            if (Configuration is not null && _DWISClient != null && _DWISClient.Connected)
            {
                await RegisterQueries(RealtimeInputsData);
                await RegisterToBlackboard(RealtimeOutputsData);
                await Loop(stoppingToken);
            }
        }

        protected override async Task Loop(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new PeriodicTimer(LoopSpan);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        await ReadBlackboardAsync(RealtimeInputsData, stoppingToken);
                        if (Configuration is not null)
                        {
                           SensorFusion.FuseData(Configuration, RealtimeInputsData, RealtimeOutputsData);
                        }
                        await PublishBlackboardAsync(RealtimeOutputsData, stoppingToken);
                        lock (_lock)
                        {
                            if (Logger is not null && Logger.IsEnabled(LogLevel.Information))
                            {
                                if (RealtimeInputsData.FlowrateIn is not null &&
                                    RealtimeInputsData.FlowrateIn.Value is not null)
                                {
                                    Logger.LogInformation("Flowrate in: " + (RealtimeInputsData.FlowrateIn.Value.Value * 60000.0).ToString("F3") + " L/min");
                                }
                                if (RealtimeInputsData.ActiveVolume is not null &&
                                    RealtimeInputsData.ActiveVolume.Value is not null)
                                {
                                    Logger.LogInformation("Active Volume: " + RealtimeInputsData.ActiveVolume.Value.Value.ToString("F3") + " m^3");

                                }
                                double flowrateOutProportion = 0.0;
                                if (RealtimeInputsData.ShakerLoadEstimates is not null && RealtimeInputsData.ShakerLoadEstimates.Values is not null)
                                {
                                    int count = 0;
                                    foreach (var shakerLoadEstimate in RealtimeInputsData.ShakerLoadEstimates.Values)
                                    {
                                        if (shakerLoadEstimate is not null && shakerLoadEstimate.Mean is not null)
                                        {
                                            flowrateOutProportion += shakerLoadEstimate.Mean.Value / 10.0;
                                            count++;
                                        }
                                    }
                                    if (count > 0)
                                    {
                                        flowrateOutProportion /= count;
                                    }
                                }
                                Logger.LogInformation("Flowrate out proportion: " + (flowrateOutProportion*100.0).ToString("F3") + " %");
                                double cuttingsFlowrate = 0.0;
                                if (RealtimeInputsData.CuttingsRecoveryRates is not null && RealtimeInputsData.CuttingsRecoveryRates.Values is not null)
                                {
                                    foreach (var cuttingsRecoveryRate in RealtimeInputsData.CuttingsRecoveryRates.Values)
                                    {
                                        if (cuttingsRecoveryRate is not null && cuttingsRecoveryRate.Mean is not null)
                                        {
                                            cuttingsFlowrate += cuttingsRecoveryRate.Mean.Value;
                                        }
                                    }
                                }
                                Logger.LogInformation("Cuttings flowrate: " + (cuttingsFlowrate * 60000.0).ToString("F3") + " L/min");

                                if (RealtimeOutputsData.CorrectedActiveVolume is not null &&
                                    RealtimeOutputsData.CorrectedActiveVolume.Value is not null)
                                {
                                    Logger.LogInformation("Corrected Active Volume: " + RealtimeOutputsData.CorrectedActiveVolume.Value.Value.ToString("F3") + "m^3");
                                }
                                if (RealtimeOutputsData.EstimatedPitVolumeFlowBias is not null &&
                                    RealtimeOutputsData.EstimatedPitVolumeFlowBias.Value is not null)
                                {
                                    Logger.LogInformation("Estimated Pit Volume Flow Bias: " + (RealtimeOutputsData.EstimatedPitVolumeFlowBias.Value.Value * 60000.0).ToString("F3") + " L/min");
                                }
                                if (RealtimeOutputsData.ReturnFlowCapacityScale is not null &&
                                    RealtimeOutputsData.ReturnFlowCapacityScale.Value is not null)
                                {
                                    Logger.LogInformation("Return Flow Capacity Scale: " + (RealtimeOutputsData.ReturnFlowCapacityScale.Value.Value * 60000.0).ToString("F3") + " L/min");
                                }
                            }
                        }

                        await TryDumpProcessLogIfDueAsync(stoppingToken);
                    }
                    catch (Exception e)
                    {
                        Logger?.LogError(e.ToString());
                    }
                    ConfigurationUpdater<ConfigurationForActiveVolume>.Instance.UpdateConfiguration(this);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }

            await ForceDumpProcessLogAsync();
        }

        private async Task TryDumpProcessLogIfDueAsync(CancellationToken cancellationToken)
        {
            if (Configuration is null || !Configuration.EnableRealtimeDataDump)
            {
                return;
            }

            TimeSpan interval = GetValidatedDumpInterval(Configuration.RealtimeDataDumpInterval);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            _processLog.Add(CreateSample(now));

            if (_nextDumpUtc is null)
            {
                _nextDumpUtc = GetNextBoundary(now, interval);
            }

            if (now < _nextDumpUtc)
            {
                return;
            }

            await DumpProcessLogAsync(interval, _nextDumpUtc.Value, cancellationToken);
            _processLog.Clear();
            _nextDumpUtc = GetNextBoundary(now, interval);
        }

        private async Task ForceDumpProcessLogAsync()
        {
            if (_processLog.Count == 0 || Configuration is null || !Configuration.EnableRealtimeDataDump)
            {
                return;
            }

            TimeSpan interval = GetValidatedDumpInterval(Configuration.RealtimeDataDumpInterval);
            DateTimeOffset dumpBoundary = _nextDumpUtc ?? DateTimeOffset.UtcNow;
            await DumpProcessLogAsync(interval, dumpBoundary, CancellationToken.None);
            _processLog.Clear();
        }

        private async Task DumpProcessLogAsync(TimeSpan interval, DateTimeOffset dumpBoundary, CancellationToken cancellationToken)
        {
            if (Configuration is null)
            {
                return;
            }

            string dumpDirectory = string.IsNullOrWhiteSpace(Configuration.RealtimeDataDumpDirectory) ? "/home" : Configuration.RealtimeDataDumpDirectory;
            Directory.CreateDirectory(dumpDirectory);

            var payload = new RealtimeDataDumpPayload
            {
                DumpTimestampUtc = DateTimeOffset.UtcNow,
                DumpInterval = interval,
                Samples = _processLog.ToArray()
            };

            string fileName = $"activevolume-realtime-{dumpBoundary:yyyyMMddTHHmmssZ}.json";
            string filePath = Path.Combine(dumpDirectory, fileName);
            string jsonPayload = JsonSerializer.Serialize(payload, _jsonSerializerOptions);
            await File.WriteAllTextAsync(filePath, jsonPayload, cancellationToken);

            Logger?.LogInformation("Realtime input/output samples dumped to {FilePath} ({Count} samples).", filePath, payload.Samples.Length);
        }

        private static TimeSpan GetValidatedDumpInterval(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
            {
                return TimeSpan.FromHours(1);
            }

            return interval;
        }

        private static DateTimeOffset GetNextBoundary(DateTimeOffset now, TimeSpan interval)
        {
            long ticks = interval.Ticks;
            long nextTicks = ((now.UtcTicks / ticks) + 1) * ticks;
            return new DateTimeOffset(nextTicks, TimeSpan.Zero);
        }

        private RealtimeDataSample CreateSample(DateTimeOffset timestampUtc)
        {
            return new RealtimeDataSample
            {
                TimestampUtc = timestampUtc,
                Inputs = new RealtimeInputsSnapshot
                {
                    ActiveVolume = GetScalarValue(RealtimeInputsData.ActiveVolume),
                    FlowrateIn = GetScalarValue(RealtimeInputsData.FlowrateIn),
                    ShakerLoadEstimates = GetGaussianValues(RealtimeInputsData.ShakerLoadEstimates),
                    CuttingsRecoveryRates = GetGaussianValues(RealtimeInputsData.CuttingsRecoveryRates)
                },
                Outputs = new RealtimeOutputsSnapshot
                {
                    CorrectedActiveVolume = GetScalarValue(RealtimeOutputsData.CorrectedActiveVolume),
                    EstimatedPitVolumeFlowBias = GetScalarValue(RealtimeOutputsData.EstimatedPitVolumeFlowBias),
                    ReturnFlowCapacityScale = GetScalarValue(RealtimeOutputsData.ReturnFlowCapacityScale)
                }
            };
        }

        private static double? GetScalarValue(ScalarProperty? property)
        {
            return property?.Value;
        }

        private static GaussianValueSnapshot[] GetGaussianValues(GaussianValuesProperty? property)
        {
            if (property?.Values is null)
            {
                return Array.Empty<GaussianValueSnapshot>();
            }

            return property.Values
                .Select(v => new GaussianValueSnapshot
                {
                    Mean = v?.Mean,
                    StandardDeviation = v?.StandardDeviation
                })
                .ToArray();
        }

        private sealed class RealtimeDataDumpPayload
        {
            public DateTimeOffset DumpTimestampUtc { get; set; }
            public TimeSpan DumpInterval { get; set; }
            public RealtimeDataSample[] Samples { get; set; } = Array.Empty<RealtimeDataSample>();
        }

        private sealed class RealtimeDataSample
        {
            public DateTimeOffset TimestampUtc { get; set; }
            public RealtimeInputsSnapshot Inputs { get; set; } = new RealtimeInputsSnapshot();
            public RealtimeOutputsSnapshot Outputs { get; set; } = new RealtimeOutputsSnapshot();
        }

        private sealed class RealtimeInputsSnapshot
        {
            public double? ActiveVolume { get; set; }
            public double? FlowrateIn { get; set; }
            public GaussianValueSnapshot[] ShakerLoadEstimates { get; set; } = Array.Empty<GaussianValueSnapshot>();
            public GaussianValueSnapshot[] CuttingsRecoveryRates { get; set; } = Array.Empty<GaussianValueSnapshot>();
        }

        private sealed class RealtimeOutputsSnapshot
        {
            public double? CorrectedActiveVolume { get; set; }
            public double? EstimatedPitVolumeFlowBias { get; set; }
            public double? ReturnFlowCapacityScale { get; set; }
        }

        private sealed class GaussianValueSnapshot
        {
            public double? Mean { get; set; }
            public double? StandardDeviation { get; set; }
        }

    }
}
