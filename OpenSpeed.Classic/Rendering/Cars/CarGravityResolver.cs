using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Cars
{
    public static class CarGravityResolver
    {
        private static float GravityAcceleration => 10.0f;

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
                if (!physicsState.IsGrounded &&
                    physicsState.VerticalVelocity == 0.0f)
                {
                    return world;
                }

                return ResolveAirborneWorld(world, physicsState, elapsedSeconds);
            }

            Vector3 position = world.Translation;
            float groundHeight = CalculateGroundHeight(
                position,
                routeProjection.Position,
                routeProjection.Normal);
            float surfaceVerticalVelocity = CalculateSurfaceVerticalVelocity(
                routeProjection,
                physicsState);
            bool isLeavingSurface = physicsState.IsGrounded &&
                physicsState.VerticalVelocity > surfaceVerticalVelocity;
            bool hasGroundContact =
                !isLeavingSurface &&
                position.Y <= groundHeight + GroundContactTolerance;

            if (hasGroundContact)
            {
                position.Y = groundHeight;
                physicsState.IsGrounded = true;
            }
            else
            {
                physicsState.IsGrounded = false;
                physicsState.VerticalVelocity -= GravityAcceleration * elapsedSeconds;
                position.Y += physicsState.VerticalVelocity * elapsedSeconds;

                if (position.Y <= groundHeight)
                {
                    position.Y = groundHeight;
                    physicsState.IsGrounded = true;
                }
            }

            if (!physicsState.IsGrounded)
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

            forward = Vector3.Normalize(forward);
            physicsState.VerticalVelocity =
                forward.Y * physicsState.LongitudinalVelocity;

            return Matrix.CreateWorld(
                position,
                forward,
                routeProjection.Normal);
        }

        private static Matrix ResolveAirborneWorld(
            Matrix world,
            CarPhysicsState physicsState,
            float elapsedSeconds)
        {
            physicsState.IsGrounded = false;
            physicsState.VerticalVelocity -= GravityAcceleration * elapsedSeconds;
            Vector3 position = world.Translation;
            position.Y += physicsState.VerticalVelocity * elapsedSeconds;

            return Matrix.CreateWorld(position, world.Forward, world.Up);
        }

        private static float CalculateSurfaceVerticalVelocity(
            TrackRouteProjection routeProjection,
            CarPhysicsState physicsState)
        {
            Vector3 surfaceForward = ProjectOntoGround(
                routeProjection.Forward,
                routeProjection.Normal);

            if (surfaceForward.LengthSquared() == 0.0f)
            {
                return 0.0f;
            }

            surfaceForward.Normalize();

            return surfaceForward.Y * physicsState.LongitudinalVelocity;
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