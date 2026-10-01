using System;

using Microsoft.Xna.Framework;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering.Cars;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Rendering.Cars
{
    [TestFixture]
    public sealed class CarWorldTransformBuilderTests
    {
        private static float VectorTolerance => 0.001f;

        [Test]
        public void GivenARoutePoint_WhenBuilding_ThenItsPositionAndBasisAreUsed()
        {
            TrackRoutePoint routePoint = BuildRoutePoint(
                new TrackPoint
                {
                    X = 8.0,
                    Y = 16.0,
                    Z = 32.0
                },
                new TrackVector
                {
                    X = 1.0
                },
                new TrackVector
                {
                    Y = 1.0
                });

            Matrix transform = CarWorldTransformBuilder.Build([routePoint]);

            Assert.Multiple(() =>
            {
                Assert.That(transform.Translation, Is.EqualTo(new Vector3(8.0f, 16.0f, 32.0f)));
                Assert.That(transform.Forward.X, Is.EqualTo(1.0f).Within(VectorTolerance));
                Assert.That(transform.Up.Y, Is.EqualTo(1.0f).Within(VectorTolerance));
            });
        }

        [Test]
        public void GivenAZeroBasis_WhenBuilding_ThenTheDefaultBasisIsUsed()
        {
            TrackRoutePoint routePoint = BuildRoutePoint(
                new TrackPoint(),
                new TrackVector(),
                new TrackVector());

            Matrix transform = CarWorldTransformBuilder.Build([routePoint]);

            Assert.Multiple(() =>
            {
                Assert.That(transform.Forward, Is.EqualTo(Vector3.Forward));
                Assert.That(transform.Up, Is.EqualTo(Vector3.Up));
            });
        }

        [Test]
        public void GivenNoRoutePoints_WhenBuilding_ThenTheInputIsRejected()
            => Assert.That(
                () => CarWorldTransformBuilder.Build([]),
                Throws.TypeOf<ArgumentException>());

        private static TrackRoutePoint BuildRoutePoint(
            TrackPoint position,
            TrackVector forward,
            TrackVector normal)
            => new()
            {
                Position = position,
                Forward = forward,
                Normal = normal
            };
    }
}