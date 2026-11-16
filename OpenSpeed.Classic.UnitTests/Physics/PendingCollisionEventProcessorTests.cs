using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class PendingCollisionEventProcessorTests
    {
        private static PendingCollisionEventProcessor.RaceProfile Profile => new(
            EventMagnitudeThreshold: 0x100000,
            CategoryTimer: 0x400,
            AngularSuppressionThreshold: 0x8000,
            EarlyRecoveryThreshold: 0x100);

        [Test]
        public void GivenNoEventAndNoTimer_WhenUpdating_ThenNothingIsConsumed()
        {
            CarMemory car = new();
            Recorder recorder = new();

            PendingCollisionEventProcessor.Update(car, Profile, recorder.Callbacks, 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorder.Restores, Is.Zero);
                Assert.That(recorder.Refreshes, Is.Zero);
                Assert.That(recorder.Repositions, Is.Zero);
                Assert.That(car[0x160], Is.Zero);
                Assert.That(car[0x578], Is.EqualTo(Profile.CategoryTimer));
            }

        }

        [Test]
        public void GivenAnEventBelowTheThreshold_WhenUpdating_ThenHeadersAreClearedWithoutResponse()
        {
            CarMemory car = new();
            car[0x160] = 0x100;
            car[0x164] = 0x1234;
            car[0x168] = 0x50001;
            Recorder recorder = new();

            PendingCollisionEventProcessor.Update(car, Profile, recorder.Callbacks, 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0x160], Is.Zero);
                Assert.That(car[0x164], Is.Zero);
                Assert.That(car[0x168], Is.Zero);
                Assert.That(recorder.Refreshes, Is.Zero);
                Assert.That(car[0x27C], Is.Zero);
            }

        }

        [Test]
        public void GivenAnEventAboveTheThreshold_WhenUpdating_ThenTheSevereResponseRuns()
        {
            CarMemory car = new();
            car[0x160] = 0x200000;
            car[0x164] = 0x1234;
            car[0x168] = 0x50001;
            Recorder recorder = new();

            PendingCollisionEventProcessor.Update(car, Profile, recorder.Callbacks, 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorder.Refreshes, Is.EqualTo(1));
                Assert.That(recorder.Restores, Is.EqualTo(1));
                Assert.That(car[0x27C], Is.EqualTo(1));
                Assert.That(car[0x58C], Is.EqualTo(1));
                Assert.That(car[0x578], Is.EqualTo(Profile.CategoryTimer - 1 - 8));
            }

        }

        [Test]
        public void GivenASpecialCarWithoutPartner_WhenUpdating_ThenTheSevereResponseIsBypassed()
        {
            CarMemory car = new();
            car[0x160] = 0x200000;
            car[0x168] = 0x50001;
            car.WriteByte(0x1F4, 0x40);
            Recorder recorder = new();

            PendingCollisionEventProcessor.Update(car, Profile, recorder.Callbacks, 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorder.Refreshes, Is.Zero);
                Assert.That(car[0x27C], Is.Zero);
                Assert.That(car[0x160], Is.Zero);
            }

        }

        [Test]
        public void GivenARecoverySuppressionTimer_WhenASevereEventArrives_ThenTheTimerBecomesTheFixedStep()
        {
            CarMemory car = new();
            car[0x160] = 0x200000;
            car[0x168] = 0x50001;
            car[0x17C] = 1;
            Recorder recorder = new();

            PendingCollisionEventProcessor.Update(car, Profile, recorder.Callbacks, 0);

            Assert.That(car[0x578], Is.EqualTo(0xFF));
        }

        [Test]
        public void GivenAPlayerRoleAndHighProjection_WhenTheTimerRuns_ThenTheDecrementIsSkipped()
        {
            CarMemory car = new();
            car.WriteByte(0x1F4, 0x04);
            car[0x100] = 0x1999;
            car[0x578] = 0x10;
            Recorder recorder = new();

            PendingCollisionEventProcessor.Update(car, Profile, recorder.Callbacks, 0);

            Assert.That(car[0x578], Is.EqualTo(8));
        }

        [Test]
        public void GivenASavedPositionAndNoCollisionCount_WhenUpdating_ThenTheSavedFlagClears()
        {
            CarMemory car = new();
            car[0x58C] = 1;
            Recorder recorder = new();

            PendingCollisionEventProcessor.Update(car, Profile, recorder.Callbacks, 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorder.Restores, Is.EqualTo(1));
                Assert.That(car[0x58C], Is.Zero);
            }

        }

        [Test]
        public void GivenASavedPositionAndANonzeroCollisionCount_WhenUpdating_ThenTheSavedFlagRemains()
        {
            CarMemory car = new();
            car[0x58C] = 1;
            car.WriteWord(0x14C, 1);
            Recorder recorder = new();

            PendingCollisionEventProcessor.Update(car, Profile, recorder.Callbacks, 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorder.Restores, Is.EqualTo(1));
                Assert.That(car[0x58C], Is.EqualTo(1));
            }

        }

        [Test]
        public void GivenAnExpiredTimerAndCollisionCount_WhenUpdating_ThenTheAngularKickApplies()
        {
            CarMemory car = new();
            car.WriteWord(0x14C, 1);
            Recorder recorder = new();

            PendingCollisionEventProcessor.Update(car, Profile, recorder.Callbacks, 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0xF0], Is.EqualTo(0x51E));
                Assert.That(car[0xE8], Is.EqualTo(0x28F));
                Assert.That(car[0x578], Is.EqualTo(Profile.CategoryTimer));
            }

        }

        [Test]
        public void GivenPresentationTimers_WhenUpdating_ThenTheyDecrementTowardZero()
        {
            CarMemory car = new();
            car[0x580] = 2;
            car[0x588] = 1;
            Recorder recorder = new();

            PendingCollisionEventProcessor.Update(car, Profile, recorder.Callbacks, 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(car[0x580], Is.EqualTo(1));
                Assert.That(car[0x588], Is.Zero);
            }

        }

        private sealed class Recorder
        {
            public int Refreshes { get; private set; }

            public int Restores { get; private set; }

            public int Repositions { get; private set; }

            public PendingCollisionEventProcessor.Callbacks Callbacks => new(
                _ => Refreshes += 1,
                _ => { },
                _ => Restores += 1,
                _ => Repositions += 1,
                _ => { });
        }
    }
}