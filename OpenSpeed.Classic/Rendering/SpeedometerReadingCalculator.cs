using System;

using Microsoft.Xna.Framework;

namespace OpenSpeed.Classic.Rendering
{
    public static class SpeedometerReadingCalculator
    {
        private static float KilometresPerHourPerMetrePerSecond => 3.6f;

        private static float MaximumDialSpeedKilometresPerHour => 240.0f;

        public static SpeedometerReading Calculate(float longitudinalVelocity)
        {
            if (!float.IsFinite(longitudinalVelocity))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(longitudinalVelocity),
                    longitudinalVelocity,
                    "The speedometer longitudinal velocity must be finite.");
            }

            float speedKilometresPerHour =
                MathF.Abs(longitudinalVelocity) *
                KilometresPerHourPerMetrePerSecond;
            float dialRatio = MathHelper.Clamp(
                speedKilometresPerHour / MaximumDialSpeedKilometresPerHour,
                0.0f,
                1.0f);
            int roundedSpeedKilometresPerHour = (int)MathF.Round(
                speedKilometresPerHour,
                MidpointRounding.AwayFromZero);

            return new SpeedometerReading(
                dialRatio,
                roundedSpeedKilometresPerHour);
        }
    }
}