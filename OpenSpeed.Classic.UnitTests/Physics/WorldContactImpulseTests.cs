using System;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class WorldContactImpulseTests
    {
        [Test]
        public void GivenPendingCollisionEvent_WhenConsuming_ThenHeadersClearAndTimerDecrements()
        {
            CarMemory car = new();
            car[0x160] = 1;
            car[0x164] = 2;
            car[0x168] = 3;
            car[0x578] = 4;

            PendingCollisionEventConsumer.Consume(car);

            Assert.Multiple(() =>
            {
                Assert.That(car[0x160], Is.Zero);
                Assert.That(car[0x164], Is.Zero);
                Assert.That(car[0x168], Is.Zero);
                Assert.That(car[0x578], Is.EqualTo(3));
            });
        }

        [Test]
        public void GivenNoRecoveryTimer_WhenConsuming_ThenTheTimerRemainsZero()
        {
            CarMemory car = new();

            PendingCollisionEventConsumer.Consume(car);

            Assert.That(car[0x578], Is.Zero);
        }

        [Test]
        public void GivenAZeroNormal_WhenApplyingWorldResponse_ThenTheUpwardNormalIsUsed()
        {
            CarMemory car = new();
            car[0xB8] = X86Math.One;
            car[0xA8] = -X86Math.One;

            WorldContactResponse.Apply(car, new FixedVector(), new FixedVector(), new FixedVector());

            Assert.That(car[0x168], Is.EqualTo(0x30000));
        }

        [Test]
        public void GivenAngularState_WhenApplyingWorldResponse_ThenNativeScaleSequenceIsPreserved()
        {
            CarMemory car = new();
            car[0xB8] = X86Math.One;
            car[0xF8] = X86Math.One;
            car[0xE8] = X86Math.One;

            WorldContactResponse.Apply(car, new FixedVector(), new FixedVector { First = X86Math.One }, new FixedVector());

            int expected = X86Math.MultiplyQ16(X86Math.MultiplyQ16(X86Math.One, 0x6487E), 0x28BE);
            Assert.That(car[0xE8], Is.EqualTo(expected));
        }

        [Test]
        public void GivenAnInwardCentralImpact_WhenApplying_ThenImpulseAndEventBytesAreExact()
        {
            CarMemory car = new();
            car[0xB8] = X86Math.One;
            car[0xA8] = -X86Math.One;
            car[0x184] = 2;
            bool applied = WorldContactImpulse.Apply(car, new FixedVector(),
                new FixedVector { First = X86Math.One }, new FixedVector { First = -X86Math.One });
            CarMemory expected = new();
            expected[0xB8] = X86Math.One;
            expected[0xA8] = -0x3334;
            expected[0x184] = 2;
            expected[0x160] = 0x60000;
            expected[0x168] = 0x30002;

            Assert.Multiple(() =>
            {
                Assert.That(applied);
                Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
            });
        }

        [Test]
        public void GivenASeparatingImpact_WhenApplying_ThenMotionIsUnchangedButTheSupportPointIsRecorded()
        {
            CarMemory car = new();
            car[0xB8] = X86Math.One;
            car[0xA8] = X86Math.One;
            car[0x9C] = 42;
            bool applied = WorldContactImpulse.Apply(car, new FixedVector { First = 42 },
                new FixedVector { First = X86Math.One }, new FixedVector { First = X86Math.One });

            Assert.Multiple(() =>
            {
                Assert.That(applied, Is.False);
                Assert.That(car[0xA8], Is.EqualTo(X86Math.One));
                Assert.That(car[0x170], Is.EqualTo(42));
                Assert.That(car[0x160], Is.EqualTo(0x60000));
            });
        }

        [Test]
        public void GivenACoupledLever_WhenCalculatingCapacity_ThenInPlaceCrossProductWritesArePreserved()
        {
            CarMemory car = new();
            car[0xF8] = 0x8000;
            FixedVector candidate = WorldContactImpulse.CalculateCapacityCandidate(car,
                new FixedVector { First = X86Math.One, Second = 2 * X86Math.One, Third = 3 * X86Math.One },
                new FixedVector { First = 4 * X86Math.One, Second = 5 * X86Math.One, Third = 6 * X86Math.One }, X86Math.One);

            Assert.Multiple(() =>
            {
                Assert.That(candidate.First, Is.EqualTo(-24 * X86Math.One));
                Assert.That(candidate.Second, Is.EqualTo(75 * X86Math.One));
                Assert.That(candidate.Third, Is.EqualTo(-123 * X86Math.One));
            });
        }

        [TestCase(0, 0)]
        [TestCase(0x10000, 0x10000)]
        [TestCase(-0x10000, 0x10000)]
        [TestCase(0x2000000, 0)]
        [TestCase(int.MinValue, 0)]
        public void GivenMagnitudeInputs_WhenSquaring_ThenRestoredScaleWraps(int value, int expected)
            => Assert.That(WorldContactImpulse.SquaredMagnitude(new FixedVector { First = value }), Is.EqualTo(expected));

        [Test]
        public void GivenAZeroDenominator_WhenApplying_ThenTheFaultPrecedesEventWrites()
        {
            CarMemory car = new();
            car[0x160] = 42;
            byte[] original = car.Snapshot();

            Assert.Multiple(() =>
            {
                Assert.That(() => WorldContactImpulse.Apply(car, new FixedVector(), new FixedVector(), new FixedVector()), Throws.TypeOf<DivideByZeroException>());
                Assert.That(car.Snapshot(), Is.EqualTo(original));
            });
        }

        [Test]
        public void GivenTangentialContactVelocity_WhenApplying_ThenFrictionReducesSliding()
        {
            CarMemory car = new();
            car[0xB8] = X86Math.One;
            car[0xA8] = -X86Math.One;
            car[0xB0] = X86Math.One;
            WorldContactImpulse.Apply(car, new FixedVector(), new FixedVector { First = X86Math.One },
                new FixedVector { First = -X86Math.One, Third = X86Math.One });

            Assert.Multiple(() =>
            {
                Assert.That(car[0xA8], Is.EqualTo(-0x3334));
                Assert.That(car[0xB0], Is.InRange(0, X86Math.One - 1));
                Assert.That(car[0xAC], Is.Zero);
            });
        }

        [TestCase(0xB332, 6)]
        [TestCase(0xB333, 4)]
        [TestCase(0xB334, 4)]
        public void GivenTheNormalThreshold_WhenRecordingSeverity_ThenTheMultiplierChangesAtEquality(int vertical, int multiplier)
        {
            CarMemory car = new();
            car[0xB8] = X86Math.One;
            car[0xA8] = -X86Math.One;
            WorldContactImpulse.Apply(car, new FixedVector(),
                new FixedVector { First = X86Math.One, Second = vertical }, new FixedVector());

            Assert.That(car[0x160], Is.EqualTo(multiplier * X86Math.One));
        }

        [Test]
        public void GivenAnOffsetImpact_WhenApplying_ThenAngularVelocityResponds()
        {
            CarMemory car = new();
            car[0xB8] = X86Math.One;
            car[0xF8] = 0x8000;
            car[0xA8] = -X86Math.One;
            WorldContactImpulse.Apply(car, new FixedVector { Third = X86Math.One },
                new FixedVector { First = X86Math.One }, new FixedVector());

            Assert.Multiple(() =>
            {
                Assert.That(car[0xEC], Is.GreaterThan(0));
                Assert.That(car[0xE8], Is.Zero);
                Assert.That(car[0xF0], Is.Zero);
                Assert.That(car[0x178], Is.EqualTo(X86Math.One));
            });
        }
    }
}