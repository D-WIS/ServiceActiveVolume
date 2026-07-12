using DWIS.Client.ReferenceImplementation.OPCFoundation;
using DWIS.RigOS.Common.Worker;
using DWIS.Service.ActiveVolume.Model;
using DWIS.Service.ActiveVolume.Model.Calibration;
using DWIS.Service.ActiveVolume.Model.Case;
using DWIS.Service.ActiveVolume.Model.Fusion;
using Microsoft.Extensions.Options;
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
        private readonly IModelServiceClients modelServiceClients_;
        private readonly CalibrationServiceClient calibrationServiceClient_;
        private readonly ActiveVolumeOnlineOptions onlineOptions_;
        private readonly ActiveVolumeFusionEngine fusionEngine_ = new();
        private OnlineCaseSpool? spool_;
        private ActiveVolumeCase? onlineCase_;
        private DateTimeOffset nextCalibrationRequestUtc_ = DateTimeOffset.MinValue;

        public Worker(
            ILogger<IDWISWorker<ConfigurationForActiveVolume>> logger,
            ILogger<DWISClientOPCF>? loggerDWISClient,
            IModelServiceClients modelServiceClients,
            CalibrationServiceClient calibrationServiceClient,
            IOptions<ActiveVolumeOnlineOptions> onlineOptions) : base(logger, loggerDWISClient)
        {
            modelServiceClients_ = modelServiceClients;
            calibrationServiceClient_ = calibrationServiceClient;
            onlineOptions_ = onlineOptions.Value;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ConnectToBlackboard();
            if (Configuration is not null && _DWISClient != null && _DWISClient.Connected)
            {
                await InitializeOnlineCaseAsync(stoppingToken);
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
                            ActiveVolumeSample sample = CreateActiveVolumeSample(DateTimeOffset.UtcNow);
                            ActiveVolumeFusionResult fusionResult = fusionEngine_.Process(new ActiveVolumeFusionInput
                            {
                                Sample = sample,
                                Context = CreateContext()
                            });
                            ApplyFusionResult(fusionResult);
                            if (spool_ is not null && onlineCase_ is not null)
                            {
                                spool_.AppendSample(onlineCase_.ID, sample, onlineOptions_.ChunkFlushInterval);
                                await UploadPendingChunksAsync(stoppingToken);
                                await RequestBackgroundCalibrationIfDueAsync(stoppingToken);
                            }
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
            if (spool_ is not null && onlineCase_ is not null)
            {
                spool_.Flush(onlineCase_.ID);
                await UploadPendingChunksAsync(CancellationToken.None);
            }
        }

        private async Task InitializeOnlineCaseAsync(CancellationToken cancellationToken)
        {
            spool_ = new OnlineCaseSpool(onlineOptions_.SpoolDirectory, onlineOptions_.ChunkSize);
            ActiveVolumeCase template = new()
            {
                Name = onlineOptions_.CaseName,
                Description = "Online ActiveVolume case created by the realtime worker.",
                FieldID = onlineOptions_.FieldID,
                ClusterID = onlineOptions_.ClusterID,
                WellID = onlineOptions_.WellID,
                WellBoreID = onlineOptions_.WellBoreID,
                WellBoreArchitectureID = onlineOptions_.WellBoreArchitectureID,
                DrillStringID = onlineOptions_.DrillStringID,
                ReturnFlowMeasurementMode = onlineOptions_.ReturnFlowMeasurementMode,
                ProcessingState = ActiveVolumeCaseProcessingState.Uploading
            };
            onlineCase_ = spool_.LoadOrCreateCase(template);
            try
            {
                await calibrationServiceClient_.EnsureCaseAsync(onlineCase_, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Unable to register online ActiveVolume case with the Calibration service. Local spool will continue.");
            }
        }

        private async Task UploadPendingChunksAsync(CancellationToken cancellationToken)
        {
            if (spool_ is null)
            {
                return;
            }

            foreach (ActiveVolumeCaseChunk chunk in spool_.ReadPendingChunks())
            {
                try
                {
                    await calibrationServiceClient_.UploadChunkAsync(chunk, cancellationToken);
                    spool_.MarkUploaded(chunk.ChunkIndex);
                }
                catch (Exception ex)
                {
                    Logger?.LogWarning(ex, "Unable to upload ActiveVolume chunk {ChunkIndex}. It remains in the local spool.", chunk.ChunkIndex);
                    break;
                }
            }
        }

        private async Task RequestBackgroundCalibrationIfDueAsync(CancellationToken cancellationToken)
        {
            if (onlineCase_ is null || DateTimeOffset.UtcNow < nextCalibrationRequestUtc_)
            {
                return;
            }

            nextCalibrationRequestUtc_ = DateTimeOffset.UtcNow + onlineOptions_.CalibrationRequestInterval;
            try
            {
                Guid? jobId = await calibrationServiceClient_.RequestProcessingAsync(onlineCase_.ID, cancellationToken);
                if (jobId.HasValue)
                {
                    Logger?.LogInformation("Requested background calibration job {JobId} for online ActiveVolume case {CaseId}.", jobId, onlineCase_.ID);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Unable to request background calibration for the online ActiveVolume case.");
            }
        }

        private ActiveVolumeSample CreateActiveVolumeSample(DateTimeOffset timestampUtc)
        {
            return new ActiveVolumeSample
            {
                TimestampUtc = timestampUtc,
                ActiveVolume = GetScalarValue(RealtimeInputsData.ActiveVolume),
                FlowrateIn = GetScalarValue(RealtimeInputsData.FlowrateIn),
                FlowPaddlePosition = GetScalarValue(RealtimeInputsData.FlowPaddlePosition),
                CoriolisVolumetricFlowrate = GetScalarValue(RealtimeInputsData.CoriolisVolumetricFlowrate),
                CoriolisMassFlowrate = GetScalarValue(RealtimeInputsData.CoriolisMassFlowrate),
                ReturnMudDensity = GetScalarValue(RealtimeInputsData.ReturnMudDensity),
                CuttingsRecoveryRate = GetScalarValues(RealtimeInputsData.CuttingsRecoveryRates).Sum(),
                StandPipePressure = GetScalarValue(RealtimeInputsData.StandPipePressure),
                BottomOfStringDepth = GetScalarValue(RealtimeInputsData.BottomOfStringDepth),
                BottomHoleDepth = GetScalarValue(RealtimeInputsData.BottomHoleDepth)
            };
        }

        private ActiveVolumeContext CreateContext()
        {
            return new ActiveVolumeContext
            {
                FieldID = onlineOptions_.FieldID,
                ClusterID = onlineOptions_.ClusterID,
                WellID = onlineOptions_.WellID,
                WellBoreID = onlineOptions_.WellBoreID,
                WellBoreArchitectureID = onlineOptions_.WellBoreArchitectureID,
                DrillStringID = onlineOptions_.DrillStringID,
                ReturnFlowMeasurementMode = onlineOptions_.ReturnFlowMeasurementMode
            };
        }

        private void ApplyFusionResult(ActiveVolumeFusionResult result)
        {
            RealtimeOutputsData.CorrectedActiveVolume ??= new ScalarProperty();
            RealtimeOutputsData.CorrectedActiveVolume.Value = result.CorrectedActiveVolume;

            RealtimeOutputsData.EstimatedPitVolumeFlowBias ??= new ScalarProperty();
            RealtimeOutputsData.EstimatedPitVolumeFlowBias.Value = result.Innovation;

            RealtimeOutputsData.ReturnFlowCapacityScale ??= new ScalarProperty();
            RealtimeOutputsData.ReturnFlowCapacityScale.Value = result.EstimatedReturnFlow;
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
