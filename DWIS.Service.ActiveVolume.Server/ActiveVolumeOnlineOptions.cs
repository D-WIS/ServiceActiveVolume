using DWIS.Service.ActiveVolume.Model.Case;

namespace DWIS.Service.ActiveVolume.Server
{
    public sealed class ActiveVolumeOnlineOptions
    {
        public const string SectionName = "ActiveVolumeOnline";

        public string CalibrationServiceUrl { get; set; } = "http://localhost:5000/activevolume/api/";
        public string SpoolDirectory { get; set; } = "/home/activevolume-online";
        public int ChunkSize { get; set; } = 600;
        public TimeSpan ChunkFlushInterval { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan CalibrationRequestInterval { get; set; } = TimeSpan.FromMinutes(10);
        public string CaseName { get; set; } = "Online ActiveVolume Case";
        public Guid FieldID { get; set; }
        public Guid ClusterID { get; set; }
        public Guid WellID { get; set; }
        public Guid WellBoreID { get; set; }
        public Guid WellBoreArchitectureID { get; set; }
        public Guid DrillStringID { get; set; }
        public ReturnFlowMeasurementMode ReturnFlowMeasurementMode { get; set; } = ReturnFlowMeasurementMode.FlowPaddle;
    }
}
