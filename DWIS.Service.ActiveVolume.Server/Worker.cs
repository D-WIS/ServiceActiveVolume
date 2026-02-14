using DWIS.Client.ReferenceImplementation.OPCFoundation;
using DWIS.RigOS.Common.Worker;
using DWIS.Service.ActiveVolume.Model;
using System.Reflection;

namespace DWIS.Service.ActiveVolume.Server
{
    public class Worker : DWISWorker<ConfigurationForActiveVolume>
    {

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
                await RegisterToBlackboard(RealtimeOutputsData);
                await Loop(stoppingToken);
            }
        }

        protected override async Task Loop(CancellationToken stoppingToken)
        {
            PeriodicTimer timer = new PeriodicTimer(LoopSpan);
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
                }
                catch (Exception e)
                {
                    Logger?.LogError(e.ToString());
                }
                ConfigurationUpdater<ConfigurationForActiveVolume>.Instance.UpdateConfiguration(this);
            }
        }

    }
}
