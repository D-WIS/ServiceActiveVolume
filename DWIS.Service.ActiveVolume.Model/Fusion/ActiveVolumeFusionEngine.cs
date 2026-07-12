using DWIS.Service.ActiveVolume.Model.Case;

namespace DWIS.Service.ActiveVolume.Model.Fusion
{
    public sealed class ActiveVolumeFusionEngine
    {
        private readonly ActiveVolumeFusionConfiguration configuration_;
        private readonly IReturnFlowModel returnFlowModel_;
        private ActiveVolumeFusionState state_ = new();

        public ActiveVolumeFusionEngine(
            ActiveVolumeFusionConfiguration? configuration = null,
            IReturnFlowModel? returnFlowModel = null)
        {
            configuration_ = configuration ?? new ActiveVolumeFusionConfiguration();
            returnFlowModel_ = returnFlowModel ?? new CompositeReturnFlowModel();
        }

        public ActiveVolumeFusionState State => CloneState(state_);

        public void Initialize(ActiveVolumeFusionState? state = null)
        {
            state_ = state is null ? new ActiveVolumeFusionState() : CloneState(state);
        }

        public ActiveVolumeFusionResult Process(ActiveVolumeFusionInput input)
        {
            ActiveVolumeSample sample = input.Sample;
            double dt = ResolveTimeStep(sample.TimestampUtc);
            double measuredVolume = sample.ActiveVolume ?? state_.CorrectedActiveVolume;

            if (!state_.IsInitialized)
            {
                state_.CorrectedActiveVolume = measuredVolume;
                state_.ReturnFlowAdjustment = 1.0;
                state_.IsInitialized = true;
            }

            double returnFlow = returnFlowModel_.EstimateReturnFlow(sample, input.Context, input.Calibration);
            returnFlow *= Clamp(state_.ReturnFlowAdjustment, configuration_.MinReturnFlowAdjustment, configuration_.MaxReturnFlowAdjustment);

            double inletFlow = Math.Max(0.0, sample.FlowrateIn ?? 0.0);
            double cuttingsFlow = Math.Max(0.0, sample.CuttingsRecoveryRate ?? 0.0);
            double predicted = state_.CorrectedActiveVolume + dt * (returnFlow - inletFlow - cuttingsFlow);
            double innovation = measuredVolume - predicted;
            double s = Math.Max(configuration_.PitVolumeMeasurementVariance + configuration_.ProcessVarianceVolume, 1e-12);
            double nis = innovation * innovation / s;
            bool accepted = sample.ActiveVolume.HasValue && nis <= configuration_.MaxNormalizedInnovationSquared;

            if (accepted)
            {
                double gain = configuration_.ProcessVarianceVolume / s;
                state_.CorrectedActiveVolume = predicted + gain * innovation;
                state_.AccumulatedInnovation = 0.98 * state_.AccumulatedInnovation + innovation;
            }
            else
            {
                state_.CorrectedActiveVolume = predicted;
                state_.AccumulatedInnovation = 0.98 * state_.AccumulatedInnovation;
            }

            state_.LastTimestampUtc = sample.TimestampUtc;
            bool formationExchangeSuspected = Math.Abs(state_.AccumulatedInnovation) > configuration_.FormationExchangeTriggerVolume;

            return new ActiveVolumeFusionResult
            {
                TimestampUtc = sample.TimestampUtc,
                State = CloneState(state_),
                EstimatedReturnFlow = returnFlow,
                CorrectedActiveVolume = state_.CorrectedActiveVolume,
                Innovation = innovation,
                NormalizedInnovationSquared = nis,
                PitVolumeMeasurementAccepted = accepted,
                FormationExchangeSuspected = formationExchangeSuspected,
                DiagnosticMessage = formationExchangeSuspected ? "Persistent pit-volume innovation exceeds nominal explanatory capacity." : string.Empty
            };
        }

        private double ResolveTimeStep(DateTimeOffset timestampUtc)
        {
            if (!state_.LastTimestampUtc.HasValue)
            {
                return configuration_.DefaultDtSeconds;
            }

            double dt = (timestampUtc - state_.LastTimestampUtc.Value).TotalSeconds;
            return Clamp(dt, configuration_.MinDtSeconds, configuration_.MaxDtSeconds);
        }

        private static ActiveVolumeFusionState CloneState(ActiveVolumeFusionState state)
        {
            return new ActiveVolumeFusionState
            {
                LastTimestampUtc = state.LastTimestampUtc,
                CorrectedActiveVolume = state.CorrectedActiveVolume,
                SurfaceRetentionVolume = state.SurfaceRetentionVolume,
                ReturnFlowAdjustment = state.ReturnFlowAdjustment,
                CompressibilityAdjustment = state.CompressibilityAdjustment,
                SurfaceRetentionAdjustment = state.SurfaceRetentionAdjustment,
                MudFilmAdjustment = state.MudFilmAdjustment,
                CumulativePitLineupCorrection = state.CumulativePitLineupCorrection,
                AccumulatedInnovation = state.AccumulatedInnovation,
                IsInitialized = state.IsInitialized
            };
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
