using System;

using Microsoft.Xna.Framework;

namespace OpenSpeed.Classic.Rendering
{
    public static class MotionBlurMaskCalculator
    {
        public static float HorizontalBlurFreeRadiusRatio => 0.3f;

        public static float VerticalBlurFreeRadiusRatio => 0.22f;

        private static float MaximumTransitionDistance => MathF.Min(
            0.5f / HorizontalBlurFreeRadiusRatio,
            0.5f / VerticalBlurFreeRadiusRatio);

        public static float CalculateStrength(
            float normalizedPositionX,
            float normalizedPositionY,
            float edgeStrength)
        {
            ValidateNormalizedPosition(
                normalizedPositionX,
                nameof(normalizedPositionX));
            ValidateNormalizedPosition(
                normalizedPositionY,
                nameof(normalizedPositionY));

            if (!float.IsFinite(edgeStrength) ||
                edgeStrength < 0.0f ||
                edgeStrength > 1.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(edgeStrength),
                    edgeStrength,
                    "The motion blur edge strength must be between 0 and 1.");
            }

            float horizontalDistance =
                (normalizedPositionX - 0.5f) / HorizontalBlurFreeRadiusRatio;
            float verticalDistance =
                (normalizedPositionY - 0.5f) / VerticalBlurFreeRadiusRatio;
            float ellipseDistance = MathF.Sqrt(
                horizontalDistance * horizontalDistance +
                verticalDistance * verticalDistance);
            float transition = MathHelper.Clamp(
                (ellipseDistance - 1.0f) /
                    (MaximumTransitionDistance - 1.0f),
                0.0f,
                1.0f);

            return edgeStrength * MathHelper.SmoothStep(0.0f, 1.0f, transition);
        }

        private static void ValidateNormalizedPosition(
            float normalizedPosition,
            string parameterName)
        {
            if (!float.IsFinite(normalizedPosition) ||
                normalizedPosition < 0.0f ||
                normalizedPosition > 1.0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    normalizedPosition,
                    "The normalized motion blur position must be between 0 and 1.");
            }
        }
    }
}