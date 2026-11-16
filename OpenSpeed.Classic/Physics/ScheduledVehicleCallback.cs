using System;

namespace OpenSpeed.Classic.Physics
{
    internal sealed class ScheduledVehicleCallback
    {
        public int Order { get; set; }

        public Action Callback { get; set; } = null!;
    }
}