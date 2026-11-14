using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class DrivetrainTests
    {
        [Test]
        public void GivenTheNeutralFixture_WhenUpdatingTheEngine_ThenTheEngineAndForceAreExact()
        {
            CarMemory car = new();
            car.WriteByte(0x2DA, 1);
            CarSpecifications descriptor = new();
            descriptor[0xF0] = 0x3000;
            PhysicsContext context = new() { Accelerator = 0x8000 };
            int force = Drivetrain.CalculateForce(car, descriptor, new CarRuntimeType(), context);
            CarMemory expected = new();
            expected.WriteByte(0x2DA, 1);
            expected[0x2F0] = 0x1F4;

            Assert.Multiple(() =>
            {
                Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
                Assert.That(force, Is.Zero);
            });
        }

        [Test]
        public void GivenTheAxleFixture_WhenSplittingForce_ThenDriveAndBrakeDistributionAreExact()
        {
            CarMemory car = new();
            car[0x2B8] = 0x1000;
            CarSpecifications descriptor = new();
            descriptor[0xF8] = 0x8000;
            descriptor[0xFC] = 0x4000;
            descriptor[0x100] = 0x8000;
            AxleForceDistribution forces = Drivetrain.SplitForce(car, descriptor,
                new CarRuntimeType(), new PhysicsContext { Brake = 0x10000 }, 0x10000);

            Assert.Multiple(() =>
            {
                Assert.That(forces.FrontDrive, Is.EqualTo(0x8000));
                Assert.That(forces.RearDrive, Is.EqualTo(0x8000));
                Assert.That(forces.FrontBrake, Is.EqualTo(-0x2000));
                Assert.That(forces.RearBrake, Is.EqualTo(-0x2000));
            });
        }

        [TestCase(0, 1, 0)]
        [TestCase(49, 1, 0)]
        [TestCase(50, 1, 0)]
        [TestCase(51, 1, 1)]
        [TestCase(1000, 0, 800)]
        public void GivenAnUncoupledEngine_WhenDecaying_ThenTheSpecifiedStepIsUsed(int engine, byte delay, int expected)
        {
            CarMemory car = new();
            car.WriteByte(0x2DA, 1);
            car.WriteByte(0x2DB, delay);
            car[0x2F0] = engine;
            CarSpecifications descriptor = new();
            descriptor[0xF0] = 0x3000;
            Drivetrain.CalculateForce(car, descriptor, new CarRuntimeType(), new PhysicsContext());

            Assert.That(car[0x2F0], Is.EqualTo(expected));
        }

        [TestCase(0, 0xA0000)]
        [TestCase(4, 0xC0000)]
        public void GivenTheAssemblyTorqueCapBranch_WhenCalculating_ThenOnlyTheNonPerformancePathIsCapped(byte flags, int expected)
        {
            CarMemory car = new();
            car.WriteByte(0x2DA, 2);
            CarSpecifications descriptor = new();
            descriptor[0xF0] = 0x3000;
            descriptor[0x4C] = 0xC0000;
            descriptor[0x1B0] = X86Math.One;
            PhysicsContext context = new() { Accelerator = X86Math.One, FeatureFlags = flags };
            int force = Drivetrain.CalculateForce(car, descriptor, new CarRuntimeType(), context);

            Assert.That(force, Is.EqualTo(expected));
        }

        [Test]
        public void GivenATargetBelowWheelSpeed_WhenCalculating_ThenTheSignedEngineBrakingBranchRuns()
        {
            CarMemory car = new();
            car.WriteByte(0x2DA, 2);
            car[0x2B8] = 0x1000000;
            car[0x2F0] = 256;
            CarSpecifications descriptor = new();
            descriptor[0xF0] = 0x3000;
            descriptor[0x14] = X86Math.One;
            descriptor[0x50] = X86Math.One;
            descriptor[0x144] = X86Math.One;
            descriptor[0x190] = X86Math.One;
            descriptor[0x1B0] = X86Math.One;
            int force = Drivetrain.CalculateForce(car, descriptor, new CarRuntimeType(), new PhysicsContext());

            Assert.Multiple(() =>
            {
                Assert.That(force, Is.EqualTo(-X86Math.One));
                Assert.That(car[0x2F0], Is.EqualTo(256));
            });
        }
    }
}