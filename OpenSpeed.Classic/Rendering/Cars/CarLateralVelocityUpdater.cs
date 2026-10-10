using System;

using Microsoft.Xna.Framework;

namespace OpenSpeed.Classic.Rendering.Cars
{
    public static class CarLateralVelocityUpdater
    {
        private static float CounterSteerRecovery => 24.0f;

        private static float DriftBuildAcceleration => 32.0f;

        private static float DriftVelocityRatio => 0.65f;

        private static float HandbrakeGripRecovery => 1.5f;

        private static float MinimumDriftVelocity => 4.0f;

        private static float NormalGripRecovery => 10.0f;

        public static void Update(
            CarPhysicsState physicsState,
            float elapsedSeconds,
            float turningInput,
            bool isHandbrakeApplied)
        {
            ArgumentNullException.ThrowIfNull(physicsState);

            if (!float.IsFinite(physicsState.LateralVelocity))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(physicsState),
                    physicsState.LateralVelocity,
                    "The lateral velocity must be finite.");
            }

            if (!float.IsFinite(elapsedSeconds) || elapsedSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedSeconds),
                    elapsedSeconds,
                    "The lateral dynamics elapsed time must be finite and non-negative.");
            }

            float clampedTurningInput = MathHelper.Clamp(
                turningInput,
                -1.0f,
                1.0f);
            float targetVelocity = 0.0f;
            float velocityChangeRate = NormalGripRecovery;

            if (isHandbrakeApplied)
            {
                bool hasSufficientVelocity =
                    MathF.Abs(physicsState.LongitudinalVelocity) >= MinimumDriftVelocity;

                if (hasSufficientVelocity)
                {
                    targetVelocity = -clampedTurningInput *
                        physicsState.LongitudinalVelocity *
                        DriftVelocityRatio;
                    velocityChangeRate = DriftBuildAcceleration;
                }

                if (clampedTurningInput == 0.0f)
                {
                    velocityChangeRate = HandbrakeGripRecovery;
                }
                else if (physicsState.LateralVelocity != 0.0f &&
                    MathF.Sign(targetVelocity) !=
                        MathF.Sign(physicsState.LateralVelocity))
                {
                    targetVelocity = 0.0f;
                    velocityChangeRate = CounterSteerRecovery;
                }
            }

            physicsState.LateralVelocity = MoveTowards(
                physicsState.LateralVelocity,
                targetVelocity,
                velocityChangeRate * elapsedSeconds);
        }

        private static float MoveTowards(
            float currentValue,
            float targetValue,
            float maximumChange)
        {
            if (currentValue < targetValue)
            {
                return MathF.Min(currentValue + maximumChange, targetValue);
            }

            if (currentValue > targetValue)
            {
                return MathF.Max(currentValue - maximumChange, targetValue);
            }

            return targetValue;
        }
    }
}