using System;

namespace OpenSpeed.Classic.Rendering
{
    public static class VignetteMaskCalculator
    {
    private static float FalloffExponent => 2.5f;

        public static float HorizontalClearRadiusRatio => 0.44f;

        public static float MaximumDarkness => 0.15f;

        public static float VerticalClearRadiusRatio => 0.4f;

        public static float CalculateDarkness(
            float normalizedPositionX,
            float normalizedPositionY)
        {
            float linearDarkness = EllipticalMaskStrengthCalculator.Calculate(
                normalizedPositionX,
                normalizedPositionY,
                HorizontalClearRadiusRatio,
                VerticalClearRadiusRatio,
                1.0f);

            return MaximumDarkness * MathF.Pow(linearDarkness, FalloffExponent);
        }
    }
}