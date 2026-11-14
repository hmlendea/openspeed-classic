using System;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class CollisionImpulseTests
    {
        [Test]
        public void GivenThePairFixture_WhenApplyingAnImpulse_ThenEveryMutatedByteIsExact()
        {
            CarMemory first = new();
            CarMemory second = new();
            first[0xB8] = second[0xB8] = 0x10000;
            first[0xB4] = second[0xB4] = 0x8000;
            second[0xA8] = 0x10000;
            bool accepted = CollisionImpulse.ApplyPair(first, second, new FixedVector(), new FixedVector { First = 0x10000 }, 42, 64);
            CarMemory expectedFirst = new();
            CarMemory expectedSecond = new();
            expectedFirst[0xB8] = expectedSecond[0xB8] = 0x10000;
            expectedFirst[0xB4] = expectedSecond[0xB4] = 0x8000;
            expectedFirst[0xA8] = 0x8CCC;
            expectedSecond[0xA8] = 0x7334;
            expectedFirst[0x160] = expectedSecond[0x160] = 0x10000;
            expectedFirst[0x164] = 64;
            expectedSecond[0x164] = 42;
            expectedFirst[0x168] = expectedSecond[0x168] = 0x50001;
            expectedFirst.WriteWord(0x14C, 1);
            expectedSecond.WriteWord(0x14C, 1);

            Assert.Multiple(() =>
            {
                Assert.That(accepted);
                Assert.That(first.Snapshot(), Is.EqualTo(expectedFirst.Snapshot()));
                Assert.That(second.Snapshot(), Is.EqualTo(expectedSecond.Snapshot()));
            });
        }

        [TestCase(0x2FFFF, true)]
        [TestCase(0x30000, false)]
        [TestCase(0x30001, false)]
        public void GivenTheBroadPhaseBoundary_WhenTesting_ThenEqualityIsExcluded(int separation, bool expected)
        {
            CarMemory first = new();
            CarMemory second = new();
            first[0x114] = 0x10000;
            second[0x114] = 0x20000;
            second[0xA4] = separation;

            Assert.That(CollisionImpulse.IntersectsBroadPhase(first, second), Is.EqualTo(expected));
        }

        [Test]
        public void GivenASeparatingPair_WhenApplyingAnImpulse_ThenBothStatesRemainUnchanged()
        {
            CarMemory first = new();
            CarMemory second = new();
            first[0xB8] = second[0xB8] = 0x10000;
            first[0xA8] = 0x10000;
            byte[] originalFirst = first.Snapshot();
            byte[] originalSecond = second.Snapshot();
            bool accepted = CollisionImpulse.ApplyPair(first, second, new FixedVector(), new FixedVector { First = 0x10000 }, 42, 64);

            Assert.Multiple(() =>
            {
                Assert.That(accepted, Is.False);
                Assert.That(first.Snapshot(), Is.EqualTo(originalFirst));
                Assert.That(second.Snapshot(), Is.EqualTo(originalSecond));
            });
        }

        [Test]
        public void GivenAZeroImpulseDenominator_WhenApplying_ThenDivisionFaults()
            => Assert.That(() => CollisionImpulse.ApplyPair(new CarMemory(), new CarMemory(),
                new FixedVector(), new FixedVector(), 42, 64), Throws.TypeOf<DivideByZeroException>());
    }
}