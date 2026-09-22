using System;

using Microsoft.Xna.Framework;

namespace OpenSpeed.Classic.Rendering.Cars
{
    public static class CarSteeringCalculator
    {
        private static float HighVelocitySteeringAngle
            => MathHelper.ToRadians(10.0f);

        private static float HandbrakeMaximumYawVelocity
            => MathHelper.ToRadians(135.0f);

        private static float HandbrakeYawMultiplier => 1.5f;

        private static float LowVelocitySteeringAngle
            => MathHelper.ToRadians(35.0f);

        private static float MaximumLongitudinalVelocity => 40.0f;

        private static float MaximumYawVelocity
            => MathHelper.ToRadians(90.0f);

        private static float Wheelbase => 2.5f;

        public static float CalculateRotation(
            float longitudinalVelocity,
            float turningInput,
            float elapsedSeconds)
            => CalculateRotation(
                longitudinalVelocity,
                turningInput,
                elapsedSeconds,
                false);

        public static float CalculateRotation(
            float longitudinalVelocity,
            float turningInput,
            float elapsedSeconds,
            bool isHandbrakeApplied)
        {
            if (!float.IsFinite(longitudinalVelocity))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(longitudinalVelocity),
                    longitudinalVelocity,
                    "The longitudinal velocity must be finite.");
            }

            if (!float.IsFinite(elapsedSeconds) || elapsedSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedSeconds),
                    elapsedSeconds,
                    "The steering elapsed time must be finite and non-negative.");
            }

            float clampedTurningInput = MathHelper.Clamp(
                turningInput,
                -1.0f,
                1.0f);
            float velocityRatio = MathHelper.Clamp(
                MathF.Abs(longitudinalVelocity) / MaximumLongitudinalVelocity,
                0.0f,
                1.0f);
            float maximumSteeringAngle = MathHelper.Lerp(
                LowVelocitySteeringAngle,
                HighVelocitySteeringAngle,
                velocityRatio);
            float steeringAngle = clampedTurningInput * maximumSteeringAngle;
            float yawVelocity = longitudinalVelocity / Wheelbase *
                MathF.Tan(steeringAngle);
            float maximumYawVelocity = MaximumYawVelocity;

            if (isHandbrakeApplied)
            {
                yawVelocity *= HandbrakeYawMultiplier;
                maximumYawVelocity = HandbrakeMaximumYawVelocity;
            }

            yawVelocity = MathHelper.Clamp(
                yawVelocity,
                -maximumYawVelocity,
                maximumYawVelocity);

            return -yawVelocity * elapsedSeconds;
        }
    }
}