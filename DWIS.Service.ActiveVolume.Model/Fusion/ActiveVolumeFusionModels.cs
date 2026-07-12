using DWIS.Service.ActiveVolume.Model.Calibration;
using DWIS.Service.ActiveVolume.Model.Case;

namespace DWIS.Service.ActiveVolume.Model.Fusion
{
    public sealed class ActiveVolumeFusionConfiguration
    {
        public double DefaultDtSeconds { get; set; } = 1.0;
        public double MinDtSeconds { get; set; } = 0.01;
        public double MaxDtSeconds { get; set; } = 60.0;
        public double PitVolumeMeasurementVariance { get; set; } = 0.25;
        public double ProcessVarianceVolume { get; set; } = 0.01;
        public double ProcessVarianceAdjustment { get; set; } = 1e-5;
        public double MaxNormalizedInnovationSquared { get; set; } = 16.0;
        public double MinReturnFlowAdjustment { get; set; } = 0.5;
        public double MaxReturnFlowAdjustment { get; set; } = 1.5;
        public double FormationExchangeTriggerVolume { get; set; } = 5.0;
    }

    public sealed class ActiveVolumeFusionState
    {
        public DateTimeOffset? LastTimestampUtc { get; set; }
        public double CorrectedActiveVolume { get; set; }
        public double SurfaceRetentionVolume { get; set; }
        public double ReturnFlowAdjustment { get; set; } = 1.0;
        public double CompressibilityAdjustment { get; set; } = 1.0;
        public double SurfaceRetentionAdjustment { get; set; } = 1.0;
        public double MudFilmAdjustment { get; set; } = 1.0;
        public double CumulativePitLineupCorrection { get; set; }
        public double AccumulatedInnovation { get; set; }
        public bool IsInitialized { get; set; }
    }

    public sealed class ActiveVolumeFusionResult
    {
        public DateTimeOffset TimestampUtc { get; set; }
        public ActiveVolumeFusionState State { get; set; } = new();
        public double EstimatedReturnFlow { get; set; }
        public double CorrectedActiveVolume { get; set; }
        public double Innovation { get; set; }
        public double NormalizedInnovationSquared { get; set; }
        public bool PitVolumeMeasurementAccepted { get; set; }
        public bool FormationExchangeSuspected { get; set; }
        public string DiagnosticMessage { get; set; } = string.Empty;
    }

    public sealed class ActiveVolumeFusionInput
    {
        public ActiveVolumeSample Sample { get; set; } = new();
        public ActiveVolumeContext Context { get; set; } = new();
        public CalibrationRecord? Calibration { get; set; }
    }
}
