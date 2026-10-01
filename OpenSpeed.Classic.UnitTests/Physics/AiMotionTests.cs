using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class AiMotionTests
    {
        [TestCase(0x20000, 0x10000, 0, 0x13333)]
        [TestCase(0x20000, 0x30000, 0, 0x2CCCD)]
        [TestCase(0x30000, 0x10000, 0x18000, 0x1B333)]
        [TestCase(0x10000, 0x30000, 0x20000, 0x1CCCD)]
        [TestCase(0x20000, 0x20000, 0, 0x20000)]
        [TestCase(0x20000, 0x1FFFF, 0, 0x20000)]
        [TestCase(0x20000, 0x20001, 0, 0x20000)]
        [TestCase(0x20000, 0x10000, 0x20000, 0x13333)]
        [TestCase(0x20000, 0x10000, 0x10000, 0x13333)]
        [TestCase(int.MaxValue, int.MaxValue - 1, 0, unchecked(int.MaxValue - 1 + 0x3333))]
        [TestCase(int.MinValue, int.MinValue + 1, 0, unchecked(int.MinValue + 1 - 0x3333))]
        public void GivenLateralTargets_WhenSmoothing_ThenOnlyTheMotionTargetChanges(
            int selected,
            int motion,
            int current,
            int expectedMotion)
        {
            CarMemory car = new();
            car[0x394] = selected;
            car[0x548] = motion;
            car[0x3AC] = current;
            CarMemory expected = new();
            expected[0x394] = selected;
            expected[0x548] = expectedMotion;
            expected[0x3AC] = current;
            AiMotion.SmoothLateralTarget(car);

            Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
        }

        [TestCase(0x20000, 0x10000, 0x80000)]
        [TestCase(0x10000, 0x20000, -0x80000)]
        [TestCase(int.MaxValue, -1, 0)]
        public void GivenMotionAndCurrentTargets_WhenCalculatingLateralVelocity_ThenTheDifferenceWraps(
            int motion,
            int current,
            int expectedVelocity)
        {
            CarMemory car = new();
            car[0x548] = motion;
            car[0x3AC] = current;
            car[0x394] = 42;
            CarMemory expected = new();
            expected[0x548] = motion;
            expected[0x3AC] = current;
            expected[0x394] = 42;
            expected[0x3CC] = expectedVelocity;
            AiMotion.CalculateLateralVelocity(car);

            Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
        }

        [TestCase(0x20000)]
        [TestCase(-0x20000)]
        public void GivenBankedBasisRows_WhenComposingVelocity_ThenAllWorldComponentsAreCalculated(int speed)
        {
            CarMemory car = new();
            CarMemory expected = new();

            foreach (CarMemory state in new[] { car, expected })
            {
                state[0x3CC] = 0x10000;
                state[0x39C] = speed;
                state[0x3A0] = 0x24000;
                state[0x118] = 0x8000;
                state[0x11C] = -0x4000;
                state[0x120] = 0x10000;
                state[0xDC] = 0x4000;
                state[0xE0] = 0x8000;
                state[0xE4] = -0x10000;
            }

            car[0xE8] = car[0xEC] = car[0xF0] = 42;
            expected[0xA8] = 0x10000;
            expected[0xAC] = 0xC000;
            expected[0xB0] = -0x10000;
            expected[0x2B8] = 0x24000;
            AiMotion.ComposeWorldVelocity(car);

            Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
        }

        [Test]
        public void GivenNullState_WhenUpdating_ThenArgumentErrorsAreReported()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => AiMotion.SmoothLateralTarget(null!), Throws.ArgumentNullException);
                Assert.That(() => AiMotion.CalculateLateralVelocity(null!), Throws.ArgumentNullException);
                Assert.That(() => AiMotion.ComposeWorldVelocity(null!), Throws.ArgumentNullException);
            });
        }

        [Test]
        public void GivenAnAiForceState_WhenApplyingTheProportionalController_ThenForceStagingMatchesTheSourceOrder()
        {
            CarMemory car = new();
            car[0x39C] = 0x10000;
            car[0x3A0] = 0x40000;
            car[0x510] = -0x10000;
            car[0x2B0] = 0x10000;
            car[0x2C0] = 0x20000;
            car[0x550] = 0x8000;
            car[0x554] = 0x10000;

            AiForceController.ApplyProportional(
                car,
                X86Math.One,
                0x20000,
                0x40000,
                X86Math.One,
                X86Math.One,
                X86Math.One,
                X86Math.One,
                0x10000,
                X86Math.One,
                new FixedVector { First = 1, Second = 2, Third = 3 });

            Assert.Multiple(() =>
            {
                Assert.That(car[0x324], Is.EqualTo(0x10000));
                Assert.That(car[0x328], Is.Zero);
                Assert.That(car[0x2A8], Is.Zero);
                Assert.That(car[0x2A4], Is.EqualTo(0x40001));
                Assert.That(car[0x2AC], Is.EqualTo(0x8000 + 2));
                Assert.That(car[0x29C], Is.EqualTo(3));
                Assert.That(car[0x2C8], Is.Zero);
                Assert.That(car[0x2CC], Is.Zero);
                Assert.That(car[0x2D0], Is.Zero);
            });
        }

        [TestCase(0, 0x800, 0x100)]
        [TestCase(0x1000, 0, 0xE00)]
        [TestCase(0x1000, 0x1000, 0x1000)]
        [TestCase(0, 7, 0)]
        public void GivenAnAiEngineValue_WhenApproachingDesired_ThenTheNativeEighthStepIsUsed(
            int current,
            int desired,
            int expected)
        {
            CarMemory car = new();
            car[0x2F0] = current;

            AiEngineController.ApproachDesired(car, desired);

            Assert.That(car[0x2F0], Is.EqualTo(expected));
        }

        [TestCase(false, 0xC0000)]
        [TestCase(true, 0xF0000)]
        public void GivenAnAiMode_WhenSelectingTargetScalar_ThenTheNativeBaseIsUsed(bool alternateMode, int expected)
        {
            CarMemory car = new();

            Assert.That(AiEngineController.SelectTargetScalar(car, alternateMode), Is.EqualTo(expected));
        }

        [Test]
        public void GivenAnAiTargetScale_WhenSelectingTargetScalar_ThenQ16ScalingIsApplied()
        {
            CarMemory car = new();
            car[0x558] = 0x20000;

            Assert.That(AiEngineController.SelectTargetScalar(car, false), Is.EqualTo(0x180000));
        }
    }
}