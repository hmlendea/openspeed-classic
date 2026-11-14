using System;
using System.Collections.Generic;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class VehicleSchedulerTests
    {
        [Test]
        public void GivenOrderedCallbacks_WhenAdvancingTicks_ThenCadenceAndRegistrationOrderArePreserved()
        {
            List<string> calls = [];
            VehicleScheduler scheduler = new();
            scheduler.Register(VehicleSchedule.Sc32, 0x1E, () => calls.Add("forces"));
            scheduler.Register(VehicleSchedule.Sc32, 0x15, () => calls.Add("controls"));
            scheduler.Register(VehicleSchedule.Sc32, 0x1E, () => calls.Add("contacts"));
            scheduler.Register(VehicleSchedule.Sc64, 0x1E, () => calls.Add("position"));
            scheduler.AdvanceOneTick();
            scheduler.AdvanceOneTick();
            scheduler.AdvanceOneTick();

            Assert.Multiple(() =>
            {
                Assert.That(calls, Is.EqualTo(new[]
                {
                    "controls", "forces", "contacts", "position", "position",
                    "controls", "forces", "contacts", "position"
                }));
                Assert.That(scheduler.BaseTick, Is.EqualTo(3));
            });
        }

        [Test]
        public void GivenPauseAndCounterWrap_WhenAdvancing_ThenPausedTicksAreNotAccumulated()
        {
            int calls = 0;
            VehicleScheduler scheduler = new() { BaseTick = int.MaxValue, IsPaused = true };
            scheduler.Register(VehicleSchedule.Sc64, 0, () => calls += 1);
            scheduler.Advance(TimeSpan.FromSeconds(1));
            scheduler.AdvanceOneTick();

            Assert.That(scheduler.BaseTick, Is.EqualTo(int.MaxValue));

            scheduler.IsPaused = false;
            scheduler.Advance(TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 64));

            Assert.Multiple(() =>
            {
                Assert.That(scheduler.BaseTick, Is.EqualTo(int.MinValue));
                Assert.That(calls, Is.EqualTo(1));
            });
        }

        [Test]
        public void GivenFragmentedElapsedTime_WhenAdvancing_ThenTheFixedTickCountIsUnchanged()
        {
            VehicleScheduler scheduler = new();

            for (int frame = 0; frame < 100; frame += 1)
            {
                scheduler.Advance(TimeSpan.FromMilliseconds(10));
            }

            Assert.That(scheduler.BaseTick, Is.EqualTo(64));
        }

        [Test]
        public void GivenDuplicateRegistrations_WhenRemoving_ThenOnlyTheFirstIsRemoved()
        {
            int calls = 0;
            Action callback = () => calls += 1;
            VehicleScheduler scheduler = new();
            scheduler.Register(VehicleSchedule.Sc64, 0, callback);
            scheduler.Register(VehicleSchedule.Sc64, 0, callback);
            scheduler.Remove(VehicleSchedule.Sc64, callback);
            scheduler.AdvanceOneTick();

            Assert.That(calls, Is.EqualTo(1));
        }
    }
}