using System;

using Microsoft.Xna.Framework;

using NUnit.Framework;

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