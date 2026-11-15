using System;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class RouteLateralTargetTests
    {
        private static PhysicsRoute BuildRoute(params (int x, int y, int z)[] positions)
        {
            byte[] records = new byte[positions.Length * PhysicsRoute.RecordSize];

            for (int index = 0; index < positions.Length; index += 1)
            {
                BitConverter.TryWriteBytes(records.AsSpan(index * PhysicsRoute.RecordSize, 4), positions[index].x);
                BitConverter.TryWriteBytes(records.AsSpan(index * PhysicsRoute.RecordSize + 4, 4), positions[index].y);
                BitConverter.TryWriteBytes(records.AsSpan(index * PhysicsRoute.RecordSize + 8, 4), positions[index].z);
            }

            return new PhysicsRoute(records);
        }

        private static PhysicsRoute BuildStraightRoute(int count)
        {
            (int, int, int)[] positions = new (int, int, int)[count];

            for (int index = 0; index < count; index += 1)
            {
                positions[index] = (index * 0x10000, 0, 0);
            }

            return BuildRoute(positions);
        }

        [Test]
        public void GivenAWideDeviation_WhenUpdating_ThenTheWideDeviationQueryIsInvoked()
        {
            CarMemory car = new();
            car[0x288] = 1;
            car[0x3AC] = 0x100000;
            car[0x2E4] = 0;
            car[0x2B8] = 0x10000;
            bool invoked = false;

            RouteLateralTarget.Update(
                car,
                new CarSpecifications(),
                BuildStraightRoute(4),
                1,
                0,
                _ => invoked = true);

            Assert.That(invoked, Is.True);
        }

        [Test]
        public void GivenANegativeProjectedNormal_WhenUpdating_ThenTheWideDeviationQueryIsInvoked()
        {
            CarMemory car = new();
            car[0x288] = 1;
            car[0x3AC] = 0;
            car[0x2B8] = -1;
            bool invoked = false;

            RouteLateralTarget.Update(
                car,
                new CarSpecifications(),
                BuildStraightRoute(4),
                1,
                0,
                _ => invoked = true);

            Assert.That(invoked, Is.True);
        }

        [TestCase(0x00, 0)]
        [TestCase(0x3F, 0)]
        public void GivenASteepnessCoefficientBelowOneUnit_WhenUpdating_ThenTheForceIsZero(
            int steepnessCoefficient,
            int expectedForce)
        {
            CarMemory car = new();
            car[0x288] = 1;
            car[0x3AC] = 0;
            car[0x2E4] = 0x20;
            car[0x2B8] = 0x20000;
            CarSpecifications descriptor = new();
            descriptor[0x12C] = steepnessCoefficient;

            RouteLateralTarget.Update(car, descriptor, BuildStraightRoute(6), 1, 1, _ => { });

            // The steepness coefficient is truncated toward zero by six before use, so any value
            // below 0x40 contributes nothing to the force scale.
            Assert.That(car[0x30C], Is.EqualTo(expectedForce));
        }

        [Test]
        public void GivenSteepnessCoefficientsSharingASixBitValue_WhenUpdating_ThenTheForcesAreEqual()
        {
            CarMemory first = new();
            first[0x288] = 1;
            first[0x3AC] = 0;
            first[0x2E4] = 0x20;
            first[0x2B8] = 0x20000;
            CarSpecifications firstDescriptor = new();
            firstDescriptor[0x12C] = 0x40;

            CarMemory second = new();
            second[0x288] = 1;
            second[0x3AC] = 0;
            second[0x2E4] = 0x20;
            second[0x2B8] = 0x20000;
            CarSpecifications secondDescriptor = new();
            secondDescriptor[0x12C] = 0x7F;

            RouteLateralTarget.Update(first, firstDescriptor, BuildStraightRoute(6), 1, 1, _ => { });
            RouteLateralTarget.Update(second, secondDescriptor, BuildStraightRoute(6), 1, 1, _ => { });

            // Truncating the steepness coefficient toward zero by six maps both 0x40 and 0x7F to
            // one unit, so the two forces must match. Reading the raw descriptor cell would make
            // them differ, which is the regression this fixture guards against.
            Assert.That(first[0x30C], Is.EqualTo(second[0x30C]));
        }

        [Test]
        public void GivenAnInRangeDeviation_WhenUpdating_ThenTheLateralTargetAndForceAreWritten()
        {
            CarMemory car = new();
            car[0x288] = 1;
            car[0x3AC] = 0;
            car[0x2E4] = 0x20;
            car[0x2B8] = 0x20000;
            CarSpecifications descriptor = new();
            descriptor[0x12C] = X86Math.One;

            RouteLateralTarget.Update(
                car,
                descriptor,
                BuildStraightRoute(6),
                1,
                1,
                _ => Assert.Fail("The wide-deviation query must not run for an in-range deviation."));

            Assert.Multiple(() =>
            {
                Assert.That(car[0x368], Is.Zero);
                Assert.That(car[0x30C], Is.Not.Zero);
                Assert.That(car[0x394], Is.Not.Zero);
            });
        }

        [TestCase(1, 0x18, 0x00)]
        [TestCase(0, 0x00, -0x18)]
        public void GivenTrackModes_WhenResolvingTheAdjacentOffset_ThenTheModeSpecificByteIsScaled(
            int trackMode,
            int expectedHigh,
            int expectedLow)
        {
            byte[] records = new byte[PhysicsRoute.RecordSize * 2];
            records[0x1F] = trackMode == 1 ? (byte)0x03 : (byte)0x30;
            records[0x20] = 0x02;
            records[0x21] = 0x02;
            PhysicsRoute route = new(records);
            CarMemory car = new();
            car[0x14] = 0;

            int expected = trackMode == 1 ? expectedHigh << 12 : expectedLow << 12;

            Assert.That(RouteLateralTarget.Sub466200(car, route, trackMode), Is.EqualTo(expected));
        }

        [Test]
        public void GivenNullArguments_WhenUpdating_ThenArgumentErrorsAreReported()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => RouteLateralTarget.Update(null!, new CarSpecifications(), BuildStraightRoute(2), 1, 0, _ => { }), Throws.ArgumentNullException);
                Assert.That(() => RouteLateralTarget.Update(new CarMemory(), null!, BuildStraightRoute(2), 1, 0, _ => { }), Throws.ArgumentNullException);
                Assert.That(() => RouteLateralTarget.Update(new CarMemory(), new CarSpecifications(), null!, 1, 0, _ => { }), Throws.ArgumentNullException);
                Assert.That(() => RouteLateralTarget.Update(new CarMemory(), new CarSpecifications(), BuildStraightRoute(2), 1, 0, null!), Throws.ArgumentNullException);
                Assert.That(() => RouteLateralTarget.Sub466200(null!, BuildStraightRoute(2), 1), Throws.ArgumentNullException);
            });
        }
    }
}
