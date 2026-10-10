using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class ControlPipelineTests
    {
        [TestCase(true, 2)]
        [TestCase(false, 0)]
        public void GivenAStartupRangePolicy_WhenSmoothing_ThenReverseIsPreservedOutsideRaceCountdown(bool forceDrive, int expected)
        {
            CarMemory car = new();
            CarSpecifications descriptor = new();
            descriptor[0x04] = 3;
            CarRuntimeType runtimeType = new();
            runtimeType[0x08] = 1;
            PhysicsContext context = new() { InputEnabled = 1, IsStartingDriveRangeForced = forceDrive };
            ControlPipeline.Smooth(car, descriptor, runtimeType, context);

            Assert.Multiple(() =>
            {
                Assert.That(car.ReadByte(0x2D6), Is.EqualTo(expected));
                Assert.That(car.ReadByte(0x2DA), Is.EqualTo(expected));
                Assert.That(context.BaseTick, Is.Zero);
            });
        }

        [Test]
        public void GivenTheRawControlFixture_WhenSampling_ThenTheExactBytesAreWritten()
        {
            CarMemory car = new();
            RawVehicleInput input = new()
            {
                SteeringWord = unchecked((int)0x81000000),
                Accelerator = 255,
                Brake = 255,
                Actions = 0x0C
            };
            ControlPipeline.Sample(car, new CarSpecifications(), new CarRuntimeType(), new PhysicsContext(), input);
            CarMemory expected = new();
            expected.WriteByte(0x2D4, 0xF8);
            expected.WriteByte(0x2D5, 0xF8);
            expected[0x2E0] = -124;
            expected.WriteByte(0x2DC, 1);
            expected.WriteByte(0x2DD, 1);

            Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
        }

        [Test]
        public void GivenTheAcceleratorFixture_WhenSmoothing_ThenTheNormalisedValueIsExact()
        {
            CarMemory car = new();
            car.WriteByte(0x2D4, 0xF8);
            CarRuntimeType runtimeType = new();
            runtimeType[0x14] = 1;
            PhysicsContext context = new() { InputEnabled = 1 };
            ControlPipeline.Smooth(car, new CarSpecifications(), runtimeType, context);
            CarMemory expected = new();
            expected.WriteByte(0x2D4, 0xF8);
            expected.WriteByte(0x2D7, 0x10);

            Assert.Multiple(() =>
            {
                Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
                Assert.That(context.Accelerator, Is.EqualTo(0x118C));
                Assert.That(context.Brake, Is.EqualTo(0x108));
                Assert.That(context.Steering, Is.EqualTo(0x200));
            });
        }

        [TestCase(-32, 32, -24)]
        [TestCase(32, -32, 24)]
        [TestCase(32, 0, 24)]
        [TestCase(0, 32, 4)]
        public void GivenSteeringTargets_WhenSmoothing_ThenCentringAndTrackingRemainSeparate(int current, int target, int expected)
        {
            CarMemory car = new();
            car[0x2E0] = target;
            car[0x2E4] = current;
            CarSpecifications descriptor = new();
            descriptor[0x130] = 4;
            descriptor[0x134] = 8;
            CarRuntimeType runtimeType = new();
            runtimeType[0x10] = 1;
            ControlPipeline.Smooth(car, descriptor, runtimeType, new PhysicsContext());

            Assert.That(car[0x2E4], Is.EqualTo(expected));
        }

        [TestCase(0x100, 0x0F, 0x123, 0x123)]
        [TestCase(0x101, 0x0F, 0x123, 0x123)]
        [TestCase(0x200, 0x0F, 0x123, 0x123)]
        [TestCase(0x201, 0x0F, 0x123, 0x123)]
        public void GivenHeadingDifferenceBoundaries_WhenCalculating_ThenTheSourceBranchSelectsTheExpectedRouteIndex(
            int previousHeading,
            int routeIndex,
            int selectedHeading,
            int expected)
        {
            CarMemory car = new();
            car[0x148] = 0x200;
            car[0x204] = previousHeading;
            car[0x14] = routeIndex;

            int actual = HeadingDifference.Calculate(car, 0x400, index => index == routeIndex + 0x0F ? selectedHeading : 0);

            Assert.That(actual, Is.EqualTo(expected - 0x200));
        }
    }
}