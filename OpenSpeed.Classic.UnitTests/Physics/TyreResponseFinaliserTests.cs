using System;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class TyreResponseFinaliserTests
    {
        [TestCase(0, 0, 0, 0x10000)]
        [TestCase(1, 0, 0, 0x8000)]
        [TestCase(-1, 0, 0, 0x8000)]
        [TestCase(2, 0, 0, 0x8000)]
        [TestCase(-2, 0, 0, 0x8000)]
        [TestCase(3, 0x8000, 0x8000, 0x18000)]
        [TestCase(-3, -0x8000, 0x8000, 0x18000)]
        [TestCase(3, 0x10000, 0x10000, 0x30000)]
        [TestCase(3, 0x20000, 0x10000, 0x30000)]
        [TestCase(4, 0, 0, 0x10000)]
        [TestCase(int.MinValue, 0, 0, 0x10000)]
        public void GivenResponseStates_WhenScaling_ThenTheSpecifiedFactorIsAppliedWithoutMutations(
            int state,
            int contactForce,
            int accelerator,
            int expectedForce)
        {
            CarMemory car = new();
            car[0x2F4] = state;
            car[0x30C] = contactForce;
            PhysicsContext context = new() { Accelerator = accelerator };
            byte[] original = car.Snapshot();

            Assert.Multiple(() =>
            {
                Assert.That(TyreResponseFinaliser.ScaleRequestedForce(car, context, 0x10000), Is.EqualTo(expectedForce));
                Assert.That(TyreResponseFinaliser.ScaleRequestedForce(car, context, -0x10000), Is.EqualTo(-expectedForce));
                Assert.That(car.Snapshot(), Is.EqualTo(original));
                Assert.That(context.Accelerator, Is.EqualTo(accelerator));
            });
        }

        [TestCase(0, 0xF333)]
        [TestCase(1, 0xE666)]
        public void GivenSurfaceModes_WhenScaling_ThenTheSelectedTableRowIsUsed(int mode, int expected)
        {
            CarMemory car = new();
            car[0x200] = 4;

            Assert.That(TyreResponseFinaliser.ScaleRequestedForce(car, new PhysicsContext { Mode = mode }, 0x10000), Is.EqualTo(expected));
        }

        [TestCase(1, -64, false, 0)]
        [TestCase(-1, 64, false, 0)]
        [TestCase(0, -32, false, 0)]
        [TestCase(0, 32, false, 1)]
        [TestCase(1, -64, true, -1)]
        [TestCase(-1, 64, true, 1)]
        [TestCase(0, -32, true, -1)]
        [TestCase(254, 32, false, 255)]
        [TestCase(255, 32, false, 256)]
        [TestCase(256, 32, false, 256)]
        [TestCase(-254, -32, false, -255)]
        [TestCase(-255, -32, false, -256)]
        [TestCase(-256, -32, false, -256)]
        [TestCase(257, 0, false, 257)]
        [TestCase(42, -31, false, 42)]
        [TestCase(42, -32, false, 41)]
        public void GivenWheelTargetBoundaries_WhenFinalising_ThenSignAndDirectionalClampsArePreserved(
            int wheelTarget,
            int adjustment,
            bool isTransition,
            int expectedTarget)
        {
            CarMemory car = new();
            CarMemory expected = new();
            car.WriteByte(0x2DA, 2);
            expected.WriteByte(0x2DA, 2);
            expected[0x304] = expectedTarget << 16;
            CarSpecifications descriptor = new();
            descriptor[0xF0] = 256;
            descriptor[0x190] = 0x10000;
            byte[] originalDescriptor = descriptor.Snapshot();
            TyreResponseFinaliser.FinaliseGrip(car, descriptor, wheelTarget, adjustment, isTransition);

            Assert.Multiple(() =>
            {
                Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
                Assert.That(descriptor.Snapshot(), Is.EqualTo(originalDescriptor));
            });
        }

        [Test]
        public void GivenNeutralGear_WhenFinalising_ThenThePreviousGearRatioIsUsed()
        {
            CarMemory car = new();
            car.WriteByte(0x2DA, 1);
            car.WriteByte(0x2D9, 3);
            CarSpecifications descriptor = new();
            descriptor[0xF0] = 256;
            descriptor[0x194] = 0x8000;
            TyreResponseFinaliser.FinaliseGrip(car, descriptor, 32, 0, false);

            Assert.That(car[0x304], Is.EqualTo(16 << 16));
        }

        [Test]
        public void GivenZeroEngineLimit_WhenFinalising_ThenDivisionFaultsWithoutWritingGrip()
        {
            CarMemory car = new();
            car[0x304] = 42;

            Assert.Multiple(() =>
            {
                Assert.That(() => TyreResponseFinaliser.FinaliseGrip(car, new CarSpecifications(), 0, 0, false), Throws.TypeOf<DivideByZeroException>());
                Assert.That(car[0x304], Is.EqualTo(42));
            });
        }

        [Test]
        public void GivenNullInputs_WhenFinalising_ThenArgumentErrorsAreReported()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => TyreResponseFinaliser.ScaleRequestedForce(null!, new PhysicsContext(), 0), Throws.ArgumentNullException);
                Assert.That(() => TyreResponseFinaliser.ScaleRequestedForce(new CarMemory(), null!, 0), Throws.ArgumentNullException);
                Assert.That(() => TyreResponseFinaliser.FinaliseGrip(null!, new CarSpecifications(), 0, 0, false), Throws.ArgumentNullException);
                Assert.That(() => TyreResponseFinaliser.FinaliseGrip(new CarMemory(), null!, 0, 0, false), Throws.ArgumentNullException);
            });
        }
    }
}