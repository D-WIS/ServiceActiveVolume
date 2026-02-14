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
        private static readonly Lazy<IReadOnlyDictionary<PropertyInfo, ManifestFile>> LocalManifests = new(BuildManifests(typeof(RealtimeInputsData), "ActiveVolumeDataManifest", "DWIS", "DWISService"));
        public override Lazy<IReadOnlyDictionary<PropertyInfo, Dictionary<string, QuerySpecification>>> SparQLQueries { get => LocalSparQLQueries; }
        public override Lazy<IReadOnlyDictionary<PropertyInfo, ManifestFile>> Manifests { get => LocalManifests; }

        [AccessToVariable(CommonProperty.VariableAccessType.Assignable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticDiracVariable("activeVolume")]
        [SemanticFact("activeVolume", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("activeVolume#01", Nouns.Enum.Measurement)]
        [SemanticFact("activeVolume#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("activeVolume#01", Verbs.Enum.HasDynamicValue, "activeVolume")]
        [SemanticFact("activeVolume#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.VolumeDrilling)]
        [SemanticFact("movingAverageActiveVolume", Nouns.Enum.MovingAverage)]
        [SemanticFact("activeVolume#01", Verbs.Enum.IsTransformationOutput, "movingAverageActiveVolume")]
        [SemanticFact("activePitLogical#01", Nouns.Enum.ActivePitLogical)]
        [SemanticFact("activeVolume#01", Verbs.Enum.IsVolumeAt, "activePitLogical#01")]
        [SemanticFact("activeVolume#01", Nouns.Enum.ActiveVolume)]
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
        [SemanticFact("movingAverageQ_tos", Nouns.Enum.MovingAverage)]
        [SemanticFact("Q_tos#01", Verbs.Enum.IsTransformationOutput, "movingAverageQ_tos")]
        [SemanticFact("topOfStringJunction#01", Nouns.Enum.TopOfStringJunction)]
        [SemanticFact("inletHydraulicBranch#01", Nouns.Enum.HydraulicBranch)]
        [SemanticFact("topOfStringJunction#01", Verbs.Enum.HasUpstreamBranch, "inletHydraulicBranch#01")]
        [SemanticFact("Q_tos#01", Verbs.Enum.IsAssociatedToHydraulicBranch, "inletHydraulicBranch#01")]
        [SemanticFact("Q_tos#01", Nouns.Enum.FlowRateIn)]
        public ScalarProperty? FlowrateIn { get; set; } = null;

        [AccessToVariable(CommonProperty.VariableAccessType.Readable)]
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
        public GaussianValuesProperty? ShakerLoadEstimates { get; set; } = null;

        [AccessToVariable(CommonProperty.VariableAccessType.Readable)]
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
        public GaussianValuesProperty? CuttingsRecoveryRates { get; set; } = null;
    }
}
