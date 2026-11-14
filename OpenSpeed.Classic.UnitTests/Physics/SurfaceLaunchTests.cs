using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class SurfaceLaunchTests
    {
        [Test]
        public void GivenLaunchGates_WhenApplying_ThenTheNativeImpulseIsWritten()
        {
            CarMemory car = new();
            car[0x184] = 1;
            car[0x128] = 0x100;
            car[0x15C] = 0x7AE;
            car[0x124] = 0x1000;
            car[0x12C] = -0x2000;
            car[0xA8] = 10;
            car[0xAC] = 20;
            car[0xB0] = 30;

            Assert.That(SurfaceLaunch.TryApply(car, [0, 1]), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(car[0xA8], Is.EqualTo(0x100A));
                Assert.That(car[0xAC], Is.EqualTo(20 - 0x14CCC));
                Assert.That(car[0xB0], Is.EqualTo(30 - 0x2000));
            });
        }

        [TestCase(0x10, 1, 0x100, 0x7AE)]
        [TestCase(0, 0, 0x100, 0x7AE)]
        [TestCase(0, 1, 0xCCCC, 0x7AE)]
        [TestCase(0, 1, 0x100, 0x7AF)]
        public void GivenAnyLaunchGateFailure_WhenApplying_ThenMotionRemainsUnchanged(
            byte flags,
            int launchFlag,
            int normalProjection,
            int correction)
        {
            CarMemory car = new();
            car[0x1F4] = flags;
            car[0x184] = 1;
            car[0x128] = normalProjection;
            car[0x15C] = correction;
            car[0xA8] = 10;
            car[0xAC] = 20;
            car[0xB0] = 30;

            Assert.That(SurfaceLaunch.TryApply(car, [0, (byte)launchFlag]), Is.False);
            Assert.Multiple(() =>
            {
                Assert.That(car[0xA8], Is.EqualTo(10));
                Assert.That(car[0xAC], Is.EqualTo(20));
                Assert.That(car[0xB0], Is.EqualTo(30));
            });
        }
    }
}