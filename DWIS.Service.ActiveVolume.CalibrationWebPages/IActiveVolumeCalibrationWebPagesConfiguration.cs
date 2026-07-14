namespace DWIS.Service.ActiveVolume.CalibrationWebPages
{
    public interface IActiveVolumeCalibrationWebPagesConfiguration
    {
        string ActiveVolumeCalibrationHostURL { get; }
        string FieldHostURL { get; }
        string ClusterHostURL { get; }
        string WellHostURL { get; }
        string WellBoreHostURL { get; }
        string WellBoreArchitectureHostURL { get; }
        string DrillStringHostURL { get; }
        string RigHostURL { get; }
        string UnitConversionHostURL { get; }
        string VerticalDatumHostURL { get; }
    }
}
