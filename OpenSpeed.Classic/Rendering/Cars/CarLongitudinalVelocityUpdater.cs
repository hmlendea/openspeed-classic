using System;

using Microsoft.Xna.Framework;

namespace OpenSpeed.Classic.Rendering.Cars
{
    public static class CarLongitudinalVelocityUpdater
    {
        private static float BrakingAcceleration => 24.0f;

        private static float CoastingDeceleration => 6.0f;

        private static float ForwardAcceleration => 12.0f;

        private static float MaximumForwardVelocity => 40.0f;

        private static float MaximumReverseVelocity => 16.0f;

        private static float ReverseAcceleration => 8.0f;

        public static void Update(
            CarPhysicsState physicsState,
            float elapsedSeconds,
            float movementInput)
        {
            ArgumentNullException.ThrowIfNull(physicsState);

            if (!float.IsFinite(physicsState.LongitudinalVelocity))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(physicsState),
                    physicsState.LongitudinalVelocity,
                    "The longitudinal velocity must be finite.");
            }

            if (!float.IsFinite(elapsedSeconds) || elapsedSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedSeconds),
                    elapsedSeconds,
                    "The acceleration elapsed time must be finite and non-negative.");
            }

            float clampedMovementInput = MathHelper.Clamp(
                movementInput,
                -1.0f,
                1.0f);
            float targetVelocity = 0.0f;
            float velocityChangeRate = CoastingDeceleration;

            if (clampedMovementInput > 0.0f)
            {
                targetVelocity = MaximumForwardVelocity * clampedMovementInput;
                velocityChangeRate = ForwardAcceleration;

                if (physicsState.LongitudinalVelocity < 0.0f)
                {
                    targetVelocity = 0.0f;
                    velocityChangeRate = BrakingAcceleration;
                }
            }
            else if (clampedMovementInput < 0.0f)
            {
                targetVelocity = MaximumReverseVelocity * clampedMovementInput;
                velocityChangeRate = ReverseAcceleration;

                if (physicsState.LongitudinalVelocity > 0.0f)
                {
                    targetVelocity = 0.0f;
                    velocityChangeRate = BrakingAcceleration;
                }
            }

            physicsState.LongitudinalVelocity = MoveTowards(
                physicsState.LongitudinalVelocity,
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