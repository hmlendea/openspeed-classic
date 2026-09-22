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
    }
}