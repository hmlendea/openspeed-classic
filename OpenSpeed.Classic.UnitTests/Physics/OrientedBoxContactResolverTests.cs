using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class OrientedBoxContactResolverTests
    {
        [Test]
        public void GivenASeparatedPair_WhenResolving_ThenNoCorrectionIsApplied()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = 32 * X86Math.One;
            second[0xA4] = 32 * X86Math.One;
            OrientedBoxScratch scratch = new();

            OrientedBoxContactResolver.Result result = OrientedBoxContactResolver.Resolve(first, second, scratch);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Iterations, Is.Zero);
                Assert.That(result.HasNormal, Is.False);
                Assert.That(first[0x9C], Is.Zero);
                Assert.That(second[0x9C], Is.EqualTo(32 * X86Math.One));
            }

        }

        [Test]
        public void GivenOverlappingBoxes_WhenResolving_ThenOneCorrectionIsAppliedToBothCars()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = 2 * X86Math.One;
            OrientedBoxScratch scratch = new();

            OrientedBoxContactResolver.Result result = OrientedBoxContactResolver.Resolve(first, second, scratch);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Iterations, Is.GreaterThanOrEqualTo(1));
                Assert.That(result.Point.First, Is.Not.Zero);
            }

        }

        [Test]
        public void GivenAnAcceptedCandidate_WhenResolving_ThenTheResultReportsTheSearchedPoint()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = 2 * X86Math.One;
            OrientedBoxScratch scratch = new();
            OrientedBoxScratchCalculator.Calculate(first, second, scratch);
            Assert.That(OrientedBoxContactSearch.TrySearch(first, second, scratch, out OrientedBoxContactCandidate? searched));

            OrientedBoxContactResolver.Result result = OrientedBoxContactResolver.Resolve(first, second, new OrientedBoxScratch());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Point.First, Is.EqualTo(searched!.Point.First));
                Assert.That(result.Point.Second, Is.EqualTo(searched.Point.Second));
                Assert.That(result.Point.Third, Is.EqualTo(searched.Point.Third));
            }

        }

        [Test]
        public void GivenAnIdentityBasisAndASmallDelta_WhenResolving_ThenTheMassSplitConstructorRuns()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = 2 * X86Math.One;
            OrientedBoxScratch scratch = new();

            OrientedBoxContactResolver.Result result = OrientedBoxContactResolver.Resolve(first, second, scratch);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Iterations, Is.EqualTo(1));
                Assert.That(first[0x9C], Is.Not.Zero);
            }

        }

        [Test]
        public void GivenMatchingMasses_WhenResolving_ThenTheCorrectionsAreMirrored()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = 2 * X86Math.One;
            OrientedBoxScratch scratch = new();
            int initialSeparation = 2 * X86Math.One;

            OrientedBoxContactResolver.Resolve(first, second, scratch);

            int finalSeparation = unchecked(second[0x9C] - first[0x9C]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first[0x9C], Is.Not.Zero);
                Assert.That(second[0x9C], Is.Not.EqualTo(initialSeparation));
                Assert.That(finalSeparation, Is.GreaterThan(initialSeparation));
            }

        }

        [Test]
        public void GivenMissingInputs_WhenResolving_ThenArgumentErrorsAreReported()
        {
            CarMemory car = BuildCar();
            OrientedBoxScratch scratch = new();

            Assert.That(
                () => OrientedBoxContactResolver.Resolve(null!, car, scratch),
                Throws.ArgumentNullException);
            Assert.That(
                () => OrientedBoxContactResolver.Resolve(car, null!, scratch),
                Throws.ArgumentNullException);
            Assert.That(
                () => OrientedBoxContactResolver.Resolve(car, car, null!),
                Throws.ArgumentNullException);
        }

        private static CarMemory BuildCar()
        {
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0x108] = X86Math.One;
            car[0x10C] = X86Math.One;
            car[0x110] = X86Math.One;
            car[0xB4] = X86Math.One;

            return car;
        }
    }
}