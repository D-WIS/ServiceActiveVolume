using DWIS.Client.ReferenceImplementation.OPCFoundation;
using DWIS.RigOS.Common.Worker;
using DWIS.Service.ActiveVolume.Model;

namespace DWIS.Service.ActiveVolume.DataSink
{
    public class Worker : DWISWorker<Configuration>
    {
        private RealtimeOutputsData RealtimeOutputsData { get; set; } = new RealtimeOutputsData();

        public Worker(ILogger<IDWISWorker<Configuration>> logger, ILogger<DWISClientOPCF>? loggerDWISClient) : base(logger, loggerDWISClient)
        {
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ConnectToBlackboard();
            if (_DWISClient != null && _DWISClient.Connected)
            {
                await RegisterQueries(RealtimeOutputsData);
                await Loop(stoppingToken);
            }
        }

        protected override async Task Loop(CancellationToken cancellationToken)
        {
            PeriodicTimer timer = new PeriodicTimer(LoopSpan);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await ReadBlackboardAsync(RealtimeOutputsData, cancellationToken);
                lock (_lock)
                {
                    if (Logger is not null && Logger.IsEnabled(LogLevel.Information))
                    {
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
        }
    }
}
