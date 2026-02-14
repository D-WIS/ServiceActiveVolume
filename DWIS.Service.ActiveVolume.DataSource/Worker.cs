using DWIS.Client.ReferenceImplementation.OPCFoundation;
using DWIS.RigOS.Common.Worker;
using DWIS.Service.ActiveVolume.Model;

namespace DWIS.Service.ActiveVolume.DataSource
{
    public class Worker : DWISWorker<Configuration>
    {
        private RealtimeInputsData RealtimeInputsData { get; set; } = new RealtimeInputsData();

        public Worker(ILogger<IDWISWorker<Configuration>> logger, ILogger<DWISClientOPCF>? loggerDWISClient) : base(logger, loggerDWISClient)
        {
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ConnectToBlackboard();
            if (_DWISClient != null && _DWISClient.Connected)
            {
                await RegisterToBlackboard(RealtimeInputsData);
                await Loop(stoppingToken);
            }
        }

        protected override async Task Loop(CancellationToken cancellationToken)
        {
            PeriodicTimer timer = new PeriodicTimer(LoopSpan);
            double flowrateIn = 2000.0/60000.0; // 2000 L/min converted to m^3/s
            double flowrateOut = flowrateIn;
            double activeVolumePeriod = 15.0; // seconds
            double activeVolumeAmplitude = 1.0; // m^3  
            double activeVolume = 30.0; // m^3
            double scalingFactor = 3000.0/60000.0; // scaling factor for flowrate out proportion in m^3/s
            double cuttingsFlowrate = 30.0/60000.0; // 20 L/min converted to m^3/s
            double cuttingsFlowrateStandardDeviation = 1.0 / 60000.0;
            double shakerLoadStandardDeviation = 0.1;
            double t = 0;
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (RealtimeInputsData.FlowrateIn is null)
                {
                    RealtimeInputsData.FlowrateIn = new ScalarProperty();
                }
                RealtimeInputsData.FlowrateIn.Value = flowrateIn;
                activeVolume += LoopSpan.TotalSeconds * (-flowrateIn + flowrateOut - cuttingsFlowrate);
                if (RealtimeInputsData.ActiveVolume is null)
                {
                    RealtimeInputsData.ActiveVolume = new ScalarProperty();
                }
                RealtimeInputsData.ActiveVolume.Value = activeVolume + activeVolumeAmplitude * Math.Sin(t * 2 * Math.PI / activeVolumePeriod);
                if (RealtimeInputsData.CuttingsRecoveryRates is null)
                {
                    RealtimeInputsData.CuttingsRecoveryRates = new GaussianValuesProperty();
                }
                if (RealtimeInputsData.CuttingsRecoveryRates.Values == null)
                {
                    RealtimeInputsData.CuttingsRecoveryRates.Values = new List<GaussianValue>();
                }
                RealtimeInputsData.CuttingsRecoveryRates.Values.Clear();
                RealtimeInputsData.CuttingsRecoveryRates.Values.Add(new GaussianValue() { Mean = 0.5 * cuttingsFlowrate, StandardDeviation = cuttingsFlowrateStandardDeviation });
                RealtimeInputsData.CuttingsRecoveryRates.Values.Add(new GaussianValue() { Mean = 0.5 * cuttingsFlowrate, StandardDeviation = cuttingsFlowrateStandardDeviation });
                if (RealtimeInputsData.ShakerLoadEstimates is null)
                {
                    RealtimeInputsData.ShakerLoadEstimates = new GaussianValuesProperty();
                }
                if (RealtimeInputsData.ShakerLoadEstimates.Values == null)
                {
                    RealtimeInputsData.ShakerLoadEstimates.Values = new List<GaussianValue>();
                }
                double totalShakerLoadEstimates = flowrateOut / scalingFactor;
                RealtimeInputsData.ShakerLoadEstimates.Values.Clear();
                RealtimeInputsData.ShakerLoadEstimates.Values.Add(new GaussianValue() { Mean = 0.5 * totalShakerLoadEstimates * 10.0, StandardDeviation = shakerLoadStandardDeviation });
                RealtimeInputsData.ShakerLoadEstimates.Values.Add(new GaussianValue() { Mean = 0.5 * totalShakerLoadEstimates * 10.0, StandardDeviation = shakerLoadStandardDeviation });
                t += LoopSpan.TotalSeconds;
                await PublishBlackboardAsync(RealtimeInputsData, cancellationToken);
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
                        Logger.LogInformation("Flowrate out proportion: " + (flowrateOutProportion * 100.0).ToString("F3") + " %");
                        double totalCuttingsFlowrate = 0.0;
                        if (RealtimeInputsData.CuttingsRecoveryRates is not null && RealtimeInputsData.CuttingsRecoveryRates.Values is not null)
                        {
                            foreach (var cuttingsRecoveryRate in RealtimeInputsData.CuttingsRecoveryRates.Values)
                            {
                                if (cuttingsRecoveryRate is not null && cuttingsRecoveryRate.Mean is not null)
                                {
                                    totalCuttingsFlowrate += cuttingsRecoveryRate.Mean.Value;
                                }
                            }
                        }
                        Logger.LogInformation("Cuttings flowrate: " + (totalCuttingsFlowrate * 60000.0).ToString("F3") + " L/min");
                    }
                }
            }
        }
    }
}
