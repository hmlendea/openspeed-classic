using System.Collections.Generic;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class PairCollisionBroadPhaseTests
    {
        [TestCase(0, 1, 0, 0, false, 1, true)]
        [TestCase(1, 1, 0, 0, true, 0, false)]
        [TestCase(0, 0, 0, 0, true, 0, false)]
        [TestCase(0, 1, 1, 0, false, 1, true)]
        [TestCase(0, 1, 0x640000, 0, false, 1, true)]
        [TestCase(0, 1, 0x640001, 0, false, 0, true)]
        [TestCase(0, 1, 0, 1, false, 1, true)]
        [TestCase(0, 1, 0, 2, false, 0, true)]
        public void GivenGates_WhenUpdating_ThenResolutionAndRefreshFollowNativeOrder(
            byte contactMode,
            byte activeBody,
            int distanceMetric,
            int coolDown,
            bool expectsEarlyReturn,
            int expectedPairs,
            bool expectsRefresh)
        {
            CarMemory car = BuildCar();
            car.WriteByte(0x8C, contactMode);
            car.WriteByte(0x8D, activeBody);
            car[0x88] = distanceMetric;
            car[0x16C] = coolDown;
            CarMemory earlierCar = BuildCar();
            List<string> calls = [];

            int resolved = PairCollisionBroadPhase.Update(
                car,
                [earlierCar],
                (current, earlier) =>
                {
                    calls.Add("resolve");

                    return true;
                },
                current => calls.Add("refresh"));

            Assert.That(resolved, Is.EqualTo(expectedPairs));
            Assert.That(calls.Contains("resolve"), Is.EqualTo(expectedPairs == 1));
            Assert.That(calls.Contains("refresh"), Is.EqualTo(expectsRefresh));

            if (expectsEarlyReturn)
            {
                Assert.That(calls, Is.Empty);
            }
        }

        [TestCase(0x20000, false)]
        [TestCase(0x1FFFF, true)]
        [TestCase(0, true)]
        public void GivenRadii_WhenCheckingPairs_ThenInclusiveSeparationIsRejected(
            int separation,
            bool expectsPair)
        {
            CarMemory car = BuildCar();
            CarMemory earlierCar = BuildCar();
            earlierCar[0xA4] = separation;

            int resolved = PairCollisionBroadPhase.Update(
                car, [earlierCar], (current, earlier) => true, current => { });

            Assert.That(resolved, Is.EqualTo(expectsPair ? 1 : 0));
        }

        [TestCase(0xA0, 0x20000)]
        [TestCase(0x9C, 0x20000)]
        [TestCase(0xA4, -0x20000)]
        public void GivenSeparationOnEitherAxis_WhenCheckingPairs_ThenNoPairIsResolved(
            int offset,
            int separation)
        {
            CarMemory car = BuildCar();
            CarMemory earlierCar = BuildCar();
            earlierCar[offset] = separation;

            int resolved = PairCollisionBroadPhase.Update(
                car, [earlierCar], (current, earlier) => true, current => { });

            Assert.That(resolved, Is.Zero);
        }

        [Test]
        public void GivenPositiveCoolDown_WhenUpdating_ThenItDecrementsOnceAndSuppressesResolution()
        {
            CarMemory car = BuildCar();
            car[0x16C] = 2;
            CarMemory earlierCar = BuildCar();
            bool refreshed = false;

            int resolved = PairCollisionBroadPhase.Update(
                car,
                [earlierCar],
                (current, earlier) =>
                {
                    Assert.Fail("No pair may be resolved.");

                    return false;
                },
                current => refreshed = true);

            Assert.That(car[0x16C], Is.EqualTo(1));
            Assert.That(resolved, Is.Zero);
            Assert.That(refreshed);
        }

        [Test]
        public void GivenMultipleOverlappingCars_WhenUpdating_ThenBothArePreScaledPostScaledAndClamped()
        {
            CarMemory car = BuildCar();
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            int pairCount = 0;

            for (int offset = 0xE8; offset <= 0xF0; offset += sizeof(int))
            {
                car[offset] = 0x4000;
                first[offset] = 0x4000;
                second[offset] = 0x4000;
            }

            int preScaled = X86Math.MultiplyQ16(0x4000, 0x6487E);

            int resolved = PairCollisionBroadPhase.Update(
                car,
                [first, second],
                (current, earlier) =>
                {
                    if (pairCount == 0)
                    {
                        Assert.That(current[0xE8], Is.EqualTo(preScaled));
                        Assert.That(earlier[0xE8], Is.EqualTo(preScaled));
                    }

                    pairCount += 1;

                    return true;
                },
                current => Assert.That(current[0xE8], Is.InRange(-0x1CCCC, 0x1CCCC)));

            Assert.That(resolved, Is.EqualTo(2));
            Assert.That(pairCount, Is.EqualTo(2));

            for (int offset = 0xE8; offset <= 0xF0; offset += sizeof(int))
            {
                Assert.That(car[offset], Is.InRange(-0x1CCCC, 0x1CCCC));
                Assert.That(first[offset], Is.InRange(-0x1CCCC, 0x1CCCC));
                Assert.That(second[offset], Is.InRange(-0x1CCCC, 0x1CCCC));
            }
        }

        [Test]
        public void GivenExtremeAngularMotion_WhenUpdating_ThenPostScaleClampBoundsEveryComponent()
        {
            CarMemory car = BuildCar();
            CarMemory earlierCar = BuildCar();

            for (int offset = 0xE8; offset <= 0xF0; offset += sizeof(int))
            {
                car[offset] = int.MaxValue;
                earlierCar[offset] = int.MinValue;
            }

            PairCollisionBroadPhase.Update(car, [earlierCar], (current, earlier) => true, current => { });

            for (int offset = 0xE8; offset <= 0xF0; offset += sizeof(int))
            {
                Assert.That(car[offset], Is.InRange(-0x1CCCC, 0x1CCCC));
                Assert.That(earlierCar[offset], Is.InRange(-0x1CCCC, 0x1CCCC));
            }
        }

        [Test]
        public void GivenMissingDependencies_WhenUpdating_ThenArgumentErrorsAreReported()
        {
            CarMemory car = BuildCar();
            List<CarMemory> cars = [];

            Assert.That(
                () => PairCollisionBroadPhase.Update(null!, cars, (current, earlier) => true, current => { }),
                Throws.ArgumentNullException);
            Assert.That(
                () => PairCollisionBroadPhase.Update(car, null!, (current, earlier) => true, current => { }),
                Throws.ArgumentNullException);
            Assert.That(
                () => PairCollisionBroadPhase.Update(car, cars, null!, current => { }),
                Throws.ArgumentNullException);
            Assert.That(
                () => PairCollisionBroadPhase.Update(car, cars, (current, earlier) => true, null!),
                Throws.ArgumentNullException);
        }

        private static CarMemory BuildCar()
        {
            CarMemory car = new();
            car.WriteByte(0x8D, 1);
            car[0x114] = X86Math.One;

            return car;
        }
    }
}