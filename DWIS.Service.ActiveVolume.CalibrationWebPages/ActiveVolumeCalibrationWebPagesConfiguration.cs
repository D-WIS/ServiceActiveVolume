namespace DWIS.Service.ActiveVolume.CalibrationWebPages
{
    public sealed class ActiveVolumeCalibrationWebPagesConfiguration : IActiveVolumeCalibrationWebPagesConfiguration
    {
        public string ActiveVolumeCalibrationHostURL { get; set; } = string.Empty;
        public string FieldHostURL { get; set; } = string.Empty;
        public string ClusterHostURL { get; set; } = string.Empty;
        public string WellHostURL { get; set; } = string.Empty;
        public string WellBoreHostURL { get; set; } = string.Empty;
        public string WellBoreArchitectureHostURL { get; set; } = string.Empty;
        public string DrillStringHostURL { get; set; } = string.Empty;
        public string RigHostURL { get; set; } = string.Empty;
        public string UnitConversionHostURL { get; set; } = string.Empty;
        public string VerticalDatumHostURL { get; set; } = string.Empty;
    }
}
