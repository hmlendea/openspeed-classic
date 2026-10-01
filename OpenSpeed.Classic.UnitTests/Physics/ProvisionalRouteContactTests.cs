using System;
using System.Buffers.Binary;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class ProvisionalRouteContactTests
    {
        [TestCase(4, 0, false)]
        [TestCase(0, 1, false)]
        [TestCase(0, -1, true)]
        [TestCase(-1, -1, true)]
        public void GivenVerticalMotion_WhenResolving_ThenUpwardMotionIsNotSnappedToTheRoad(int height, int velocity, bool expectedGrounded)
        {
            CarMemory car = CreateCar();
            car[0xA0] = height * X86Math.One;
            car[0xAC] = velocity * X86Math.One;
            bool grounded = ProvisionalRouteContact.Resolve(car, CreateRoute());

            Assert.That(grounded, Is.EqualTo(expectedGrounded));

            if (expectedGrounded)
            {
                Assert.That(car[0xA0], Is.Zero);
                Assert.That(car[0xAC], Is.Zero);
            }
            else
            {
                Assert.That(car[0xA0], Is.EqualTo(height * X86Math.One));
                Assert.That(car[0xAC], Is.EqualTo(velocity * X86Math.One));
            }
        }

        [TestCase(8, 4)]
        [TestCase(-8, -4)]
        public void GivenAnOutwardWallVelocity_WhenResolving_ThenTheCarIsReturnedInsideTheCorridor(int lateral, int velocity)
        {
            CarMemory car = CreateCar();
            car[0x9C] = lateral * X86Math.One;
            car[0xA8] = velocity * X86Math.One;
            ProvisionalRouteContact.Resolve(car, CreateRoute());

            Assert.Multiple(() =>
            {
                Assert.That(Math.Abs(car[0x9C]), Is.LessThan(Math.Abs(lateral * X86Math.One)));
                Assert.That(Math.Abs(car[0xA8]), Is.LessThan(Math.Abs(velocity * X86Math.One)));
                Assert.That(car[0xA0], Is.Zero);
                Assert.That(car[0x168], Is.EqualTo(0x30000));
                Assert.That(car[0x160], Is.GreaterThan(0));
            });
        }

        [Test]
        public void GivenAnInvalidGroundNormal_WhenResolving_ThenTheCarRemainsAirborne()
        {
            CarMemory car = CreateCar();
            PhysicsRoute route = new(new byte[PhysicsRoute.RecordSize]);

            Assert.That(ProvisionalRouteContact.Resolve(car, route), Is.False);
        }

        [Test]
        public void GivenNullInputs_WhenResolving_ThenArgumentErrorsAreReported()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => ProvisionalRouteContact.Resolve(null!, CreateRoute()), Throws.ArgumentNullException);
                Assert.That(() => ProvisionalRouteContact.Resolve(CreateCar(), null!), Throws.ArgumentNullException);
            });
        }

        private static CarMemory CreateCar()
        {
            CarMemory car = new();
            PlayerStateInitialiser.Initialise(car);
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());

            return car;
        }

        private static PhysicsRoute CreateRoute()
        {
            byte[] bytes = new byte[PhysicsRoute.RecordSize * 2];

            for (int index = 0; index < 2; index += 1)
            {
                int offset = index * PhysicsRoute.RecordSize;
                BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset + 8), index * 64 * X86Math.One);
                bytes[offset + 0x0D] = 127;
                bytes[offset + 0x11] = 127;
                bytes[offset + 0x12] = 127;
                BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(offset + 0x1A), 0x400);
                BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(offset + 0x1C), 0x400);
            }

            return new PhysicsRoute(bytes);
        }
    }
}