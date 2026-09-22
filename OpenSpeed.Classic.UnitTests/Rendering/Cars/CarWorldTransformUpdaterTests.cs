using System;

using Microsoft.Xna.Framework;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering.Cars;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Rendering.Cars
{
    [TestFixture]
    public sealed class CarWorldTransformUpdaterTests
    {
        private static float ValueTolerance => 0.001f;

        [TestCase(1.0f, -20.0f)]
        [TestCase(-1.0f, 20.0f)]
        [TestCase(8.0f, -20.0f)]
        public void GivenMovementInput_WhenUpdating_ThenTheCarMovesAlongItsForwardAxis(
            float movementInput,
            float expectedPositionZ)
        {
            Matrix world = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up);

            Matrix updatedWorld = CarWorldTransformUpdater.Update(
                world,
                1.0f,
                movementInput,
                0.0f);

            Assert.That(
                updatedWorld.Translation,
                Is.EqualTo(new Vector3(0.0f, 0.0f, expectedPositionZ)));
        }

        [Test]
        public void GivenRightTurningInput_WhenUpdating_ThenTheCarTurnsRight()
        {
            Matrix world = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up);

            Matrix updatedWorld = CarWorldTransformUpdater.Update(
                world,
                1.0f,
                0.0f,
                1.0f);

            Assert.Multiple(() =>
            {
                Assert.That(updatedWorld.Forward.X, Is.EqualTo(1.0f).Within(ValueTolerance));
                Assert.That(updatedWorld.Forward.Z, Is.Zero.Within(ValueTolerance));
                Assert.That(updatedWorld.Translation, Is.EqualTo(Vector3.Zero));
            });
        }

        [Test]
        public void GivenMovementTowardsAWall_WhenUpdating_ThenTheCarStopsAtTheWall()
        {
            Matrix world = Matrix.CreateWorld(
                new Vector3(6.0f, 0.0f, 0.0f),
                Vector3.Right,
                Vector3.Up);
            TrackRoutePoint routePoint = new()
            {
                LeftBorderDistance = 8.0,
                Right = new TrackVector
                {
                    X = 127.0
                },
                RightBorderDistance = 8.0
            };

            Matrix updatedWorld = CarWorldTransformUpdater.Update(
                world,
                [routePoint],
                1.0f,
                1.0f,
                0.0f);

            Assert.That(updatedWorld.Translation.X, Is.EqualTo(7.0f));
        }

        [Test]
        public void GivenRepeatedForwardInput_WhenUpdating_ThenTheCarAccelerates()
        {
            CarPhysicsState physicsState = new();
            Matrix world = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up);

            Matrix firstWorld = CarWorldTransformUpdater.Update(
                world,
                [],
                physicsState,
                1.0f,
                1.0f,
                0.0f);
            Matrix secondWorld = CarWorldTransformUpdater.Update(
                firstWorld,
                [],
                physicsState,
                1.0f,
                1.0f,
                0.0f);

            Assert.Multiple(() =>
            {
                Assert.That(firstWorld.Translation.Z, Is.EqualTo(-6.0f));
                Assert.That(secondWorld.Translation.Z, Is.EqualTo(-24.0f));
                Assert.That(physicsState.LongitudinalVelocity, Is.EqualTo(24.0f));
            });
        }

        [Test]
        public void GivenNoInputWhileMoving_WhenUpdating_ThenMomentumMovesTheCarForwards()
        {
            CarPhysicsState physicsState = new()
            {
                LongitudinalVelocity = 12.0f
            };
            Matrix world = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up);

            Matrix updatedWorld = CarWorldTransformUpdater.Update(
                world,
                [],
                physicsState,
                1.0f,
                0.0f,
                0.0f);

            Assert.Multiple(() =>
            {
                Assert.That(updatedWorld.Translation.Z, Is.EqualTo(-9.0f));
                Assert.That(physicsState.LongitudinalVelocity, Is.EqualTo(6.0f));
            });
        }

        [Test]
        public void GivenTheHandbrakeWhileMoving_WhenUpdating_ThenMostMomentumIsRetained()
        {
            CarPhysicsState physicsState = new()
            {
                LongitudinalVelocity = 12.0f
            };
            Matrix world = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up);

            Matrix updatedWorld = CarWorldTransformUpdater.Update(
                world,
                [],
                physicsState,
                0.25f,
                0.0f,
                0.0f,
                true);

            Assert.Multiple(() =>
            {
                Assert.That(updatedWorld.Translation.Z, Is.EqualTo(-2.625f));
                Assert.That(physicsState.LongitudinalVelocity, Is.EqualTo(9.0f));
            });
        }

        [Test]
        public void GivenTheHandbrakeWhileTurning_WhenUpdating_ThenTheCarDriftsLaterally()
        {
            CarPhysicsState physicsState = new()
            {
                LongitudinalVelocity = 16.0f
            };
            Matrix world = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up);

            Matrix updatedWorld = CarWorldTransformUpdater.Update(
                world,
                [],
                physicsState,
                0.25f,
                0.0f,
                1.0f,
                true);
            Vector3 displacement = updatedWorld.Translation - world.Translation;
            float lateralDisplacement = Vector3.Dot(
                displacement,
                updatedWorld.Right);

            Assert.Multiple(() =>
            {
                Assert.That(lateralDisplacement, Is.LessThan(0.0f));
                Assert.That(physicsState.LateralVelocity, Is.LessThan(0.0f));
            });
        }

        [Test]
        public void GivenDriftMomentum_WhenReleasingTheHandbrake_ThenTheCarKeepsSliding()
        {
            CarPhysicsState physicsState = new()
            {
                LateralVelocity = -8.0f,
                LongitudinalVelocity = 16.0f
            };
            Matrix world = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up);

            Matrix updatedWorld = CarWorldTransformUpdater.Update(
                world,
                [],
                physicsState,
                0.25f,
                0.0f,
                0.0f,
                false);
            Vector3 displacement = updatedWorld.Translation - world.Translation;
            float lateralDisplacement = Vector3.Dot(
                displacement,
                updatedWorld.Right);

            Assert.Multiple(() =>
            {
                Assert.That(lateralDisplacement, Is.LessThan(0.0f));
                Assert.That(physicsState.LateralVelocity, Is.EqualTo(-5.5f));
            });
        }

        [Test]
        public void GivenADriftIntoAWall_WhenUpdating_ThenLateralMomentumIsCancelled()
        {
            CarPhysicsState physicsState = new()
            {
                LateralVelocity = 8.0f,
                LongitudinalVelocity = 16.0f
            };
            Matrix world = Matrix.CreateWorld(
                new Vector3(6.0f, 0.0f, 0.0f),
                Vector3.Forward,
                Vector3.Up);
            TrackRoutePoint routePoint = new()
            {
                LeftBorderDistance = 8.0,
                Normal = new TrackVector
                {
                    Y = 1.0
                },
                Right = new TrackVector
                {
                    X = 1.0
                },
                RightBorderDistance = 8.0
            };

            Matrix updatedWorld = CarWorldTransformUpdater.Update(
                world,
                [routePoint],
                physicsState,
                0.25f,
                0.0f,
                0.0f,
                true);

            Assert.Multiple(() =>
            {
                Assert.That(updatedWorld.Translation.X, Is.EqualTo(7.0f));
                Assert.That(physicsState.LateralVelocity, Is.Zero);
            });
        }

        [Test]
        public void GivenNoMomentum_WhenTurning_ThenTheCarDoesNotPivot()
        {
            Matrix world = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up);

            Matrix updatedWorld = CarWorldTransformUpdater.Update(
                world,
                [],
                new CarPhysicsState(),
                1.0f,
                0.0f,
                1.0f);

            Assert.That(updatedWorld.Forward, Is.EqualTo(Vector3.Forward));
        }

        [Test]
        public void GivenReverseMomentum_WhenTurningRight_ThenTheCarSteersInReverse()
        {
            CarPhysicsState physicsState = new()
            {
                LongitudinalVelocity = -16.0f
            };
            Matrix world = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up);

            Matrix updatedWorld = CarWorldTransformUpdater.Update(
                world,
                [],
                physicsState,
                1.0f,
                -1.0f,
                1.0f);

            Assert.That(updatedWorld.Forward.X, Is.LessThan(0.0f));
        }

        [Test]
        public void GivenMovementAlongAnAscendingRoad_WhenUpdating_ThenTheCarAscendsTheSlope()
        {
            Vector3 slopeNormal = Vector3.Normalize(new Vector3(0.0f, 2.0f, 1.0f));
            Vector3 slopeForward = Vector3.Normalize(new Vector3(0.0f, 1.0f, -2.0f));
            Matrix world = Matrix.CreateWorld(Vector3.Zero, slopeForward, slopeNormal);
            TrackRoutePoint start = BuildSlopeRoutePoint(0.0, 0.0, slopeForward, slopeNormal);
            TrackRoutePoint end = BuildSlopeRoutePoint(8.0, -16.0, slopeForward, slopeNormal);

            Matrix updatedWorld = CarWorldTransformUpdater.Update(
                world,
                [start, end],
                new CarPhysicsState(),
                0.4f,
                1.0f,
                0.0f);

            Assert.Multiple(() =>
            {
                Assert.That(updatedWorld.Translation.Y, Is.GreaterThan(0.0f));
                Assert.That(updatedWorld.Translation.Z, Is.LessThan(0.0f));
                Assert.That(updatedWorld.Up.Y, Is.EqualTo(slopeNormal.Y).Within(ValueTolerance));
                Assert.That(updatedWorld.Up.Z, Is.EqualTo(slopeNormal.Z).Within(ValueTolerance));
            });
        }

        [Test]
        public void GivenMovementAlongADescendingRoad_WhenUpdating_ThenTheCarDescendsTheSlope()
        {
            Vector3 slopeNormal = Vector3.Normalize(new Vector3(0.0f, 2.0f, -1.0f));
            Vector3 slopeForward = Vector3.Normalize(new Vector3(0.0f, -1.0f, -2.0f));
            Matrix world = Matrix.CreateWorld(
                new Vector3(0.0f, 8.0f, 0.0f),
                slopeForward,
                slopeNormal);
            TrackRoutePoint start = BuildSlopeRoutePoint(8.0, 0.0, slopeForward, slopeNormal);
            TrackRoutePoint end = BuildSlopeRoutePoint(0.0, -16.0, slopeForward, slopeNormal);

            Matrix updatedWorld = CarWorldTransformUpdater.Update(
                world,
                [start, end],
                new CarPhysicsState(),
                0.4f,
                1.0f,
                0.0f);

            Assert.Multiple(() =>
            {
                Assert.That(updatedWorld.Translation.Y, Is.LessThan(8.0f));
                Assert.That(updatedWorld.Translation.Z, Is.LessThan(0.0f));
                Assert.That(updatedWorld.Up.Y, Is.EqualTo(slopeNormal.Y).Within(ValueTolerance));
                Assert.That(updatedWorld.Up.Z, Is.EqualTo(slopeNormal.Z).Within(ValueTolerance));
            });
        }

        [TestCase(float.NaN)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-1.0f)]
        public void GivenInvalidElapsedTime_WhenUpdating_ThenTheElapsedTimeIsRejected(
            float elapsedSeconds)
            => Assert.That(
                () => CarWorldTransformUpdater.Update(
                    Matrix.Identity,
                    elapsedSeconds,
                    0.0f,
                    0.0f),
                Throws.TypeOf<ArgumentOutOfRangeException>());

        private static TrackRoutePoint BuildSlopeRoutePoint(
            double positionY,
            double positionZ,
            Vector3 forward,
            Vector3 normal)
            => new()
            {
                Forward = new TrackVector
                {
                    X = forward.X,
                    Y = forward.Y,
                    Z = forward.Z
                },
                LeftBorderDistance = 8.0,
                Normal = new TrackVector
                {
                    X = normal.X,
                    Y = normal.Y,
                    Z = normal.Z
                },
                Position = new TrackPoint
                {
                    Y = positionY,
                    Z = positionZ
                },
                Right = new TrackVector
                {
                    X = 1.0
                },
                RightBorderDistance = 8.0
            };
    }
}