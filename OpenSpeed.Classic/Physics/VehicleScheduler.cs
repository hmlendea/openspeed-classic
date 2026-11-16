using System;
using System.Collections.Generic;

namespace OpenSpeed.Classic.Physics
{
    public sealed class VehicleScheduler : IVehicleScheduler
    {
        private readonly List<ScheduledVehicleCallback> callbacks32 = [];
        private readonly List<ScheduledVehicleCallback> callbacks64 = [];
        private long elapsedTicks;

        public int BaseTick { get; set; }

        public bool IsPaused { get; set; }

        private static long BaseTickDuration => TimeSpan.TicksPerSecond / 64;

        public void Register(VehicleSchedule schedule, int order, Action callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            List<ScheduledVehicleCallback> records = GetCallbacks(schedule);
            int index = records.FindIndex(record => record.Order > order);

            if (index < 0)
            {
                index = records.Count;
            }

            records.Insert(index, new ScheduledVehicleCallback { Order = order, Callback = callback });
        }

        public void Remove(VehicleSchedule schedule, Action callback)
        {
            List<ScheduledVehicleCallback> records = GetCallbacks(schedule);
            int index = records.FindIndex(record => Equals(record.Callback, callback));

            if (index >= 0)
            {
                records.RemoveAt(index);
            }
        }

        public void Advance(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsed), elapsed, "Simulation elapsed time must be non-negative.");
            }

            if (IsPaused)
            {
                return;
            }

            elapsedTicks = checked(elapsedTicks + elapsed.Ticks);

            while (elapsedTicks >= BaseTickDuration && !IsPaused)
            {
                AdvanceOneTick();
                elapsedTicks -= BaseTickDuration;
            }
        }

        public void AdvanceOneTick()
        {
            if (IsPaused)
            {
                return;
            }

            if ((BaseTick & 1) == 0)
            {
                Execute(callbacks32);
            }

            Execute(callbacks64);
            BaseTick = unchecked(BaseTick + 1);
        }

        private List<ScheduledVehicleCallback> GetCallbacks(VehicleSchedule schedule)
            => schedule switch
            {
                VehicleSchedule.Sc32 => callbacks32,
                VehicleSchedule.Sc64 => callbacks64,
                _ => throw new ArgumentOutOfRangeException(nameof(schedule), schedule, "The vehicle schedule is unsupported.")
            };

        private static void Execute(List<ScheduledVehicleCallback> callbacks)
        {
            foreach (ScheduledVehicleCallback record in callbacks)
            {
                record.Callback();
            }
        }
    }
}