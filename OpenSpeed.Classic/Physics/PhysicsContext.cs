using System;

namespace OpenSpeed.Classic.Physics
{
    public sealed class PhysicsContext
    {
        public int Mode { get; set; }

        public int RaceMode { get; set; }

        public int PlayerIndex { get; set; }

        public byte FeatureFlags { get; set; }

        public byte CollisionFlags { get; set; }

        public int InputEnabled { get; set; }

        public int BaseTick { get; set; }

        public int HeadingDifference { get; set; }

        public int Accelerator { get; set; }

        public int Brake { get; set; }

        public int Steering { get; set; }

        public int SteeringScale { get; set; } = 0x1999;

        public int RearCoefficient { get; set; }

        public int SurfaceCoefficient { get; set; }

        public int GravityFirst { get; set; }

        public int GravitySecond { get; set; }

        public int GravityThird { get; set; }

        public int FrontCoefficient { get; set; }

        public Action<CarMemory, int>? GearChanged { get; set; }

        public Action<CarMemory>? RecoveryRequested { get; set; }
    }
}