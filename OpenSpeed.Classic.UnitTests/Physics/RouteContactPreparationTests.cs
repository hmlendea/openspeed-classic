using System.Collections.Generic;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class RouteContactPreparationTests
    {
        [TestCase(0, 0, 0, 0, 0, "", 42)]
        [TestCase(1, 1, 0, 0, 0, "transition", 42)]
        [TestCase(1, 1, 1, 0, 0, "transition,query,surface", 0xC1)]
        [TestCase(1, 0, 0, 0, 0, "transition,query,surface", 0xC1)]
        [TestCase(1, 0, 0, 1, 0, "transition,query,basis,surface", 0xC1)]
        [TestCase(1, 0, 0, 1, 1, "transition,query,basis", 42)]
        public void GivenPreparationGates_WhenUpdating_ThenDependenciesAndStoresRespectSourceOrder(
            byte activeBody,
            int contactTimer,
            int extension,
            int refreshMarker,
            byte contactMode,
            string expectedCalls,
            int expectedSurfaceQuery)
        {
            CarMemory car = new();
            car.WriteByte(0x8D, activeBody);
            car.WriteByte(0x8C, contactMode);
            car.WriteWord(0x76, (ushort)refreshMarker);
            car[0x17C] = contactTimer;
            car[0x18] = extension;
            car[0x180] = 42;
            car[0x184] = 42;
            car[0xA0] = 613;
            car[0x14C] = 0x12345678;
            List<string> calls = [];

            RouteContactPreparation.Update(
                car,
                currentCar => calls.Add("transition"),
                currentCar => calls.Add("query"),
                currentCar => calls.Add("basis"),
                currentCar =>
                {
                    calls.Add("surface");

                    return 0xC1;
                });

            Assert.That(string.Join(",", calls), Is.EqualTo(expectedCalls));
            Assert.That(car[0x180], Is.EqualTo(expectedSurfaceQuery));
            Assert.That(car[0x184], Is.EqualTo(expectedSurfaceQuery & 0x3F));
            Assert.That(car[0xA0], Is.EqualTo(613));
            Assert.That(car[0x14C], Is.EqualTo(0x12345678));
        }

        [Test]
        public void GivenATransitionChangingTheTimer_WhenUpdating_ThenTheChangedGateIsObserved()
        {
            CarMemory car = new();
            car.WriteByte(0x8D, 1);

            RouteContactPreparation.Update(
                car,
                currentCar => currentCar[0x17C] = 1,
                currentCar => Assert.Fail("The route query must not execute."),
                currentCar => Assert.Fail("The basis refresh must not execute."),
                currentCar => throw new AssertionException("The surface query must not execute."));

            Assert.That(car[0x17C], Is.EqualTo(1));
        }
    }
}