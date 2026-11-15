using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class TyreResponseTablesTests
    {
        [Test]
        public void GivenTheResponseTable_WhenReadingItsShape_ThenTwelveEntriesAreExposed()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TyreResponseTables.ResponseStates.Length, Is.EqualTo(12));
                Assert.That(TyreResponseTables.CapacityStates.Length, Is.EqualTo(12));
            }

        }

        [Test]
        public void GivenTheDocumentedResponseTable_WhenReadingCells_ThenTheyMatchTheLiterals()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TyreResponseTables.ResponseStates[0], Is.EqualTo(X86Math.One));
                Assert.That(TyreResponseTables.ResponseStates[3], Is.EqualTo(0xE666));
                Assert.That(TyreResponseTables.ResponseStates[4], Is.EqualTo(0x18000));
                Assert.That(TyreResponseTables.ResponseStates[5], Is.EqualTo(X86Math.One));
                Assert.That(TyreResponseTables.ResponseStates[11], Is.Zero);
            }

        }

        [Test]
        public void GivenTheDocumentedCapacityTable_WhenReadingCells_ThenTheyMatchTheLiterals()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TyreResponseTables.CapacityStates[0], Is.EqualTo(0x1999));
                Assert.That(TyreResponseTables.CapacityStates[1], Is.EqualTo(X86Math.One));
                Assert.That(TyreResponseTables.CapacityStates[4], Is.EqualTo(0x8000));
                Assert.That(TyreResponseTables.CapacityStates[8], Is.EqualTo(0x28000));
                Assert.That(TyreResponseTables.CapacityStates[10], Is.EqualTo(0x40000));
            }

        }

        [Test]
        public void GivenAZeroState_WhenReadingBothTables_ThenEntryZeroIsReturned()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TyreResponseTables.ResponseState(0), Is.EqualTo(X86Math.One));
                Assert.That(TyreResponseTables.CapacityState(0), Is.EqualTo(0x1999));
            }

        }

        [Test]
        public void GivenASignedState_WhenReadingBothTables_ThenTheAbsoluteIndexIsUsed()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TyreResponseTables.ResponseState(-4), Is.EqualTo(TyreResponseTables.ResponseState(4)));
                Assert.That(TyreResponseTables.CapacityState(-4), Is.EqualTo(TyreResponseTables.CapacityState(4)));
            }

        }

        [Test]
        public void GivenASignedStateOfFour_WhenReadingBothTables_ThenTheFifthCellsAreReturned()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TyreResponseTables.ResponseState(-4), Is.EqualTo(0x18000));
                Assert.That(TyreResponseTables.CapacityState(-4), Is.EqualTo(0x8000));
            }

        }
    }
}