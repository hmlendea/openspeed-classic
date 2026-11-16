using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class PairCollisionRaceEventTests
    {
        [TestCase(6, -0xA0001, 1, 0, true)]
        [TestCase(7, -0xA0001, 0, 1, true)]
        [TestCase(6, -0xA0000, 1, 0, false)]
        [TestCase(7, -0x9FFFF, 1, 0, false)]
        [TestCase(5, -0xA0001, 1, 0, false)]
        [TestCase(8, -0xA0001, 1, 0, false)]
        [TestCase(6, -0xA0001, 0, 0, false)]
        [TestCase(6, int.MinValue, 255, 0, true)]
        public void GivenPairCompletionInputs_WhenEvaluating_ThenOnlyTheNativeRaceGateRequestsALatch(
            int raceType,
            int velocity,
            byte firstAction,
            byte secondAction,
            bool expectsLatch)
        {
            CarMemory player = new();
            CarMemory specialCar = new();
            player[0x1F4] = 4;
            specialCar[0x1F4] = 0x40;
            player[0x2B8] = velocity;
            player.WriteByte(0x2DD, firstAction);
            specialCar.WriteByte(0x2DD, secondAction);
            byte[] playerBefore = player.Snapshot();
            byte[] specialBefore = specialCar.Snapshot();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                            PairCollisionRaceEvent.ShouldLatch(player, specialCar, raceType),
                            Is.EqualTo(expectsLatch));
                Assert.That(
                    PairCollisionRaceEvent.ShouldLatch(specialCar, player, raceType),
                    Is.EqualTo(expectsLatch));
                Assert.That(player.Snapshot(), Is.EqualTo(playerBefore));
                Assert.That(specialCar.Snapshot(), Is.EqualTo(specialBefore));
            }

        }

        [TestCase(0, 0x40)]
        [TestCase(4, 0)]
        [TestCase(0x400, 0x40)]
        [TestCase(4, 0x4000)]
        public void GivenMissingLowByteRoles_WhenEvaluating_ThenNoRaceEventIsRequested(
            int playerRole,
            int specialRole)
        {
            CarMemory player = new();
            CarMemory specialCar = new();
            player[0x1F4] = playerRole;
            specialCar[0x1F4] = specialRole;
            player[0x2B8] = int.MinValue;
            player.WriteByte(0x2DD, 1);

            Assert.That(PairCollisionRaceEvent.ShouldLatch(player, specialCar, 6), Is.False);
        }

        [Test]
        public void GivenMissingBodies_WhenEvaluating_ThenArgumentErrorsAreReported()
        {
            Assert.That(
                () => PairCollisionRaceEvent.ShouldLatch(null!, new CarMemory(), 6),
                Throws.ArgumentNullException);
            Assert.That(
                () => PairCollisionRaceEvent.ShouldLatch(new CarMemory(), null!, 6),
                Throws.ArgumentNullException);
        }
    }
}