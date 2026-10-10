using System;

using Microsoft.Xna.Framework;

namespace OpenSpeed.Classic.Rendering
{
    internal static class EllipticalMaskStrengthCalculator
    {
        internal static float Calculate(
            float normalizedPositionX,
            float normalizedPositionY,
            float horizontalClearRadiusRatio,
            float verticalClearRadiusRatio,
            float maximumStrength)
        {
            ValidateNormalizedPosition(
                normalizedPositionX,
                nameof(normalizedPositionX));
            ValidateNormalizedPosition(
                normalizedPositionY,
                nameof(normalizedPositionY));
            ValidateRadiusRatio(
                horizontalClearRadiusRatio,
                nameof(horizontalClearRadiusRatio));
            ValidateRadiusRatio(
                verticalClearRadiusRatio,
                nameof(verticalClearRadiusRatio));

            if (!float.IsFinite(maximumStrength) ||
                maximumStrength < 0.0f ||
                maximumStrength > 1.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumStrength),
                    maximumStrength,
                    "The elliptical mask strength must be between 0 and 1.");
            }

            float offsetX = normalizedPositionX - 0.5f;
            float offsetY = normalizedPositionY - 0.5f;
            float horizontalDistance = offsetX / horizontalClearRadiusRatio;
            float verticalDistance = offsetY / verticalClearRadiusRatio;
            float ellipseDistance = MathF.Sqrt(
                horizontalDistance * horizontalDistance +
                verticalDistance * verticalDistance);

            if (ellipseDistance <= 1.0f)
            {
                return 0.0f;
            }

            float horizontalEdgeScale = float.PositiveInfinity;
            float verticalEdgeScale = float.PositiveInfinity;

            if (offsetX != 0.0f)
            {
                horizontalEdgeScale = 0.5f / MathF.Abs(offsetX);
            }

            if (offsetY != 0.0f)
            {
                verticalEdgeScale = 0.5f / MathF.Abs(offsetY);
            }

            float edgeScale = MathF.Min(horizontalEdgeScale, verticalEdgeScale);
            float edgeEllipseDistance = ellipseDistance * edgeScale;
            float transition = MathHelper.Clamp(
                (ellipseDistance - 1.0f) /
                    (edgeEllipseDistance - 1.0f),
                0.0f,
                1.0f);

            return maximumStrength * MathHelper.SmoothStep(0.0f, 1.0f, transition);
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
                    "The normalized mask position must be between 0 and 1.");
            }
        }

        private static void ValidateRadiusRatio(
            float radiusRatio,
            string parameterName)
        {
            if (!float.IsFinite(radiusRatio) ||
                radiusRatio <= 0.0f ||
                radiusRatio >= 0.5f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    radiusRatio,
                    "The elliptical mask radius ratio must be between 0 and 0.5.");
            }
        }
    }
}