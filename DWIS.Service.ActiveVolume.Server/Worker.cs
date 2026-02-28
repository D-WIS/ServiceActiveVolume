using DWIS.Client.ReferenceImplementation.OPCFoundation;
using DWIS.RigOS.Common.Worker;
using DWIS.Service.ActiveVolume.Model;
using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using System.Text.Json;
using System.Reflection;

namespace DWIS.Service.ActiveVolume.Server
{
    public class RealtimeDataDumpPayload
    {
        public DateTimeOffset DumpTimestampUtc { get; set; }
        public TimeSpan DumpInterval { get; set; }
        public RealtimeDataSample[] Samples { get; set; } = Array.Empty<RealtimeDataSample>();
    }

    public class RealtimeDataSample
    {
        public DateTimeOffset TimestampUtc { get; set; }
        public RealtimeInputsSnapshot Inputs { get; set; } = new RealtimeInputsSnapshot();
        public RealtimeOutputsSnapshot Outputs { get; set; } = new RealtimeOutputsSnapshot();
    }

    public class RealtimeInputsSnapshot
    {
        public double? ActiveVolume { get; set; }
        public double? FlowrateIn { get; set; }
        public double[] ShakerLoadEstimates { get; set; } = Array.Empty<double>();
        public double[] CuttingsRecoveryRates { get; set; } = Array.Empty<double>();
    }

    public class RealtimeOutputsSnapshot
    {
        public double? CorrectedActiveVolume { get; set; }
        public double? EstimatedPitVolumeFlowBias { get; set; }
        public double? ReturnFlowCapacityScale { get; set; }
    }

    public class GaussianValueSnapshot
    {
        public double? Mean { get; set; }
        public double? StandardDeviation { get; set; }
    }
    public class Worker : DWISWorker<ConfigurationForActiveVolume, RealtimeDataSample>
    {
        private string Prefix { get; set; } = "ActiveVolumeCorrection";

        private RealtimeInputsData RealtimeInputsData { get; set; } = new RealtimeInputsData();
        private RealtimeOutputsData RealtimeOutputsData { get; set; } = new RealtimeOutputsData();

        public Worker(ILogger<IDWISWorker<ConfigurationForActiveVolume>> logger, ILogger<DWISClientOPCF>? loggerDWISClient) : base(logger, loggerDWISClient)
        {
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ConnectToBlackboard();
            if (Configuration is not null && _DWISClient != null && _DWISClient.Connected)
            {
                await RegisterQueries(RealtimeInputsData);
                await RegisterToBlackboard(RealtimeOutputsData, false);
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
                                if (RealtimeInputsData.ShakerLoadEstimates is not null && RealtimeInputsData.ShakerLoadEstimates.Value is not null)
                                {
                                    int count = 0;
                                    foreach (var shakerLoadEstimate in RealtimeInputsData.ShakerLoadEstimates.Value)
                                    {
                                        flowrateOutProportion += shakerLoadEstimate / 10.0;
                                        count++;
                                    }
                                    if (count > 0)
                                    {
                                        flowrateOutProportion /= count;
                                    }
                                }
                                Logger.LogInformation("Flowrate out proportion: " + (flowrateOutProportion * 100.0).ToString("F3") + " %");
                                double cuttingsFlowrate = 0.0;
                                if (RealtimeInputsData.CuttingsRecoveryRates is not null && RealtimeInputsData.CuttingsRecoveryRates.Value is not null)
                                {
                                    foreach (var cuttingsRecoveryRate in RealtimeInputsData.CuttingsRecoveryRates.Value)
                                    {
                                            cuttingsFlowrate += cuttingsRecoveryRate;
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

                        await TryDumpProcessLogIfDueAsync(Prefix,stoppingToken);
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

            await ForceDumpProcessLogAsync(Prefix);
        }


        protected override RealtimeDataSample CreateSample(DateTimeOffset timestampUtc, ILogger<IDWISWorker<ConfigurationForActiveVolume>>? logger)
        {
            return new RealtimeDataSample
            {
                TimestampUtc = timestampUtc,
                Inputs = new RealtimeInputsSnapshot
                {
                    ActiveVolume = GetScalarValue(RealtimeInputsData.ActiveVolume),
                    FlowrateIn = GetScalarValue(RealtimeInputsData.FlowrateIn),
                    ShakerLoadEstimates = GetScalarValues(RealtimeInputsData.ShakerLoadEstimates),
                    CuttingsRecoveryRates = GetScalarValues(RealtimeInputsData.CuttingsRecoveryRates)
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

        private static double[] GetScalarValues(ScalarsProperty? property)
        {
            if (property?.Value is null)
            {
                return Array.Empty<double>();
            }

            return property.Value.ToArray();
        }
    }
}
