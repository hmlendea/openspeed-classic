using System;

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

        [Test]
        public void GivenTheRecoveredTable_WhenReadingEntries_ThenTheyMatchTheAssemblyLiterals()
        {
            int[] expected = [1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0];
            int[] table = PhysicsTables.SurfaceLaunchFlags.ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(table.Length, Is.EqualTo(PhysicsTables.SurfaceLaunchFlagCount));
                Assert.That(table.Length, Is.EqualTo(expected.Length));

                for (int index = 0; index < expected.Length; index += 1)
                {
                    Assert.That(table[index], Is.EqualTo(expected[index]), $"entry {index}");
                }
            });
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        public void GivenALaunchingSurfaceInTheRecoveredTable_WhenApplying_ThenTheImpulseIsWritten(int surface)
        {
            CarMemory car = new();
            car[0x184] = surface;
            car[0x128] = 0;
            car[0x15C] = 0;
            car[0x124] = 0x1000;
            car[0x12C] = 0x2000;

            Assert.That(SurfaceLaunch.TryApply(car), Is.True);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(15)]
        public void GivenANonLaunchingSurfaceInTheRecoveredTable_WhenApplying_ThenTheImpulseIsSkipped(int surface)
        {
            CarMemory car = new();
            car[0x184] = surface;
            car[0x128] = 0;
            car[0x15C] = 0;

            Assert.That(SurfaceLaunch.TryApply(car), Is.False);
        }

        [TestCase(16)]
        [TestCase(63)]
        public void GivenASurfaceBeyondTheRecoveredTable_WhenApplying_ThenTheImpulseIsRejected(int surface)
        {
            CarMemory car = new();
            car[0x184] = surface;
            car[0x128] = 0;
            car[0x15C] = 0;

            // The original table reads adjacent process storage beyond sixteen entries. The
            // managed port rejects those indices rather than reproducing the out-of-bounds read.
            Assert.That(SurfaceLaunch.TryApply(car), Is.False);
        }
    }
}