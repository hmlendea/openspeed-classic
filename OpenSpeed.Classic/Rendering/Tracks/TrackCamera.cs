using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;

using OpenSpeed.Classic.Input;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    public sealed class TrackCamera
    {
        public Vector3 Direction { get; private set; }

        public float FarPlane { get; }

        public Vector3 Position { get; private set; }

        public Vector3 Up { get; private set; }

        private static float CameraHeightOffset => 3.0f;

        private static float CameraDistance => 6.0f;

        private static float FarCameraDistance => 12.0f;

        private static float HighSpeedKilometresPerHour => 120.0f;

        private static float HighSpeedFieldOfViewRadians => MathHelper.ToRadians(40.0f);

        private static float FieldOfViewOffsetRadians => MathHelper.ToRadians(15.0f);

        private static float LowSpeedFieldOfViewRadians => MathHelper.ToRadians(60.0f);

        private static float TargetHeightOffset => 1.5f;

        private static float MinimumFarPlane => 1024.0f;

        private static float MovementVelocity => 20.0f;

        private static float NearPlane => 0.1f;

        private float currentLongitudinalVelocity;

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

            Position = targetPosition -
                trackDirection * CameraDistance +
                Vector3.Up * CameraHeightOffset;
            UpdateFollow(Matrix.CreateWorld(targetPosition, trackDirection, up));
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
                CalculateFieldOfViewRadians(),
                aspectRatio,
                NearPlane,
                FarPlane);
        }

        public Matrix CreateView()
            => Matrix.CreateLookAt(Position, Position + Direction, Up);

        public void Follow(Matrix targetWorld)
        {
            UpdateFollow(targetWorld);
        }

        public void Follow(Matrix targetWorld, float elapsedSeconds)
        {
            if (!float.IsFinite(elapsedSeconds) || elapsedSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedSeconds),
                    elapsedSeconds,
                    "The camera elapsed time must be finite and non-negative.");
            }

            UpdateFollow(targetWorld);
        }

        public void Follow(
            Matrix targetWorld,
            float elapsedSeconds,
            float longitudinalVelocity,
            TrackCameraView cameraView)
            => Follow(
                targetWorld,
                elapsedSeconds,
                longitudinalVelocity,
                cameraView,
                TrackCameraMode.Close);

        public void Follow(
            Matrix targetWorld,
            float elapsedSeconds,
            float longitudinalVelocity,
            TrackCameraView cameraView,
            TrackCameraMode cameraMode)
        {
            if (!float.IsFinite(elapsedSeconds) || elapsedSeconds < 0.0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(elapsedSeconds),
                    elapsedSeconds,
                    "The camera elapsed time must be finite and non-negative.");
            }

            if (!float.IsFinite(longitudinalVelocity))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(longitudinalVelocity),
                    longitudinalVelocity,
                    "The camera longitudinal velocity must be finite.");
            }

            currentLongitudinalVelocity = longitudinalVelocity;
            UpdateFollow(targetWorld, cameraView, cameraMode);
        }

        public void Follow(
            Matrix targetWorld,
            float elapsedSeconds,
            float longitudinalVelocity)
            => Follow(
                targetWorld,
                elapsedSeconds,
                longitudinalVelocity,
                TrackCameraView.Centre,
                TrackCameraMode.Close);

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

        private float CalculateFieldOfViewRadians()
        {
            float speedKilometresPerHour = MathF.Abs(currentLongitudinalVelocity) * 3.6f;
            float speedRatio = MathHelper.Clamp(
                speedKilometresPerHour / HighSpeedKilometresPerHour,
                0.0f,
                1.0f);

            return MathHelper.Lerp(
                LowSpeedFieldOfViewRadians,
                HighSpeedFieldOfViewRadians,
                speedRatio) +
                FieldOfViewOffsetRadians;
        }

        private void UpdateFollow(Matrix targetWorld)
            => UpdateFollow(
                targetWorld,
                TrackCameraView.Centre,
                TrackCameraMode.Close);

        private void UpdateFollow(
            Matrix targetWorld,
            TrackCameraView cameraView,
            TrackCameraMode cameraMode)
        {
            Vector3 targetPosition = targetWorld.Translation;
            Vector3 targetPositionOnGround = targetPosition;
            targetPositionOnGround.Y = 0.0f;
            Vector3 cameraPositionOnGround = Position;
            cameraPositionOnGround.Y = 0.0f;
            Vector3 direction = cameraPositionOnGround - targetPositionOnGround;
            float distance = direction.Length();
            float desiredDistance = CalculateCameraDistance(cameraMode);

            if (distance > desiredDistance ||
                cameraMode == TrackCameraMode.Far && distance > 0.0f)
            {
                direction /= distance;
                cameraPositionOnGround = targetPositionOnGround +
                    direction * desiredDistance;
            }

            cameraPositionOnGround.Y = targetPosition.Y + CameraHeightOffset;
            Position = cameraPositionOnGround;
            Vector3 targetPoint = targetPosition;
            targetPoint.Y += TargetHeightOffset;
            float yaw = CalculateYaw(cameraView);

            if (yaw != 0.0f)
            {
                Position = targetPoint +
                    Vector3.Transform(
                        Position - targetPoint,
                        Matrix.CreateFromAxisAngle(Vector3.Up, yaw));
            }

            Direction = NormaliseOrFallback(
                targetPoint - Position,
                Vector3.Forward);
            Up = Vector3.Up;
        }

        private static float CalculateCameraDistance(TrackCameraMode cameraMode)
        {
            if (cameraMode == TrackCameraMode.Far)
            {
                return FarCameraDistance;
            }

            return CameraDistance;
        }

        private static float CalculateYaw(TrackCameraView cameraView)
        {
            if (cameraView == TrackCameraView.Right)
            {
                return MathHelper.ToRadians(60.0f);
            }

            if (cameraView == TrackCameraView.Left)
            {
                return MathHelper.ToRadians(-60.0f);
            }

            if (cameraView == TrackCameraView.Rear)
            {
                return MathHelper.Pi;
            }

            return 0.0f;
        }

        private static Vector3 ToVector3(TrackPoint point)
            => new((float)point.X, (float)point.Y, (float)point.Z);

        private static Vector3 ToVector3(TrackVector vector)
            => new((float)vector.X, (float)vector.Y, (float)vector.Z);
    }
}