using System.Reflection;

namespace DWIS.Service.ActiveVolume.CalibrationWebApp
{
    public static class ExternalRazorAssemblies
    {
        public static IReadOnlyList<Assembly> All { get; } =
        [
            typeof(DWIS.Service.ActiveVolume.CalibrationWebPages.ActiveVolumeCalibrationNavMenu).Assembly,
            typeof(NORCE.Drilling.Field.WebPages.Field).Assembly,
            typeof(NORCE.Drilling.Cluster.WebPages.ClusterMain).Assembly,
            typeof(NORCE.Drilling.Well.WebPages.WellMain).Assembly,
            typeof(NORCE.Drilling.WellBore.WebPages.WellBoreMain).Assembly,
            typeof(NORCE.Drilling.WellBoreArchitecture.WebPages.Pages.WellBoreArchitectureMain).Assembly,
            typeof(NORCE.Drilling.DrillString.WebPages.Pages.DrillStringPages.DrillStringMain).Assembly,
            typeof(OSDC.UnitConversion.WebPages.SingleUnitConversionMain).Assembly
        ];
    }
}
