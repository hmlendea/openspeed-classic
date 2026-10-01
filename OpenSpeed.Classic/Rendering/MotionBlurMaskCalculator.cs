namespace OpenSpeed.Classic.Rendering
{
    public static class MotionBlurMaskCalculator
    {
        public static float HorizontalBlurFreeRadiusRatio => 0.3f;

        public static float VerticalBlurFreeRadiusRatio => 0.22f;

        public static float CalculateStrength(
            float normalizedPositionX,
            float normalizedPositionY,
            float edgeStrength)
            => EllipticalMaskStrengthCalculator.Calculate(
                normalizedPositionX,
                normalizedPositionY,
                HorizontalBlurFreeRadiusRatio,
                VerticalBlurFreeRadiusRatio,
                edgeStrength);
    }
}