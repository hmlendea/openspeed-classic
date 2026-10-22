using System;
using System.Buffers.Binary;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class RouteBasisTests
    {
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void GivenAnOrdinaryBody_WhenRefreshing_ThenReferenceAndDirectionsFollowIndependentExtensionGates(
            bool hasExtension,
            bool usesExtensionReference)
        {
            byte[] records = new byte[PhysicsRoute.RecordSize];
            records[0x0D] = 64;
            records[0x11] = 64;
            records[0x12] = 127;
            PhysicsRoute route = new(records);
            CarMemory car = new();
            car.WriteByte(0x8D, 1);
            car[0x28] = 613;
            car[0x2C] = 42;
            car[0x30] = -32;
            byte[] directions = new byte[12];
            BinaryPrimitives.WriteInt16LittleEndian(directions.AsSpan(2), 8192);
            BinaryPrimitives.WriteInt16LittleEndian(directions.AsSpan(10), -16384);

            if (hasExtension)
            {
                car[0x18] = 1;
            }

            int calls = 0;
            RouteBasis.Refresh(car, route, body =>
            {
                Assert.That(body, Is.SameAs(car));
                calls += 1;

                return usesExtensionReference;
            }, directions);

            Assert.That(calls, Is.EqualTo(1));

            if (hasExtension)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(car[0x128], Is.EqualTo(16384));
                    Assert.That(car[0x138], Is.EqualTo(-32768));
                    Assert.That(car[0x118], Is.EqualTo(-8192));
                }

            }
            else
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(car[0x128], Is.EqualTo(32768));
                    Assert.That(car[0x138], Is.EqualTo(32768));
                    Assert.That(car[0x118], Is.EqualTo(16384));
                }

            }

            int expectedReference = 0;

            if (hasExtension && usesExtensionReference)
            {
                expectedReference = 613;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0x13C], Is.EqualTo(expectedReference));
                Assert.That(car[0x11C], Is.Zero);
                Assert.That(car[0x120], Is.Zero);
                Assert.That(car[0x148], Is.EqualTo(PhysicsAngles.RatioAngle(0, 0)));
            }

        }

        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        public void GivenAnInactiveOrContactModeBody_WhenRefreshing_ThenTheOrdinaryQueryIsBypassed(
            byte activeBody,
            byte contactMode)
        {
            CarMemory car = new();
            car.WriteByte(0x8D, activeBody);
            car.WriteByte(0x8C, contactMode);
            PhysicsRoute route = new(new byte[PhysicsRoute.RecordSize]);

            RouteBasis.Refresh(
                car, route,
                body => throw new AssertionException("No ordinary query is expected."),
                ReadOnlySpan<byte>.Empty);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car.ReadByte(0x8D), Is.EqualTo(activeBody));
                Assert.That(car.ReadByte(0x8C), Is.EqualTo(contactMode));
            }

        }

        [TestCase(0)]
        [TestCase(11)]
        public void GivenAShortExtensionBuffer_WhenRefreshing_ThenItIsRejected(int length)
        {
            CarMemory car = new();
            car.WriteByte(0x8D, 1);
            car[0x18] = 1;
            PhysicsRoute route = new(new byte[PhysicsRoute.RecordSize]);

            Assert.That(
                () => RouteBasis.Refresh(car, route, body => false, new byte[length]),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("extensionDirections"));
        }

        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(1, 0)]
        public void GivenAnUnsupportedRefreshMode_WhenRefreshing_ThenAllStateRemainsUnchanged(
            byte activeBody,
            byte contactMode)
        {
            CarMemory car = new();
            car.WriteByte(0x8D, activeBody);
            car.WriteByte(0x8C, contactMode);
            car[0x14] = int.MaxValue;
            car[0x118] = 42;
            car[0x13C] = 613;
            byte[] expected = car.Snapshot();
            PhysicsRoute route = new(new byte[PhysicsRoute.RecordSize]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RouteBasis.TryRefreshContactMode(car, route), Is.False);
                Assert.That(car.Snapshot(), Is.EqualTo(expected));
            }

        }

        [TestCase(1, 0)]
        [TestCase(255, 32)]
        public void GivenAnActiveContactMode_WhenRefreshing_ThenOnlyTheSelectedRecordAndHeadingAreStored(
            byte contactMode,
            int extension)
        {
            byte[] records = new byte[PhysicsRoute.RecordSize * 2];
            int recordOffset = PhysicsRoute.RecordSize;
            BinaryPrimitives.WriteInt32LittleEndian(records.AsSpan(recordOffset), 42);
            BinaryPrimitives.WriteInt32LittleEndian(records.AsSpan(recordOffset + 4), -613);
            BinaryPrimitives.WriteInt32LittleEndian(records.AsSpan(recordOffset + 8), 42);
            records[recordOffset + 0x12] = 127;
            records[recordOffset + 0x13] = 128;
            records[recordOffset + 0x14] = 4;
            records[recordOffset + 0x0C] = 8;
            records[recordOffset + 0x0D] = 16;
            records[recordOffset + 0x0E] = 32;
            records[recordOffset + 0x0F] = 64;
            records[recordOffset + 0x10] = 128;
            records[recordOffset + 0x11] = 255;
            PhysicsRoute route = new(records);
            CarMemory car = BuildContactModeCar(contactMode, extension);
            CarMemory expected = BuildContactModeCar(contactMode, extension);
            expected[0x13C] = 42;
            expected[0x140] = -613;
            expected[0x144] = 42;
            expected[0x118] = 127 << 9;
            expected[0x11C] = -128 << 9;
            expected[0x120] = 4 << 9;
            expected[0x124] = 8 << 9;
            expected[0x128] = 16 << 9;
            expected[0x12C] = 32 << 9;
            expected[0x130] = 64 << 9;
            expected[0x134] = -128 << 9;
            expected[0x138] = -1 << 9;
            expected[0x148] = PhysicsAngles.RatioAngle(-42, -42);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RouteBasis.TryRefreshContactMode(car, route));
                Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
            }

        }

        [Test]
        public void GivenMissingRefreshDependencies_WhenRefreshing_ThenArgumentErrorsAreReported()
        {
            PhysicsRoute route = new(new byte[PhysicsRoute.RecordSize]);

            Assert.That(
                () => RouteBasis.TryRefreshContactMode(null!, route),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("car"));
            Assert.That(
                () => RouteBasis.TryRefreshContactMode(new CarMemory(), null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("route"));
        }

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

        private static CarMemory BuildContactModeCar(byte contactMode, int extension)
        {
            CarMemory car = new();
            car.WriteByte(0x8D, 1);
            car.WriteByte(0x8C, contactMode);
            car[0x14] = 1;
            car[0x18] = extension;
            car[0x28] = 613;
            car[0x9C] = 32;
            car[0xA8] = 42;
            car[0xE8] = -42;
            car[0x14C] = 0x12345678;
            car[0x15C] = 16;
            car[0x184] = 8;
            car[0x188] = 4;

            return car;
        }
    }
}