using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    public sealed class TrackCamera
    {
        public Vector3 Direction { get; private set; }

        public float FarPlane { get; }

        public Vector3 Position { get; private set; }

        public Vector3 Up { get; private set; }

        private static float CameraHeight => 3.0f;

        private static float ChaseDistance => 6.0f;

        private static float FieldOfViewRadians => MathHelper.ToRadians(90.0f);

        private static float LookAheadDistance => 12.0f;

        private static float MinimumFarPlane => 1024.0f;

        private static float MovementVelocity => 20.0f;

        private static float NearPlane => 0.1f;

        private static float TurnVelocity => MathHelper.ToRadians(90.0f);

        public TrackCamera(IEnumerable<TrackBlock> trackBlocks)
            : this(trackBlocks, [])
        {
        }

        public TrackCamera(
            IEnumerable<TrackBlock> trackBlocks,
            IEnumerable<TrackRoutePoint> routePoints)
        {
            ArgumentNullException.ThrowIfNull(trackBlocks);
            ArgumentNullException.ThrowIfNull(routePoints);

            Vector3[] centres = trackBlocks
                .Select(trackBlock => ToVector3(trackBlock.Centre))
                .ToArray();

            if (centres.Length == 0)
            {
                throw new ArgumentException(
                    "At least one track block is required to position the camera.",
                    nameof(trackBlocks));
            }

            TrackRoutePoint? firstRoutePoint = routePoints.FirstOrDefault();
            Vector3 targetPosition = centres[0];
            Vector3 trackDirection = GetTrackDirection(centres);
            Vector3 up = Vector3.Up;

            if (firstRoutePoint is not null)
            {
                targetPosition = ToVector3(firstRoutePoint.Position);
                trackDirection = NormaliseOrFallback(
                    ToVector3(firstRoutePoint.Forward),
                    trackDirection);
                up = NormaliseOrFallback(
                    ToVector3(firstRoutePoint.Normal),
                    Vector3.Up);

                if (Vector3.Cross(trackDirection, up).LengthSquared() == 0.0f)
                {
                    up = Vector3.Up;
                }
            }

            Follow(Matrix.CreateWorld(targetPosition, trackDirection, up));
            FarPlane = CalculateFarPlane(centres, Position);
        }

        public Matrix CreateProjection(int viewportWidth, int viewportHeight)
        {
            if (viewportWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(viewportWidth),
                    viewportWidth,
                    "The camera viewport width must be positive.");
            }

            if (viewportHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(viewportHeight),
                    viewportHeight,
                    "The camera viewport height must be positive.");
            }

            float aspectRatio = (float)viewportWidth / viewportHeight;

            return Matrix.CreatePerspectiveFieldOfView(
                FieldOfViewRadians,
                aspectRatio,
                NearPlane,
                FarPlane);
        }

        public Matrix CreateView()
            => Matrix.CreateLookAt(Position, Position + Direction, Up);

        public void Follow(Matrix targetWorld)
        {
            Vector3 targetPosition = targetWorld.Translation;
            Vector3 targetDirection = NormaliseOrFallback(
                targetWorld.Forward,
                Vector3.Forward);
            Vector3 targetUp = NormaliseOrFallback(targetWorld.Up, Vector3.Up);

            if (Vector3.Cross(targetDirection, targetUp).LengthSquared() == 0.0f)
            {
                targetUp = Vector3.Up;
            }

            Position = targetPosition -
                targetDirection * ChaseDistance +
                targetUp * CameraHeight;
            Vector3 lookAtPosition = targetPosition +
                targetDirection * LookAheadDistance;
            Direction = Vector3.Normalize(lookAtPosition - Position);
            Up = targetUp;
        }

        public void Update(
            float elapsedSeconds,
            float movementInput,
            float turningInput)
        {
            if (!float.IsFinite(elapsedSeconds) || elapsedSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedSeconds),
                    elapsedSeconds,
                    "The camera elapsed time must be finite and non-negative.");
            }

            float clampedMovementInput = MathHelper.Clamp(
                movementInput,
                -1.0f,
                1.0f);
            float clampedTurningInput = MathHelper.Clamp(
                turningInput,
                -1.0f,
                1.0f);
            float rotation = -clampedTurningInput * TurnVelocity * elapsedSeconds;
            Direction = Vector3.Normalize(Vector3.TransformNormal(
                Direction,
                Matrix.CreateFromAxisAngle(Up, rotation)));
            Vector3 horizontalDirection = Direction -
                Up * Vector3.Dot(Direction, Up);

            if (horizontalDirection.LengthSquared() > 0.0f)
            {
                horizontalDirection = Vector3.Normalize(horizontalDirection);
                Position += horizontalDirection *
                    clampedMovementInput *
                    MovementVelocity *
                    elapsedSeconds;
            }
        }

        private static float CalculateFarPlane(
            IEnumerable<Vector3> centres,
            Vector3 position)
        {
            float maximumDistance = 0.0f;

            foreach (Vector3 centre in centres)
            {
                maximumDistance = MathF.Max(
                    maximumDistance,
                    Vector3.Distance(position, centre));
            }

            return MathF.Max(MinimumFarPlane, maximumDistance * 2.0f);
        }

        private static Vector3 GetTrackDirection(Vector3[] centres)
        {
            if (centres.Length < 2)
            {
                return Vector3.Forward;
            }

            Vector3 direction = centres[1] - centres[0];
            direction.Y = 0.0f;

            if (direction.LengthSquared() == 0.0f)
            {
                return Vector3.Forward;
            }

            return Vector3.Normalize(direction);
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

        private static Vector3 ToVector3(TrackPoint point)
            => new((float)point.X, (float)point.Y, (float)point.Z);

        private static Vector3 ToVector3(TrackVector vector)
            => new((float)vector.X, (float)vector.Y, (float)vector.Z);
    }
}