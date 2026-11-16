using System.Collections.Generic;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class RouteContactPreparationTests
    {
        [TestCase(0, 1)]
        [TestCase(1, 0)]
        [TestCase(2, -1)]
        [TestCase(255, -254)]
        public void GivenAContactMode_WhenDispatching_ThenTheQueryReceivesThePositionAndSignedMode(
            byte contactMode,
            int expectedMode)
        {
            CarMemory car = new();
            car.WriteByte(0x8C, contactMode);
            car[0x9C] = 42;
            car[0xA0] = -613;
            car[0xA4] = int.MinValue;
            car[0x180] = 32;
            car[0x184] = 16;
            int calls = 0;

            RouteContactPreparation.DispatchRouteQuery(car, (position, mode) =>
            {
                calls += 1;
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(position.First, Is.EqualTo(42));
                    Assert.That(position.Second, Is.EqualTo(-613));
                    Assert.That(position.Third, Is.EqualTo(int.MinValue));
                    Assert.That(mode, Is.EqualTo(expectedMode));
                }

                position.First = 0;
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(car[0x9C], Is.EqualTo(42));
                Assert.That(car[0xA0], Is.EqualTo(-613));
                Assert.That(car[0xA4], Is.EqualTo(int.MinValue));
                Assert.That(car.ReadByte(0x8C), Is.EqualTo(contactMode));
                Assert.That(car[0x180], Is.EqualTo(32));
                Assert.That(car[0x184], Is.EqualTo(16));
            }

        }

        [Test]
        public void GivenAMissingCar_WhenDispatching_ThenTheQueryIsNotCalled()
        {
            int calls = 0;

            Assert.That(
                () => RouteContactPreparation.DispatchRouteQuery(null!, (position, mode) => calls += 1),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("car"));
            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void GivenAMissingQuery_WhenDispatching_ThenTheDependencyIsRejected()
        {
            CarMemory car = new();

            Assert.That(
                () => RouteContactPreparation.DispatchRouteQuery(car, null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("queryRoute"));
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        public void GivenAQueryFailure_WhenDispatching_ThenTheOriginalExceptionPropagates(
            byte contactMode,
            byte activeBody)
        {
            CarMemory car = new();
            car.WriteByte(0x8C, contactMode);
            car.WriteByte(0x8D, activeBody);
            AssertionException failure = new("The route query failed.");

            Assert.That(
                () => RouteContactPreparation.DispatchRouteQuery(car, (position, mode) => throw failure),
                Throws.Exception.SameAs(failure));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(car.ReadByte(0x8C), Is.EqualTo(contactMode));
                Assert.That(car.ReadByte(0x8D), Is.EqualTo(activeBody));
            }

        }

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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(string.Join(",", calls), Is.EqualTo(expectedCalls));
                Assert.That(car[0x180], Is.EqualTo(expectedSurfaceQuery));
                Assert.That(car[0x184], Is.EqualTo(expectedSurfaceQuery & 0x3F));
                Assert.That(car[0xA0], Is.EqualTo(613));
                Assert.That(car[0x14C], Is.EqualTo(0x12345678));
            }

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