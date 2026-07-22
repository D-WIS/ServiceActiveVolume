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

            ActiveVolumeContext context = ToContext(activeCase);
            List<ActiveVolumeSample> orderedSamples = chunks
                .OrderBy(x => x.ChunkIndex)
                .SelectMany(chunk => chunk.Samples.OrderBy(sample => sample.TimestampUtc))
                .ToList();
            PitLineupCorrectionResult pitLineupCorrection = DetectPitLineupCorrections(orderedSamples);
            ActiveVolumeFusionEngine engine = new();
            int processed = 0;
            double innovationEnergy = 0.0;
            long sampleCount = 0;

            foreach (ActiveVolumeCaseChunk chunk in chunks.OrderBy(x => x.ChunkIndex))
            {
                foreach (ActiveVolumeSample sample in chunk.Samples.OrderBy(x => x.TimestampUtc))
                {
                    ActiveVolumeSample correctedSample = ApplyPitLineupCorrection(sample, pitLineupCorrection.Events);
                    ActiveVolumeFusionResult result = engine.Process(new ActiveVolumeFusionInput
                    {
                        Sample = correctedSample,
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
            CalibrationRecord calibration = CreateBaselineCalibration(activeCase, context, quality, pitLineupCorrection);
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

        private static CalibrationRecord CreateBaselineCalibration(
            ActiveVolumeCase activeCase,
            ActiveVolumeContext context,
            double quality,
            PitLineupCorrectionResult pitLineupCorrection)
        {
            return new CalibrationRecord
            {
                SourceCaseID = activeCase.ID,
                Context = context,
                Quality = quality,
                Notes = "Baseline replay calibration with active-volume pit-lineup jump correction.",
                Components =
                [
                    CreatePitLineupCorrectionComponent(pitLineupCorrection, quality),
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

        private static CalibrationParameterSet CreatePitLineupCorrectionComponent(PitLineupCorrectionResult result, double quality)
        {
            CalibrationParameterSet component = new()
            {
                Component = CalibrationComponent.PitLineupCorrection,
                Quality = quality,
                Message = result.Events.Count == 0
                    ? "No active-volume pit-lineup discontinuities were detected."
                    : $"Detected {result.Events.Count} active-volume pit-lineup discontinuit{(result.Events.Count == 1 ? "y" : "ies")}."
            };
            component.Parameters["JumpCount"] = result.Events.Count;
            component.Parameters["DetectionThresholdVolume"] = result.DetectionThresholdVolume;
            for (int index = 0; index < result.Events.Count; index++)
            {
                PitLineupCorrectionEvent correctionEvent = result.Events[index];
                component.Parameters[$"Jump{index + 1}TimeUnixSeconds"] = correctionEvent.TimestampUtc.ToUnixTimeSeconds();
                component.Parameters[$"Jump{index + 1}Offset"] = correctionEvent.OffsetVolume;
                component.Parameters[$"Jump{index + 1}CumulativeOffset"] = correctionEvent.CumulativeOffsetVolume;
            }

            return component;
        }

        private static PitLineupCorrectionResult DetectPitLineupCorrections(List<ActiveVolumeSample> orderedSamples)
        {
            List<(DateTimeOffset TimestampUtc, double Delta)> deltas = new();
            ActiveVolumeSample? previous = null;
            foreach (ActiveVolumeSample sample in orderedSamples.Where(sample => sample.ActiveVolume.HasValue).OrderBy(sample => sample.TimestampUtc))
            {
                if (previous?.ActiveVolume is double previousVolume)
                {
                    deltas.Add((sample.TimestampUtc, sample.ActiveVolume!.Value - previousVolume));
                }

                previous = sample;
            }

            if (deltas.Count == 0)
            {
                return new PitLineupCorrectionResult([], 0.0);
            }

            double medianAbsoluteDelta = Median(deltas.Select(delta => Math.Abs(delta.Delta)).Where(value => value > 0.0).ToList());
            double threshold = Math.Max(5.0, Math.Max(8.0 * medianAbsoluteDelta, 1.0));
            List<PitLineupCorrectionEvent> events = new();
            double cumulativeOffset = 0.0;
            foreach ((DateTimeOffset timestampUtc, double delta) in deltas)
            {
                if (Math.Abs(delta) < threshold)
                {
                    continue;
                }

                cumulativeOffset += delta;
                events.Add(new PitLineupCorrectionEvent(timestampUtc, delta, cumulativeOffset));
            }

            return new PitLineupCorrectionResult(events, threshold);
        }

        private static ActiveVolumeSample ApplyPitLineupCorrection(ActiveVolumeSample sample, List<PitLineupCorrectionEvent> events)
        {
            if (!sample.ActiveVolume.HasValue || events.Count == 0)
            {
                return sample;
            }

            double offset = events
                .Where(correctionEvent => correctionEvent.TimestampUtc <= sample.TimestampUtc)
                .Select(correctionEvent => correctionEvent.CumulativeOffsetVolume)
                .LastOrDefault();

            return new ActiveVolumeSample
            {
                TimestampUtc = sample.TimestampUtc,
                ActiveVolume = sample.ActiveVolume.Value - offset,
                FlowrateIn = sample.FlowrateIn,
                FlowPaddlePosition = sample.FlowPaddlePosition,
                CoriolisVolumetricFlowrate = sample.CoriolisVolumetricFlowrate,
                CoriolisMassFlowrate = sample.CoriolisMassFlowrate,
                ReturnMudDensity = sample.ReturnMudDensity,
                CuttingsRecoveryRate = sample.CuttingsRecoveryRate,
                CuttingsParticleSizeDistribution = sample.CuttingsParticleSizeDistribution,
                StandPipePressure = sample.StandPipePressure,
                InletMudTemperature = sample.InletMudTemperature,
                OutletMudTemperature = sample.OutletMudTemperature,
                BottomOfStringDepth = sample.BottomOfStringDepth,
                BottomHoleDepth = sample.BottomHoleDepth,
                AxialPipeVelocity = sample.AxialPipeVelocity,
                AdditionalSignals = sample.AdditionalSignals,
                QualityFlags = sample.QualityFlags
            };
        }

        private static double Median(List<double> values)
        {
            if (values.Count == 0)
            {
                return 0.0;
            }

            values.Sort();
            int middle = values.Count / 2;
            return values.Count % 2 == 0
                ? 0.5 * (values[middle - 1] + values[middle])
                : values[middle];
        }

        private sealed record PitLineupCorrectionResult(
            List<PitLineupCorrectionEvent> Events,
            double DetectionThresholdVolume);

        private sealed record PitLineupCorrectionEvent(
            DateTimeOffset TimestampUtc,
            double OffsetVolume,
            double CumulativeOffsetVolume);
    }
}
