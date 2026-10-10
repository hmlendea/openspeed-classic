using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class PhysicsAnglesTests
    {
        [TestCase(1, 1, 0x80, 0x2000)]
        [TestCase(1, -1, 0x180, 0x6000)]
        [TestCase(-1, 1, -0x80, -0x2000)]
        [TestCase(-1, -1, -0x180, -0x6000)]
        [TestCase(0, 0, 0x80, 0x2000)]
        [TestCase(0, 1, 0, 0)]
        [TestCase(1, 0, 0x100, 0x4000)]
        [TestCase(-1, 0, -0x100, -0x4000)]
        public void GivenAngleFixtures_WhenLookingUp_ThenBothRepresentationsAreExact(
            int first,
            int second,
            int ratioExpected,
            int interpolatedExpected)
        {
            Assert.Multiple(() =>
            {
                Assert.That(PhysicsAngles.RatioAngle(first, second), Is.EqualTo(ratioExpected));
                Assert.That(PhysicsAngles.InterpolatedAngle(first, second), Is.EqualTo(interpolatedExpected));
            });
        }

        [TestCase(0, 0x10000, 0)]
        [TestCase(0x4000, 0, 0x10000)]
        [TestCase(0x8000, -0x10000, 0)]
        [TestCase(0xC000, 0, -0x10000)]
        public void GivenAxisAngles_WhenCalculatingRotation_ThenThePairIsExact(int angle, int cosine, int sine)
        {
            FixedAnglePair result = PhysicsAngles.Rotation(angle);

            Assert.Multiple(() =>
            {
                Assert.That(result.Cosine, Is.EqualTo(cosine));
                Assert.That(result.Sine, Is.EqualTo(sine));
            });
        }
    }
}