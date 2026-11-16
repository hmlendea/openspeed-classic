using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class ContactSolverTests
    {
        [Test]
        public void GivenTheSlipFixture_WhenSolving_ThenAllRecordFieldsAreExact()
        {
            ContactRecord record = new();
            record[0x04] = 0x20;
            record[0x10] = 0x80;
            record[0x18] = 0x40;
            ContactSolver.CalculateSlip(record, new PhysicsContext { RearCoefficient = 0x10000 });
            ContactRecord expected = new();
            expected[0x04] = 0x20;
            expected[0x10] = 0x80;
            expected[0x18] = 0x40;
            expected[0x1C] = 0x40;
            expected[0x24] = 0x20;
            expected[0x2C] = 0x70;

            Assert.That(record.Snapshot(), Is.EqualTo(expected.Snapshot()));
        }

        [Test]
        public void GivenTheCapacityFixture_WhenLimiting_ThenRecordAndCarMutationsAreExact()
        {
            CarMemory car = new();
            car[0x200] = 3;
            car[0x2B8] = 0x60000;
            ContactRecord record = new();
            record[0x1C] = 0x100;
            record[0x24] = 0x80;
            ContactSolver.LimitCapacity(car, new CarRuntimeType(), record,
                new PhysicsContext { RearCoefficient = 0x10000 });
            ContactRecord expectedRecord = new();
            expectedRecord[0x24] = 0x80;
            expectedRecord[0x2C] = 0x120;
            CarMemory expectedCar = new();
            expectedCar[0x200] = 3;
            expectedCar[0x2B8] = 0x60000;

            Assert.Multiple(() =>
            {
                Assert.That(record.Snapshot(), Is.EqualTo(expectedRecord.Snapshot()));
                Assert.That(car.Snapshot(), Is.EqualTo(expectedCar.Snapshot()));
            });
        }

        [Test]
        public void GivenTheZeroContactFixture_WhenSolving_ThenCarAndRecordRemainZero()
        {
            CarMemory car = new();
            ContactRecord record = new();
            PhysicsContext context = new() { FrontCoefficient = 0x10000, RearCoefficient = 0x10000 };
            ContactSolver.UpdateRecord(car, new CarSpecifications(), new CarRuntimeType(), context, record);

            Assert.Multiple(() =>
            {
                Assert.That(car.Snapshot(), Is.EqualTo(new CarMemory().Snapshot()));
                Assert.That(record.Snapshot(), Is.EqualTo(new ContactRecord().Snapshot()));
            });
        }
    }
}