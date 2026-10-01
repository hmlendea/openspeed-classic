using System;
using System.Buffers.Binary;
using System.IO;

using Moq;

using NuciLog.Core;

using NUnit.Framework;

using OpenSpeed.Classic.Assets;
using OpenSpeed.Classic.Cars;
using OpenSpeed.Classic.Cars.NeedForSpeed2;
using OpenSpeed.Classic.Physics;
using OpenSpeed.Classic.Rendering.Cars;
using OpenSpeed.Classic.Tracks;
using OpenSpeed.Classic.Tracks.NeedForSpeed2;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    [Explicit("Requires OPENSPEED_TEST_ASSET_ROOT pointing to an original NFS II SE installation.")]
    public sealed class PlayerVehicleSimulationTests
    {
        [Test]
        public void GivenNoInput_WhenStarting_ThenTheNativeGearRemainsNeutral()
        {
            PlayerVehicleSimulation simulation = new(LoadDescriptor("pFF50.dat"), CreateRoute());
            Advance(simulation, 1, 0, 0, false);

            Assert.Multiple(() =>
            {
                Assert.That(simulation.State.ReadByte(0x2DA), Is.EqualTo(1));
                Assert.That(simulation.State.ReadByte(0x2D6), Is.EqualTo(1));
                Assert.That(simulation.State[0xA4], Is.Zero);
                Assert.That(simulation.State[0xB4], Is.EqualTo(0xA0000));
                Assert.That(simulation.State[0xF4], Is.EqualTo(0xF0000));
            });
        }

        [TestCase("Outback")]
        [TestCase("MysticPeaks")]
        public void GivenOriginalTrackAndCarData_WhenDriving_ThenTheNativeSimulationRemainsUsable(string trackIdentifier)
        {
            string root = Environment.GetEnvironmentVariable("OPENSPEED_TEST_ASSET_ROOT")!;
            NeedForSpeed2TrackLoader loader = new(new FilePathResolver(Mock.Of<ILogger>()));
            LoadedTrack track = loader.Load(root, trackIdentifier);
            PlayerVehicleSimulation simulation = new(LoadDescriptor("pFF50.dat"), track.PhysicsRoute!, CarIdentifier.FerrariF50);
            int initialFirst = simulation.State[0x9C];
            int initialThird = simulation.State[0xA4];
            Advance(simulation, 8, 1, 0, false);

            Assert.Multiple(() =>
            {
                Assert.That(simulation.State[0x9C] != initialFirst || simulation.State[0xA4] != initialThird);
                Assert.That(simulation.State[0x2F0], Is.GreaterThan(0));
                Assert.That(float.IsFinite(PhysicsRenderAdapter.CreateWorld(simulation.State).Forward.X));
            });
        }

        [TestCase("pFF50.dat")]
        [TestCase("pMCF1.dat")]
        public void GivenOriginalSpecifications_WhenAccelerating_ThenNativeMotionAdvances(string member)
        {
            CarSpecifications descriptor = LoadDescriptor(member);
            byte[] source = descriptor.Snapshot();
            PlayerVehicleSimulation simulation = new(descriptor, CreateRoute());
            Advance(simulation, 4, 1, 0, false);

            Assert.Multiple(() =>
            {
                Assert.That(simulation.State[0xA4], Is.GreaterThan(0));
                Assert.That(PhysicsRenderAdapter.GetLongitudinalVelocity(simulation.State), Is.GreaterThan(1));
                Assert.That(simulation.State[0x2F0], Is.GreaterThan(0));
                Assert.That(simulation.IsGrounded);
                Assert.That(descriptor.Snapshot(), Is.EqualTo(source));
            });
        }

        [Test]
        public void GivenForwardMotion_WhenBrakingAndReversing_ThenVelocityChangesDirection()
        {
            PlayerVehicleSimulation simulation = new(LoadDescriptor("pFF50.dat"), CreateRoute());
            Advance(simulation, 3, 1, 0, false);
            float forwardSpeed = PhysicsRenderAdapter.GetLongitudinalVelocity(simulation.State);
            Advance(simulation, 1, -1, 0, false);

            Assert.That(PhysicsRenderAdapter.GetLongitudinalVelocity(simulation.State), Is.LessThan(forwardSpeed));

            Advance(simulation, 10, -1, 0, false);

            Assert.Multiple(() =>
            {
                Assert.That(simulation.State.ReadByte(0x2DA), Is.Zero);
                Assert.That(PhysicsRenderAdapter.GetLongitudinalVelocity(simulation.State), Is.LessThan(0));
            });
        }

        [TestCase(-1.0f)]
        [TestCase(1.0f)]
        public void GivenForwardMotion_WhenSteering_ThenTheNativeHeadingChanges(float steering)
        {
            PlayerVehicleSimulation simulation = new(LoadDescriptor("pFF50.dat"), CreateRoute());
            Advance(simulation, 2, 1, 0, false);
            int initialHeading = simulation.State[0xDC];
            Advance(simulation, 1, 1, steering, false);

            Assert.Multiple(() =>
            {
                Assert.That(simulation.State[0xDC], Is.Not.EqualTo(initialHeading));
                Assert.That(simulation.State[0xDC] * steering, Is.GreaterThan(0));
            });
        }

        [Test]
        public void GivenEquivalentElapsedTime_WhenUsingDifferentFrameSizes_ThenNativeStateIsIdentical()
        {
            CarSpecifications descriptor = LoadDescriptor("pFF50.dat");
            PlayerVehicleSimulation first = new(descriptor, CreateRoute());
            PlayerVehicleSimulation second = new(descriptor, CreateRoute());
            first.Advance(TimeSpan.FromSeconds(2), 1, 0, false);
            Advance(second, 2, 1, 0, false);

            Assert.That(first.State.Snapshot(), Is.EqualTo(second.State.Snapshot()));
        }

        [Test]
        public void GivenForwardMotion_WhenApplyingHandbrake_ThenTheResponseDiffers()
        {
            PlayerVehicleSimulation ordinary = new(LoadDescriptor("pFF50.dat"), CreateRoute());
            PlayerVehicleSimulation handbrake = new(LoadDescriptor("pFF50.dat"), CreateRoute());
            Advance(ordinary, 3, 1, 0, false);
            Advance(handbrake, 3, 1, 0, false);
            Advance(ordinary, 1, 1, 1, false);
            Advance(handbrake, 1, 1, 1, true);

            Assert.That(handbrake.State.Snapshot(), Is.Not.EqualTo(ordinary.State.Snapshot()));
        }

        [Test]
        public void GivenAnAirborneCar_WhenAdvancing_ThenGravityReturnsItToTheRoad()
        {
            PlayerVehicleSimulation simulation = new(LoadDescriptor("pFF50.dat"), CreateRoute());
            simulation.State[0xA0] = 4 * X86Math.One;
            simulation.Advance(TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 64), 0, 0, false);

            Assert.That(simulation.IsGrounded, Is.False);

            Advance(simulation, 2, 0, 0, false);

            Assert.Multiple(() =>
            {
                Assert.That(simulation.IsGrounded);
                Assert.That(simulation.State[0xA0], Is.Zero);
            });
        }

        [Test]
        public void GivenForwardMotion_WhenCoasting_ThenTheCarDecelerates()
        {
            PlayerVehicleSimulation simulation = new(LoadDescriptor("pFF50.dat"), CreateRoute());
            Advance(simulation, 5, 1, 0, false);
            float initial = PhysicsRenderAdapter.GetLongitudinalVelocity(simulation.State);
            Advance(simulation, 2, 0, 0, false);

            Assert.That(PhysicsRenderAdapter.GetLongitudinalVelocity(simulation.State), Is.LessThan(initial));
        }

        [Test]
        public void GivenSurfaceLaunchFlags_WhenAdvancing_ThenTheLaunchCallbackCanApplyItsImpulse()
        {
            PlayerVehicleSimulation simulation = new(
                LoadDescriptor("pFF50.dat"),
                CreateRoute(),
                CarIdentifier.McLarenF1,
                new byte[] { 0, 1 });
            simulation.State[0x184] = 1;
            simulation.State[0x128] = 0;
            simulation.State[0x15C] = 0;
            simulation.State[0x124] = 0x1000;
            simulation.State[0x12C] = 0x2000;
            simulation.State[0xA0] = 4 * X86Math.One;
            int initialVertical = simulation.State[0xAC];

            simulation.Advance(
                TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 64),
                0,
                0,
                false);

            Assert.That(simulation.State[0xAC], Is.LessThan(initialVertical));
        }

        private static void Advance(PlayerVehicleSimulation simulation, int seconds, float movement, float steering, bool handbrake)
        {
            for (int tick = 0; tick < seconds * 64; tick += 1)
            {
                simulation.Advance(TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 64), movement, steering, handbrake);
            }
        }

        private static CarSpecifications LoadDescriptor(string member)
        {
            string root = Environment.GetEnvironmentVariable("OPENSPEED_TEST_ASSET_ROOT")!;
            ArgumentException.ThrowIfNullOrWhiteSpace(root);
            byte[] archive = File.ReadAllBytes(Path.Combine(root, "GameData", "Sim", "CarData", "CARDATA.VIV"));

            return CarSpecifications.Decode(NeedForSpeed2CarArchiveReader.Read(archive, member));
        }

        private static PhysicsRoute CreateRoute()
        {
            byte[] records = new byte[PhysicsRoute.RecordSize * 4];
            int[] firstCoordinates = [0, 0, 2000, 2000];
            int[] thirdCoordinates = [0, 2000, 2000, 0];

            for (int index = 0; index < 4; index += 1)
            {
                int offset = index * PhysicsRoute.RecordSize;
                BinaryPrimitives.WriteInt32LittleEndian(records.AsSpan(offset), firstCoordinates[index] * X86Math.One);
                BinaryPrimitives.WriteInt32LittleEndian(records.AsSpan(offset + 8), thirdCoordinates[index] * X86Math.One);
                records[offset + 0x0D] = 127;
                records[offset + 0x11] = 127;
                records[offset + 0x12] = 127;
                BinaryPrimitives.WriteInt16LittleEndian(records.AsSpan(offset + 0x1A), 0x4000);
                BinaryPrimitives.WriteInt16LittleEndian(records.AsSpan(offset + 0x1C), 0x4000);
            }

            return new PhysicsRoute(records);
        }
    }
}