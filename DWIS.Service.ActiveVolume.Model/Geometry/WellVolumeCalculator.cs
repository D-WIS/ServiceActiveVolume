namespace DWIS.Service.ActiveVolume.Model.Geometry
{
    public sealed class WellVolumeInput
    {
        public double BottomOfStringDepth { get; set; }
        public double BottomHoleDepth { get; set; }
        public List<DepthIntervalDiameter> BoreholeIntervals { get; set; } = new();
        public List<DepthIntervalPipeSize> DrillStringIntervals { get; set; } = new();
    }

    public sealed record DepthIntervalDiameter(double StartDepth, double EndDepth, double Diameter);
    public sealed record DepthIntervalPipeSize(double StartDepth, double EndDepth, double OuterDiameter, double InnerDiameter);

    public sealed class WellVolumeResult
    {
        public double AnnularMudVolume { get; set; }
        public double InternalStringMudVolume { get; set; }
        public double TotalMudVolume => AnnularMudVolume + InternalStringMudVolume;
        public double EffectiveMovingPipeDisplacementArea { get; set; }
    }

    public static class WellVolumeCalculator
    {
        public static WellVolumeResult Calculate(WellVolumeInput input)
        {
            double annular = 0.0;
            foreach (DepthIntervalDiameter borehole in input.BoreholeIntervals)
            {
                double start = Math.Max(0.0, borehole.StartDepth);
                double end = Math.Min(input.BottomHoleDepth, borehole.EndDepth);
                if (end <= start || borehole.Diameter <= 0.0)
                {
                    continue;
                }

                double boreholeArea = Math.PI * borehole.Diameter * borehole.Diameter / 4.0;
                double stringArea = ResolvePipeOuterArea(input.DrillStringIntervals, 0.5 * (start + end));
                annular += Math.Max(0.0, boreholeArea - stringArea) * (end - start);
            }

            double internalString = 0.0;
            double movingArea = 0.0;
            foreach (DepthIntervalPipeSize pipe in input.DrillStringIntervals)
            {
                double start = Math.Max(0.0, pipe.StartDepth);
                double end = Math.Min(input.BottomOfStringDepth, pipe.EndDepth);
                if (end <= start || pipe.InnerDiameter <= 0.0)
                {
                    continue;
                }

                double innerArea = Math.PI * pipe.InnerDiameter * pipe.InnerDiameter / 4.0;
                double outerArea = Math.PI * pipe.OuterDiameter * pipe.OuterDiameter / 4.0;
                internalString += innerArea * (end - start);
                movingArea = Math.Max(movingArea, Math.Max(0.0, outerArea - innerArea));
            }

            return new WellVolumeResult
            {
                AnnularMudVolume = annular,
                InternalStringMudVolume = internalString,
                EffectiveMovingPipeDisplacementArea = movingArea
            };
        }

        private static double ResolvePipeOuterArea(IReadOnlyList<DepthIntervalPipeSize> pipeIntervals, double depth)
        {
            foreach (DepthIntervalPipeSize pipe in pipeIntervals)
            {
                if (depth >= pipe.StartDepth && depth <= pipe.EndDepth && pipe.OuterDiameter > 0.0)
                {
                    return Math.PI * pipe.OuterDiameter * pipe.OuterDiameter / 4.0;
                }
            }

            return 0.0;
        }
    }
}
