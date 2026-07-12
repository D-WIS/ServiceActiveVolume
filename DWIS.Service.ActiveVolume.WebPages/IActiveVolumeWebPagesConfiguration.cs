namespace DWIS.Service.ActiveVolume.WebPages
{
    public interface IActiveVolumeWebPagesConfiguration
    {
        string ActiveVolumeCalibrationHostURL { get; }
        string FieldHostURL { get; }
        string ClusterHostURL { get; }
        string WellHostURL { get; }
        string WellBoreHostURL { get; }
        string WellBoreArchitectureHostURL { get; }
        string DrillStringHostURL { get; }
        string UnitConversionHostURL { get; }
        string VerticalDepthHostURL { get; }
    }
}
