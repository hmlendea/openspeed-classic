using System;

namespace OpenSpeed.Classic.Physics
{
    public interface IVehicleScheduler
    {
        int BaseTick { get; set; }

        bool IsPaused { get; set; }

        void Register(VehicleSchedule schedule, int order, Action callback);

        void Remove(VehicleSchedule schedule, Action callback);

        void Advance(TimeSpan elapsed);

        void AdvanceOneTick();
    }
}