using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class CollisionFallbackTests
    {
        [TestCase(0, 1, 1)]
        [TestCase(4, -1, 1)]
        [TestCase(8, 1, -1)]
        [TestCase(12, -1, -1)]
        [TestCase(0x400, 1, 1)]
        public void GivenTickBits_WhenApplyingFallback_ThenTheLowByteControlsAxisSigns(
            int tick,
            int firstSign,
            int thirdSign)
        {
            CarMemory car = new();
            car[0xBC] = 0x2C0000;
            FixedMatrices.Write(car, 0x188, FixedMatrices.Identity());
            car.WriteWord(0x14C, ushort.MaxValue);
            car.WriteWord(0x14E, 613);
            int notifications = 0;

            CollisionFallback.Apply(car, 0, tick, body =>
            {
                Assert.That(body, Is.SameAs(car));
                Assert.That(body.ReadWord(0x14C), Is.Zero);
                notifications += 1;
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0xE8], Is.EqualTo(firstSign * X86Math.One));
                Assert.That(car[0xEC], Is.Zero);
                Assert.That(car[0xF0], Is.EqualTo(thirdSign * X86Math.One));
                Assert.That(car[0xAC], Is.EqualTo(X86Math.One));
                Assert.That(car[0xA8], Is.Zero);
                Assert.That(car[0xB0], Is.Zero);
                Assert.That(car.ReadWord(0x14E), Is.EqualTo(613));
                Assert.That(notifications, Is.EqualTo(1));
            }

        }

        [TestCase(0, 0, 0x10000)]
        [TestCase(1, 0x40, 0x10000)]
        [TestCase(1, 0, 0x26666)]
        public void GivenModeAndRole_WhenApplyingFallback_ThenTheNativeTableAndFinalClampAreUsed(
            int mode,
            int role,
            int expectedAngular)
        {
            CarMemory car = new();
            car[0xBC] = 0x2C0000;
            car[0x1F4] = role;
            FixedMatrices.Write(car, 0x188, FixedMatrices.Identity());

            CollisionFallback.Apply(car, mode, 0, body => Assert.That(body[0xE8], Is.GreaterThan(0)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0xE8], Is.EqualTo(expectedAngular));
                Assert.That(car.ReadWord(0x14C), Is.EqualTo(1));
            }

        }

        [Test]
        public void GivenANotificationChangingAngularMotion_WhenApplyingFallback_ThenTheClampRunsAfterward()
        {
            CarMemory car = new();

            CollisionFallback.Apply(car, 0, 0, body =>
            {
                body[0xE8] = int.MinValue;
                body[0xEC] = int.MaxValue;
                body[0xF0] = -42;
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0xE8], Is.EqualTo(-0x26666));
                Assert.That(car[0xEC], Is.EqualTo(0x26666));
                Assert.That(car[0xF0], Is.EqualTo(-42));
            }

        }
    }
}