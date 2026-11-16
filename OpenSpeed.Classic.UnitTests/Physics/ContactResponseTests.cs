using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class ContactResponseTests
    {
        [TestCase(0, 0, 0)]
        [TestCase(0x1000, 0x10000, 0x10168)]
        [TestCase(0x4000, 0x10000, 0x10BE7)]
        [TestCase(0x8000, 0x10000, 0x124BC)]
        [TestCase(0xC000, 0x10000, 0x117CE)]
        [TestCase(-0x1000, 0x10000, 0xFE95)]
        public void GivenTheResponseFixtures_WhenCalculating_ThenTheSignedResultIsExact(int first, int second, int expected)
        {
            Assert.Multiple(() =>
            {
                Assert.That(ContactResponse.Calculate(first, second), Is.EqualTo(expected));
                Assert.That(ContactResponse.Calculate(second, first), Is.EqualTo(expected));
            });
        }
    }
}