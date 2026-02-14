using DWIS.RigOS.Common.Worker;

namespace DWIS.Service.ActiveVolume.Model
{
    public class ConfigurationForActiveVolume : Configuration
    {
        public double MinDtSeconds { get; set; } = 0.2;
        public double MaxDtSeconds { get; set; } = 5.0;
        public double DefaultDtSeconds { get; set; } = 1.0;

        public double MinReturnProportion { get; set; } = 0.0;
        public double MaxReturnProportion { get; set; } = 1.0;
        public double InitReturnProportionEpsilon { get; set; } = 1e-3;

        public double MinCapacityScale { get; set; } = 1e-6;
        public double InitCapacityScaleWhenNoReturn { get; set; } = 0.02;

        public double MeasurementVarianceR { get; set; } = 0.05 * 0.05;
        public double ProcessVarianceBiasQb { get; set; } = 1e-8;
        public double ProcessVarianceCapacityQc { get; set; } = 1e-8;
        public double ProcessVarianceModelQmodel { get; set; } = 1e-5;

        public double SigmaReturnProportion { get; set; } = 0.03;
        public double SigmaCuttingsFlow { get; set; } = 5e-4;
        public double SigmaInletFlow { get; set; } = 5e-4;

        public double MaxNis { get; set; } = 16.0;
        public double InnovationCovarianceFloor { get; set; } = 1e-12;
        public double MinStateVarianceFloor { get; set; } = 1e-12;

        public double InitVolumeVariance { get; set; } = 1.0;
        public double InitBiasVariance { get; set; } = 1e-4;
        public double InitCapacityVariance { get; set; } = 1e-2;
    }
}
