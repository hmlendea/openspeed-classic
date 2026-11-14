using System;

namespace OpenSpeed.Classic.Physics
{
    public static class ForceDispatcher
    {
        private static int MinimumContactProjection => 0x199A;

        private static int EngineDecay => 0x1F4;

        private static int CorrectionGate => 0x8000;

        public static void Update(
            CarMemory car,
            PhysicsContext context,
            Action<CarMemory> opponentCollision,
            Action<CarMemory> mainSolver,
            Action<CarMemory> alternateSolver)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(opponentCollision);
            ArgumentNullException.ThrowIfNull(mainSolver);
            ArgumentNullException.ThrowIfNull(alternateSolver);

            if ((context.CollisionFlags & 8) != 0 && car.ReadByte(0x2DD) != 0)
            {
                opponentCollision(car);
            }

            if (car[0x100] < MinimumContactProjection)
            {
                car[0x310] = 0;
                car[0x324] = 0;
                car[0x328] = 0;
                car[0x2F4] = 0;
                int engine = 0;

                if (car[0x2F0] > EngineDecay)
                {
                    engine = unchecked(car[0x2F0] - EngineDecay);
                }

                car[0x2F0] = engine;

                if (car[0x15C] < CorrectionGate)
                {
                    ApplyLowProjectionDamping(car);
                }

                return;
            }

            if (context.Mode != 0)
            {
                alternateSolver(car);
            }
            else
            {
                mainSolver(car);
            }
        }

        public static void ApplyLowProjectionDamping(CarMemory car)
        {
            FixedVectors.Write(car, 0xA8, FixedVectors.Scale(FixedVectors.Read(car, 0xA8), 0xF0A3));

            if (car[0x100] < 0x3333)
            {
                car[0xEC] = X86Math.MultiplyQ16(car[0xEC], 0xFAE1);
            }
        }
    }
}