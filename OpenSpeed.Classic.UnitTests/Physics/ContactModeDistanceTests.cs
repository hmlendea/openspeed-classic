using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class ContactModeDistanceTests
    {
        [TestCase(32, 16, 0, 36)]
        [TestCase(16, 32, 0, 36)]
        [TestCase(-32, -16, 0, 36)]
        [TestCase(0, 0, 15, 0x40000)]
        [TestCase(int.MinValue, 0, 0, 0)]
        [TestCase(int.MaxValue, 4, 0, 0)]
        public void GivenASingleActiveCar_WhenCalculating_ThenSignedPlanarAndCircularMetricsAreCombined(
            int first,
            int third,
            int routeIndex,
            int expectedDistance)
        {
            CarMemory car = BuildCar(first, third, routeIndex);
            CarMemory activeCar = BuildCar(0, 0, 0);
            byte[] carBefore = car.Snapshot();
            byte[] activeBefore = activeCar.Snapshot();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                            ContactModeDistance.Calculate(car, activeCar, 16),
                            Is.EqualTo(expectedDistance));
                Assert.That(car.Snapshot(), Is.EqualTo(carBefore));
                Assert.That(activeCar.Snapshot(), Is.EqualTo(activeBefore));
            }

        }

        [TestCase(false)]
        [TestCase(true)]
        public void GivenDifferentPlanarAndRouteNeighbours_WhenCalculating_ThenTheMinimaAreIndependent(
            bool isReversed)
        {
            CarMemory car = BuildCar(0, 0, 0);
            CarMemory planarNeighbour = BuildCar(32, 16, 8);
            CarMemory routeNeighbour = BuildCar(0x1000000, 0, 0);

            if (isReversed)
            {
                Assert.That(
                    ContactModeDistance.Calculate(car, routeNeighbour, planarNeighbour, 16),
                    Is.EqualTo(36));

                return;
            }

            Assert.That(
                ContactModeDistance.Calculate(car, planarNeighbour, routeNeighbour, 16),
                Is.EqualTo(36));
        }

        [Test]
        public void GivenMissingBodies_WhenCalculating_ThenDependenciesAreRejected()
        {
            CarMemory car = new();

            Assert.That(() => ContactModeDistance.Calculate(null!, car, 16), Throws.ArgumentNullException);
            Assert.That(() => ContactModeDistance.Calculate(car, null!, 16), Throws.ArgumentNullException);
            Assert.That(() => ContactModeDistance.Calculate(null!, car, car, 16), Throws.ArgumentNullException);
            Assert.That(() => ContactModeDistance.Calculate(car, null!, car, 16), Throws.ArgumentNullException);
            Assert.That(() => ContactModeDistance.Calculate(car, car, null!, 16), Throws.ArgumentNullException);
        }

        private static CarMemory BuildCar(int first, int third, int routeIndex)
        {
            CarMemory car = new();
            car[0x9C] = first;
            car[0xA0] = 613;
            car[0xA4] = third;
            car[0x14] = routeIndex;

            return car;
        }
    }
}