using System;
namespace DWIS.Service.ActiveVolume.Model
{
    public class SensorFusion
    {
        private static readonly object _stateLock = new object();

        private static bool _isInitialized = false;
        private static DateTime _lastTimestampUtc = DateTime.MinValue;

        // EKF state x = [V, b, C]
        private static double _vHat;
        private static double _bHat;
        private static double _cHat;

        // 3x3 covariance matrix
        private static readonly double[,] _p = new double[3, 3];

        public static void FuseData(ConfigurationForActiveVolume configuration, RealtimeInputsData inputs, RealtimeOutputsData outputs)
        {
            double flowrateOutProportion = 0.0;
            if (inputs.ShakerLoadEstimates is not null && inputs.ShakerLoadEstimates.Values is not null)
            {
                int count = 0;
                foreach (var shakerLoadEstimate in inputs.ShakerLoadEstimates.Values)
                {
                    if (shakerLoadEstimate is not null && shakerLoadEstimate.Mean is not null)
                    {
                        flowrateOutProportion += shakerLoadEstimate.Mean.Value / 10.0;
                        count++;
                    }
                }
                if (count > 0)
                {
                    flowrateOutProportion /= count;
                }
            }

            double cuttingsFlowrate = 0.0;
            if (inputs.CuttingsRecoveryRates is not null && inputs.CuttingsRecoveryRates.Values is not null)
            {
                foreach (var cuttingsRecoveryRate in inputs.CuttingsRecoveryRates.Values)
                {
                    if (cuttingsRecoveryRate is not null && cuttingsRecoveryRate.Mean is not null)
                    {
                        cuttingsFlowrate += cuttingsRecoveryRate.Mean.Value;
                    }
                }
            }

            double flowrateIn = 0.0;
            if (inputs.FlowrateIn is not null && inputs.FlowrateIn.Value is not null)
            {
                flowrateIn = inputs.FlowrateIn.Value.Value;
            }

            double activeVolumeMeasured = 0.0;
            if (inputs.ActiveVolume is not null && inputs.ActiveVolume.Value is not null)
            {
                activeVolumeMeasured = inputs.ActiveVolume.Value.Value;
            }

            lock (_stateLock)
            {
                double minDtSeconds = Math.Max(configuration.MinDtSeconds, configuration.InnovationCovarianceFloor);
                double maxDtSeconds = Math.Max(configuration.MaxDtSeconds, minDtSeconds);
                double defaultDtSeconds = Clamp(configuration.DefaultDtSeconds, minDtSeconds, maxDtSeconds);
                double minReturnProportion = configuration.MinReturnProportion;
                double maxReturnProportion = Math.Max(configuration.MaxReturnProportion, minReturnProportion);
                double minCapacityScale = Math.Max(configuration.MinCapacityScale, configuration.InnovationCovarianceFloor);
                double maxNis = Math.Max(configuration.MaxNis, 0.0);
                double r = Math.Max(configuration.MeasurementVarianceR, configuration.InnovationCovarianceFloor);
                double qb = Math.Max(configuration.ProcessVarianceBiasQb, 0.0);
                double qc = Math.Max(configuration.ProcessVarianceCapacityQc, 0.0);
                double qmodel = Math.Max(configuration.ProcessVarianceModelQmodel, 0.0);
                double sigmaR = Math.Max(configuration.SigmaReturnProportion, 0.0);
                double sigmaCut = Math.Max(configuration.SigmaCuttingsFlow, 0.0);
                double sigmaIn = Math.Max(configuration.SigmaInletFlow, 0.0);
                double covarianceFloor = Math.Max(configuration.InnovationCovarianceFloor, 1e-15);
                double minStateVarianceFloor = Math.Max(configuration.MinStateVarianceFloor, covarianceFloor);

                double nowDt = defaultDtSeconds;
                DateTime now = DateTime.UtcNow;
                if (_lastTimestampUtc != DateTime.MinValue)
                {
                    nowDt = (now - _lastTimestampUtc).TotalSeconds;
                }
                _lastTimestampUtc = now;
                double dt = Clamp(nowDt, minDtSeconds, maxDtSeconds);

                double rRet = Clamp(flowrateOutProportion, minReturnProportion, maxReturnProportion);
                double qCut = Math.Max(0.0, cuttingsFlowrate);
                double qIn = Math.Max(0.0, flowrateIn);
                bool hasMeasuredVolume = inputs.ActiveVolume is not null &&
                                         inputs.ActiveVolume.Value is not null &&
                                         !double.IsNaN(activeVolumeMeasured) &&
                                         !double.IsInfinity(activeVolumeMeasured);

                if (!_isInitialized)
                {
                    InitializeFilter(configuration, hasMeasuredVolume ? activeVolumeMeasured : 0.0, qIn, qCut, rRet, minCapacityScale);
                }

                // Prediction step
                double vPred = _vHat + dt * ((_cHat * rRet) - qCut - qIn + _bHat);
                double bPred = _bHat;
                double cPred = _cHat;

                // Jacobian F
                double f00 = 1.0;
                double f01 = dt;
                double f02 = dt * rRet;
                double f10 = 0.0;
                double f11 = 1.0;
                double f12 = 0.0;
                double f20 = 0.0;
                double f21 = 0.0;
                double f22 = 1.0;

                // Qv from linearized input-noise + small model term
                double sigmaQDeltaSquared = (_cHat * _cHat * sigmaR * sigmaR) + (sigmaCut * sigmaCut) + (sigmaIn * sigmaIn);
                double qv = (dt * dt * sigmaQDeltaSquared) + qmodel;

                // PPred = F * P * F' + Q
                double[,] pPred = new double[3, 3];
                PredictCovariance(
                    _p,
                    pPred,
                    f00, f01, f02,
                    f10, f11, f12,
                    f20, f21, f22,
                    qv, qb, qc);

                // Update step with direct pit volume measurement
                double vPost = vPred;
                double bPost = bPred;
                double cPost = cPred;
                double[,] pPost = pPred;
                if (hasMeasuredVolume)
                {
                    double innovation = activeVolumeMeasured - vPred;
                    double s = pPred[0, 0] + r;
                    if (s > covarianceFloor)
                    {
                        double nis = (innovation * innovation) / s;
                        if (nis <= maxNis)
                        {
                            double k0 = pPred[0, 0] / s;
                            double k1 = pPred[1, 0] / s;
                            double k2 = pPred[2, 0] / s;

                            vPost = vPred + k0 * innovation;
                            bPost = bPred + k1 * innovation;
                            cPost = cPred + k2 * innovation;

                            pPost = new double[3, 3];
                            JosephUpdate(pPred, pPost, k0, k1, k2, r);
                        }
                    }
                }

                _vHat = Math.Max(0.0, vPost);
                _bHat = bPost;
                _cHat = Math.Max(minCapacityScale, cPost);

                CopyMatrix(pPost, _p);
                _p[0, 0] = Math.Max(_p[0, 0], minStateVarianceFloor);
                _p[1, 1] = Math.Max(_p[1, 1], minStateVarianceFloor);
                _p[2, 2] = Math.Max(_p[2, 2], minStateVarianceFloor);

                outputs.CorrectedActiveVolume = EnsureInitialized(outputs.CorrectedActiveVolume);
                outputs.CorrectedActiveVolume!.Value = _vHat;

                outputs.EstimatedPitVolumeFlowBias = EnsureInitialized(outputs.EstimatedPitVolumeFlowBias);
                outputs.EstimatedPitVolumeFlowBias!.Value = _bHat;

                outputs.ReturnFlowCapacityScale = EnsureInitialized(outputs.ReturnFlowCapacityScale);
                outputs.ReturnFlowCapacityScale!.Value = _cHat;
            }
        }

        private static void InitializeFilter(
            ConfigurationForActiveVolume configuration,
            double measuredVolume,
            double qIn,
            double qCut,
            double rRet,
            double minCapacityScale)
        {
            _vHat = Math.Max(0.0, measuredVolume);
            _bHat = 0.0;
            if (rRet > Math.Max(configuration.InitReturnProportionEpsilon, 0.0))
            {
                _cHat = Math.Max(minCapacityScale, (qIn + qCut) / rRet);
            }
            else
            {
                _cHat = Math.Max(minCapacityScale, Math.Max(qIn + qCut, configuration.InitCapacityScaleWhenNoReturn));
            }

            // Broad prior for C, narrower for V and b.
            _p[0, 0] = Math.Max(configuration.InitVolumeVariance, 0.0);
            _p[0, 1] = 0.0;
            _p[0, 2] = 0.0;
            _p[1, 0] = 0.0;
            _p[1, 1] = Math.Max(configuration.InitBiasVariance, 0.0);
            _p[1, 2] = 0.0;
            _p[2, 0] = 0.0;
            _p[2, 1] = 0.0;
            _p[2, 2] = Math.Max(configuration.InitCapacityVariance, 0.0);

            _isInitialized = true;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static void PredictCovariance(
            double[,] p,
            double[,] pPred,
            double f00, double f01, double f02,
            double f10, double f11, double f12,
            double f20, double f21, double f22,
            double qv, double qb, double qc)
        {
            double[,] fp = new double[3, 3];
            fp[0, 0] = (f00 * p[0, 0]) + (f01 * p[1, 0]) + (f02 * p[2, 0]);
            fp[0, 1] = (f00 * p[0, 1]) + (f01 * p[1, 1]) + (f02 * p[2, 1]);
            fp[0, 2] = (f00 * p[0, 2]) + (f01 * p[1, 2]) + (f02 * p[2, 2]);
            fp[1, 0] = (f10 * p[0, 0]) + (f11 * p[1, 0]) + (f12 * p[2, 0]);
            fp[1, 1] = (f10 * p[0, 1]) + (f11 * p[1, 1]) + (f12 * p[2, 1]);
            fp[1, 2] = (f10 * p[0, 2]) + (f11 * p[1, 2]) + (f12 * p[2, 2]);
            fp[2, 0] = (f20 * p[0, 0]) + (f21 * p[1, 0]) + (f22 * p[2, 0]);
            fp[2, 1] = (f20 * p[0, 1]) + (f21 * p[1, 1]) + (f22 * p[2, 1]);
            fp[2, 2] = (f20 * p[0, 2]) + (f21 * p[1, 2]) + (f22 * p[2, 2]);

            pPred[0, 0] = (fp[0, 0] * f00) + (fp[0, 1] * f01) + (fp[0, 2] * f02) + qv;
            pPred[0, 1] = (fp[0, 0] * f10) + (fp[0, 1] * f11) + (fp[0, 2] * f12);
            pPred[0, 2] = (fp[0, 0] * f20) + (fp[0, 1] * f21) + (fp[0, 2] * f22);
            pPred[1, 0] = (fp[1, 0] * f00) + (fp[1, 1] * f01) + (fp[1, 2] * f02);
            pPred[1, 1] = (fp[1, 0] * f10) + (fp[1, 1] * f11) + (fp[1, 2] * f12) + qb;
            pPred[1, 2] = (fp[1, 0] * f20) + (fp[1, 1] * f21) + (fp[1, 2] * f22);
            pPred[2, 0] = (fp[2, 0] * f00) + (fp[2, 1] * f01) + (fp[2, 2] * f02);
            pPred[2, 1] = (fp[2, 0] * f10) + (fp[2, 1] * f11) + (fp[2, 2] * f12);
            pPred[2, 2] = (fp[2, 0] * f20) + (fp[2, 1] * f21) + (fp[2, 2] * f22) + qc;
        }

        private static void JosephUpdate(double[,] pPred, double[,] pPost, double k0, double k1, double k2, double r)
        {
            // A = I - K*H with H = [1 0 0]
            double[,] a = new double[3, 3];
            a[0, 0] = 1.0 - k0;
            a[0, 1] = 0.0;
            a[0, 2] = 0.0;
            a[1, 0] = -k1;
            a[1, 1] = 1.0;
            a[1, 2] = 0.0;
            a[2, 0] = -k2;
            a[2, 1] = 0.0;
            a[2, 2] = 1.0;

            double[,] ap = new double[3, 3];
            ap[0, 0] = a[0, 0] * pPred[0, 0] + a[0, 1] * pPred[1, 0] + a[0, 2] * pPred[2, 0];
            ap[0, 1] = a[0, 0] * pPred[0, 1] + a[0, 1] * pPred[1, 1] + a[0, 2] * pPred[2, 1];
            ap[0, 2] = a[0, 0] * pPred[0, 2] + a[0, 1] * pPred[1, 2] + a[0, 2] * pPred[2, 2];
            ap[1, 0] = a[1, 0] * pPred[0, 0] + a[1, 1] * pPred[1, 0] + a[1, 2] * pPred[2, 0];
            ap[1, 1] = a[1, 0] * pPred[0, 1] + a[1, 1] * pPred[1, 1] + a[1, 2] * pPred[2, 1];
            ap[1, 2] = a[1, 0] * pPred[0, 2] + a[1, 1] * pPred[1, 2] + a[1, 2] * pPred[2, 2];
            ap[2, 0] = a[2, 0] * pPred[0, 0] + a[2, 1] * pPred[1, 0] + a[2, 2] * pPred[2, 0];
            ap[2, 1] = a[2, 0] * pPred[0, 1] + a[2, 1] * pPred[1, 1] + a[2, 2] * pPred[2, 1];
            ap[2, 2] = a[2, 0] * pPred[0, 2] + a[2, 1] * pPred[1, 2] + a[2, 2] * pPred[2, 2];

            // P = A*P*A' + K*R*K'
            pPost[0, 0] = ap[0, 0] * a[0, 0] + ap[0, 1] * a[0, 1] + ap[0, 2] * a[0, 2] + (k0 * k0 * r);
            pPost[0, 1] = ap[0, 0] * a[1, 0] + ap[0, 1] * a[1, 1] + ap[0, 2] * a[1, 2] + (k0 * k1 * r);
            pPost[0, 2] = ap[0, 0] * a[2, 0] + ap[0, 1] * a[2, 1] + ap[0, 2] * a[2, 2] + (k0 * k2 * r);
            pPost[1, 0] = ap[1, 0] * a[0, 0] + ap[1, 1] * a[0, 1] + ap[1, 2] * a[0, 2] + (k1 * k0 * r);
            pPost[1, 1] = ap[1, 0] * a[1, 0] + ap[1, 1] * a[1, 1] + ap[1, 2] * a[1, 2] + (k1 * k1 * r);
            pPost[1, 2] = ap[1, 0] * a[2, 0] + ap[1, 1] * a[2, 1] + ap[1, 2] * a[2, 2] + (k1 * k2 * r);
            pPost[2, 0] = ap[2, 0] * a[0, 0] + ap[2, 1] * a[0, 1] + ap[2, 2] * a[0, 2] + (k2 * k0 * r);
            pPost[2, 1] = ap[2, 0] * a[1, 0] + ap[2, 1] * a[1, 1] + ap[2, 2] * a[1, 2] + (k2 * k1 * r);
            pPost[2, 2] = ap[2, 0] * a[2, 0] + ap[2, 1] * a[2, 1] + ap[2, 2] * a[2, 2] + (k2 * k2 * r);
        }

        private static void CopyMatrix(double[,] source, double[,] destination)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    destination[i, j] = source[i, j];
                }
            }
        }

        private static T? EnsureInitialized<T>(T? value) where T : class, new()
        {
            return value ?? new T();
        }
    }
}
