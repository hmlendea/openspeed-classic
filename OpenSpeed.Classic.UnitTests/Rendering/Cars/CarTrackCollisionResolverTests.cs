using System;

using Microsoft.Xna.Framework;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering.Cars;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Rendering.Cars
{
    [TestFixture]
    public sealed class CarTrackCollisionResolverTests
    {
        [TestCase(-8.0f, -7.0f)]
        [TestCase(8.0f, 7.0f)]
        public void GivenACarBeyondAWall_WhenResolving_ThenTheCarStopsAtTheWall(
            float positionX,
            float expectedPositionX)
        {
            Matrix world = Matrix.CreateWorld(
                new Vector3(positionX, 0.0f, -16.0f),
                Vector3.Forward,
                Vector3.Up);

            Matrix resolvedWorld = CarTrackCollisionResolver.Resolve(
                world,
                [BuildRoutePoint()]);

            Assert.That(
                resolvedWorld.Translation,
                Is.EqualTo(new Vector3(expectedPositionX, 0.0f, -16.0f)));
        }

        [Test]
        public void GivenACarWithinTheWalls_WhenResolving_ThenItsPositionIsUnchanged()
        {
            Matrix world = Matrix.CreateWorld(
                new Vector3(4.0f, 0.0f, -16.0f),
                Vector3.Forward,
                Vector3.Up);

            Matrix resolvedWorld = CarTrackCollisionResolver.Resolve(
                world,
                [BuildRoutePoint()]);

            Assert.That(resolvedWorld.Translation, Is.EqualTo(world.Translation));
        }

        [Test]
        public void GivenMultipleRoutePoints_WhenResolving_ThenTheNearestBordersAreUsed()
        {
            TrackRoutePoint distantRoutePoint = BuildRoutePoint();
            distantRoutePoint.Position.Z = 64.0;
            distantRoutePoint.LeftBorderDistance = 4.0;
            distantRoutePoint.RightBorderDistance = 4.0;
            Matrix world = Matrix.CreateWorld(
                new Vector3(6.0f, 0.0f, -16.0f),
                Vector3.Forward,
                Vector3.Up);

            Matrix resolvedWorld = CarTrackCollisionResolver.Resolve(
                world,
                [distantRoutePoint, BuildRoutePoint()]);

            Assert.That(resolvedWorld.Translation.X, Is.EqualTo(6.0f));
        }

        [Test]
        public void GivenNoRoutePoints_WhenResolving_ThenThePositionIsUnchanged()
        {
            Matrix world = Matrix.CreateTranslation(4.0f, 8.0f, 16.0f);

            Matrix resolvedWorld = CarTrackCollisionResolver.Resolve(world, []);

            Assert.That(resolvedWorld, Is.EqualTo(world));
        }

        [Test]
        public void GivenAZeroRightVector_WhenResolving_ThenThePositionIsUnchanged()
        {
            TrackRoutePoint routePoint = BuildRoutePoint();
            routePoint.Right = new TrackVector();
            Matrix world = Matrix.CreateTranslation(8.0f, 0.0f, -16.0f);

            Matrix resolvedWorld = CarTrackCollisionResolver.Resolve(
                world,
                [routePoint]);

            Assert.That(resolvedWorld, Is.EqualTo(world));
        }

        [Test]
        public void GivenNullRoutePoints_WhenResolving_ThenAnArgumentNullExceptionIsThrown()
            => Assert.That(
                () => CarTrackCollisionResolver.Resolve(Matrix.Identity, null!),
                Throws.TypeOf<ArgumentNullException>());

        private static TrackRoutePoint BuildRoutePoint()
            => new()
            {
                LeftBorderDistance = 8.0,
                Position = new TrackPoint
                {
                    Z = -16.0
                },
                Right = new TrackVector
                {
                    X = 127.0
                },
                RightBorderDistance = 8.0
            };
    }
}