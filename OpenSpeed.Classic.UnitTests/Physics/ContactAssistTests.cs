using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class ContactAssistTests
    {
        [Test]
        public void GivenNoGatesMet_WhenUpdating_ThenNothingChanges()
        {
            CarMemory car = BuildCar();
            CarSpecifications descriptor = BuildDescriptor();

            ContactAssistResult result = ContactAssist.Update(car, descriptor, BuildDependencies());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo(ContactAssistResult.None));
                Assert.That(car[0xE8], Is.Zero);
                Assert.That(car[0x2F4], Is.Zero);
            }

        }

        [Test]
        public void GivenTheUpwardImpulseGate_WhenUpdating_ThenTheAngularImpulseIsWritten()
        {
            CarMemory car = BuildCar();
            CarSpecifications descriptor = BuildDescriptor();
            car.WriteWord(0x14C, 1);
            car[0xAC] = -X86Math.One;
            car[0x198] = 0x10000;
            car[0x124] = 0x10000;

            ContactAssistResult result = ContactAssist.Update(car, descriptor, BuildDependencies());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo(ContactAssistResult.None));
                Assert.That(car[0xE8], Is.Zero);
                Assert.That(car[0xF0], Is.Not.Zero);
            }

        }

        [Test]
        public void GivenANonNegativeProjection_WhenUpdating_ThenNoImpulseIsWritten()
        {
            CarMemory car = BuildCar();
            CarSpecifications descriptor = BuildDescriptor();
            car.WriteWord(0x14C, 1);
            car[0xAC] = -X86Math.One;
            car[0x198] = 0x10000;
            car[0x124] = -0x10000;

            ContactAssist.Update(car, descriptor, BuildDependencies());

            Assert.That(car[0xF0], Is.Zero);
        }

        [Test]
        public void GivenNonnegativeVerticalVelocity_WhenUpdating_ThenTheImpulseGateIsSkipped()
        {
            CarMemory car = BuildCar();
            CarSpecifications descriptor = BuildDescriptor();
            car.WriteWord(0x14C, 1);
            car[0xAC] = 0;
            car[0x198] = 0x10000;
            car[0x124] = 0x10000;

            ContactAssistResult result = ContactAssist.Update(car, descriptor, BuildDependencies());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo(ContactAssistResult.None));
                Assert.That(car[0xF0], Is.Zero);
            }

        }

        [Test]
        public void GivenHighCorrection_WhenUpdating_ThenTheImpulseGateIsSkipped()
        {
            CarMemory car = BuildCar();
            CarSpecifications descriptor = BuildDescriptor();
            car[0x15C] = X86Math.One;
            car.WriteWord(0x14C, 1);
            car[0xAC] = -X86Math.One;
            car[0x198] = 0x10000;
            car[0x124] = 0x10000;

            ContactAssist.Update(car, descriptor, BuildDependencies());

            Assert.That(car[0xF0], Is.Zero);
        }

        [Test]
        public void GivenTheModeBitWithoutTheSamplingTable_WhenUpdating_ThenTheAssertionHookRuns()
        {
            CarMemory car = BuildCar();
            CarSpecifications descriptor = BuildDescriptor();
            car.WriteByte(0x1F4, 0x04);
            bool reported = false;
            ContactAssist.Dependencies dependencies = new(default, () => reported = true);

            ContactAssistResult result = ContactAssist.Update(car, descriptor, dependencies);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo(ContactAssistResult.MissingTable));
                Assert.That(reported, Is.True);
            }

        }

        [Test]
        public void GivenANonzeroCollisionCount_WhenUpdating_ThenTheModeBlockIsSkipped()
        {
            CarMemory car = BuildCar();
            CarSpecifications descriptor = BuildDescriptor();
            car.WriteByte(0x1F4, 0x04);
            car.WriteWord(0x14C, 1);

            ContactAssistResult result = ContactAssist.Update(car, descriptor, BuildDependencies());

            Assert.That(result, Is.EqualTo(ContactAssistResult.None));
        }

        [Test]
        public void GivenAWeakProjection_WhenUpdating_ThenTheResponseStateIsCleared()
        {
            CarMemory car = BuildCar();
            CarSpecifications descriptor = BuildDescriptor();
            car.WriteByte(0x1F4, 0x04);
            car[0x124] = 0x10000;
            car[0x2F4] = 3;

            ContactAssistResult result = ContactAssist.Update(car, descriptor, BuildDependencies());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo(ContactAssistResult.ResponseStateAssigned));
                Assert.That(car[0x2F4], Is.Zero);
            }

        }

        [Test]
        public void GivenAWeakProjectionAndAStateOfZero_WhenUpdating_ThenNoStateChangeOccurs()
        {
            CarMemory car = BuildCar();
            CarSpecifications descriptor = BuildDescriptor();
            car.WriteByte(0x1F4, 0x04);
            car[0x124] = 0x10000;
            car[0x2F4] = 0;

            ContactAssistResult result = ContactAssist.Update(car, descriptor, BuildDependencies());

            Assert.That(result, Is.EqualTo(ContactAssistResult.None));
        }

        [Test]
        public void GivenAHighThreshold_WhenUpdating_ThenNoStateIsAssigned()
        {
            CarMemory car = BuildCar();
            CarSpecifications descriptor = BuildDescriptor();
            car.WriteByte(0x1F4, 0x04);
            descriptor[0xF4] = 0x10000;
            car[0x2B8] = 0;

            byte[] table = new byte[256];
            table[0] = 0xFF;
            ContactAssist.Dependencies dependencies = new(table);

            ContactAssistResult result = ContactAssist.Update(car, descriptor, dependencies);

            Assert.That(result, Is.EqualTo(ContactAssistResult.None));
        }

        [Test]
        public void GivenALowThresholdAndANegativeForce_WhenUpdating_ThenTheNegativeStateIsAssigned()
        {
            CarMemory car = BuildCar();
            CarSpecifications descriptor = BuildDescriptor();
            car.WriteByte(0x1F4, 0x04);
            car[0x30C] = -0x1000;
            byte[] table = new byte[256];
            ContactAssist.Dependencies dependencies = new(table);

            ContactAssistResult result = ContactAssist.Update(car, descriptor, dependencies);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo(ContactAssistResult.ResponseStateAssigned));
                Assert.That(car[0x2F4], Is.EqualTo(-3));
            }

        }

        [Test]
        public void GivenALowThresholdAndAPositiveForce_WhenUpdating_ThenThePositiveStateIsAssigned()
        {
            CarMemory car = BuildCar();
            CarSpecifications descriptor = BuildDescriptor();
            car.WriteByte(0x1F4, 0x04);
            car[0x30C] = 0x10000;
            byte[] table = new byte[256];
            ContactAssist.Dependencies dependencies = new(table);

            ContactAssistResult result = ContactAssist.Update(car, descriptor, dependencies);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo(ContactAssistResult.ResponseStateAssigned));
                Assert.That(car[0x2F4], Is.EqualTo(3));
            }

        }

        [Test]
        public void GivenALowThresholdAndAForceInsideTheBand_WhenUpdating_ThenNoStateIsAssigned()
        {
            CarMemory car = BuildCar();
            CarSpecifications descriptor = BuildDescriptor();
            car.WriteByte(0x1F4, 0x04);
            byte[] table = new byte[256];
            ContactAssist.Dependencies dependencies = new(table);

            ContactAssistResult result = ContactAssist.Update(car, descriptor, dependencies);

            Assert.That(result, Is.EqualTo(ContactAssistResult.None));
        }

        [Test]
        public void GivenMissingInputs_WhenUpdating_ThenArgumentErrorsAreReported()
        {
            CarMemory car = BuildCar();
            CarSpecifications descriptor = BuildDescriptor();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    () => ContactAssist.Update(null!, descriptor, BuildDependencies()),
                    Throws.ArgumentNullException);
                Assert.That(
                    () => ContactAssist.Update(car, null!, BuildDependencies()),
                    Throws.ArgumentNullException);
            }

        }

        private static ContactAssist.Dependencies BuildDependencies() => new(new byte[256]);

        private static CarSpecifications BuildDescriptor()
        {
            CarSpecifications descriptor = new();
            descriptor[0xF4] = 0x10000;

            return descriptor;
        }

        private static CarMemory BuildCar()
        {
            CarMemory car = new();
            FixedMatrices.Write(car, 0x188, FixedMatrices.Identity());
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0xD0] = X86Math.One;

            return car;
        }
    }
}