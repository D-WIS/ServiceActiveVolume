using DWIS.Service.ActiveVolume.Model.Calibration;
using DWIS.Service.ActiveVolume.Model.Case;

namespace DWIS.Service.ActiveVolume.Model.Fusion
{
    public interface IReturnFlowModel
    {
        double EstimateReturnFlow(ActiveVolumeSample sample, ActiveVolumeContext context, CalibrationRecord? calibration);
    }

    public sealed class CompositeReturnFlowModel : IReturnFlowModel
    {
        private readonly FlowPaddleReturnFlowModel flowPaddle_ = new();
        private readonly CoriolisReturnFlowModel coriolis_ = new();

        public double EstimateReturnFlow(ActiveVolumeSample sample, ActiveVolumeContext context, CalibrationRecord? calibration)
        {
            return context.ReturnFlowMeasurementMode switch
            {
                ReturnFlowMeasurementMode.FlowPaddle => flowPaddle_.EstimateReturnFlow(sample, context, calibration),
                ReturnFlowMeasurementMode.CoriolisVolumetric => coriolis_.EstimateReturnFlow(sample, context, calibration),
                ReturnFlowMeasurementMode.CoriolisMass => coriolis_.EstimateReturnFlow(sample, context, calibration),
                _ => Math.Max(0.0, sample.FlowrateIn ?? 0.0)
            };
        }
    }

    public sealed class FlowPaddleReturnFlowModel : IReturnFlowModel
    {
        public double EstimateReturnFlow(ActiveVolumeSample sample, ActiveVolumeContext context, CalibrationRecord? calibration)
        {
            double paddle = Math.Max(0.0, sample.FlowPaddlePosition ?? 0.0);
            double onset = GetParameter(calibration, CalibrationComponent.ReturnFlow, "PaddleOnset", 0.05);
            double scale = GetParameter(calibration, CalibrationComponent.ReturnFlow, "PaddleScale", Math.Max(0.0, sample.FlowrateIn ?? 0.0));
            double exponent = GetParameter(calibration, CalibrationComponent.ReturnFlow, "PaddleExponent", 1.0);
            double lowFlowDelayFactor = GetParameter(calibration, CalibrationComponent.LowFlowSurrogate, "LowFlowGain", 0.85);

            if (paddle <= onset)
            {
                return Math.Max(0.0, lowFlowDelayFactor * (sample.FlowrateIn ?? 0.0));
            }

            return Math.Max(0.0, scale * Math.Pow(paddle - onset, exponent));
        }

        private static double GetParameter(CalibrationRecord? calibration, CalibrationComponent component, string name, double fallback)
        {
            return calibration?.Components.FirstOrDefault(x => x.Component == component)?.Parameters.TryGetValue(name, out double value) == true
                ? value
                : fallback;
        }
    }

    public sealed class CoriolisReturnFlowModel : IReturnFlowModel
    {
        public double EstimateReturnFlow(ActiveVolumeSample sample, ActiveVolumeContext context, CalibrationRecord? calibration)
        {
            double scale = calibration?.Components.FirstOrDefault(x => x.Component == CalibrationComponent.ReturnFlow)?.Parameters.TryGetValue("CoriolisScale", out double value) == true
                ? value
                : 1.0;

            if (context.ReturnFlowMeasurementMode == ReturnFlowMeasurementMode.CoriolisMass)
            {
                double density = sample.ReturnMudDensity ?? 0.0;
                if (density <= 0.0)
                {
                    return 0.0;
                }

                return Math.Max(0.0, scale * (sample.CoriolisMassFlowrate ?? 0.0) / density);
            }

            return Math.Max(0.0, scale * (sample.CoriolisVolumetricFlowrate ?? 0.0));
        }
    }
}
