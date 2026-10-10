using System;

namespace OpenSpeed.Classic.Physics
{
    public static class TyreResponseFinaliser
    {
        private static int ResponseStateOffset => 0x2F4;

        private static int ContactForceOffset => 0x30C;

        private static int SurfaceOffset => 0x200;

        private static int SurfaceCount => 10;

        private static int MaximumResponseFactor => 0x30000;

        private static int HalfResponseFactor => 0x8000;

        private static int EngineLimitOffset => 0xF0;

        private static int GearOffset => 0x2DA;

        private static int PreviousGearOffset => 0x2D9;

        private static int ReciprocalRatioOffset => 0x188;

        private static int GripOffset => 0x304;

        public static int ScaleRequestedForce(CarMemory car, PhysicsContext context, int requestedForce)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(context);

            int state = X86Math.Abs(car[ResponseStateOffset]);

            if (state == 3)
            {
                int factor = Math.Min(unchecked(X86Math.One + X86Math.MultiplyQ16(
                    X86Math.Abs(unchecked(car[ContactForceOffset] * 2)), context.Accelerator)), MaximumResponseFactor);
                requestedForce = X86Math.MultiplyQ16(requestedForce, factor);
            }
            else if (state == 1 || state == 2)
            {
                requestedForce = X86Math.MultiplyQ16(requestedForce, HalfResponseFactor);
            }

            return X86Math.MultiplyQ16(requestedForce,
                PhysicsTables.SurfaceCoefficients[context.Mode * SurfaceCount + car[SurfaceOffset]]);
        }

        public static void FinaliseGrip(
            CarMemory car,
            CarSpecifications descriptor,
            int wheelTarget,
            int signedAdjustment,
            bool isTransition)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(descriptor);

            int adjustment = X86Math.TruncatePowerOfTwo(signedAdjustment, 5);
            int originalTarget = wheelTarget;
            wheelTarget = unchecked(wheelTarget + adjustment);

            if (!isTransition &&
                (adjustment < 0 && originalTarget > 0 && wheelTarget < 0 ||
                 adjustment > 0 && originalTarget < 0 && wheelTarget > 0 ||
                 adjustment < 0 && originalTarget == 0))
            {
                wheelTarget = 0;
            }

            int limit = descriptor[EngineLimitOffset];

            if (adjustment > 0)
            {
                if (wheelTarget >= limit)
                {
                    wheelTarget = limit;
                }
            }
            else if (wheelTarget < unchecked(-limit))
            {
                wheelTarget = unchecked(-limit);
            }

            int ratio = X86Math.DivideQ16(unchecked(wheelTarget << 16), unchecked(limit << 16));
            int selector = car.ReadByte(GearOffset);

            if (selector == 1)
            {
                selector = car.ReadByte(PreviousGearOffset);
            }

            car[GripOffset] = X86Math.MultiplyQ16(ratio, X86Math.MultiplyQ16(
                unchecked(limit << 16), descriptor[ReciprocalRatioOffset + selector * sizeof(int)]));
        }
    }
}