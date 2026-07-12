using DWIS.Service.ActiveVolume.Model.Case;

namespace DWIS.Service.ActiveVolume.Model.Import
{
    public enum DelimitedFileSeparator
    {
        Comma = 0,
        Semicolon = 1,
        Tab = 2,
        Whitespace = 3,
        Custom = 4
    }

    public enum ActiveVolumeSignalKind
    {
        Ignore = 0,
        Timestamp = 1,
        RelativeSeconds = 2,
        ActiveVolume = 3,
        FlowrateIn = 4,
        FlowPaddlePosition = 5,
        CoriolisVolumetricFlowrate = 6,
        CoriolisMassFlowrate = 7,
        ReturnMudDensity = 8,
        CuttingsRecoveryRate = 9,
        StandPipePressure = 10,
        InletMudTemperature = 11,
        OutletMudTemperature = 12,
        BottomOfStringDepth = 13,
        BottomHoleDepth = 14,
        AxialPipeVelocity = 15
    }

    public sealed class DelimitedColumnMapping
    {
        public int ColumnIndex { get; set; }
        public ActiveVolumeSignalKind SignalKind { get; set; }
        public string Unit { get; set; } = "SI";
        public string ReferenceSystem { get; set; } = "WGS84";
    }

    public sealed class DelimitedImportDefinition
    {
        public DelimitedFileSeparator Separator { get; set; } = DelimitedFileSeparator.Comma;
        public string CustomSeparator { get; set; } = ",";
        public bool HasHeader { get; set; } = true;
        public DateTimeOffset? RelativeTimeOriginUtc { get; set; }
        public string TimestampFormat { get; set; } = "O";
        public List<DelimitedColumnMapping> Columns { get; set; } = new();
    }

    public sealed class ActiveVolumeCaseBatchImport
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
        public ActiveVolumeCaseProcessingState ProcessingState { get; set; } = ActiveVolumeCaseProcessingState.Created;
        public double ProcessingProgress { get; set; }
        public string ProcessingMessage { get; set; } = string.Empty;
        public List<ActiveVolumeCaseBatchImportItem> Items { get; set; } = new();
    }

    public sealed class ActiveVolumeCaseBatchImportItem
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public string FileName { get; set; } = string.Empty;
        public ActiveVolumeCase CaseMetadata { get; set; } = new();
        public DelimitedImportDefinition ImportDefinition { get; set; } = new();
        public Guid? CreatedCaseID { get; set; }
        public ActiveVolumeCaseProcessingState ProcessingState { get; set; } = ActiveVolumeCaseProcessingState.Created;
        public string ProcessingMessage { get; set; } = string.Empty;
    }
}
