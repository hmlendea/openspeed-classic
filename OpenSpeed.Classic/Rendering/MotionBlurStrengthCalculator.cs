using System;

using Microsoft.Xna.Framework;

namespace OpenSpeed.Classic.Rendering
{
    public static class MotionBlurStrengthCalculator
    {
        private static float MaximumBlurStrength => 0.45f;

        private static float MaximumSpeedKilometresPerHour => 120.0f;

        public static float Calculate(
            float longitudinalVelocity,
            float minimumSpeedKilometresPerHour)
        {
            if (!float.IsFinite(longitudinalVelocity))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(longitudinalVelocity),
                    longitudinalVelocity,
                    "The motion blur longitudinal velocity must be finite.");
            }

            if (!float.IsFinite(minimumSpeedKilometresPerHour) ||
                minimumSpeedKilometresPerHour < 0.0f ||
                minimumSpeedKilometresPerHour >= MaximumSpeedKilometresPerHour)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumSpeedKilometresPerHour),
                    minimumSpeedKilometresPerHour,
                    "The motion blur minimum speed must be finite and between " +
                        $"0 and {MaximumSpeedKilometresPerHour} km/h.");
            }

            float speedKilometresPerHour = MathF.Abs(longitudinalVelocity) * 3.6f;
            float speedRatio = MathHelper.Clamp(
                (speedKilometresPerHour - minimumSpeedKilometresPerHour) /
                    (MaximumSpeedKilometresPerHour - minimumSpeedKilometresPerHour),
                0.0f,
                1.0f);

            return MaximumBlurStrength * speedRatio;
        }
    }
}