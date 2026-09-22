using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Cars
{
    public static class CarGravityResolver
    {
        private static float GravityAcceleration => 9.81f;

        private static float GroundContactTolerance => 0.01f;

        private static float MinimumGroundNormalY => 0.1f;

        public static Matrix Resolve(
            Matrix world,
            IEnumerable<TrackRoutePoint> routePoints,
            CarPhysicsState physicsState,
            float elapsedSeconds)
        {
            ArgumentNullException.ThrowIfNull(routePoints);
            ArgumentNullException.ThrowIfNull(physicsState);

            if (!float.IsFinite(elapsedSeconds) || elapsedSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedSeconds),
                    elapsedSeconds,
                    "The gravity elapsed time must be finite and non-negative.");
            }

            TrackRouteProjection? routeProjection = TrackRouteProjector.Project(
                world.Translation,
                routePoints);

            return Resolve(world, routeProjection, physicsState, elapsedSeconds);
        }

        internal static Matrix Resolve(
            Matrix world,
            TrackRouteProjection? routeProjection,
            CarPhysicsState physicsState,
            float elapsedSeconds)
        {
            ArgumentNullException.ThrowIfNull(physicsState);

            if (routeProjection is null ||
                MathF.Abs(routeProjection.Normal.Y) < MinimumGroundNormalY)
            {
                return world;
            }

            Vector3 position = world.Translation;
            float groundHeight = CalculateGroundHeight(
                position,
                routeProjection.Position,
                routeProjection.Normal);
            bool isGrounded = position.Y <= groundHeight + GroundContactTolerance;

            if (isGrounded)
            {
                position.Y = groundHeight;
                physicsState.VerticalVelocity = 0.0f;
            }
            else
            {
                physicsState.VerticalVelocity -= GravityAcceleration * elapsedSeconds;
                position.Y += physicsState.VerticalVelocity * elapsedSeconds;

                if (position.Y <= groundHeight)
                {
                    position.Y = groundHeight;
                    physicsState.VerticalVelocity = 0.0f;
                    isGrounded = true;
                }
            }

            if (!isGrounded)
            {
                return Matrix.CreateWorld(position, world.Forward, world.Up);
            }

            Vector3 forward = ProjectOntoGround(world.Forward, routeProjection.Normal);

            if (forward.LengthSquared() == 0.0f)
            {
                forward = ProjectOntoGround(
                    routeProjection.Forward,
                    routeProjection.Normal);
            }

            if (forward.LengthSquared() == 0.0f)
            {
                forward = Vector3.Forward;
            }

            return Matrix.CreateWorld(
                position,
                Vector3.Normalize(forward),
                routeProjection.Normal);
        }

        private static float CalculateGroundHeight(
            Vector3 worldPosition,
            Vector3 routePosition,
            Vector3 groundNormal)
            => routePosition.Y -
                (groundNormal.X * (worldPosition.X - routePosition.X) +
                    groundNormal.Z * (worldPosition.Z - routePosition.Z)) /
                groundNormal.Y;

        private static Vector3 ProjectOntoGround(Vector3 vector, Vector3 groundNormal)
            => vector - groundNormal * Vector3.Dot(vector, groundNormal);
    }
}