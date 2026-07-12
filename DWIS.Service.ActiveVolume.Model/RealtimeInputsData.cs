using DWIS.API.DTO;
using DWIS.Vocabulary.Schemas;
using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using OSDC.UnitConversion.Conversion;
using OSDC.UnitConversion.Conversion.DrillingEngineering;
using System.Reflection;
using DWIS.RigOS.Common.Worker;

namespace DWIS.Service.ActiveVolume.Model
{
    public class RealtimeInputsData :DWISData
    {

        private static readonly Lazy<IReadOnlyDictionary<PropertyInfo, Dictionary<string, QuerySpecification>>> LocalSparQLQueries = new(BuildSparQLQueries(typeof(RealtimeInputsData)));
        private static readonly Lazy<IReadOnlyDictionary<PropertyInfo, ManifestFile>> LocalManifests = new(BuildManifests(typeof(RealtimeInputsData), "ActiveVolumeData", "DWIS", "DWISService"));
        public override Lazy<IReadOnlyDictionary<PropertyInfo, Dictionary<string, QuerySpecification>>> SparQLQueries { get => LocalSparQLQueries; }
        public override Lazy<IReadOnlyDictionary<PropertyInfo, ManifestFile>> Manifests { get => LocalManifests; }

        [AccessToVariable(CommonProperty.VariableAccessType.Assignable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticExclusiveOr(1, 2)]
        [SemanticDiracVariable("activeVolume")]
        [SemanticFact("activeVolume", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("activeVolume#01", Nouns.Enum.Measurement)]
        [SemanticFact("activeVolume#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("activeVolume#01", Verbs.Enum.HasDynamicValue, "activeVolume")]
        [SemanticFact("activeVolume#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.VolumeDrilling)]
        [OptionalFact(1, "movingAverageActiveVolume", Nouns.Enum.MovingAverage)]
        [OptionalFact(1, "activeVolume#01", Verbs.Enum.IsTransformationOutput, "movingAverageActiveVolume")]
        [OptionalFact(1, "activePitLogical#01", Nouns.Enum.ActivePitLogical)]
        [OptionalFact(1, "activeVolume#01", Verbs.Enum.IsVolumeAt, "activePitLogical#01")]
        [OptionalFact(2, "activeVolume#01", Nouns.Enum.ActiveVolume)]
        public ScalarProperty? ActiveVolume { get; set; } = null;

        [AccessToVariable(CommonProperty.VariableAccessType.Readable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticExclusiveOr(1, 2)]
        [SemanticDiracVariable("Q_tos")]
        [SemanticFact("Q_tos", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("Q_tos#01", Nouns.Enum.Measurement)]
        [SemanticFact("Q_tos#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("Q_tos#01", Verbs.Enum.HasDynamicValue, "Q_tos")]
        [SemanticFact("Q_tos#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.VolumetricFlowrateDrilling)]
        [OptionalFact(1, "movingAverageQ_tos", Nouns.Enum.MovingAverage)]
        [OptionalFact(1, "Q_tos#01", Verbs.Enum.IsTransformationOutput, "movingAverageQ_tos")]
        [OptionalFact(1, "topOfStringJunction#01", Nouns.Enum.TopOfStringJunction)]
        [OptionalFact(1, "inletHydraulicBranch#01", Nouns.Enum.HydraulicBranch)]
        [OptionalFact(1, "topOfStringJunction#01", Verbs.Enum.HasUpstreamBranch, "inletHydraulicBranch#01")]
        [OptionalFact(1, "Q_tos#01", Verbs.Enum.IsAssociatedToHydraulicBranch, "inletHydraulicBranch#01")]
        [OptionalFact(2, "Q_tos#01", Nouns.Enum.FlowRateIn)]
        public ScalarProperty? FlowrateIn { get; set; } = null;

        [AccessToVariable(CommonProperty.VariableAccessType.Readable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticExclusiveOr(1, 2)]
        [SemanticDiracVariable("BH_depth")]
        [SemanticFact("BH_depth", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("BH_depth#01", Nouns.Enum.Measurement)]
        [SemanticFact("BH_depth#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("BH_depth#01", Verbs.Enum.HasDynamicValue, "BH_depth")]
        [SemanticFact("BH_depth#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.DepthDrilling)]
        [OptionalFact(1, "movingAverageBH_depth", Nouns.Enum.MovingAverage)]
        [OptionalFact(1, "BH_depth#01", Verbs.Enum.IsTransformationOutput, "movingAverageBH_depth")]
        [OptionalFact(1, "curvilinearAbscissaFrame#01", Nouns.Enum.OneDimensionalCurviLinearReferenceFrame)]
        [OptionalFact(1, "BH_depth#01", Verbs.Enum.HasReferenceFrame, "curvilinearAbscissaFrame#01")]
        [OptionalFact(1, "bh#01", Nouns.Enum.HoleBottomLocation)]
        [OptionalFact(1, "BH_depth#01", Verbs.Enum.IsPhysicallyLocatedAt, "bh#01")]
        [OptionalFact(2, "BH_depth#01", Nouns.Enum.HoleDepth)]
        public ScalarProperty? BottomHoleDepth { get; set; } = null;

        [AccessToVariable(CommonProperty.VariableAccessType.Readable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticExclusiveOr(1, 2)]
        [SemanticDiracVariable("BOS_depth")]
        [SemanticFact("BOS_depth", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("BOS_depth#01", Nouns.Enum.Measurement)]
        [SemanticFact("BOS_depth#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("BOS_depth#01", Verbs.Enum.HasDynamicValue, "BOS_depth")]
        [SemanticFact("BOS_depth#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.DepthDrilling)]
        [OptionalFact(1, "movingAverageBOS_depth", Nouns.Enum.MovingAverage)]
        [OptionalFact(1, "BOS_depth#01", Verbs.Enum.IsTransformationOutput, "movingAverageBOS_depth")]
        [OptionalFact(1, "curvilinearAbscissaFrame#01", Nouns.Enum.OneDimensionalCurviLinearReferenceFrame)]
        [OptionalFact(1, "BOS_depth#01", Verbs.Enum.HasReferenceFrame, "curvilinearAbscissaFrame#01")]
        [OptionalFact(1, "bos#01", Nouns.Enum.BottomOfStringReferenceLocation)]
        [OptionalFact(1, "BOS_depth#01", Verbs.Enum.IsPhysicallyLocatedAt, "bos#01")]
        [OptionalFact(2, "BOS_depth#01", Nouns.Enum.BitDepth)]
        public ScalarProperty? BottomOfStringDepth { get; set; } = null;

        [AccessToVariable(CommonProperty.VariableAccessType.Readable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticDiracVariable("returnFlowPaddle")]
        [SemanticFact("returnFlowPaddle", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("returnFlowPaddle#01", Nouns.Enum.Measurement)]
        [SemanticFact("returnFlowPaddle#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("returnFlowPaddle#01", Verbs.Enum.HasDynamicValue, "returnFlowPaddle")]
        [SemanticFact("returnFlowPaddle#01", Verbs.Enum.IsOfMeasurableQuantity, BasePhysicalQuantity.QuantityEnum.ProportionStandard)]
        public ScalarProperty? FlowPaddlePosition { get; set; } = null;

        [AccessToVariable(CommonProperty.VariableAccessType.Readable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticDiracVariable("flowRateOut")]
        [SemanticFact("flowRateOut", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("flowRateOut#01", Nouns.Enum.Measurement)]
        [SemanticFact("flowRateOut#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("flowRateOut#01", Verbs.Enum.HasDynamicValue, "flowRateOut")]
        [SemanticFact("flowRateOut#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.VolumetricFlowrateDrilling)]
        public ScalarProperty? CoriolisVolumetricFlowrate { get; set; } = null;

        [AccessToVariable(CommonProperty.VariableAccessType.Readable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticDiracVariable("massFlowRateOut")]
        [SemanticFact("massFlowRateOut", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("massFlowRateOut#01", Nouns.Enum.Measurement)]
        [SemanticFact("massFlowRateOut#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("massFlowRateOut#01", Verbs.Enum.HasDynamicValue, "massFlowRateOut")]
        [SemanticFact("massFlowRateOut#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.MassRateDrilling)]
        public ScalarProperty? CoriolisMassFlowrate { get; set; } = null;

        [AccessToVariable(CommonProperty.VariableAccessType.Readable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticDiracVariable("SPP")]
        [SemanticFact("SPP", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("SPP#01", Nouns.Enum.Measurement)]
        [SemanticFact("SPP#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("SPP#01", Verbs.Enum.HasDynamicValue, "SPP")]
        [SemanticFact("SPP#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.PressureDrilling)]
        public ScalarProperty? StandPipePressure { get; set; } = null;

        [AccessToVariable(CommonProperty.VariableAccessType.Readable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticDiracVariable("returnMudDensity")]
        [SemanticFact("returnMudDensity", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("returnMudDensity#01", Nouns.Enum.Measurement)]
        [SemanticFact("returnMudDensity#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("returnMudDensity#01", Verbs.Enum.HasDynamicValue, "returnMudDensity")]
        [SemanticFact("returnMudDensity#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.MassDensityDrilling)]
        public ScalarProperty? ReturnMudDensity { get; set; } = null;

        [AccessToVariable(CommonProperty.VariableAccessType.Assignable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticStringVariable("CleanSightShakerLoadEstimate")]
        [SemanticFact("CleanSightShakerLoadEstimate", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("CleanSightShakerLoadEstimate#01", Nouns.Enum.ComputedData)]
        [SemanticFact("CleanSightShakerLoadEstimate#01", Nouns.Enum.JSonDataType)]
        [SemanticFact("CleanSightShakerLoadEstimate#01", Verbs.Enum.HasDynamicValue, "CleanSightShakerLoadEstimate")]
        [SemanticFact("CleanSightShakerLoadEstimate#01", Verbs.Enum.IsOfMeasurableQuantity, BasePhysicalQuantity.QuantityEnum.DimensionLessStandard)]
        [SemanticFact("topSideTelemetry", Nouns.Enum.TopSideTelemetry)]
        [SemanticFact("CleanSightShakerLoadEstimate#01", Verbs.Enum.IsTransmittedBy, "topSideTelemetry")]
        [SemanticFact("movingAverageCleanSightShakerLoadEstimate", Nouns.Enum.MovingAverage)]
        [SemanticFact("CleanSightShakerLoadEstimate#01", Verbs.Enum.IsTransformationOutput, "movingAverageCleanSightShakerLoadEstimate")]
        [SemanticFact("ShaleShakerElement#01", Nouns.Enum.CuttingSeparatorLogical)]
        [SemanticFact("DrillingFluid#01", Nouns.Enum.DrillingLiquidType)]
        [SemanticFact("DrillingFluid#01", Verbs.Enum.IsFluidTypeLocatedAt, "ShaleShakerElement#01")]
        [SemanticFact("CleanSightShakerLoadEstimate#01", Verbs.Enum.IsHydraulicEstimationAt, "ShaleShakerElement#01")]
        [SemanticFact("ImageInterpreter#01", Nouns.Enum.Interpreter)]
        [SemanticFact("CleanSightShakerLoadEstimate#01", Verbs.Enum.IsComputedBy, "ImageInterpreter#01")]
        [SemanticFact("DrillDocs#01", Nouns.Enum.InstrumentationCompany)]
        [SemanticFact("CleanSightShakerLoadEstimate#01", Verbs.Enum.IsProvidedBy, "DrillDocs#01")]
        public ScalarsProperty? ShakerLoadEstimates { get; set; } = null;

        [AccessToVariable(CommonProperty.VariableAccessType.Assignable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticStringVariable("CleanSightCuttingsRecoveryRate")]
        [SemanticFact("CleanSightCuttingsRecoveryRate", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("CleanSightCuttingsRecoveryRate#01", Nouns.Enum.ComputedData)]
        [SemanticFact("CleanSightCuttingsRecoveryRate#01", Nouns.Enum.JSonDataType)]
        [SemanticFact("CleanSightCuttingsRecoveryRate#01", Verbs.Enum.HasDynamicValue, "CleanSightCuttingsRecoveryRate")]
        [SemanticFact("CleanSightCuttingsRecoveryRate#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.VolumetricFlowrateDrilling)]
        [SemanticFact("topSideTelemetry", Nouns.Enum.TopSideTelemetry)]
        [SemanticFact("CleanSightCuttingsRecoveryRate#01", Verbs.Enum.IsTransmittedBy, "topSideTelemetry")]
        [SemanticFact("movingAverageCleanSightCuttingsRecoveryRate", Nouns.Enum.MovingAverage)]
        [SemanticFact("CleanSightCuttingsRecoveryRate#01", Verbs.Enum.IsTransformationOutput, "movingAverageCleanSightCuttingsRecoveryRate")]
        [SemanticFact("ShaleShakerElement#01", Nouns.Enum.CuttingSeparatorLogical)]
        [SemanticFact("DrillingFluid#01", Nouns.Enum.DrillingLiquidType)]
        [SemanticFact("Cuttings#01", Nouns.Enum.CuttingsComponent)]
        [SemanticFact("Cuttings#01", Verbs.Enum.IsAComponentOf, "DrillingFluid#01")]
        [SemanticFact("CleanSightCuttingsRecoveryRate#01", Verbs.Enum.ConcernsAFluidComponent, "Cuttings#01")]
        [SemanticFact("DrillingFluid#01", Verbs.Enum.IsFluidTypeLocatedAt, "ShaleShakerElement#01")]
        [SemanticFact("CleanSightCuttingsRecoveryRate#01", Verbs.Enum.IsHydraulicEstimationAt, "ShaleShakerElement#01")]
        [SemanticFact("ImageInterpreter#01", Nouns.Enum.Interpreter)]
        [SemanticFact("CleanSightCuttingsRecoveryRate#01", Verbs.Enum.IsComputedBy, "ImageInterpreter#01")]
        [SemanticFact("DrillDocs#01", Nouns.Enum.InstrumentationCompany)]
        [SemanticFact("CleanSightCuttingsRecoveryRate#01", Verbs.Enum.IsProvidedBy, "DrillDocs#01")]
        public ScalarsProperty? CuttingsRecoveryRates { get; set; } = null;
    }
}
