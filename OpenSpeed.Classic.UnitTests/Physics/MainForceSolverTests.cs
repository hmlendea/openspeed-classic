using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class MainForceSolverTests
    {
        [Test]
        public void GivenANeutralStationaryCar_WhenSolving_ThenMotionRemainsZero()
        {
            CarMemory car = new();
            car.WriteByte(0x2DA, 1);
            car.WriteByte(0x2D6, 1);
            car[0x94] = X86Math.One;
            FixedMatrices.Write(car, 0x188, FixedMatrices.Identity());
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            CarSpecifications descriptor = new();
            descriptor[0xF0] = 0x3000;
            descriptor[0x138] = X86Math.One;
            descriptor[0x128] = 0x8000;
            descriptor[0x1D4] = X86Math.One;
            byte[] routeBytes = new byte[PhysicsRoute.RecordSize];
            routeBytes[0x0D] = 128;
            routeBytes[0x11] = 127;
            PhysicsContext context = new() { InputEnabled = 1 };
            MainForceSolver.Update(car, descriptor, new CarRuntimeType(), context, new PhysicsRoute(routeBytes));

            Assert.Multiple(() =>
            {
                Assert.That(car[0xA8], Is.Zero);
                Assert.That(car[0xAC], Is.Zero);
                Assert.That(car[0xB0], Is.Zero);
                Assert.That(car[0xE8], Is.Zero);
                Assert.That(car[0xEC], Is.Zero);
                Assert.That(car[0xF0], Is.Zero);
                Assert.That(context.GravitySecond, Is.EqualTo(-0xA0000));
                Assert.That(context.FrontCoefficient, Is.EqualTo(X86Math.One));
                Assert.That(context.RearCoefficient, Is.EqualTo(X86Math.One));
            });
        }

        [Test]
        public void GivenAnAlternateStationaryCar_WhenSolving_ThenTheAlternatePathDoesNotThrow()
        {
            CarMemory car = new();
            car.WriteByte(0x2DA, 1);
            car[0x94] = X86Math.One;
            FixedMatrices.Write(car, 0x188, FixedMatrices.Identity());
            CarSpecifications descriptor = new();
            descriptor[0xF0] = 0x3000;
            descriptor[0x138] = X86Math.One;
            descriptor[0x128] = 0x8000;
            descriptor[0x1D4] = X86Math.One;
            byte[] routeBytes = new byte[PhysicsRoute.RecordSize];
            routeBytes[0x0D] = 128;
            routeBytes[0x11] = 127;

            AlternateForceSolver.Update(
                car,
                descriptor,
                new CarRuntimeType(),
                new PhysicsContext { Mode = 1 },
                new PhysicsRoute(routeBytes));

            Assert.That(car[0xA8], Is.Zero);
        }

        [Test]
        public void GivenLateralResponse_WhenApplyingInAlternateMode_ThenTheBoundedQuarterResponseIsUsed()
        {
            CarMemory car = new();
            car[0x1F4] = 0;
            car.WriteByte(0x2D8, 0x0A);
            car[0x94] = X86Math.One;
            car[0x2AC] = X86Math.One;
            car[0xE0] = X86Math.One;
            PhysicsContext context = new() { Mode = 1, SteeringScale = 0x1999 };

            LateralResponse.Apply(car, context, true);

            Assert.That(car[0x2AC], Is.Not.EqualTo(X86Math.One));
        }
    }
}