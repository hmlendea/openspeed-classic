using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class WorldContactResponseTests
    {
        [TestCase(65535, true, 65535)]
        [TestCase(65536, true, 65536)]
        [TestCase(65537, true, 65537)]
        [TestCase(0, true, 32768)]
        [TestCase(0, false, 0)]
        public void GivenPlaneSeparation_WhenResolving_ThenOnlyPenetrationCanCorrectPosition(
            int height,
            bool isCorrectionEnabled,
            int expectedHeight)
        {
            CarMemory car = BuildCar();
            car[0xA0] = height;
            car[0x160] = 613;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                            WorldContactResponse.ApplyPlane(
                                car, new FixedVector { Second = X86Math.One }, new FixedVector(), isCorrectionEnabled),
                            Is.False);
                Assert.That(car[0xA0], Is.EqualTo(expectedHeight));
                Assert.That(car[0x160], Is.EqualTo(613));
                Assert.That(car[0xAC], Is.Zero);
            }

        }

        [TestCase(true)]
        [TestCase(false)]
        public void GivenInwardContactVelocity_WhenResolving_ThenTheNativeImpulseRecordsAWorldEvent(
            bool isCorrectionEnabled)
        {
            CarMemory car = BuildCar();
            car[0xAC] = -X86Math.One;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WorldContactResponse.ApplyPlane(
                            car, new FixedVector { Second = X86Math.One }, new FixedVector(), isCorrectionEnabled));
                Assert.That(car[0x160], Is.GreaterThan(0));
                Assert.That(car[0x168], Is.EqualTo(0x30000));
                Assert.That(car[0x164], Is.Zero);
                Assert.That(car[0x174], Is.EqualTo(-X86Math.One));
                Assert.That(car[0xAC], Is.GreaterThan(-X86Math.One));
            }

        }

        [Test]
        public void GivenOutwardContactVelocity_WhenResolving_ThenThereIsNoImpulseButVerticalEnergyIsAdjusted()
        {
            CarMemory car = BuildCar();
            car[0xAC] = X86Math.One;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WorldContactResponse.ApplyPlane(
                            car, new FixedVector { Second = X86Math.One }, new FixedVector(), true), Is.False);
                Assert.That(car[0x160], Is.Zero);
                Assert.That(car[0xAC], Is.Zero);
            }

        }

        [Test]
        public void GivenAZeroNormal_WhenResolving_ThenUpwardFallbackDoesNotMutateTheCallerNormal()
        {
            CarMemory car = BuildCar();
            FixedVector normal = new();

            WorldContactResponse.ApplyPlane(car, normal, new FixedVector(), true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0xA0], Is.EqualTo(0x8000));
                Assert.That(normal.First, Is.Zero);
                Assert.That(normal.Second, Is.Zero);
                Assert.That(normal.Third, Is.Zero);
            }

        }

        private static CarMemory BuildCar()
        {
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0x108] = X86Math.One;
            car[0x10C] = X86Math.One;
            car[0x110] = X86Math.One;
            car[0xB8] = X86Math.One;
            car[0xF8] = X86Math.One;

            return car;
        }
    }
}