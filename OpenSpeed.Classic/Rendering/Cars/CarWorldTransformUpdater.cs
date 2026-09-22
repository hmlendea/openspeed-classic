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
            => Update(
                world,
                routePoints,
                new CarPhysicsState(),
                elapsedSeconds,
                movementInput,
                turningInput);

        public static Matrix Update(
            Matrix world,
            IEnumerable<TrackRoutePoint> routePoints,
            CarPhysicsState physicsState,
            float elapsedSeconds,
            float movementInput,
            float turningInput)
            => Update(
                world,
                routePoints,
                physicsState,
                elapsedSeconds,
                movementInput,
                turningInput,
                false);

        public static Matrix Update(
            Matrix world,
            IEnumerable<TrackRoutePoint> routePoints,
            CarPhysicsState physicsState,
            float elapsedSeconds,
            float movementInput,
            float turningInput,
            bool isHandbrakeApplied)
        {
            ArgumentNullException.ThrowIfNull(routePoints);
            ArgumentNullException.ThrowIfNull(physicsState);

            Matrix movedWorld = UpdateWithMomentum(
                world,
                physicsState,
                elapsedSeconds,
                movementInput,
                turningInput,
                isHandbrakeApplied);
            TrackRouteProjection? routeProjection = TrackRouteProjector.Project(
                movedWorld.Translation,
                routePoints);
            Matrix collisionResolvedWorld = CarTrackCollisionResolver.Resolve(
                movedWorld,
                routeProjection);

            if (collisionResolvedWorld.Translation != movedWorld.Translation)
            {
                physicsState.LateralVelocity = 0.0f;
            }

            return CarGravityResolver.Resolve(
                collisionResolvedWorld,
                routeProjection,
                physicsState,
                elapsedSeconds);
        }

        private static Matrix UpdateWithMomentum(
            Matrix world,
            CarPhysicsState physicsState,
            float elapsedSeconds,
            float movementInput,
            float turningInput,
            bool isHandbrakeApplied)
        {
            float previousVelocity = physicsState.LongitudinalVelocity;
            float previousLateralVelocity = physicsState.LateralVelocity;
            CarLongitudinalVelocityUpdater.Update(
                physicsState,
                elapsedSeconds,
                movementInput,
                isHandbrakeApplied);
            CarLateralVelocityUpdater.Update(
                physicsState,
                elapsedSeconds,
                turningInput,
                isHandbrakeApplied);
            float averageVelocity =
                (previousVelocity + physicsState.LongitudinalVelocity) / 2.0f;
            float averageLateralVelocity =
                (previousLateralVelocity + physicsState.LateralVelocity) / 2.0f;
            Vector3 up = NormaliseOrFallback(world.Up, Vector3.Up);
            float rotation = CarSteeringCalculator.CalculateRotation(
                averageVelocity,
                turningInput,
                elapsedSeconds,
                isHandbrakeApplied);
            Vector3 forward = Vector3.Normalize(Vector3.TransformNormal(
                world.Forward,
                Matrix.CreateFromAxisAngle(up, rotation)));
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, up));
            Vector3 position = world.Translation +
                (forward * averageVelocity + right * averageLateralVelocity) *
                elapsedSeconds;

            return Matrix.CreateWorld(position, forward, up);
        }

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