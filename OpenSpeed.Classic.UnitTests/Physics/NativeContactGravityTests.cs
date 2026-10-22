using System;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class NativeContactGravityTests
    {
        [TestCase(7, 0x143, 4, 0x10000)]
        [TestCase(7, 0x144, 4, 0x10000)]
        [TestCase(7, 0x145, 4, 0x30000)]
        [TestCase(7, 0x22F, 4, 0x30000)]
        [TestCase(7, 0x230, 4, 0x10000)]
        [TestCase(7, 0x231, 4, 0x10000)]
        [TestCase(7, int.MinValue, 4, 0x10000)]
        [TestCase(7, int.MaxValue, 4, 0x10000)]
        [TestCase(6, 0x145, 4, 0x10000)]
        [TestCase(8, 0x145, 4, 0x10000)]
        [TestCase(7, 0x145, 0, 0x10000)]
        [TestCase(7, 0x145, 0x40, 0x10000)]
        [TestCase(7, 0x145, 0x44, 0x30000)]
        public void GivenTrackAndRoleGates_WhenApplyingGravity_ThenStrictRouteScalingIsUsed(
            int trackIdentifier,
            int routeIndex,
            int roleFlags,
            int expectedScale)
        {
            CarMemory car = BuildCar(X86Math.One, 42);
            car[0x14] = routeIndex;
            car[0x1F4] = roleFlags;
            CarMemory expected = BuildCar(X86Math.One, unchecked(42 - 0x7599 * expectedScale));
            expected[0x14] = routeIndex;
            expected[0x1F4] = roleFlags;
            expected[0x150] = 0x640000;

            int scaledAngle = NativeContactGravity.Apply(car, trackIdentifier, X86Math.One);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(scaledAngle, Is.EqualTo(expectedScale));
                Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
            }

        }

        [TestCase(0, 0x10000, 42, 42)]
        [TestCase(1, 0x10000, 42, -30063)]
        [TestCase(-1, 0x10000, 42, 30147)]
        [TestCase(1, 0x8000, 42, -15011)]
        [TestCase(-1, 0x8000, 42, 15094)]
        [TestCase(1, -0x8000, 42, 15094)]
        [TestCase(0x10000, 0x10000, 0, -1972961280)]
        [TestCase(0x20000, 0x10000, 0, 349044736)]
        [TestCase(int.MinValue, 0x10000, 0, int.MinValue)]
        [TestCase(1, 0x10000, int.MinValue, 2147453543)]
        public void GivenScheduledGravity_WhenApplying_ThenDwordWrappingAndQ16RoundingArePreserved(
            int scheduleScalar,
            int motionScale,
            int verticalVelocity,
            int expectedVelocity)
        {
            CarMemory car = BuildCar(motionScale, verticalVelocity);
            CarMemory expected = BuildCar(motionScale, expectedVelocity);
            expected[0x150] = 0x640000;

            int scaledAngle = NativeContactGravity.Apply(car, 7, scheduleScalar);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(scaledAngle, Is.EqualTo(motionScale));
                Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
            }

        }

        [Test]
        public void GivenAnOverflowingTrackScale_WhenApplying_ThenTheScaledAngleWraps()
        {
            CarMemory car = BuildCar(int.MaxValue, 42);
            car[0x14] = 0x145;
            car[0x1F4] = 4;
            CarMemory expected = BuildCar(int.MaxValue, -986480597);
            expected[0x14] = 0x145;
            expected[0x1F4] = 4;
            expected[0x150] = 0x640000;

            int scaledAngle = NativeContactGravity.Apply(car, 7, 1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(scaledAngle, Is.EqualTo(2147483645));
                Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
            }

        }

        [Test]
        public void GivenRepeatedGravitySteps_WhenApplying_ThenVelocityAccumulatesWithoutChangingScale()
        {
            CarMemory car = BuildCar(X86Math.One, 42);
            CarMemory expected = BuildCar(X86Math.One, -60168);
            expected[0x150] = 0x640000;

            NativeContactGravity.Apply(car, 7, 1);
            NativeContactGravity.Apply(car, 7, 1);

            Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
        }

        [Test]
        public void GivenAnAbsentCar_WhenApplying_ThenTheParameterIsRejected()
            => Assert.That(
                () => NativeContactGravity.Apply(null!, 7, 1),
                Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("car"));

        private static CarMemory BuildCar(int motionScale, int verticalVelocity)
        {
            CarMemory car = new();

            for (int offset = 0; offset < CarMemory.SizeInBytes; offset += sizeof(int))
            {
                car[offset] = 0x12345678;
            }

            car[0x1F4] = 0;
            car[0x94] = motionScale;
            car[0xAC] = verticalVelocity;

            return car;
        }
    }
}