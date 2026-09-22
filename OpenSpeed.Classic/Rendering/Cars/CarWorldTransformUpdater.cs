using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Cars
{
    public static class CarWorldTransformUpdater
    {
        private static float MovementVelocity => 20.0f;

        private static float TurnVelocity => MathHelper.ToRadians(90.0f);

        public static Matrix Update(
            Matrix world,
            float elapsedSeconds,
            float movementInput,
            float turningInput)
        {
            if (!float.IsFinite(elapsedSeconds) || elapsedSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedSeconds),
                    elapsedSeconds,
                    "The car elapsed time must be finite and non-negative.");
            }

            float clampedMovementInput = MathHelper.Clamp(
                movementInput,
                -1.0f,
                1.0f);
            float clampedTurningInput = MathHelper.Clamp(
                turningInput,
                -1.0f,
                1.0f);
            Vector3 up = NormaliseOrFallback(world.Up, Vector3.Up);
            float rotation = -clampedTurningInput * TurnVelocity * elapsedSeconds;
            Vector3 forward = Vector3.Normalize(Vector3.TransformNormal(
                world.Forward,
                Matrix.CreateFromAxisAngle(up, rotation)));
            Vector3 position = world.Translation +
                forward * clampedMovementInput * MovementVelocity * elapsedSeconds;

            return Matrix.CreateWorld(position, forward, up);
        }

        public static Matrix Update(
            Matrix world,
            IEnumerable<TrackRoutePoint> routePoints,
            float elapsedSeconds,
            float movementInput,
            float turningInput)
            => CarTrackCollisionResolver.Resolve(
                Update(
                    world,
                    elapsedSeconds,
                    movementInput,
                    turningInput),
                routePoints);

        private static Vector3 NormaliseOrFallback(
            Vector3 direction,
            Vector3 fallback)
        {
            if (direction.LengthSquared() == 0.0f)
            {
                return fallback;
            }

            return Vector3.Normalize(direction);
        }
    }
}