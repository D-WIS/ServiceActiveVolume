using DWIS.Service.ActiveVolume.Model.Case;

namespace DWIS.Service.ActiveVolume.Model.Calibration
{
    public sealed class CalibrationParameterSet
    {
        public CalibrationComponent Component { get; set; }
        public Dictionary<string, double> Parameters { get; set; } = new();
        public Dictionary<string, double> StandardDeviations { get; set; } = new();
        public Dictionary<string, double> LowerBounds { get; set; } = new();
        public Dictionary<string, double> UpperBounds { get; set; } = new();
        public double Quality { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class CalibrationRecord
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public Guid? SourceCaseID { get; set; }
        public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
        public string ModelVersion { get; set; } = "1.0";
        public ActiveVolumeContext Context { get; set; } = new();
        public List<CalibrationParameterSet> Components { get; set; } = new();
        public double SimilarityRadius { get; set; }
        public double Quality { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public sealed class ActiveVolumeContext
    {
        public Guid FieldID { get; set; }
        public Guid ClusterID { get; set; }
        public Guid WellID { get; set; }
        public Guid WellBoreID { get; set; }
        public Guid WellBoreArchitectureID { get; set; }
        public Guid DrillStringID { get; set; }
        public ReturnFlowMeasurementMode ReturnFlowMeasurementMode { get; set; }
        public string RigName { get; set; } = string.Empty;
        public string MudSystem { get; set; } = string.Empty;
        public string HoleSection { get; set; } = string.Empty;
        public Dictionary<string, double> NumericFeatures { get; set; } = new();
    }

    public sealed class BestMatchCalibrationRequest
    {
        public ActiveVolumeContext Context { get; set; } = new();
        public int MaxResults { get; set; } = 3;
    }

    public sealed class BestMatchCalibrationResult
    {
        public CalibrationRecord? Calibration { get; set; }
        public double Distance { get; set; } = double.PositiveInfinity;
        public double Weight { get; set; }
    }

    public sealed class CalibrationJob
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public Guid CaseID { get; set; }
        public CalibrationJobState State { get; set; } = CalibrationJobState.Queued;
        public double Progress { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? StartedUtc { get; set; }
        public DateTimeOffset? CompletedUtc { get; set; }
        public Guid? CalibrationRecordID { get; set; }
    }
}
