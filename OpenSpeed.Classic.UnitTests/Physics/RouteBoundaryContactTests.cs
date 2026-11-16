using System;
using System.Buffers.Binary;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class RouteBoundaryContactTests
    {
        private const int Boundary = 0x1000;
        private const int DefinitionByte = 0x5A;
        private const int NeighbouringLateral = -0x100000;
        private const int OppositeLateral = 0x100000;
        private const int NeighbouringDefinition = 0x05;
        private const int OppositeDefinition = 0x0A;
        private const int RouteSurface = 6;
        private const int OverrideSurface = 7;
        private const int Accelerator = 0x2D7;

        [Test]
        public void GivenNoBoundaryCrossing_WhenResolving_ThenNothingIsReported()
        {
            CarMemory car = BuildCar(lateral: 0, velocity: 0);
            PhysicsRoute route = BuildRoute();
            RouteBoundaryContact.Environment environment = new();

            int sign = RouteBoundaryContact.Resolve(car, route, environment);

            Assert.Multiple(() =>
            {
                Assert.That(sign, Is.Zero);
                Assert.That(environment.BoundarySurface, Is.Zero);
                Assert.That(car[0x170], Is.Zero);
            });
        }

        [Test]
        public void GivenTheRecordDefinition_WhenCrossingEachBoundary_ThenTheSurfaceAndSignAreRecorded()
        {
            (int Lateral, int Definitions, int Sign, int Surface)[] cases =
            [
                (NeighbouringLateral, 0xA0, -1, 0x0A),
                (OppositeLateral, 0x0A, 1, 0x0A)
            ];

            foreach ((int lateral, int definitions, int expectedSign, int expectedSurface) in cases)
            {
                CarMemory car = BuildCar(lateral, velocity: 0);
                PhysicsRoute route = BuildRoute(definitions);
                RouteBoundaryContact.Environment environment = new();

                int sign = RouteBoundaryContact.Resolve(car, route, environment);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(sign, Is.EqualTo(expectedSign));
                    Assert.That(environment.BoundarySurface, Is.EqualTo(expectedSurface));
                }
            }
        }

        [Test]
        public void GivenNoDefinitionNibble_WhenCrossing_ThenTheContactIsIgnored()
        {
            CarMemory car = BuildCar(NeighbouringLateral, velocity: 0);
            PhysicsRoute route = BuildRoute(definitions: 0x00);
            RouteBoundaryContact.Environment environment = new();

            int sign = RouteBoundaryContact.Resolve(car, route, environment);

            Assert.Multiple(() =>
            {
                Assert.That(sign, Is.Zero);
                Assert.That(environment.BoundarySurface, Is.Zero);
            });
        }

        [Test]
        public void GivenAnActiveSuppression_WhenCrossing_ThenTheResponseIsSkipped()
        {
            CarMemory car = BuildCar(NeighbouringLateral, velocity: 0);
            car[0x17C] = 5;
            PhysicsRoute route = BuildRoute();
            RouteBoundaryContact.Environment environment = new();

            int sign = RouteBoundaryContact.Resolve(car, route, environment);

            Assert.Multiple(() =>
            {
                Assert.That(sign, Is.Zero);
                Assert.That(car[0x170], Is.Zero);
                Assert.That(environment.BoundarySurface, Is.EqualTo(NeighbouringDefinition));
            });
        }

        [Test]
        public void GivenNoReaction_WhenCrossingSlowly_ThenTheGlancingResponseIsReported()
        {
            CarMemory car = BuildCar(NeighbouringLateral, velocity: 0);
            PhysicsRoute route = BuildRoute();
            RouteBoundaryContact.Environment environment = new();
            int? appliedPenetration = null;
            environment.ApplyGlance = (_, penetration, _) => appliedPenetration = penetration;

            int sign = RouteBoundaryContact.Resolve(car, route, environment);

            Assert.Multiple(() =>
            {
                Assert.That(sign, Is.EqualTo(-1));
                Assert.That(appliedPenetration, Is.Not.Null);
                Assert.That(car[0x578], Is.Zero);
            });
        }

        [Test]
        public void GivenALowLongitudinalProjection_WhenCrossing_ThenTheWorldContactResponseRuns()
        {
            CarMemory car = BuildCar(NeighbouringLateral, velocity: 0);
            car[0x100] = 0x1000;
            PhysicsRoute route = BuildRoute();
            RouteBoundaryContact.Environment environment = new();
            bool glanced = false;
            environment.ApplyGlance = (_, _, _) => glanced = true;

            int sign = RouteBoundaryContact.Resolve(car, route, environment);

            Assert.Multiple(() =>
            {
                Assert.That(sign, Is.EqualTo(-1));
                Assert.That(car[0x168], Is.EqualTo(car[0x184] | 0x30000));
                Assert.That(car[0x164], Is.Zero);
                Assert.That(glanced, Is.False);
            });
        }

        [Test]
        public void GivenAnExistingCollisionCount_WhenCrossing_ThenTheWorldContactResponseRuns()
        {
            CarMemory car = BuildCar(NeighbouringLateral, velocity: 0);
            car.WriteWord(0x14C, 1);
            PhysicsRoute route = BuildRoute();
            RouteBoundaryContact.Environment environment = new();
            bool glanced = false;
            environment.ApplyGlance = (_, _, _) => glanced = true;

            int sign = RouteBoundaryContact.Resolve(car, route, environment);

            Assert.Multiple(() =>
            {
                Assert.That(sign, Is.EqualTo(-1));
                Assert.That(glanced, Is.False);
            });
        }

        [Test]
        public void GivenAReactionFlagAndLowSpeed_WhenCrossingTheRouteSurface_ThenTheLightRecoveryStarts()
        {
            CarMemory car = BuildCar(NeighbouringLateral, velocity: 0);
            car.WriteByte(0x1F4, 0x04);
            car[0x15C] = 0x40000;
            PhysicsRoute route = BuildRoute(definitions: 0x66);
            RouteBoundaryContact.Environment environment = new();

            int sign = RouteBoundaryContact.Resolve(car, route, environment);

            Assert.Multiple(() =>
            {
                Assert.That(sign, Is.Zero);
                Assert.That(environment.BoundarySurface, Is.EqualTo(RouteSurface));
                Assert.That(car[0x578], Is.EqualTo(0x60));
                Assert.That(car[0x58C], Is.EqualTo(1));
                Assert.That(car[0x17C], Is.EqualTo(car[0x14]));
                Assert.That(car[0x168], Is.Zero);
            });
        }

        [Test]
        public void GivenAReactionFlagAndHighSpeed_WhenCrossingTheOverrideSurface_ThenTheSevereEventRuns()
        {
            CarMemory car = BuildCar(NeighbouringLateral, velocity: -0x400000);
            car.WriteByte(0x1F4, 0x04);
            car.WriteByte(Accelerator, 0xC9);
            PhysicsRoute route = BuildRoute(definitions: 0x77);
            RouteBoundaryContact.Environment environment = new();
            bool forced = false;
            environment.ApplyForcedImpulse = _ => forced = true;

            int sign = RouteBoundaryContact.Resolve(car, route, environment);

            Assert.Multiple(() =>
            {
                Assert.That(sign, Is.Zero);
                Assert.That(environment.BoundarySurface, Is.EqualTo(OverrideSurface));
                Assert.That(car[0x168], Is.EqualTo(0x40004));
                Assert.That(car[0x160], Is.EqualTo(0x1E0000));
                Assert.That(car[0x578], Is.EqualTo(0x80));
                Assert.That(car[0x58C], Is.EqualTo(1));
                Assert.That(car.ReadWord(0xF2), Is.EqualTo(1));
                Assert.That(forced, Is.True);
            });
        }

        [Test]
        public void GivenAHighSpeedOverrideCrossing_WhenTheAcceleratorIsLow_ThenNoForcedImpulseIsRequested()
        {
            CarMemory car = BuildCar(NeighbouringLateral, velocity: -0x400000);
            car.WriteByte(0x1F4, 0x04);
            PhysicsRoute route = BuildRoute(definitions: 0x77);
            RouteBoundaryContact.Environment environment = new();
            bool forced = false;
            environment.ApplyForcedImpulse = _ => forced = true;

            RouteBoundaryContact.Resolve(car, route, environment);

            Assert.Multiple(() =>
            {
                Assert.That(car[0x168], Is.EqualTo(0x40004));
                Assert.That(forced, Is.False);
            });
        }

        [Test]
        public void GivenAnApproachWindow_WhenCrossing_ThenTheRouteSurfaceIsPromoted()
        {
            CarMemory car = BuildCar(NeighbouringLateral, velocity: -0x400000);
            car.WriteByte(0x1F4, 0x04);
            car[0xBC] = 0x300000;
            car[0x14] = 0x0300;
            PhysicsRoute route = BuildLongRoute(definitions: 0x66);
            RouteBoundaryContact.Environment environment = new()
            {
                RaceType = 6,
                RaceEventFlag = 1
            };

            int sign = RouteBoundaryContact.Resolve(car, route, environment);

            Assert.Multiple(() =>
            {
                Assert.That(sign, Is.Zero);
                Assert.That(environment.BoundarySurface, Is.EqualTo(OverrideSurface));
                Assert.That(car[0x168], Is.EqualTo(0x40004));
            });
        }

        [Test]
        public void GivenAnApproachIndexOutsideEveryWindow_WhenCrossing_ThenTheSurfaceIsNotPromoted()
        {
            CarMemory car = BuildCar(NeighbouringLateral, velocity: -0x400000);
            car.WriteByte(0x1F4, 0x04);
            car[0xBC] = 0x300000;
            car[0x14] = 0;
            PhysicsRoute route = BuildRoute(definitions: 0x66);
            RouteBoundaryContact.Environment environment = new()
            {
                RaceType = 6,
                RaceEventFlag = 1
            };

            RouteBoundaryContact.Resolve(car, route, environment);

            Assert.That(environment.BoundarySurface, Is.EqualTo(RouteSurface));
        }

        [Test]
        public void GivenARouteWindowForTheRouteRaceType_WhenCrossing_ThenTheSurfaceIsPromoted()
        {
            CarMemory car = BuildCar(NeighbouringLateral, velocity: -0x400000);
            car.WriteByte(0x1F4, 0x04);
            car[0xBC] = 0x300000;
            car[0x14] = 0x0500;
            PhysicsRoute route = BuildLongRoute(definitions: 0x66);
            RouteBoundaryContact.Environment environment = new()
            {
                RaceType = 7,
                RaceEventFlag = 1
            };

            RouteBoundaryContact.Resolve(car, route, environment);

            Assert.That(environment.BoundarySurface, Is.EqualTo(OverrideSurface));
        }

        [Test]
        public void GivenAReactionIntervalWithoutTheFlag_WhenCrossing_ThenTheReactionPathStillRuns()
        {
            CarMemory car = BuildCar(NeighbouringLateral, velocity: 0);
            car[0x53C] = 1;
            car[0x15C] = 0x40000;
            PhysicsRoute route = BuildRoute(definitions: 0x66);
            RouteBoundaryContact.Environment environment = new();

            int sign = RouteBoundaryContact.Resolve(car, route, environment);

            Assert.Multiple(() =>
            {
                Assert.That(sign, Is.Zero);
                Assert.That(car[0x578], Is.EqualTo(0x60));
            });
        }

        [Test]
        public void GivenAReactionIntervalWithoutTheFlagAndLowLongitudinalProjection_WhenCrossing_ThenTheRecoveryIsSkipped()
        {
            CarMemory car = BuildCar(NeighbouringLateral, velocity: 0);
            car[0x53C] = 1;
            PhysicsRoute route = BuildRoute(definitions: 0x66);
            RouteBoundaryContact.Environment environment = new();
            bool glanced = false;
            environment.ApplyGlance = (_, _, _) => glanced = true;

            int sign = RouteBoundaryContact.Resolve(car, route, environment);

            Assert.Multiple(() =>
            {
                Assert.That(sign, Is.EqualTo(-1));
                Assert.That(car[0x578], Is.Zero);
                Assert.That(glanced, Is.True);
            });
        }

        [Test]
        public void GivenTheSupportOffset_WhenCrossing_ThenTheProjectionIsStored()
        {
            CarMemory car = BuildCar(NeighbouringLateral, velocity: 0);
            PhysicsRoute route = BuildRoute();
            RouteBoundaryContact.Environment environment = new();

            RouteBoundaryContact.Resolve(car, route, environment);

            FixedVector right = route.Direction(0, 0x12);
            int expected = X86Math.MultiplyQ16(right.First, NeighbouringLateral) +
                X86Math.MultiplyQ16(right.Second, 0) +
                X86Math.MultiplyQ16(right.Third, 0);

            Assert.That(car[0xC0], Is.EqualTo(expected));
        }

        [Test]
        public void GivenMissingInputs_WhenResolving_ThenDependenciesAreRejected()
        {
            CarMemory car = BuildCar(0, 0);
            PhysicsRoute route = BuildRoute();
            RouteBoundaryContact.Environment environment = new();

            Assert.Multiple(() =>
            {
                Assert.That(() => RouteBoundaryContact.Resolve(null!, route, environment), Throws.ArgumentNullException);
                Assert.That(() => RouteBoundaryContact.Resolve(car, null!, environment), Throws.ArgumentNullException);
                Assert.That(() => RouteBoundaryContact.Resolve(car, route, null!), Throws.ArgumentNullException);
            });
        }

        private static CarMemory BuildCar(int lateral, int velocity)
        {
            CarMemory car = new();
            PlayerStateInitialiser.Initialise(car);
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0x14] = 0;
            car[0x9C] = lateral;
            car[0xA8] = velocity;
            car[0xB0] = velocity;

            return car;
        }

        private static PhysicsRoute BuildLongRoute(int definitions)
        {
            byte[] bytes = new byte[PhysicsRoute.RecordSize * 0x700];

            for (int index = 0; index < 0x700; index += 1)
            {
                int offset = index * PhysicsRoute.RecordSize;
                bytes[offset + 0x12] = 127;
                bytes[offset + 0x0F + 2] = 127;
                bytes[offset + 0x1E] = (byte)definitions;
                BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(offset + 0x1A), Boundary);
                BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(offset + 0x1C), Boundary);
            }

            return new PhysicsRoute(bytes);
        }

        private static PhysicsRoute BuildRoute(int definitions = DefinitionByte)
        {
            byte[] bytes = new byte[PhysicsRoute.RecordSize * 2];

            for (int index = 0; index < 2; index += 1)
            {
                int offset = index * PhysicsRoute.RecordSize;
                bytes[offset + 0x12] = 127;
                bytes[offset + 0x0C + 1] = 127;
                bytes[offset + 0x1E] = (byte)definitions;
                BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(offset + 0x1A), Boundary);
                BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(offset + 0x1C), Boundary);            }

            return new PhysicsRoute(bytes);
        }
    }
}
