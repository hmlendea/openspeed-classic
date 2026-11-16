using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class OpponentCollisionSelectionTests
    {
        [Test]
        public void GivenAnInactiveCountdown_WhenSelecting_ThenNoCandidateIsReturned()
        {
            CarMemory car = BuildCar();
            CarMemory opponent = BuildCar();
            opponent[0x9C] = X86Math.One;
            car[0x2E8] = 1;

            Assert.That(OpponentCollisionSelection.Select(car, [opponent], 1, 0), Is.Null);
        }

        [Test]
        public void GivenRaceSelectorTwo_WhenSelecting_ThenNoCandidateIsReturned()
        {
            CarMemory car = BuildCar();
            CarMemory opponent = BuildCar();
            opponent[0x9C] = X86Math.One;
            car[0x280] = 2;

            Assert.That(OpponentCollisionSelection.Select(car, [opponent], 1, 0), Is.Null);
        }

        [Test]
        public void GivenAZeroReplayFlag_WhenSelecting_ThenNoCandidateIsReturned()
        {
            CarMemory car = BuildCar();
            CarMemory opponent = BuildCar();
            opponent[0x9C] = X86Math.One;

            Assert.That(OpponentCollisionSelection.Select(car, [opponent], 0, 0), Is.Null);
        }

        [Test]
        public void GivenNoPlayerRoleBit_WhenSelecting_ThenNoCandidateIsReturned()
        {
            CarMemory car = BuildCar();
            CarMemory opponent = BuildCar();
            opponent[0x9C] = X86Math.One;
            car.WriteByte(0x1F4, 0);

            Assert.That(OpponentCollisionSelection.Select(car, [opponent], 1, 0), Is.Null);
        }

        [Test]
        public void GivenARecoverySuppression_WhenSelecting_ThenNoCandidateIsReturned()
        {
            CarMemory car = BuildCar();
            CarMemory opponent = BuildCar();
            opponent[0x9C] = X86Math.One;
            car[0x17C] = 1;

            Assert.That(OpponentCollisionSelection.Select(car, [opponent], 1, 0), Is.Null);
        }

        [Test]
        public void GivenAnOpponentAheadWithinRange_WhenSelecting_ThenTheOpponentIsReturned()
        {
            CarMemory car = BuildCar();
            CarMemory opponent = BuildCar();
            opponent[0xA4] = 0x10000;

            Assert.That(OpponentCollisionSelection.Select(car, [opponent], 1, 0), Is.SameAs(opponent));
        }

        [Test]
        public void GivenAnOpponentBehind_WhenSelecting_ThenNoCandidateIsReturned()
        {
            CarMemory car = BuildCar();
            CarMemory opponent = BuildCar();
            opponent[0xA4] = -0x10000;

            Assert.That(OpponentCollisionSelection.Select(car, [opponent], 1, 0), Is.Null);
        }

        [Test]
        public void GivenAnOpponentBeyondTheMaximumDistance_WhenSelecting_ThenNoCandidateIsReturned()
        {
            CarMemory car = BuildCar();
            CarMemory opponent = BuildCar();
            opponent[0xA4] = 0x280000;

            Assert.That(OpponentCollisionSelection.Select(car, [opponent], 1, 0), Is.Null);
        }

        [Test]
        public void GivenTwoOpponentsAhead_WhenSelecting_ThenTheNearestIsReturned()
        {
            CarMemory car = BuildCar();
            CarMemory near = BuildCar();
            CarMemory far = BuildCar();
            near[0xA4] = 0x10000;
            far[0xA4] = 0x20000;

            Assert.That(OpponentCollisionSelection.Select(car, [far, near], 1, 0), Is.SameAs(near));
        }

        [Test]
        public void GivenAnInactiveCandidate_WhenSelecting_ThenItIsSkipped()
        {
            CarMemory car = BuildCar();
            CarMemory inactive = BuildCar();
            CarMemory active = BuildCar();
            inactive[0xA4] = 0x8000;
            inactive.WriteByte(0x8D, 0);
            active[0xA4] = 0x10000;

            Assert.That(OpponentCollisionSelection.Select(car, [inactive, active], 1, 0), Is.SameAs(active));
        }

        [Test]
        public void GivenACandidateWithANonzeroCollisionCount_WhenSelecting_ThenItIsSkipped()
        {
            CarMemory car = BuildCar();
            CarMemory busy = BuildCar();
            busy[0xA4] = 0x8000;
            busy.WriteWord(0x14C, 3);

            Assert.That(OpponentCollisionSelection.Select(car, [busy], 1, 0), Is.Null);
        }

        [Test]
        public void GivenTheSelfCarInTheList_WhenSelecting_ThenItIsSkipped()
        {
            CarMemory car = BuildCar();
            car.WriteByte(0x8D, 1);

            Assert.That(OpponentCollisionSelection.Select(car, [car], 1, 0), Is.Null);
        }

        [Test]
        public void GivenADiagonalOpponent_WhenSelecting_ThenTheProjectionThresholdRejectsIt()
        {
            CarMemory car = BuildCar();
            CarMemory opponent = BuildCar();
            opponent[0x9C] = 0x10000;
            opponent[0xA4] = 0x10000;

            Assert.That(OpponentCollisionSelection.Select(car, [opponent], 1, 0), Is.Null);
        }

        [Test]
        public void GivenAxisAlignedSeparations_WhenMeasuring_ThenTheQuarterCombinationIsReturned()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            first[0x9C] = 0x40;
            first[0xA0] = 0x20;
            first[0xA4] = 0x10;

            int distance = OpponentCollisionSelection.Distance(first, second);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(distance, Is.EqualTo(0x4C));
                Assert.That(first[0x9C], Is.EqualTo(0x40));
                Assert.That(second[0x9C], Is.Zero);
            }

        }

        [Test]
        public void GivenNegativeSeparations_WhenMeasuring_ThenTheAbsoluteValuesAreUsed()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            first[0x9C] = -0x40;
            first[0xA0] = -0x20;
            first[0xA4] = -0x10;

            Assert.That(OpponentCollisionSelection.Distance(first, second), Is.EqualTo(0x4C));
        }

        [Test]
        public void GivenMissingInputs_WhenSelecting_ThenArgumentErrorsAreReported()
        {
            CarMemory car = BuildCar();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    () => OpponentCollisionSelection.Select(null!, [car], 1, 0),
                    Throws.ArgumentNullException);
                Assert.That(
                    () => OpponentCollisionSelection.Select(car, null!, 1, 0),
                    Throws.ArgumentNullException);
            }

        }

        private static CarMemory BuildCar()
        {
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car.WriteByte(0x8D, 1);
            car.WriteByte(0x1F4, 0x04);

            return car;
        }
    }
}