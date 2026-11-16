using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class MainForceSolverTests
    {
        private static int ProjectionConstant => unchecked((int)0xFFF60000);

        private static int HalfProjectionConstant => unchecked((int)0xFFFB0000);

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
            car[0x14] = 0;
            FixedMatrices.Write(car, 0x188, FixedMatrices.Identity());
            CarSpecifications descriptor = new();
            descriptor[0xF0] = 0x3000;
            descriptor[0x138] = X86Math.One;
            descriptor[0x128] = 0x8000;
            descriptor[0x1D4] = X86Math.One;
            byte[] routeBytes = new byte[PhysicsRoute.RecordSize * 16];
            for (int i = 0; i < 16; i++)
            {
                int baseOffset = i * PhysicsRoute.RecordSize;
                routeBytes[baseOffset + 0x0D] = 128;
                routeBytes[baseOffset + 0x11] = 127;
            }

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

        [TestCase(0x10)]
        [TestCase(0x40)]
        public void GivenALateralSuppressionFlag_WhenApplying_ThenTheLocalForceIsUntouched(int flag)
        {
            CarMemory car = new();
            car[0x1F4] = flag;
            car.WriteByte(0x2D8, 0x0A);
            car[0x2AC] = 0x1234;

            LateralResponse.Apply(car, new PhysicsContext { Mode = 1 }, true);

            Assert.That(car[0x2AC], Is.EqualTo(0x1234));
        }

        [Test]
        public void GivenAnInBoundsResponseInBoundedMode_WhenApplying_ThenOnlyAQuarterOfTheScaledValueIsAdded()
        {
            CarMemory car = new();
            car.WriteByte(0x2D8, 0x0A);
            car[0x94] = X86Math.One;
            car[0xE0] = X86Math.One;
            car[0x2AC] = 0x100;
            PhysicsContext context = new() { Mode = 1 };

            LateralResponse.Apply(car, context, false);

            // The projection is q16(0x10000, 0xFFF60000) = 0xFFF60000, and the local force of
            // 0x100 makes the response -0x1000, which is inside the 0x1999 bound. The source then
            // adds a quarter of the scaled projection rather than the projection multiplied by
            // the local force, so the local force moves by exactly -0x28000.
            Assert.That(car[0x2AC], Is.EqualTo(unchecked(0x100 - 0x28000)));
        }

        [Test]
        public void GivenAnOutOfBoundsResponse_WhenApplying_ThenTheWholeScaledValueIsAdded()
        {
            CarMemory car = new();
            car.WriteByte(0x2D8, 0x0A);
            car[0x94] = X86Math.One;
            car[0xE0] = X86Math.One;
            car[0x2AC] = 0x2000;
            PhysicsContext context = new() { Mode = 1 };

            LateralResponse.Apply(car, context, false);

            Assert.That(car[0x2AC], Is.EqualTo(unchecked(0x2000 + ProjectionConstant)));
        }

        [Test]
        public void GivenAnUnboundedMode_WhenApplying_ThenTheQuarterBranchIsSkipped()
        {
            CarMemory car = new();
            car.WriteByte(0x2D8, 0x0A);
            car[0x94] = X86Math.One;
            car[0xE0] = X86Math.One;
            car[0x2AC] = 0x100;
            PhysicsContext context = new() { Mode = 0 };

            LateralResponse.Apply(car, context, false);

            // Mode zero fails the source's mode test, so the full scaled value is added even when
            // the response sits inside the bounds.
            Assert.That(car[0x2AC], Is.EqualTo(unchecked(0x100 + ProjectionConstant)));
        }

        [Test]
        public void GivenAConfiguredBound_WhenApplying_ThenTheConfiguredBoundSelectsTheBranch()
        {
            CarMemory car = new();
            car.WriteByte(0x2D8, 0x0A);
            car[0x94] = X86Math.One;
            car[0xE0] = 0x8000;
            car[0x2AC] = 0x100;
            PhysicsContext context = new() { Mode = 1 };
            int original = PhysicsTables.LateralResponseBound;

            try
            {
                PhysicsTables.LateralResponseBound = 0x41;
                LateralResponse.Apply(car, context, false);

                // The bound is compared against the response, not the scaled projection, so the
                // narrowed configuration bound alone pushes this response out of range and
                // selects the full addition.
                Assert.That(car[0x2AC], Is.EqualTo(unchecked(0x100 + HalfProjectionConstant)));
            }
            finally
            {
                PhysicsTables.LateralResponseBound = original;
            }
        }
    }
}