using System;

using Microsoft.Xna.Framework;

using NUnit.Framework;

using OpenSpeed.Classic.Input;
using OpenSpeed.Classic.Rendering.Tracks;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Rendering.Tracks
{
    [TestFixture]
    public sealed class TrackCameraTests
    {
        private static float PositionTolerance => 0.001f;

        private TrackCamera trackCamera = null!;

        [SetUp]
        public void SetUp()
        {
            TrackBlock[] trackBlocks =
            [
                BuildTrackBlock(0.0, 0.0, 0.0),
                BuildTrackBlock(0.0, 0.0, -16.0)
            ];
            trackCamera = new TrackCamera(trackBlocks);
        }

        [Test]
        public void GivenTrackCentres_WhenConstructing_ThenTheCameraFacesAlongTheTrack()
        {
            Assert.Multiple(() =>
            {
                Assert.That(trackCamera.Position.X, Is.EqualTo(0.0f));
                Assert.That(trackCamera.Position.Y, Is.EqualTo(3.0f));
                Assert.That(trackCamera.Position.Z, Is.EqualTo(6.0f));
                Assert.That(trackCamera.Direction.Z, Is.LessThan(0.0f));
                Assert.That(trackCamera.FarPlane, Is.EqualTo(1024.0f));
            });
        }

        [Test]
        public void GivenARoutePoint_WhenConstructing_ThenItsPositionAndBasisAreUsed()
        {
            TrackBlock[] trackBlocks =
            [
                BuildTrackBlock(0.0, 0.0, 0.0),
                BuildTrackBlock(0.0, 0.0, -16.0)
            ];
            TrackRoutePoint routePoint = new()
            {
                Position = new TrackPoint
                {
                    X = 10.0,
                    Y = 20.0,
                    Z = 30.0
                },
                Forward = new TrackVector
                {
                    X = 1.0
                },
                Normal = new TrackVector
                {
                    Y = 1.0
                }
            };

            TrackCamera camera = new(trackBlocks, [routePoint]);

            Assert.Multiple(() =>
            {
                Assert.That(camera.Position, Is.EqualTo(new Vector3(4.0f, 23.0f, 30.0f)));
                Assert.That(camera.Direction.X, Is.GreaterThan(0.0f));
                Assert.That(camera.Direction.Y, Is.LessThan(0.0f));
                Assert.That(camera.Direction.Z, Is.Zero.Within(PositionTolerance));
                Assert.That(camera.Up, Is.EqualTo(Vector3.Up));
            });
        }

        [Test]
        public void GivenAZeroRouteBasis_WhenConstructing_ThenTheBlockDirectionIsUsed()
        {
            TrackBlock[] trackBlocks =
            [
                BuildTrackBlock(0.0, 0.0, 0.0),
                BuildTrackBlock(0.0, 0.0, -16.0)
            ];
            TrackRoutePoint routePoint = new()
            {
                Position = new TrackPoint()
            };

            TrackCamera camera = new(trackBlocks, [routePoint]);

            Assert.Multiple(() =>
            {
                Assert.That(camera.Position, Is.EqualTo(new Vector3(0.0f, 3.0f, 6.0f)));
                Assert.That(camera.Direction.Z, Is.LessThan(0.0f));
                Assert.That(camera.Up, Is.EqualTo(Vector3.Up));
            });
        }

        [TestCase(1.0f, -14.0f)]
        [TestCase(-1.0f, 26.0f)]
        public void GivenMovementInput_WhenUpdating_ThenTheCameraMovesHorizontally(
            float movementInput,
            float expectedPositionZ)
        {
            trackCamera.Update(1.0f, movementInput, 0.0f);

            Assert.Multiple(() =>
            {
                Assert.That(
                    trackCamera.Position.Z,
                    Is.EqualTo(expectedPositionZ).Within(PositionTolerance));
                Assert.That(trackCamera.Position.Y, Is.EqualTo(3.0f));
            });
        }

        [Test]
        public void GivenRightTurnInput_WhenUpdating_ThenTheCameraTurnsRight()
        {
            float initialHorizontalMagnitude = MathF.Sqrt(
                trackCamera.Direction.X * trackCamera.Direction.X +
                trackCamera.Direction.Z * trackCamera.Direction.Z);
            float initialVerticalDirection = trackCamera.Direction.Y;

            trackCamera.Update(1.0f, 0.0f, 1.0f);

            Assert.Multiple(() =>
            {
                Assert.That(
                    trackCamera.Direction.X,
                    Is.EqualTo(initialHorizontalMagnitude).Within(PositionTolerance));
                Assert.That(
                    trackCamera.Direction.Y,
                    Is.EqualTo(initialVerticalDirection).Within(PositionTolerance));
                Assert.That(
                    trackCamera.Direction.Z,
                    Is.Zero.Within(PositionTolerance));
                Assert.That(
                    trackCamera.Direction.Length(),
                    Is.EqualTo(1.0f).Within(PositionTolerance));
            });
        }

        [Test]
        public void GivenACarWorldTransform_WhenFollowing_ThenTheCameraChasesTheCar()
        {
            Matrix carWorld = Matrix.CreateWorld(
                new Vector3(42.0f, 8.0f, 16.0f),
                Vector3.Right,
                Vector3.Up);

            trackCamera.Follow(carWorld);
            Vector3 horizontalOffset = trackCamera.Position - carWorld.Translation;
            horizontalOffset.Y = 0.0f;
            Vector3 expectedLookTarget = carWorld.Translation + Vector3.Up * 1.5f;
            Vector3 expectedLookDirection = expectedLookTarget - trackCamera.Position;
            expectedLookDirection.Normalize();

            Assert.Multiple(() =>
            {
                Assert.That(horizontalOffset.Length(), Is.EqualTo(6.0f).Within(PositionTolerance));
                Assert.That(trackCamera.Position.Y, Is.EqualTo(11.0f).Within(PositionTolerance));
                Assert.That(trackCamera.Direction.X, Is.EqualTo(expectedLookDirection.X).Within(PositionTolerance));
                Assert.That(trackCamera.Direction.Y, Is.EqualTo(expectedLookDirection.Y).Within(PositionTolerance));
                Assert.That(trackCamera.Direction.Z, Is.EqualTo(expectedLookDirection.Z).Within(PositionTolerance));
                Assert.That(trackCamera.Up, Is.EqualTo(Vector3.Up));
            });
        }

        [Test]
        public void GivenACarInsideTheCameraRadius_WhenFollowing_ThenTheCameraKeepsItsHorizontalPosition()
        {
            Matrix carWorld = Matrix.CreateWorld(
                new Vector3(0.0f, 8.0f, 2.0f),
                Vector3.Right,
                Vector3.Up);
            Vector3 initialPosition = trackCamera.Position;

            trackCamera.Follow(carWorld);

            Assert.Multiple(() =>
            {
                Assert.That(trackCamera.Position.X, Is.EqualTo(initialPosition.X).Within(PositionTolerance));
                Assert.That(trackCamera.Position.Z, Is.EqualTo(initialPosition.Z).Within(PositionTolerance));
                Assert.That(trackCamera.Position.Y, Is.EqualTo(11.0f).Within(PositionTolerance));
            });
        }

        [Test]
        public void GivenAChangingCarHeading_WhenFollowing_ThenTheCameraUsesWorldSpacePositionAndWorldUp()
        {
            Matrix carWorld = Matrix.CreateWorld(
                new Vector3(0.0f, 8.0f, -4.0f),
                Vector3.Right,
                Vector3.Normalize(new Vector3(0.0f, 1.0f, 1.0f)));

            trackCamera.Follow(carWorld);
            Vector3 expectedLookTarget = carWorld.Translation + Vector3.Up * 1.5f;
            Vector3 expectedLookDirection = expectedLookTarget - trackCamera.Position;
            expectedLookDirection.Normalize();

            Assert.Multiple(() =>
            {
                Assert.That(trackCamera.Direction.X, Is.EqualTo(expectedLookDirection.X).Within(PositionTolerance));
                Assert.That(trackCamera.Direction.Y, Is.EqualTo(expectedLookDirection.Y).Within(PositionTolerance));
                Assert.That(trackCamera.Direction.Z, Is.EqualTo(expectedLookDirection.Z).Within(PositionTolerance));
                Assert.That(trackCamera.Up, Is.EqualTo(Vector3.Up));
                Assert.That(trackCamera.Position.Y, Is.EqualTo(11.0f).Within(PositionTolerance));
            });
        }

        [Test]
        public void GivenDifferentLongitudinalSpeeds_WhenCreatingProjection_ThenTheFieldOfViewNarrows()
        {
            Matrix carWorld = Matrix.CreateWorld(
                new Vector3(42.0f, 8.0f, 16.0f),
                Vector3.Forward,
                Vector3.Up);

            trackCamera.Follow(carWorld, 0.0f, 0.0f);
            float lowSpeedProjectionScale = trackCamera.CreateProjection(1280, 720).M11;
            trackCamera.Follow(carWorld, 0.0f, 120.0f / 3.6f);
            float highSpeedProjectionScale = trackCamera.CreateProjection(1280, 720).M11;

            Assert.That(highSpeedProjectionScale, Is.GreaterThan(lowSpeedProjectionScale));
        }

        [Test]
        public void GivenMaximumCameraSpeed_WhenCreatingProjection_ThenTheFovIncludesTheConfiguredOffset()
        {
            Matrix carWorld = Matrix.CreateWorld(
                new Vector3(42.0f, 8.0f, 16.0f),
                Vector3.Forward,
                Vector3.Up);
            trackCamera.Follow(carWorld, 0.0f, 120.0f / 3.6f);
            Matrix projection = trackCamera.CreateProjection(1280, 720);
            float aspectRatio = 1280.0f / 720.0f;
            float fieldOfViewRadians = 2.0f * MathF.Atan(
                1.0f / (projection.M11 * aspectRatio));

            Assert.That(
                fieldOfViewRadians,
                Is.EqualTo(MathHelper.ToRadians(55.0f)).Within(PositionTolerance));
        }

        [TestCase(TrackCameraView.Right, 60.0f)]
        [TestCase(TrackCameraView.Left, -60.0f)]
        [TestCase(TrackCameraView.Centre, 0.0f)]
        [TestCase(TrackCameraView.Rear, 180.0f)]
        public void GivenACameraViewPreset_WhenFollowing_ThenTheCameraOrbitsByThePresetYaw(
            TrackCameraView cameraView,
            float expectedYawDegrees)
        {
            Matrix carWorld = Matrix.CreateWorld(
                new Vector3(42.0f, 8.0f, 16.0f),
                Vector3.Forward,
                Vector3.Up);
            trackCamera.Follow(carWorld, 0.0f, 0.0f, TrackCameraView.Centre);
            Vector3 centreOffset = trackCamera.Position - carWorld.Translation;
            centreOffset.Y = 0.0f;
            centreOffset.Normalize();
            trackCamera.Follow(carWorld, 0.0f, 0.0f, cameraView);
            Vector3 offset = trackCamera.Position - carWorld.Translation;
            offset.Y = 0.0f;
            offset.Normalize();
            Vector3 expectedOffset = Vector3.Transform(
                centreOffset,
                Matrix.CreateRotationY(MathHelper.ToRadians(expectedYawDegrees)));

            Assert.That(offset.X, Is.EqualTo(expectedOffset.X).Within(PositionTolerance));
            Assert.That(offset.Z, Is.EqualTo(expectedOffset.Z).Within(PositionTolerance));
        }

        [Test]
        public void GivenFarCameraMode_WhenFollowing_ThenTheCameraUsesTheFarOrbitDistance()
        {
            Matrix carWorld = Matrix.CreateWorld(
                new Vector3(42.0f, 8.0f, 16.0f),
                Vector3.Forward,
                Vector3.Up);

            trackCamera.Follow(
                carWorld,
                0.0f,
                0.0f,
                TrackCameraView.Centre,
                TrackCameraMode.Far);
            Vector3 offset = trackCamera.Position - carWorld.Translation;
            offset.Y = 0.0f;

            Assert.That(offset.Length(), Is.EqualTo(12.0f).Within(PositionTolerance));
        }

        [TestCase(0, 720)]
        [TestCase(1280, 0)]
        public void GivenAnInvalidViewport_WhenCreatingProjection_ThenTheDimensionIsRejected(
            int viewportWidth,
            int viewportHeight)
            => Assert.That(
                () => trackCamera.CreateProjection(viewportWidth, viewportHeight),
                Throws.TypeOf<ArgumentOutOfRangeException>());

        [Test]
        public void GivenNegativeElapsedTime_WhenUpdating_ThenTheElapsedTimeIsRejected()
            => Assert.That(
                () => trackCamera.Update(-1.0f, 0.0f, 0.0f),
                Throws.TypeOf<ArgumentOutOfRangeException>());

        [Test]
        public void GivenNoTrackBlocks_WhenConstructing_ThenTheInputIsRejected()
            => Assert.That(
                () => new TrackCamera([]),
                Throws.TypeOf<ArgumentException>());

        private static TrackBlock BuildTrackBlock(
            double positionX,
            double positionY,
            double positionZ)
            => new()
            {
                Centre = new TrackPoint
                {
                    X = positionX,
                    Y = positionY,
                    Z = positionZ
                }
            };
    }
}