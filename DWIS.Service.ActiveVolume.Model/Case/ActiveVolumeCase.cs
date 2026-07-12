using System.Text.Json.Serialization;

namespace DWIS.Service.ActiveVolume.Model.Case
{
    public sealed class ActiveVolumeCase
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastModificationDate { get; set; } = DateTimeOffset.UtcNow;
        public Guid FieldID { get; set; }
        public Guid ClusterID { get; set; }
        public Guid WellID { get; set; }
        public Guid WellBoreID { get; set; }
        public Guid WellBoreArchitectureID { get; set; }
        public Guid DrillStringID { get; set; }
        public ReturnFlowMeasurementMode ReturnFlowMeasurementMode { get; set; } = ReturnFlowMeasurementMode.Unknown;
        public string RigName { get; set; } = string.Empty;
        public string MudSystem { get; set; } = string.Empty;
        public string HoleSection { get; set; } = string.Empty;
        public ActiveVolumeCaseProcessingState ProcessingState { get; set; } = ActiveVolumeCaseProcessingState.Created;
        public double ProcessingProgress { get; set; }
        public string ProcessingMessage { get; set; } = string.Empty;
        public int LastProcessedChunkIndex { get; set; } = -1;
        public int ChunkCount { get; set; }
        public long SampleCount { get; set; }
        public Guid? ActiveJobID { get; set; }
        public Guid? BestCalibrationID { get; set; }
        public double? CalibrationQuality { get; set; }
        public DateTimeOffset? LastCalibrationDate { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
        public List<ActiveVolumeCaseChunk> Chunks { get; set; } = new();

        [JsonIgnore]
        public ActiveVolumeCaseLight Light => ActiveVolumeCaseLight.FromCase(this);
    }

    public sealed class ActiveVolumeCaseLight
    {
        public Guid ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid FieldID { get; set; }
        public Guid ClusterID { get; set; }
        public Guid WellID { get; set; }
        public Guid WellBoreID { get; set; }
        public Guid WellBoreArchitectureID { get; set; }
        public Guid DrillStringID { get; set; }
        public ReturnFlowMeasurementMode ReturnFlowMeasurementMode { get; set; }
        public ActiveVolumeCaseProcessingState ProcessingState { get; set; }
        public double ProcessingProgress { get; set; }
        public string ProcessingMessage { get; set; } = string.Empty;
        public int LastProcessedChunkIndex { get; set; }
        public int ChunkCount { get; set; }
        public long SampleCount { get; set; }
        public Guid? ActiveJobID { get; set; }
        public Guid? BestCalibrationID { get; set; }
        public double? CalibrationQuality { get; set; }
        public DateTimeOffset? LastCalibrationDate { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public DateTimeOffset LastModificationDate { get; set; }

        public static ActiveVolumeCaseLight FromCase(ActiveVolumeCase data)
        {
            return new ActiveVolumeCaseLight
            {
                ID = data.ID,
                Name = data.Name,
                FieldID = data.FieldID,
                ClusterID = data.ClusterID,
                WellID = data.WellID,
                WellBoreID = data.WellBoreID,
                WellBoreArchitectureID = data.WellBoreArchitectureID,
                DrillStringID = data.DrillStringID,
                ReturnFlowMeasurementMode = data.ReturnFlowMeasurementMode,
                ProcessingState = data.ProcessingState,
                ProcessingProgress = data.ProcessingProgress,
                ProcessingMessage = data.ProcessingMessage,
                LastProcessedChunkIndex = data.LastProcessedChunkIndex,
                ChunkCount = data.ChunkCount,
                SampleCount = data.SampleCount,
                ActiveJobID = data.ActiveJobID,
                BestCalibrationID = data.BestCalibrationID,
                CalibrationQuality = data.CalibrationQuality,
                LastCalibrationDate = data.LastCalibrationDate,
                CreationDate = data.CreationDate,
                LastModificationDate = data.LastModificationDate
            };
        }
    }
}
