using System;
using System.Buffers.Binary;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class RouteBasisTests
    {
        [Test]
        public void GivenSignedRouteBytes_WhenLoading_ThenRowsArePreservedWithoutNormalisation()
        {
            byte[] records = new byte[PhysicsRoute.RecordSize * 2];
            BinaryPrimitives.WriteInt32LittleEndian(records, 42);
            BinaryPrimitives.WriteInt32LittleEndian(records.AsSpan(4), -64);
            BinaryPrimitives.WriteInt32LittleEndian(records.AsSpan(8), 128);
            BinaryPrimitives.WriteInt32LittleEndian(records.AsSpan(36), 42);
            BinaryPrimitives.WriteInt32LittleEndian(records.AsSpan(44), 256);
            records[0x12] = 127;
            records[0x0D] = 128;
            records[0x11] = 64;
            CarMemory car = new();
            RouteBasis.LoadRecord(car, new PhysicsRoute(records), 0);
            CarMemory expected = new();
            expected[0x13C] = 42;
            expected[0x140] = -64;
            expected[0x144] = 128;
            expected[0x118] = 0xFE00;
            expected[0x128] = -0x10000;
            expected[0x138] = 0x8000;

            Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GivenARouteBasis_WhenCopying_ThenOnlyRightAndForwardRowsAreReflected(bool isReversed)
        {
            CarMemory car = new();
            CarMemory expected = new();

            for (int index = 0; index < FixedMatrix.CellCount; index += 1)
            {
                int value = index + 42;
                car[0x118 + index * sizeof(int)] = value;
                expected[0x118 + index * sizeof(int)] = value;

                if (isReversed && index / FixedMatrix.Dimension != 1)
                {
                    value = -value;
                }

                expected[0xC4 + index * sizeof(int)] = value;
                expected[0x188 + index * sizeof(int)] = value;
            }

            RouteBasis.CopyPrimary(car, isReversed);
            RouteBasis.CopySecondary(car, isReversed);

            Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
        }

        [Test]
        public void GivenMinimumDword_WhenReflecting_ThenNegationWraps()
        {
            CarMemory car = new();
            car[0x118] = int.MinValue;
            RouteBasis.CopyPrimary(car, true);

            Assert.That(car[0xC4], Is.EqualTo(int.MinValue));
        }

        [Test]
        public void GivenNullArguments_WhenCopying_ThenArgumentErrorsAreReported()
        {
            PhysicsRoute route = new(new byte[PhysicsRoute.RecordSize]);

            Assert.Multiple(() =>
            {
                Assert.That(() => RouteBasis.LoadRecord(null!, route, 0), Throws.ArgumentNullException);
                Assert.That(() => RouteBasis.LoadRecord(new CarMemory(), null!, 0), Throws.ArgumentNullException);
                Assert.That(() => RouteBasis.CopyPrimary(null!, false), Throws.ArgumentNullException);
                Assert.That(() => RouteBasis.CopySecondary(null!, false), Throws.ArgumentNullException);
            });
        }
    }
}