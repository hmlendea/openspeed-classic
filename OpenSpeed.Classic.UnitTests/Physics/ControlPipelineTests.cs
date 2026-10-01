using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class ControlPipelineTests
    {
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
    }
}