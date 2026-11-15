using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class PhysicsHelpersTests
    {
        [Test]
        public void GivenAnInRangeIndex_WhenSamplingTorque_ThenTheCurveCellIsReturned()
        {
            CarSpecifications descriptor = new();
            descriptor[0x4C + 4 * sizeof(int)] = 0x12345;

            Assert.That(PhysicsHelpers.SampleTorqueCurve(descriptor, 4), Is.EqualTo(0x12345));
        }

        [Test]
        public void GivenAnOversizedIndex_WhenSamplingTorque_ThenTheUpperCellIsReturned()
        {
            CarSpecifications descriptor = new();
            descriptor[0x4C + 0x28 * sizeof(int)] = 0x9999;
            descriptor[0x4C + 0x29 * sizeof(int)] = 0x1111;

            Assert.That(PhysicsHelpers.SampleTorqueCurve(descriptor, 0x100), Is.EqualTo(0x9999));
        }

        [Test]
        public void GivenANegativeIndex_WhenSamplingTorque_ThenTheFirstCellIsReturned()
        {
            CarSpecifications descriptor = new();
            descriptor[0x4C] = 0x4321;

            Assert.That(PhysicsHelpers.SampleTorqueCurve(descriptor, -5), Is.EqualTo(0x4321));
        }

        [Test]
        public void GivenTheUpperBoundIndex_WhenSamplingTorque_ThenItIsAccepted()
        {
            CarSpecifications descriptor = new();
            descriptor[0x4C + 0x28 * sizeof(int)] = 0x77;

            Assert.That(PhysicsHelpers.SampleTorqueCurve(descriptor, 0x28), Is.EqualTo(0x77));
        }

        [Test]
        public void GivenRoutineUnrelatedRaceType_WhenResolvingContactScale_ThenTheDefaultIsReturned()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(PhysicsHelpers.RouteContactScale(6, 0x200), Is.EqualTo(X86Math.One));
                Assert.That(PhysicsHelpers.RouteContactScale(7, 0x144), Is.EqualTo(X86Math.One));
                Assert.That(PhysicsHelpers.RouteContactScale(7, 0x230), Is.EqualTo(X86Math.One));
            }

        }

        [Test]
        public void GivenRaceTypeSevenInsideTheWindow_WhenResolvingContactScale_ThenTheHighScaleIsReturned()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(PhysicsHelpers.RouteContactScale(7, 0x145), Is.EqualTo(0x30000));
                Assert.That(PhysicsHelpers.RouteContactScale(7, 0x22F), Is.EqualTo(0x30000));
            }

        }
    }
}