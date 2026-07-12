using DWIS.Service.ActiveVolume.CalibrationWebPages;

namespace DWIS.Service.ActiveVolume.CalibrationWebApp
{
#pragma warning disable CS8767
    public sealed class WebPagesHostConfiguration :
        IActiveVolumeCalibrationWebPagesConfiguration,
        NORCE.Drilling.Field.WebPages.IFieldWebPagesConfiguration,
        NORCE.Drilling.Cluster.WebPages.IClusterWebPagesConfiguration,
        NORCE.Drilling.Well.WebPages.IWellWebPagesConfiguration,
        NORCE.Drilling.WellBore.WebPages.IWellBoreWebPagesConfiguration,
        NORCE.Drilling.WellBoreArchitecture.WebPages.IWellBoreArchitectureWebPagesConfiguration,
        NORCE.Drilling.DrillString.WebPages.IDrillStringWebPagesConfiguration,
        NORCE.Drilling.VerticalDatum.WebPage.IVerticalDatumWebPageConfiguration
    {
        public string ActiveVolumeCalibrationHostURL { get; set; } = string.Empty;
        public string FieldHostURL { get; set; } = string.Empty;
        public string ClusterHostURL { get; set; } = string.Empty;
        public string WellHostURL { get; set; } = string.Empty;
        public string WellBoreHostURL { get; set; } = string.Empty;
        public string WellBoreArchitectureHostURL { get; set; } = string.Empty;
        public string DrillStringHostURL { get; set; } = string.Empty;
        public string UnitConversionHostURL { get; set; } = string.Empty;
        public string VerticalDatumHostURL { get; set; } = string.Empty;
        public string RigHostURL { get; set; } = string.Empty;
        public string TrajectoryHostURL { get; set; } = string.Empty;
        public string CartographicProjectionHostURL { get; set; } = string.Empty;
    }
#pragma warning restore CS8767
}
