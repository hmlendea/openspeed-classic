using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class WorldContactSupportTests
    {
        [TestCase(0, 65536, 0, -65536, -131072, -196608)]
        [TestCase(0, -65536, 0, -65536, 131072, -196608)]
        [TestCase(-65536, 0, 65536, 65536, -131072, -196608)]
        [TestCase(65536, -65536, -65536, -65536, 131072, 196608)]
        public void GivenAnOrientedBox_WhenSelectingSupport_ThenEachProjectedExtentSelectsItsNativeSign(
            int normalFirst,
            int normalSecond,
            int normalThird,
            int expectedFirst,
            int expectedSecond,
            int expectedThird)
        {
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0x108] = X86Math.One;
            car[0x10C] = X86Math.One * 2;
            car[0x110] = X86Math.One * 3;
            car[0x9C] = 42;
            car[0xA0] = 613;
            car[0xA4] = -32;
            byte[] before = car.Snapshot();
            FixedVector normal = new()
            {
                First = normalFirst,
                Second = normalSecond,
                Third = normalThird
            };

            FixedVector point = WorldContactSupport.CalculatePoint(car, normal);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(point.First, Is.EqualTo(expectedFirst + 42));
                Assert.That(point.Second, Is.EqualTo(expectedSecond + 613));
                Assert.That(point.Third, Is.EqualTo(expectedThird - 32));
                Assert.That(car.Snapshot(), Is.EqualTo(before));
                Assert.That(normal.First, Is.EqualTo(normalFirst));
                Assert.That(normal.Second, Is.EqualTo(normalSecond));
                Assert.That(normal.Third, Is.EqualTo(normalThird));
            }

        }

        [Test]
        public void GivenAFractionalRow_WhenSelectingSupport_ThenNegationFollowsRoundedMultiplication()
        {
            CarMemory car = new();
            car[0xC4] = 1;
            car[0x108] = 0x8000;

            FixedVector point = WorldContactSupport.CalculatePoint(
                car, new FixedVector { First = X86Math.One });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(point.First, Is.EqualTo(-1));
                Assert.That(point.Second, Is.Zero);
                Assert.That(point.Third, Is.Zero);
            }

        }

        [Test]
        public void GivenAWrappedPosition_WhenSelectingSupport_ThenTheCornerSumWraps()
        {
            CarMemory car = new();
            car[0xC4] = X86Math.One;
            car[0x108] = X86Math.One;
            car[0x9C] = int.MaxValue;

            FixedVector point = WorldContactSupport.CalculatePoint(
                car, new FixedVector { First = -X86Math.One });

            Assert.That(point.First, Is.EqualTo(unchecked(int.MaxValue + 65536)));
        }

        [Test]
        public void GivenMissingInputs_WhenSelectingSupport_ThenArgumentErrorsAreReported()
        {
            Assert.That(
                () => WorldContactSupport.CalculatePoint(null!, new FixedVector()),
                Throws.ArgumentNullException);
            Assert.That(
                () => WorldContactSupport.CalculatePoint(new CarMemory(), null!),
                Throws.ArgumentNullException);
        }
    }
}