using System;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class NativeContactCeilingTests
    {
        [TestCase(0x02, true)]
        [TestCase(0x20, true)]
        [TestCase(0x22, true)]
        [TestCase(0x12, true)]
        [TestCase(0x21, true)]
        [TestCase(0x00, false)]
        [TestCase(0x11, false)]
        [TestCase(0xFF, false)]
        public void GivenARouteMarker_WhenResolving_ThenEitherNibbleCanEnableContact(
            byte marker,
            bool expectsContact)
        {
            CarMemory car = new();
            car[0xA0] = 0xA0001;
            int calls = 0;

            bool wasResolved = NativeContactCeiling.TryResolve(
                car, BuildRoute(0, marker), 0, 0, (body, normal, reference) => calls += 1);

            Assert.That(wasResolved, Is.EqualTo(expectsContact));
            Assert.That(calls, Is.EqualTo(Convert.ToInt32(expectsContact)));
            Assert.That(car[0xA0], Is.EqualTo(0xA0001));
        }

        [TestCase(0, 0, 0xA0000)]
        [TestCase(8, 0, 0x140000)]
        [TestCase(4, 0x571, 0xA0000)]
        [TestCase(4, 0x572, 0xA0000)]
        [TestCase(4, 0x573, 0x180000)]
        [TestCase(4, 0x59F, 0x180000)]
        [TestCase(4, 0x5A0, 0xA0000)]
        [TestCase(4, 0x5A1, 0xA0000)]
        [TestCase(4, 0x3EB, 0xA0000)]
        [TestCase(4, 0x3EC, 0xA0000)]
        [TestCase(4, 0x3ED, 0xE0000)]
        [TestCase(4, 0x429, 0xE0000)]
        [TestCase(4, 0x42A, 0xA0000)]
        [TestCase(4, 0x42B, 0xA0000)]
        public void GivenTrackClearance_WhenCrossingTheCeiling_ThenOnlyStrictPenetrationResolves(
            int trackIdentifier,
            int routeIndex,
            int clearance)
        {
            CarMemory car = new();
            car[0x14] = routeIndex;
            PhysicsRoute route = BuildRoute(routeIndex, 0x20);
            int calls = 0;

            for (int displacement = -1; displacement <= 1; displacement += 1)
            {
                car[0xA0] = 0x70000 + clearance + displacement;
                bool wasResolved = NativeContactCeiling.TryResolve(
                    car, route, trackIdentifier, 0x70000, (body, normal, reference) => calls += 1);

                Assert.That(wasResolved, Is.EqualTo(displacement > 0));
            }

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void GivenCeilingContact_WhenResolving_ThenTheNormalIsNegatedAndAngularClampsFollowTheCallback()
        {
            CarMemory car = new();
            car[0xA0] = 0xA0001;
            car[0x124] = int.MinValue;
            car[0x128] = 0x10000;
            car[0x12C] = -42;
            car[0x13C] = 42;
            car[0x140] = 613;
            car[0x144] = -32;
            car[0xE8] = 32;

            bool wasResolved = NativeContactCeiling.TryResolve(
                car, BuildRoute(0, 0x20), 0, 0, (body, normal, reference) =>
                {
                    Assert.That(body, Is.SameAs(car));
                    Assert.That(body[0xE8], Is.EqualTo(32));
                    Assert.That(normal.First, Is.EqualTo(int.MinValue));
                    Assert.That(normal.Second, Is.EqualTo(-0x10000));
                    Assert.That(normal.Third, Is.EqualTo(42));
                    Assert.That(reference.First, Is.EqualTo(42));
                    Assert.That(reference.Second, Is.EqualTo(613));
                    Assert.That(reference.Third, Is.EqualTo(-32));
                    body[0xE8] = int.MinValue;
                    body[0xEC] = int.MaxValue;
                    body[0xF0] = -42;
                    normal.Second = 0;
                    reference.Second = 0;
                });

            Assert.That(wasResolved);
            Assert.That(car[0xE8], Is.EqualTo(-0x26666));
            Assert.That(car[0xEC], Is.EqualTo(0x26666));
            Assert.That(car[0xF0], Is.EqualTo(-42));
            Assert.That(car[0x128], Is.EqualTo(0x10000));
            Assert.That(car[0x140], Is.EqualTo(613));
        }

        [Test]
        public void GivenWrappedCeilingHeight_WhenResolving_ThenTheSignedComparisonUsesTheWrappedSum()
        {
            CarMemory car = new();
            int calls = 0;

            Assert.That(NativeContactCeiling.TryResolve(
                car, BuildRoute(0, 2), 0, int.MaxValue,
                (body, normal, reference) => calls += 1));
            Assert.That(calls, Is.EqualTo(1));
        }

        private static PhysicsRoute BuildRoute(int routeIndex, byte marker)
        {
            byte[] records = new byte[(routeIndex + 1) * PhysicsRoute.RecordSize];
            records[routeIndex * PhysicsRoute.RecordSize + 0x1E] = marker;
            records[routeIndex * PhysicsRoute.RecordSize + 0x1F] = 0xFF;

            return new PhysicsRoute(records);
        }
    }
}