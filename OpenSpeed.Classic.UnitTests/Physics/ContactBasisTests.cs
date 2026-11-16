using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class ContactBasisTests
    {
        [TestCase(0, 0xE667, true)]
        [TestCase(0, 0xE666, false)]
        [TestCase(4, 0xE667, false)]
        public void GivenRoleAndProjection_WhenConstructing_ThenOnlyTheStrictCopyGateSkipsNormalisation(
            int role,
            int projection,
            bool isCopied)
        {
            CarMemory car = new();
            car[0x1F4] = role;
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0xC4] = 0x8000;
            FixedVector normal = new() { Second = X86Math.One };

            ContactBasis.Update(car, normal, projection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0x198], Is.EqualTo(X86Math.One));
                Assert.That(car[0xC4], Is.EqualTo(0x8000));
            }


            if (isCopied)
            {
                Assert.That(car[0x188], Is.EqualTo(0x8000));

                return;
            }

            Assert.That(car[0x188], Is.GreaterThan(0x8000));
        }

        [TestCase(0x7FFF)]
        [TestCase(0x8000)]
        public void GivenANormalParallelToTheFirstRow_WhenConstructing_ThenTheProjectionSelectsTheSourceAxis(
            int projection)
        {
            CarMemory car = new();
            car[0x1F4] = 4;
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            FixedVector normal = new() { First = X86Math.One };

            ContactBasis.Update(car, normal, projection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0x194], Is.EqualTo(X86Math.One));
                Assert.That(car[0x198], Is.Zero);
            }


            if (projection < 0x8000)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(car[0x18C], Is.LessThan(0));
                    Assert.That(car[0x1A8], Is.GreaterThan(0));
                }


                return;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0x188], Is.Zero);
                Assert.That(car[0x18C], Is.Zero);
                Assert.That(car[0x1A8], Is.Zero);
            }

        }

        [Test]
        public void GivenAZeroNormal_WhenConstructing_ThenTheCrossRowsRemainZero()
        {
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());

            ContactBasis.Update(car, new FixedVector(), 0);

            for (int offset = 0x188; offset <= 0x1A8; offset += sizeof(int))
            {
                Assert.That(car[offset], Is.Zero);
            }
        }
    }
}