using System;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class X86MathTests
    {
        [TestCase(0x10000, 0x10000, 0x10000)]
        [TestCase(0x8000, 0x8000, 0x4000)]
        [TestCase(-0x10000, 0x8000, -0x8000)]
        [TestCase(0x8000, 0x10001, 0x8001)]
        [TestCase(int.MaxValue, int.MaxValue, -0x10000)]
        [TestCase(int.MinValue, 0x10000, int.MinValue)]
        public void GivenTheMultiplyFixtures_WhenMultiplying_ThenTheDwordIsExact(int left, int right, int expected)
            => Assert.That(X86Math.MultiplyQ16(left, right), Is.EqualTo(expected));

        [TestCase(33, 5, 1)]
        [TestCase(-33, 5, -1)]
        [TestCase(-31, 5, 0)]
        [TestCase(int.MinValue, 5, -0x4000000)]
        [TestCase(0x10001, 6, 0x400)]
        [TestCase(-0x10001, 6, -0x400)]
        public void GivenTheShiftFixtures_WhenTruncating_ThenTheDwordIsExact(int value, int shift, int expected)
            => Assert.That(X86Math.TruncatePowerOfTwo(value, shift), Is.EqualTo(expected));

        [Test]
        public void GivenDwordOverflow_WhenCalculating_ThenTheResultWraps()
        {
            Assert.Multiple(() =>
            {
                Assert.That(X86Math.Negate(int.MinValue), Is.EqualTo(int.MinValue));
                Assert.That(X86Math.Abs(int.MinValue), Is.EqualTo(int.MinValue));
                Assert.That(X86Math.Add(int.MaxValue, 0x400), Is.EqualTo(unchecked((int)0x800003FF)));
                Assert.That(X86Math.Subtract(int.MinValue, 1), Is.EqualTo(int.MaxValue));
                Assert.That(X86Math.Multiply(int.MaxValue, 2), Is.EqualTo(-2));
            });
        }

        [TestCase(33, 32, 1)]
        [TestCase(-33, 32, -1)]
        [TestCase(33, -32, -1)]
        public void GivenSignedOperands_WhenDividing_ThenTheQuotientTruncates(int numerator, int denominator, int expected)
            => Assert.That(X86Math.Divide(numerator, denominator), Is.EqualTo(expected));

        [Test]
        public void GivenFaultingDivision_WhenDividing_ThenTheOriginalFaultIsPreserved()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => X86Math.Divide(1, 0), Throws.TypeOf<DivideByZeroException>());
                Assert.That(() => X86Math.Divide(int.MinValue, -1), Throws.TypeOf<OverflowException>());
                Assert.That(() => X86Math.DivideUnsigned(1, 0), Throws.TypeOf<DivideByZeroException>());
                Assert.That(() => X86Math.DivideUnsigned(1UL << 32, 1), Throws.TypeOf<OverflowException>());
                Assert.That(X86Math.DivideUnsigned(uint.MaxValue, 1), Is.EqualTo(uint.MaxValue));
            });
        }
    }
}