using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class ContactGeometryTests
    {
        [TestCase(0x1998, unchecked((int)0x83000000))]
        [TestCase(0x1999, unchecked((int)0x83000000))]
        [TestCase(0x199A, 0x80000)]
        public void GivenTheHeightProjectionBoundary_WhenCalculating_ThenTheNormalIsCopiedAndTheGateIsStrict(
            int normalSecond,
            int expected)
        {
            CarMemory car = new();
            car[0x124] = 42;
            car[0x128] = normalSecond;
            car[0x12C] = -64;
            car[0x140] = 0x80000;
            byte[] original = car.Snapshot();
            FixedVector output = new();
            int height = ContactGeometry.CalculateContactHeight(car, output);

            Assert.Multiple(() =>
            {
                Assert.That(height, Is.EqualTo(expected));
                Assert.That(output.First, Is.EqualTo(42));
                Assert.That(output.Second, Is.EqualTo(normalSecond));
                Assert.That(output.Third, Is.EqualTo(-64));
                Assert.That(car.Snapshot(), Is.EqualTo(original));
            });
        }

        [TestCase(0x10000)]
        [TestCase(-0x10000)]
        public void GivenASlopedPlane_WhenCalculating_ThenHorizontalOffsetsDetermineHeight(int normalSecond)
        {
            CarMemory car = new();
            car[0x9C] = 0x60000;
            car[0xA0] = int.MaxValue;
            car[0xA4] = 0x80000;
            car[0x124] = 0x8000;
            car[0x128] = normalSecond;
            car[0x12C] = -0x4000;
            car[0x13C] = 0x20000;
            car[0x140] = 0x80000;
            car[0x144] = 0x40000;
            byte[] original = car.Snapshot();

            Assert.Multiple(() =>
            {
                Assert.That(ContactGeometry.CalculatePlaneHeight(car), Is.EqualTo(0x70000));
                Assert.That(car.Snapshot(), Is.EqualTo(original));
            });
        }

        [TestCase(0, -6553)]
        [TestCase(9, -6553)]
        [TestCase(10, -6553)]
        [TestCase(11, -5957)]
        [TestCase(-11, -5957)]
        [TestCase(int.MinValue, -6553)]
        public void GivenASmallNormalDivisor_WhenCalculating_ThenTheOriginalMinimumIsApplied(int normalSecond, int expected)
        {
            CarMemory car = new();
            car[0x9C] = X86Math.One;
            car[0x124] = 1;
            car[0x128] = normalSecond;

            Assert.That(ContactGeometry.CalculatePlaneHeight(car), Is.EqualTo(expected));
        }

        [Test]
        public void GivenHeightOverflow_WhenCalculating_ThenTheResultWraps()
        {
            CarMemory car = new();
            car[0x124] = -X86Math.One;
            car[0x128] = X86Math.One;
            car[0x9C] = 1;
            car[0x140] = int.MaxValue;

            Assert.That(ContactGeometry.CalculatePlaneHeight(car), Is.EqualTo(int.MinValue));
        }

        [TestCase(-1, -0x60000, -0x60000)]
        [TestCase(0, -0x20000, 0)]
        [TestCase(1, 0x20000, 0x20000)]
        public void GivenAnOrientedBody_WhenCalculatingClearance_ThenBothCorrectionFormsAreExact(
            int normalSign,
            int expectedRelative,
            int expectedSupport)
        {
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0xA0] = 0x80000;
            car[0x108] = 0x10000;
            car[0x10C] = 0x20000;
            car[0x110] = 0x40000;
            FixedVector normal = new() { Second = normalSign * X86Math.One };
            FixedVector reference = new() { Second = 0x40000 };
            byte[] original = car.Snapshot();

            Assert.Multiple(() =>
            {
                Assert.That(ContactGeometry.CalculateRelativeHeight(car, normal, reference), Is.EqualTo(expectedRelative));
                Assert.That(ContactGeometry.CalculateSupportCorrection(car, normal, reference), Is.EqualTo(expectedSupport));
                Assert.That(car.Snapshot(), Is.EqualTo(original));
            });
        }

        [Test]
        public void GivenAnObliqueNormal_WhenCalculatingSupport_ThenEveryBasisRowContributes()
        {
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0x108] = X86Math.One;
            car[0x10C] = 2 * X86Math.One;
            car[0x110] = 4 * X86Math.One;
            FixedVector normal = new() { First = X86Math.One, Second = -X86Math.One, Third = X86Math.One };

            Assert.That(ContactGeometry.CalculateSupportCorrection(car, normal, new FixedVector()), Is.EqualTo(-7 * X86Math.One));
        }

        [Test]
        public void GivenNullInputs_WhenCalculating_ThenArgumentErrorsAreReported()
        {
            CarMemory car = new();
            FixedVector vector = new();

            Assert.Multiple(() =>
            {
                Assert.That(() => ContactGeometry.CalculateContactHeight(null!, vector), Throws.ArgumentNullException);
                Assert.That(() => ContactGeometry.CalculateContactHeight(car, null!), Throws.ArgumentNullException);
                Assert.That(() => ContactGeometry.CalculatePlaneHeight(null!), Throws.ArgumentNullException);
                Assert.That(() => ContactGeometry.CalculateRelativeHeight(null!, vector, vector), Throws.ArgumentNullException);
                Assert.That(() => ContactGeometry.CalculateRelativeHeight(car, null!, vector), Throws.ArgumentNullException);
                Assert.That(() => ContactGeometry.CalculateRelativeHeight(car, vector, null!), Throws.ArgumentNullException);
                Assert.That(() => ContactGeometry.CalculateSupportCorrection(null!, vector, vector), Throws.ArgumentNullException);
                Assert.That(() => ContactGeometry.CalculateSupportCorrection(car, null!, vector), Throws.ArgumentNullException);
                Assert.That(() => ContactGeometry.CalculateSupportCorrection(car, vector, null!), Throws.ArgumentNullException);
            });
        }
    }
}