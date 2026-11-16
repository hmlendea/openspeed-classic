using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class CollisionMotionRefreshTests
    {
        [TestCase(0, 1024)]
        [TestCase(2, 1024)]
        [TestCase(1, 613)]
        public void GivenAGear_WhenRefreshingAlternate_ThenOnlyDrivenGearsUpdateEngineSpeed(
            int gear,
            int expectedEngineSpeed)
        {
            CarMemory car = BuildCar();
            car.WriteByte(0x2DA, (byte)gear);
            car[0x2F0] = 613;
            CarSpecifications descriptor = new();
            descriptor[0x168 + gear * sizeof(int)] = X86Math.One;
            descriptor[0xF0] = 1024;

            CollisionMotionRefresh.RefreshAlternate(car, descriptor);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0x2B8], Is.EqualTo(X86Math.One));
                Assert.That(car[0x2F0], Is.EqualTo(expectedEngineSpeed));
                Assert.That(car[0x310], Is.Zero);
                Assert.That(car[0x324], Is.Zero);
                Assert.That(car[0x328], Is.Zero);
                Assert.That(car[0x2F4], Is.Zero);
            }

        }

        [TestCase(0x1999, 1, 0, 0)]
        [TestCase(0x199A, 1, -3, 0x666)]
        [TestCase(-0x199A, 1, 3, -0x666)]
        [TestCase(0x199A, 0, 0, 0)]
        public void GivenLateralMotion_WhenRefreshingPrimary_ThenModeAndRatioThresholdsSelectResponse(
            int lateral,
            int mode,
            int expectedResponse,
            int expectedTransfer)
        {
            CarMemory car = BuildCar();
            car[0xA8] = lateral;

            CollisionMotionRefresh.RefreshPrimary(car, mode);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0x2B0], Is.EqualTo(lateral));
                Assert.That(car[0x2B8], Is.EqualTo(X86Math.One));
                Assert.That(car[0x2F4], Is.EqualTo(expectedResponse));
                Assert.That(car[0x310], Is.EqualTo(expectedTransfer));
                Assert.That(car[0x2F0], Is.EqualTo(1024));
            }

            int expectedAccumulator = 0;

            if (expectedResponse != 0)
            {
                expectedAccumulator = 0x20000;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0x324], Is.EqualTo(expectedAccumulator));
                Assert.That(car[0x328], Is.EqualTo(expectedAccumulator));
            }

        }

        [TestCase(0, 4)]
        [TestCase(1, 0)]
        [TestCase(-1, 4)]
        public void GivenACorrection_WhenUpdating_ThenOnlyAcceptedPlayerCorrectionsRefreshMotion(
            int correction,
            int role)
        {
            CarMemory car = BuildCar();
            car[0x1F4] = role;
            car[0x284] = 613;

            CollisionMotionRefresh.Update(car, new CarSpecifications(), 0, body => correction);

            if (correction == 0)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(car[0x284], Is.EqualTo(613));
                    Assert.That(car[0x324], Is.EqualTo(42));
                }


                return;
            }

            Assert.That(car[0x284], Is.EqualTo(correction));

            if (role == 0)
            {
                Assert.That(car[0x324], Is.EqualTo(42));

                return;
            }

            Assert.That(car[0x324], Is.Zero);
        }

        private static CarMemory BuildCar()
        {
            CarMemory car = new();
            FixedMatrices.Write(car, 0x188, FixedMatrices.Identity());
            car[0xB0] = X86Math.One;
            car[0x2F0] = 1024;
            car[0x310] = 42;
            car[0x324] = 42;
            car[0x328] = 42;
            car[0x2F4] = 42;

            return car;
        }
    }
}