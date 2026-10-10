using NUnit.Framework;

using OpenSpeed.Classic.Rendering.Cars;

namespace OpenSpeed.Classic.UnitTests.Rendering.Cars
{
    [TestFixture]
    public sealed class FixedPointCarSolverTests
    {
        [TestCase(0x1999, 0, 0, 0, 0)]
        [TestCase(0x199A, 0, 123, 123, 123)]
        public void HeightBranchUsesTheStrictThreshold(
            int height,
            int expectedVelocityProjection,
            int expectedPersistentForce,
            int expectedSecondaryForce,
            int expectedResponseState)
        {
            FixedPointCarState car = new()
            {
                Height = height
            };
            car[0x2B4] = 123;
            car[0x310] = 123;
            car[0x324] = 123;
            car[0x328] = 123;
            car[0x2F4] = 123;

            new FixedPointCarSolver().Update(car);

            Assert.Multiple(() =>
            {
                Assert.That(car[0x310], Is.EqualTo(expectedVelocityProjection));
                Assert.That(car[0x324], Is.EqualTo(expectedPersistentForce));
                Assert.That(car[0x328], Is.EqualTo(expectedSecondaryForce));
                Assert.That(car[0x2F4], Is.EqualTo(expectedResponseState));
            });
        }

        [Test]
        public void LowHeightDampingUsesTheHandbrakeGateAndQ16Constants()
        {
            FixedPointCarState car = new()
            {
                Height = 0x1000,
                HandbrakeGate = 0x7FFF
            };
            car[0xA8] = 0x10000;
            car[0xAC] = -0x10000;
            car[0xB0] = 0x20000;
            car[0xEC] = 0x10000;

            new FixedPointCarSolver().Update(car);

            Assert.Multiple(() =>
            {
                Assert.That(car[0xA8], Is.EqualTo(0xF0A3));
                Assert.That(car[0xAC], Is.EqualTo(-0xF0A3));
                Assert.That(car[0xB0], Is.EqualTo(0x1E146));
                Assert.That(car[0xEC], Is.EqualTo(0xFAE1));
            });
        }

        [Test]
        public void BasisProjectionPreservesSourceOrder()
        {
            FixedPointCarState car = new();
            car[0xA8] = 0x10000;
            car[0xAC] = 0x20000;
            car[0xB0] = 0x30000;
            car[0x188] = 0x10000;
            car[0x18C] = 0x20000;
            car[0x190] = 0x30000;

            FixedPointCarSolver.ProjectBasis(car, 0xA8, 0x2B0);

            Assert.That(car[0x2B0], Is.EqualTo(0xE0000));
        }
    }
}