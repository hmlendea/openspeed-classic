using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class PairCollisionSolverTests
    {
        [Test]
        public void GivenASeparatedPair_WhenResolving_ThenNoImpulseIsApplied()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = 32 * X86Math.One;
            second[0xA4] = 32 * X86Math.One;
            OrientedBoxScratch scratch = new();
            int refreshes = 0;

            bool resolved = PairCollisionSolver.Resolve(
                first, second, scratch, _ => refreshes += 1, _ => { }, 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolved, Is.False);
                Assert.That(refreshes, Is.Zero);
                Assert.That(first[0x160], Is.Zero);
                Assert.That(second[0x160], Is.Zero);
            }

        }

        [Test]
        public void GivenOverlappingStaticBoxes_WhenResolving_ThenTheZeroResponseImpulseIsAccepted()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = 2 * X86Math.One;
            OrientedBoxScratch scratch = new();

            bool resolved = PairCollisionSolver.Resolve(first, second, scratch, _ => { }, _ => { }, 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolved, Is.True);
                Assert.That(first[0xA8], Is.Zero);
                Assert.That(second[0xA8], Is.Zero);
                Assert.That(first[0xE8], Is.Zero);
                Assert.That(second[0xE8], Is.Zero);
            }

        }

        [Test]
        public void GivenApproachingBoxes_WhenResolving_ThenTheImpulseIsAppliedAndEventCodesAreWritten()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = X86Math.One;
            second[0xA8] = -2 * X86Math.One;
            OrientedBoxScratch scratch = new();
            int refreshes = 0;

            bool resolved = PairCollisionSolver.Resolve(
                first, second, scratch, _ => refreshes += 1, _ => { }, 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolved, Is.True);
                Assert.That(refreshes, Is.GreaterThanOrEqualTo(2));
                Assert.That(first[0x168], Is.EqualTo(0x50001));
                Assert.That(second[0x168], Is.EqualTo(0x50001));
                Assert.That(first[0x164], Is.Not.Zero);
                Assert.That(second[0x164], Is.Not.Zero);
            }

        }

        [Test]
        public void GivenApproachingBoxes_WhenResolving_ThenCollisionCountersIncrement()
        {
            CarMemory first = BuildCar();
            CarMemory second = BuildCar();
            second[0x9C] = X86Math.One;
            second[0xA8] = -2 * X86Math.One;
            OrientedBoxScratch scratch = new();

            PairCollisionSolver.Resolve(first, second, scratch, _ => { }, _ => { }, 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.ReadWord(0x14C), Is.GreaterThan(0));
                Assert.That(second.ReadWord(0x14C), Is.GreaterThan(0));
            }

        }

        [Test]
        public void GivenMissingInputs_WhenResolving_ThenArgumentErrorsAreReported()
        {
            CarMemory car = BuildCar();
            OrientedBoxScratch scratch = new();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    () => PairCollisionSolver.Resolve(null!, car, scratch, _ => { }, _ => { }, 0),
                    Throws.ArgumentNullException);
                Assert.That(
                    () => PairCollisionSolver.Resolve(car, null!, scratch, _ => { }, _ => { }, 0),
                    Throws.ArgumentNullException);
                Assert.That(
                    () => PairCollisionSolver.Resolve(car, car, null!, _ => { }, _ => { }, 0),
                    Throws.ArgumentNullException);
                Assert.That(
                    () => PairCollisionSolver.Resolve(car, car, scratch, null!, _ => { }, 0),
                    Throws.ArgumentNullException);
                Assert.That(
                    () => PairCollisionSolver.Resolve(car, car, scratch, _ => { }, null!, 0),
                    Throws.ArgumentNullException);
            }

        }

        private static CarMemory BuildCar()
        {
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0x108] = X86Math.One;
            car[0x10C] = X86Math.One;
            car[0x110] = X86Math.One;
            car[0xB4] = X86Math.One;
            car[0xB8] = X86Math.One;
            car[0xF8] = X86Math.One;

            return car;
        }
    }
}