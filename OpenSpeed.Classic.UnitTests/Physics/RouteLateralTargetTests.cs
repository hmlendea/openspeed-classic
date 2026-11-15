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
