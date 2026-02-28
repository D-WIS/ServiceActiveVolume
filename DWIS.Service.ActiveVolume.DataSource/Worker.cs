using DWIS.Client.ReferenceImplementation.OPCFoundation;
using DWIS.RigOS.Common.Worker;
using DWIS.Service.ActiveVolume.Model;

namespace DWIS.Service.ActiveVolume.DataSource
{
    public class Worker : DWISWorker<Configuration, object>
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
                await RegisterToBlackboard(RealtimeInputsData, false);
                await Loop(stoppingToken);
            }
        }

        protected override async Task Loop(CancellationToken cancellationToken)
        {
            PeriodicTimer timer = new PeriodicTimer(LoopSpan);
            double flowrateIn = 2000.0 / 60000.0; // 2000 L/min converted to m^3/s
            double flowrateOut = flowrateIn;
            double activeVolumePeriod = 15.0; // seconds
            double activeVolumeAmplitude = 1.0; // m^3  
            double activeVolume = 30.0; // m^3
            double scalingFactor = 3000.0 / 60000.0; // scaling factor for flowrate out proportion in m^3/s
            double cuttingsFlowrate = 30.0 / 60000.0; // 20 L/min converted to m^3/s
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
                    RealtimeInputsData.CuttingsRecoveryRates = new ScalarsProperty();
                }
                if (RealtimeInputsData.CuttingsRecoveryRates.Value == null)
                {
                    RealtimeInputsData.CuttingsRecoveryRates.Value = new List<double>();
                }
                RealtimeInputsData.CuttingsRecoveryRates.Value.Clear();
                RealtimeInputsData.CuttingsRecoveryRates.Value.Add(0.5 * cuttingsFlowrate);
                RealtimeInputsData.CuttingsRecoveryRates.Value.Add(0.5 * cuttingsFlowrate);
                if (RealtimeInputsData.ShakerLoadEstimates is null)
                {
                    RealtimeInputsData.ShakerLoadEstimates = new ScalarsProperty();
                }
                if (RealtimeInputsData.ShakerLoadEstimates.Value == null)
                {
                    RealtimeInputsData.ShakerLoadEstimates.Value = new List<double>();
                }
                double totalShakerLoadEstimates = flowrateOut / scalingFactor;
                RealtimeInputsData.ShakerLoadEstimates.Value.Clear();
                RealtimeInputsData.ShakerLoadEstimates.Value.Add(0.5 * totalShakerLoadEstimates * 10.0);
                RealtimeInputsData.ShakerLoadEstimates.Value.Add(0.5 * totalShakerLoadEstimates * 10.0);
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
                        double totalCuttingsFlowrate = 0.0;
                        if (RealtimeInputsData.CuttingsRecoveryRates is not null && RealtimeInputsData.CuttingsRecoveryRates.Value is not null)
                        {
                            foreach (var cuttingsRecoveryRate in RealtimeInputsData.CuttingsRecoveryRates.Value)
                            {
                                totalCuttingsFlowrate += cuttingsRecoveryRate;
                            }
                        }
                        Logger.LogInformation("Cuttings flowrate: " + (totalCuttingsFlowrate * 60000.0).ToString("F3") + " L/min");
                    }
                }
            }
        }
    }
}
