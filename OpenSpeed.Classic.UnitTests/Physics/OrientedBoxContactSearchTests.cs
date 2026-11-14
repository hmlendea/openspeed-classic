using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class OrientedBoxContactSearchTests
    {
        [Test]
        public void GivenOverlappingAxisAlignedBoxes_WhenSearching_ThenTheFirstAxisCandidateAccepts()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            OrientedBoxScratch scratch = BuildScratch(first, second);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                            OrientedBoxContactSearch.TrySearch(first, second, scratch, out OrientedBoxContactCandidate result));
                Assert.That(result.Routine, Is.EqualTo(OrientedBoxContactCandidateRoutine.AxisCornerFromSecond));
                Assert.That(result.Point.First, Is.EqualTo(X86Math.One));
                Assert.That(result.Point.Second, Is.Zero);
                Assert.That(result.Point.Third, Is.Zero);
                Assert.That(result.Normal, Is.Null);
            }

        }

        [Test]
        public void GivenAnAxisAlignedPairInTheNegatedRange_WhenSearching_ThenTheCornerComesFromTheSecondBox()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = -2 * X86Math.One;
            second[0xA0] = -4 * X86Math.One;
            OrientedBoxScratch scratch = BuildScratch(first, second);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                            OrientedBoxContactSearch.TrySearch(first, second, scratch, out OrientedBoxContactCandidate result));
                Assert.That(result.Routine, Is.EqualTo(OrientedBoxContactCandidateRoutine.AxisCornerFromSecond));
                Assert.That(result.Point, Is.Not.Null);
                Assert.That(result.Normal, Is.Null);
            }

        }

        [Test]
        public void GivenAnAxisAlignedPairRequiringSignTriples_WhenSearching_ThenTheSignCandidateAccepts()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = -2 * X86Math.One;
            second[0xA0] = 3 * X86Math.One;
            OrientedBoxScratch scratch = BuildScratch(first, second);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                            OrientedBoxContactSearch.TrySearch(first, second, scratch, out OrientedBoxContactCandidate result));
                Assert.That(result.Routine, Is.EqualTo(OrientedBoxContactCandidateRoutine.SignCornerFromSecond));
                Assert.That(result.Normal, Is.Not.Null);
            }

        }

        [Test]
        public void GivenASeparatedPair_WhenSearching_ThenNoCandidateIsAccepted()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = 32 * X86Math.One;
            second[0xA4] = 32 * X86Math.One;
            OrientedBoxScratch scratch = BuildScratch(first, second);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                            OrientedBoxContactSearch.TrySearch(first, second, scratch, out OrientedBoxContactCandidate result),
                            Is.False);
                Assert.That(result, Is.Null);
            }

        }

        [Test]
        public void GivenAMirroredProbePair_WhenSearching_ThenTheFirstBoxSuppliesTheCorner()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildRotatedCar();
            second[0x9C] = -6 * X86Math.One;
            second[0xA0] = -2 * X86Math.One;
            second[0xA4] = -2 * X86Math.One;
            OrientedBoxScratch scratch = BuildScratch(first, second);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                            OrientedBoxContactSearch.TrySearch(first, second, scratch, out OrientedBoxContactCandidate result));
                Assert.That(result.Routine, Is.EqualTo(OrientedBoxContactCandidateRoutine.AxisCornerFromFirst));
                Assert.That(result.Point, Is.Not.Null);
                Assert.That(result.Normal, Is.Null);
            }

        }

        [Test]
        public void GivenAMirroredSignProbePair_WhenSearching_ThenTheNegatedFirstNormalIsEmitted()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildRotatedCar();
            second[0x9C] = -6 * X86Math.One;
            second[0xA0] = -4 * X86Math.One;
            second[0xA4] = -2 * X86Math.One;
            OrientedBoxScratch scratch = BuildScratch(first, second);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                            OrientedBoxContactSearch.TrySearch(first, second, scratch, out OrientedBoxContactCandidate result));
                Assert.That(result.Routine, Is.EqualTo(OrientedBoxContactCandidateRoutine.SignCornerFromFirst));
                Assert.That(result.Normal, Is.Not.Null);
            }

        }

        [Test]
        public void GivenMissingInputs_WhenSearching_ThenArgumentErrorsAreReported()
        {
            CarMemory car = BuildCar();
            OrientedBoxScratch scratch = new();

            Assert.That(
                () => OrientedBoxContactSearch.TrySearch(null!, car, scratch, out _),
                Throws.ArgumentNullException);
            Assert.That(
                () => OrientedBoxContactSearch.TrySearch(car, null!, scratch, out _),
                Throws.ArgumentNullException);
            Assert.That(
                () => OrientedBoxContactSearch.TrySearch(car, car, null!, out _),
                Throws.ArgumentNullException);
        }

        private static OrientedBoxScratch BuildScratch(CarMemory first, CarMemory second)
        {
            OrientedBoxScratch scratch = new();
            OrientedBoxScratchCalculator.Calculate(first, second, scratch);

            return scratch;
        }

        private static CarMemory BuildCar()
        {
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0x108] = X86Math.One;
            car[0x10C] = 2 * X86Math.One;
            car[0x110] = 3 * X86Math.One;

            return car;
        }

        private static CarMemory BuildRotatedCar()
        {
            CarMemory car = BuildCar();
            FixedMatrix basis = new();
            basis[0, 2] = X86Math.One;
            basis[1, 1] = X86Math.One;
            basis[2, 0] = -X86Math.One;
            FixedMatrices.Write(car, 0xC4, basis);

            return car;
        }
    }
}