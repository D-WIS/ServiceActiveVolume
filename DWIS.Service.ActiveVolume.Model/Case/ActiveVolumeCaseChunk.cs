namespace DWIS.Service.ActiveVolume.Model.Case
{
    public sealed class ActiveVolumeCaseChunk
    {
        public Guid CaseID { get; set; }
        public int ChunkIndex { get; set; }
        public DateTimeOffset StartTimeUtc { get; set; }
        public DateTimeOffset EndTimeUtc { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public List<ActiveVolumeSample> Samples { get; set; } = new();
    }

    public sealed class ActiveVolumeSample
    {
        public DateTimeOffset TimestampUtc { get; set; }
        public double? ActiveVolume { get; set; }
        public double? FlowrateIn { get; set; }
        public double? FlowPaddlePosition { get; set; }
        public double? CoriolisVolumetricFlowrate { get; set; }
        public double? CoriolisMassFlowrate { get; set; }
        public double? ReturnMudDensity { get; set; }
        public double? CuttingsRecoveryRate { get; set; }
        public double[] CuttingsParticleSizeDistribution { get; set; } = Array.Empty<double>();
        public double? StandPipePressure { get; set; }
        public double? InletMudTemperature { get; set; }
        public double? OutletMudTemperature { get; set; }
        public double? BottomOfStringDepth { get; set; }
        public double? BottomHoleDepth { get; set; }
        public double? AxialPipeVelocity { get; set; }
        public Dictionary<string, double?> AdditionalSignals { get; set; } = new();
        public Dictionary<string, string> QualityFlags { get; set; } = new();
    }
}
