using DWIS.Service.ActiveVolume.Model.Calibration;
using DWIS.Service.ActiveVolume.Model.Case;
using DWIS.Service.ActiveVolume.Model.Fusion;

namespace DWIS.Service.ActiveVolume.CalibrationService.Managers
{
    public sealed class CalibrationJobWorker : BackgroundService
    {
        private readonly CalibrationJobQueue queue_;
        private readonly ActiveVolumeSqliteStore store_;
        private readonly ILogger<CalibrationJobWorker> logger_;
        private readonly SemaphoreSlim concurrency_;

        public CalibrationJobWorker(
            CalibrationJobQueue queue,
            ActiveVolumeSqliteStore store,
            IConfiguration configuration,
            ILogger<CalibrationJobWorker> logger)
        {
            queue_ = queue;
            store_ = store;
            logger_ = logger;
            int maxConcurrentJobs = Math.Max(1, configuration.GetValue("MaxConcurrentCalibrationJobs", 2));
            concurrency_ = new SemaphoreSlim(maxConcurrentJobs, maxConcurrentJobs);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (Guid jobId in queue_.DequeueAllAsync(stoppingToken))
            {
                await concurrency_.WaitAsync(stoppingToken);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessJobAsync(jobId, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger_.LogError(ex, "Unhandled error while processing calibration job {JobId}", jobId);
                    }
                    finally
                    {
                        concurrency_.Release();
                    }
                }, stoppingToken);
            }
        }

        private async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
        {
            CalibrationJob? job = store_.GetJob(jobId);
            if (job is null)
            {
                logger_.LogWarning("Calibration job {JobId} was not found", jobId);
                return;
            }

            ActiveVolumeCase? activeCase = store_.GetCase(job.CaseID);
            if (activeCase is null)
            {
                job.State = CalibrationJobState.Failed;
                job.Message = "Case not found.";
                job.CompletedUtc = DateTimeOffset.UtcNow;
                store_.UpdateJob(job);
                return;
            }

            job.State = CalibrationJobState.Running;
            job.StartedUtc = DateTimeOffset.UtcNow;
            job.Message = "Loading chunks.";
            store_.UpdateJob(job);

            List<ActiveVolumeCaseChunk> chunks = store_.GetChunks(activeCase.ID);
            if (chunks.Count == 0)
            {
                job.State = CalibrationJobState.Failed;
                job.Message = "No time-series chunks are available for calibration.";
                job.CompletedUtc = DateTimeOffset.UtcNow;
                store_.UpdateJob(job);
                return;
            }

            ActiveVolumeFusionEngine engine = new();
            ActiveVolumeContext context = ToContext(activeCase);
            int processed = 0;
            double innovationEnergy = 0.0;
            long sampleCount = 0;

            foreach (ActiveVolumeCaseChunk chunk in chunks.OrderBy(x => x.ChunkIndex))
            {
                foreach (ActiveVolumeSample sample in chunk.Samples.OrderBy(x => x.TimestampUtc))
                {
                    ActiveVolumeFusionResult result = engine.Process(new ActiveVolumeFusionInput
                    {
                        Sample = sample,
                        Context = context
                    });
                    innovationEnergy += result.Innovation * result.Innovation;
                    sampleCount++;
                }

                processed++;
                job.Progress = processed / (double)chunks.Count;
                job.Message = $"Processed {processed} of {chunks.Count} chunks.";
                store_.UpdateJob(job);
                await Task.Delay(1, cancellationToken);
            }

            double rmsInnovation = sampleCount > 0 ? Math.Sqrt(innovationEnergy / sampleCount) : double.PositiveInfinity;
            double quality = double.IsFinite(rmsInnovation) ? 1.0 / (1.0 + rmsInnovation) : 0.0;
            CalibrationRecord calibration = CreateBaselineCalibration(activeCase, context, quality);
            store_.SaveCalibration(calibration);

            job.State = CalibrationJobState.Succeeded;
            job.Progress = 1.0;
            job.Message = "Background calibration completed.";
            job.CompletedUtc = DateTimeOffset.UtcNow;
            job.CalibrationRecordID = calibration.ID;
            store_.UpdateJob(job);
        }

        private static ActiveVolumeContext ToContext(ActiveVolumeCase activeCase)
        {
            return new ActiveVolumeContext
            {
                FieldID = activeCase.FieldID,
                ClusterID = activeCase.ClusterID,
                WellID = activeCase.WellID,
                WellBoreID = activeCase.WellBoreID,
                WellBoreArchitectureID = activeCase.WellBoreArchitectureID,
                DrillStringID = activeCase.DrillStringID,
                ReturnFlowMeasurementMode = activeCase.ReturnFlowMeasurementMode,
                RigName = activeCase.RigName,
                MudSystem = activeCase.MudSystem,
                HoleSection = activeCase.HoleSection
            };
        }

        private static CalibrationRecord CreateBaselineCalibration(ActiveVolumeCase activeCase, ActiveVolumeContext context, double quality)
        {
            return new CalibrationRecord
            {
                SourceCaseID = activeCase.ID,
                Context = context,
                Quality = quality,
                Notes = "Baseline replay calibration. Detailed observability-aware regression can update this record shape without changing the API.",
                Components =
                [
                    new CalibrationParameterSet
                    {
                        Component = CalibrationComponent.ReturnFlow,
                        Quality = quality,
                        Parameters =
                        {
                            ["PaddleOnset"] = 0.05,
                            ["PaddleScale"] = 1.0,
                            ["PaddleExponent"] = 1.0,
                            ["CoriolisScale"] = 1.0
                        },
                        LowerBounds =
                        {
                            ["ReturnFlowAdjustment"] = 0.5
                        },
                        UpperBounds =
                        {
                            ["ReturnFlowAdjustment"] = 1.5
                        }
                    },
                    new CalibrationParameterSet
                    {
                        Component = CalibrationComponent.LowFlowSurrogate,
                        Quality = quality,
                        Parameters =
                        {
                            ["LowFlowGain"] = 0.85
                        }
                    }
                ]
            };
        }
    }
}
