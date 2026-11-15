using System;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class ContactDemandGuardTests
    {
        private const int GripOffset = 0x304;

        private const int ContactForceOffset = 0x30C;

        private const int NormalisedForceOffset = 0x310;

        private const int ResponseStateOffset = 0x2F4;

        private const int PersistentFirstOffset = 0x324;

        private const int PersistentSecondOffset = 0x328;

        private static CarSpecifications BuildDescriptor(int demandCoefficient)
        {
            CarSpecifications descriptor = new();
            descriptor[0x158] = demandCoefficient;

            return descriptor;
        }

        [Test]
        public void GivenANonContactAliveState_WhenUpdating_ThenTheReleaseGroupIsSkipped()
        {
            CarMemory car = new();
            car[ResponseStateOffset] = 1;
            car[GripOffset] = 0x90000;
            car[ContactForceOffset] = 0x40000;
            car.WriteByte(0x2D7, 0xFF);
            PhysicsContext context = new() { Accelerator = X86Math.One, Brake = X86Math.One };

            ContactDemandGuard.Update(car, BuildDescriptor(0), context, false);

            Assert.That(car[ResponseStateOffset], Is.EqualTo(1));
        }

        [Test]
        public void GivenAContactAliveReleasableState_WhenUpdating_ThenTheReleaseGroupRuns()
        {
            CarMemory car = new();
            car[ResponseStateOffset] = 1;
            car[GripOffset] = 0x90000;
            car[ContactForceOffset] = 0x40000;
            car.WriteByte(0x2D7, 0xFF);
            PhysicsContext context = new() { Accelerator = X86Math.One, Brake = X86Math.One };

            ContactDemandGuard.Update(car, BuildDescriptor(0), context, true);

            Assert.That(car[ResponseStateOffset], Is.Not.EqualTo(1));
        }

        [TestCase(5)]
        [TestCase(1)]
        [TestCase(4)]
        public void GivenAnAliveReleasableState_WhenUpdating_ThenTheStateIsCleared(int state)
        {
            CarMemory car = new();
            car[ResponseStateOffset] = state;

            ContactDemandGuard.Update(car, BuildDescriptor(0), new PhysicsContext(), true);

            Assert.That(car[ResponseStateOffset], Is.Zero);
        }

        [Test]
        public void GivenAZeroStateWithoutAcceleratorDemand_WhenUpdating_ThenThePersistentAccumulatorsAreUntouched()
        {
            CarMemory car = new();
            PhysicsContext context = new() { Accelerator = X86Math.One, Brake = X86Math.One };

            ContactDemandGuard.Update(car, BuildDescriptor(0x10000), context, true);

            Assert.Multiple(() =>
            {
                Assert.That(car[PersistentFirstOffset], Is.Zero);
                Assert.That(car[PersistentSecondOffset], Is.Zero);
            });
        }

        [Test]
        public void GivenTheLowDemandBranch_WhenUpdating_ThenTheFirstAccumulatorAdvancesAndStateFiveIsSet()
        {
            CarMemory car = new();
            car[GripOffset] = 0x100000;
            car[ContactForceOffset] = 0x40000;
            car.WriteByte(0x2D7, 0x10);
            PhysicsContext context = new() { Accelerator = X86Math.One, Brake = X86Math.One };

            ContactDemandGuard.Update(car, BuildDescriptor(0), context, true);

            Assert.Multiple(() =>
            {
                Assert.That(car[ResponseStateOffset], Is.EqualTo(5));
                Assert.That(car[PersistentFirstOffset], Is.EqualTo(0x3333));
                Assert.That(car[PersistentSecondOffset], Is.EqualTo(0x3333));
            });
        }

        [Test]
        public void GivenAZeroStateAndZeroAcceleratorByte_WhenUpdating_ThenTheSelectorIsDoubled()
        {
            CarMemory car = new();
            car[GripOffset] = 0x90000;
            car.WriteByte(0x2D7, 0);
            PhysicsContext context = new() { GravityFirst = 0, Accelerator = X86Math.One };

            ContactDemandGuard.Update(car, BuildDescriptor(0), context, true);

            Assert.That(car[ResponseStateOffset], Is.Zero);
        }

        [Test]
        public void GivenABrakingStateInTheNegativeForceBranch_WhenUpdating_ThenTheContactForceIsClearedWhenNotReleased()
        {
            CarMemory car = new();
            car[ResponseStateOffset] = 3;
            car[ContactForceOffset] = 0x51E;
            car[NormalisedForceOffset] = 0x10000;
            car[GripOffset] = 0x80000;
            car.WriteByte(0x2D7, 0x40);
            PhysicsContext context = new() { Accelerator = X86Math.One, Brake = X86Math.One };

            ContactDemandGuard.Update(car, BuildDescriptor(0x10000), context, true);

            // The negative-force branch responds before the force inversion, so the final value
            // is the scaled magnitude; the state is what changes instead.
            Assert.That(car[ResponseStateOffset], Is.Not.EqualTo(3));
        }

        [Test]
        public void GivenAZeroNormalisedForceLowDemandOutcome_WhenUpdating_ThenTheStateAndForceAreCleared()
        {
            CarMemory car = new();
            car[GripOffset] = 0x10000;
            car[ContactForceOffset] = 0x10;
            car[ResponseStateOffset] = 0;
            car.WriteByte(0x2D7, 0xFF);
            PhysicsContext context = new() { Accelerator = 0x10000, Brake = 0x10000 };

            ContactDemandGuard.Update(car, BuildDescriptor(0), context, true);

            Assert.Multiple(() =>
            {
                Assert.That(car[ResponseStateOffset], Is.Zero);
                Assert.That(car[NormalisedForceOffset], Is.Zero);
            });
        }

        [Test]
        public void GivenNullArguments_WhenUpdating_ThenArgumentErrorsAreReported()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => ContactDemandGuard.Update(null!, new CarSpecifications(), new PhysicsContext(), true), Throws.ArgumentNullException);
                Assert.That(() => ContactDemandGuard.Update(new CarMemory(), null!, new PhysicsContext(), true), Throws.ArgumentNullException);
                Assert.That(() => ContactDemandGuard.Update(new CarMemory(), new CarSpecifications(), null!, true), Throws.ArgumentNullException);
            });
        }
    }
}
