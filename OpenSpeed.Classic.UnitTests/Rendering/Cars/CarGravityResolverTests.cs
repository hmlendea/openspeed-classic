using System;

using Microsoft.Xna.Framework;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering.Cars;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Rendering.Cars
{
    [TestFixture]
    public sealed class CarGravityResolverTests
    {
        private static float ValueTolerance => 0.001f;

        [Test]
        public void GivenACarAboveTheRoad_WhenResolvingTwice_ThenGravityAcceleratesItDownwards()
        {
            CarPhysicsState physicsState = new();
            Matrix world = Matrix.CreateWorld(
                new Vector3(0.0f, 16.0f, 0.0f),
                Vector3.Forward,
                Vector3.Up);
            TrackRoutePoint routePoint = BuildRoutePoint(0.0, 0.0, Vector3.Up);

            Matrix firstWorld = CarGravityResolver.Resolve(
                world,
                [routePoint],
                physicsState,
                0.5f);
            Matrix secondWorld = CarGravityResolver.Resolve(
                firstWorld,
                [routePoint],
                physicsState,
                0.5f);

            Assert.Multiple(() =>
            {
                Assert.That(firstWorld.Translation.Y, Is.EqualTo(13.5475f).Within(ValueTolerance));
                Assert.That(secondWorld.Translation.Y, Is.EqualTo(8.6425f).Within(ValueTolerance));
                Assert.That(physicsState.VerticalVelocity, Is.EqualTo(-9.81f).Within(ValueTolerance));
            });
        }

        [Test]
        public void GivenAnAscendingRoad_WhenResolving_ThenHeightAndNormalAreInterpolated()
        {
            Vector3 slopeNormal = Vector3.Normalize(new Vector3(0.0f, 2.0f, 1.0f));
            Vector3 expectedNormal = Vector3.Normalize(Vector3.Lerp(
                Vector3.Up,
                slopeNormal,
                0.5f));
            TrackRoutePoint start = BuildRoutePoint(0.0, 0.0, Vector3.Up);
            TrackRoutePoint end = BuildRoutePoint(8.0, -16.0, slopeNormal);
            Matrix world = Matrix.CreateWorld(
                new Vector3(0.0f, 0.0f, -8.0f),
                Vector3.Forward,
                Vector3.Up);

            Matrix resolvedWorld = CarGravityResolver.Resolve(
                world,
                [start, end],
                new CarPhysicsState(),
                0.0f);

            Assert.Multiple(() =>
            {
                Assert.That(resolvedWorld.Translation.Y, Is.EqualTo(4.0f).Within(ValueTolerance));
                Assert.That(resolvedWorld.Up.X, Is.EqualTo(expectedNormal.X).Within(ValueTolerance));
                Assert.That(resolvedWorld.Up.Y, Is.EqualTo(expectedNormal.Y).Within(ValueTolerance));
                Assert.That(resolvedWorld.Up.Z, Is.EqualTo(expectedNormal.Z).Within(ValueTolerance));
            });
        }

        [Test]
        public void GivenABankedRoad_WhenResolving_ThenLateralPositionAffectsGroundHeight()
        {
            Vector3 bankNormal = Vector3.Normalize(new Vector3(-1.0f, 2.0f, 0.0f));
            TrackRoutePoint routePoint = BuildRoutePoint(0.0, 0.0, bankNormal);
            Matrix world = Matrix.CreateWorld(
                new Vector3(4.0f, 0.0f, 0.0f),
                Vector3.Forward,
                Vector3.Up);

            Matrix resolvedWorld = CarGravityResolver.Resolve(
                world,
                [routePoint],
                new CarPhysicsState(),
                0.0f);

            Assert.That(resolvedWorld.Translation.Y, Is.EqualTo(2.0f).Within(ValueTolerance));
        }

        [Test]
        public void GivenNoRoutePoints_WhenResolving_ThenTheWorldIsUnchanged()
        {
            Matrix world = Matrix.CreateTranslation(4.0f, 8.0f, 16.0f);

            Matrix resolvedWorld = CarGravityResolver.Resolve(
                world,
                [],
                new CarPhysicsState(),
                1.0f);

            Assert.That(resolvedWorld, Is.EqualTo(world));
        }

        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-1.0f)]
        public void GivenInvalidElapsedTime_WhenResolving_ThenTheElapsedTimeIsRejected(
            float elapsedSeconds)
            => Assert.That(
                () => CarGravityResolver.Resolve(
                    Matrix.Identity,
                    [],
                    new CarPhysicsState(),
                    elapsedSeconds),
                Throws.TypeOf<ArgumentOutOfRangeException>());

        private static TrackRoutePoint BuildRoutePoint(
            double positionY,
            double positionZ,
            Vector3 normal)
            => new()
            {
                Forward = new TrackVector
                {
                    Y = 1.0,
                    Z = -2.0
                },
                LeftBorderDistance = 8.0,
                Normal = new TrackVector
                {
                    X = normal.X,
                    Y = normal.Y,
                    Z = normal.Z
                },
                Position = new TrackPoint
                {
                    Y = positionY,
                    Z = positionZ
                },
                Right = new TrackVector
                {
                    X = 1.0
                },
                RightBorderDistance = 8.0
            };
    }
}