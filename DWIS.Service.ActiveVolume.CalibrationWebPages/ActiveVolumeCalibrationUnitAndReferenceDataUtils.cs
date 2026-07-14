using OSDC.UnitConversion.DrillingRazorMudComponents;

namespace DWIS.Service.ActiveVolume.CalibrationWebPages
{
    public static class ActiveVolumeCalibrationUnitAndReferenceDataUtils
    {
        public static class UnitAndReferenceParameters
        {
            public static string? UnitSystemName { get; set; } = "Metric";
            public static string? DepthReferenceName { get; set; } = "Rotary table";
        }

        public static GroundMudLineDepthReferenceSource GroundMudLineDepthReferenceSource { get; } = new();
        public static MeanSeaLevelDepthReferenceSource MeanSeaLevelDepthReferenceSource { get; } = new();
        public static SeaWaterLevelDepthReferenceSource SeaWaterLevelDepthReferenceSource { get; } = new();
        public static RotaryTableDepthReferenceSource RotaryTableDepthReferenceSource { get; } = new();

        public static void UpdateUnitSystemName(string value) => UnitAndReferenceParameters.UnitSystemName = value;
        public static void UpdateDepthReferenceName(string value) => UnitAndReferenceParameters.DepthReferenceName = value;
    }

    public sealed class GroundMudLineDepthReferenceSource : IGroundMudLineDepthReferenceSource
    {
        public double? GroundMudLineDepthReference { get; set; }
    }

    public sealed class MeanSeaLevelDepthReferenceSource : IMeanSeaLevelDepthReferenceSource
    {
        public double? MeanSeaLevelDepthReference { get; set; }
    }

    public sealed class RotaryTableDepthReferenceSource : IRotaryTableDepthReferenceSource
    {
        public double? RotaryTableDepthReference { get; set; }
    }

    public sealed class SeaWaterLevelDepthReferenceSource : ISeaWaterLevelDepthReferenceSource
    {
        public double? SeaWaterLevelDepthReference { get; set; }
    }

}
