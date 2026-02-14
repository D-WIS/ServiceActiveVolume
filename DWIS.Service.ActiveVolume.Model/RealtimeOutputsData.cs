using DWIS.API.DTO;
using DWIS.Vocabulary.Schemas;
using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using OSDC.UnitConversion.Conversion;
using OSDC.UnitConversion.Conversion.DrillingEngineering;
using System.Reflection;
using DWIS.RigOS.Common.Worker;

namespace DWIS.Service.ActiveVolume.Model
{
    public class RealtimeOutputsData : DWISData
    {
        private static readonly Lazy<IReadOnlyDictionary<PropertyInfo, Dictionary<string, QuerySpecification>>> LocalSparQLQueries = new(BuildSparQLQueries(typeof(RealtimeOutputsData)));
        private static readonly Lazy<IReadOnlyDictionary<PropertyInfo, ManifestFile>> LocalManifests = new(BuildManifests(typeof(RealtimeOutputsData), "ActiveVolumeDataManifest", "DWIS", "DWISService"));
        public override Lazy<IReadOnlyDictionary<PropertyInfo, Dictionary<string, QuerySpecification>>> SparQLQueries { get => LocalSparQLQueries; }
        public override Lazy<IReadOnlyDictionary<PropertyInfo, ManifestFile>> Manifests { get => LocalManifests; }

        [AccessToVariable(CommonProperty.VariableAccessType.Assignable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticDiracVariable("correctedActiveVolume")]
        [SemanticFact("correctedActiveVolume", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("correctedActiveVolume#01", Nouns.Enum.CorrectedMeasurement)]
        [SemanticFact("correctedActiveVolume#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("correctedActiveVolume#01", Verbs.Enum.HasDynamicValue, "correctedActiveVolume")]
        [SemanticFact("correctedActiveVolume#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.VolumeDrilling)]
        [SemanticFact("movingAverageCorrectedActiveVolume", Nouns.Enum.MovingAverage)]
        [SemanticFact("correctedActiveVolume#01", Verbs.Enum.IsTransformationOutput, "movingAverageCorrectedActiveVolume")]
        [SemanticFact("correctedActivePitLogical#01", Nouns.Enum.ActivePitLogical)]
        [SemanticFact("correctedActiveVolume#01", Verbs.Enum.IsVolumeAt, "correctedActivePitLogical#01")]
        [SemanticFact("correctionService#01", Nouns.Enum.DataAnalysisServiceCompany)]
        [SemanticFact("correctedActiveVolume#01", Verbs.Enum.IsProvidedBy, "correctionService#01")]
        [SemanticFact("correctedActiveVolume#01", Nouns.Enum.ActiveVolume)]
        public ScalarProperty? CorrectedActiveVolume { get; set; } = null;

        [AccessToVariable(CommonProperty.VariableAccessType.Assignable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticDiracVariable("estimatedPitVolumeFlowBias")]
        [SemanticFact("estimatedPitVolumeFlowBias", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("estimatedPitVolumeFlowBias#01", Nouns.Enum.CorrectionFactor)]
        [SemanticFact("estimatedPitVolumeFlowBias#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("estimatedPitVolumeFlowBias#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.VolumetricFlowrateDrilling)]
        [SemanticFact("estimatedPitVolumeFlowBias#01", Verbs.Enum.HasDynamicValue, "estimatedPitVolumeFlowBias")]
        [SemanticFact("correctedActiveVolume#01", Nouns.Enum.CorrectedMeasurement)]
        [SemanticFact("correctedActiveVolume#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("correctedActiveVolume#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.VolumeDrilling)]
        [SemanticFact("movingAverageCorrectedActiveVolume", Nouns.Enum.MovingAverage)]
        [SemanticFact("correctedActiveVolume#01", Verbs.Enum.IsTransformationOutput, "movingAverageCorrectedActiveVolume")]
        [SemanticFact("correctedActivePitLogical#01", Nouns.Enum.ActivePitLogical)]
        [SemanticFact("correctedActiveVolume#01", Verbs.Enum.IsVolumeAt, "correctedActivePitLogical#01")]
        [SemanticFact("estimatedPitVolumeFlowBias#01", Verbs.Enum.Corrects, "correctedActiveVolume#01")]
        [SemanticFact("pitVolumeEstimator#01", Nouns.Enum.DataAnalysisServiceCompany)]
        [SemanticFact("estimatedPitVolumeFlowBias#01", Verbs.Enum.IsProvidedBy, "pitVolumeEstimator#01")]
        public ScalarProperty? EstimatedPitVolumeFlowBias { get; set; } = null;

        [AccessToVariable(CommonProperty.VariableAccessType.Assignable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticDiracVariable("returnFlowCapacityScale")]
        [SemanticFact("returnFlowCapacityScale", Nouns.Enum.DynamicDrillingSignal)]
        [SemanticFact("returnFlowCapacityScale#01", Nouns.Enum.CalibrationParameter)]
        [SemanticFact("returnFlowCapacityScale#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("returnFlowCapacityScale#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.VolumetricFlowrateDrilling)]
        [SemanticFact("returnFlowCapacityScale#01", Verbs.Enum.HasDynamicValue, "returnFlowCapacityScale")]
        [SemanticFact("returnFlowProportion#01", Nouns.Enum.ComputedData)]
        [SemanticFact("returnFlowProportion#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("returnFlowProportion#01", Verbs.Enum.IsOfMeasurableQuantity, BasePhysicalQuantity.QuantityEnum.DimensionLessStandard)]
        [SemanticFact("topSideTelemetry", Nouns.Enum.TopSideTelemetry)]
        [SemanticFact("returnFlowProportion#01", Verbs.Enum.IsTransmittedBy, "topSideTelemetry")]
        [SemanticFact("movingAverageReturnFlowProportion", Nouns.Enum.MovingAverage)]
        [SemanticFact("returnFlowProportion#01", Verbs.Enum.IsTransformationOutput, "movingAverageReturnFlowProportion")]
        [SemanticFact("ShaleShakerElement#01", Nouns.Enum.CuttingSeparatorLogical)]
        [SemanticFact("DrillingFluid#01", Nouns.Enum.DrillingLiquidType)]
        [SemanticFact("DrillingFluid#01", Verbs.Enum.IsFluidTypeLocatedAt, "ShaleShakerElement#01")]
        [SemanticFact("returnFlowProportion#01", Verbs.Enum.IsHydraulicEstimationAt, "ShaleShakerElement#01")]
        [SemanticFact("ImageInterpreter#01", Nouns.Enum.Interpreter)]
        [SemanticFact("returnFlowProportion#01", Verbs.Enum.IsComputedBy, "ImageInterpreter#01")]
        [SemanticFact("returnFlowCapacityScale#01", Verbs.Enum.Scales, "returnFlowProportion#01")]
        [SemanticFact("returnFlowConversion", Nouns.Enum.Transformation)]
        [SemanticFact("returnFlowCapacityScale#01", Verbs.Enum.IsGainOf, "returnFlowConversion")]
        [SemanticFact("pitVolumeEstimator#01", Nouns.Enum.DataAnalysisServiceCompany)]
        [SemanticFact("returnFlowCapacityScale#01", Verbs.Enum.IsProvidedBy, "pitVolumeEstimator#01")]
        public ScalarProperty? ReturnFlowCapacityScale { get; set; } = null;
    }
}
