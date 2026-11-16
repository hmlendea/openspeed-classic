using System;

namespace OpenSpeed.Classic.Physics
{
    public interface IPlayerVehicleSimulation
    {
        CarMemory State { get; }

        bool IsGrounded { get; }

        void Advance(TimeSpan elapsed, float movement, float steering, bool isHandbrakeApplied);
    }
}