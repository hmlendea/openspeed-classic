using System;

namespace OpenSpeed.Classic.Physics
{
    public static class TyreStateMachine
    {
        private static int EngineOffset => 0x2F0;
        private static int GearOffset => 0x2DA;
        private static int ShiftDelayOffset => 0x2DB;
        private static int ResponseStateOffset => 0x2F4;
        private static int ResponseFlagOffset => 0x2F8;
        private static int ResponseCounterOffset => 0x2FC;
        private static int GripOffset => 0x304;
        private static int ContactForceOffset => 0x30C;
        private static int NormalisedForceOffset => 0x310;
        private static int SurfaceOffset => 0x200;
        private static int AcceleratorSmoothedOffset => 0x2D7;
        private static int CarIndexOffset => 0x1E8;
        private static int InputComparisonOffset => 0x14C;
        private static int CategoryOffset => 0x2DA;
        private static int TargetSteeringOffset => 0x2E0;
        private static int SmoothedSteeringOffset => 0x2E4;
        private static int CountdownOffset => 0x2E8;
        private static int DescriptorOffset => 0x2EC;
        private static int VelocityProjectedOffset => 0x2B8;
        private static int GravityThirdOffset => 0x2C4;

        public static int Update(
            CarMemory car,
            CarSpecifications descriptor,
            CarRuntimeType runtimeType,
            PhysicsContext context)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentNullException.ThrowIfNull(runtimeType);
            ArgumentNullException.ThrowIfNull(context);

            int transition = 0;
            int secondaryResponse = 0;
            int count = car[EngineOffset];
            int limit = descriptor[0xF0];

            if (count > limit)
            {
                transition = 1;
                int transitionLimit = unchecked(limit + X86Math.TruncatePowerOfTwo(limit, 1));
                if (count > transitionLimit)
                {
                    car.WriteByte(0x2DC, 1);
                }
            }

            int baseScale = unchecked(
                X86Math.TruncatePowerOfTwo(
                    unchecked(runtimeType[0x1C] + runtimeType[0x20] - 0x20000), 3) + X86Math.One);
            int scaled = X86Math.MultiplyQ16(car[VelocityProjectedOffset], baseScale);
            int integer = X86Math.TruncatePowerOfTwo(scaled, 16);
            int cube = unchecked(integer * integer);
            cube = unchecked(cube * integer);
            int baseResponse = X86Math.MultiplyQ16(descriptor[0x1C8], cube);

            if ((context.FeatureFlags & 4) != 0 && runtimeType[0] < 0x0F)
            {
                baseResponse = X86Math.TruncatePowerOfTwo(baseResponse, 1);
            }

            int target = X86Math.MultiplyQ16(descriptor[0xF0], PhysicsTables.EngineTargetScalar);
            int category = car.ReadByte(CategoryOffset);

            if (category == 1 || car.ReadByte(ShiftDelayOffset) > 0 || car.ReadWord(InputComparisonOffset) != 0)
            {
                goto ApproachTarget;
            }

            if (PhysicsTables.OpponentFlags[car[CarIndexOffset] * 16] == 1)
            {
                AiForceController.ApplyProportional(car, 0, 0, 0, 0, 0, 0, 0, 0, 0, new FixedVector());
            }

            int categoryInteger = X86Math.TruncatePowerOfTwo(
                X86Math.MultiplyQ16(car[VelocityProjectedOffset], descriptor[0x0C + category * sizeof(int)]), 16);

            int coefficient = descriptor[0x1A8 + category * sizeof(int)];
            int source;

            if (transition != 0)
            {
                int index = X86Math.TruncatePowerOfTwo(descriptor[0xF0], 8);
                source = descriptor[0x4C + index * sizeof(int)];
            }
            else
            {
                int index = X86Math.TruncatePowerOfTwo(car[EngineOffset], 8);
                source = AiEngineController.SelectTargetScalar(car, false);
            }

            secondaryResponse = X86Math.MultiplyQ16(source, coefficient);

            if ((context.FeatureFlags & 4) == 0 || runtimeType[0] >= 0x0F)
            {
                int cap = category == 2 ? 0xA0000 : 0x80000;
                if (secondaryResponse > cap)
                {
                    secondaryResponse = cap;
                }
            }

            int signedTargetDifference = unchecked(target - categoryInteger);
            int absoluteTargetDifference = signedTargetDifference;
            if (absoluteTargetDifference <= 0)
            {
                absoluteTargetDifference = unchecked(-absoluteTargetDifference);
            }

            if (absoluteTargetDifference < 0x7D && target < unchecked(limit - 0x12C))
            {
                signedTargetDifference = 0;
            }

            int savedDifference = unchecked(car[EngineOffset] - categoryInteger);
            car[ResponseStateOffset] = 0;
            car[ResponseFlagOffset] = 0;

            if (savedDifference > 0x4E2 && car.ReadByte(ShiftDelayOffset) == 0 && category <= 4 ||
                category > 1 && car[GripOffset] < unchecked((int)0xFFFFE667) && car.ReadByte(AcceleratorSmoothedOffset) > 0x40 ||
                category == 0 && car[GripOffset] > 0x1999 && car.ReadByte(AcceleratorSmoothedOffset) > 0x40)
            {
                goto MismatchResponse;
            }

            if (signedTargetDifference < 0)
            {
                secondaryResponse = unchecked(-X86Math.MultiplyQ16(secondaryResponse, descriptor[0x144]));

                if (PhysicsTables.GravityThirdSign > 0 && category > 1 && secondaryResponse < 0)
                {
                    if (category < 3)
                    {
                        secondaryResponse = X86Math.TruncatePowerOfTwo(secondaryResponse, 1);
                    }
                }
                else if (PhysicsTables.GravityThirdSign < 0 && category == 0 && secondaryResponse > 0)
                {
                    secondaryResponse = X86Math.TruncatePowerOfTwo(secondaryResponse, 1);
                }

                int rawStep = unchecked(descriptor[0x188 + category * sizeof(int)] << 3);
                int maximumStep = X86Math.TruncatePowerOfTwo(X86Math.MultiplyQ16(rawStep, 0x28000000), 16);
                int requestedStep = unchecked(-savedDifference);
                int step = requestedStep <= maximumStep ? requestedStep : maximumStep;
                car[EngineOffset] = unchecked(car[EngineOffset] + step);
                if (target > car[EngineOffset])
                {
                    car[EngineOffset] = target;
                }
                goto NegativeCounterClamp;
            }

            if (signedTargetDifference == 0)
            {
                car[EngineOffset] = categoryInteger;
                secondaryResponse = baseResponse;
                goto NegativeCounterClamp;
            }

            if (savedDifference > 0xC8)
            {
                car[EngineOffset] = unchecked(car[EngineOffset] - 0xC8);
            }
            else if (savedDifference < -0x12C)
            {
                car[EngineOffset] = unchecked(car[EngineOffset] + 0xC8);
            }
            else
            {
                car[EngineOffset] = categoryInteger;
            }

            if (target < car[EngineOffset])
            {
                car[EngineOffset] = target;
            }

            secondaryResponse = X86Math.MultiplyQ16(secondaryResponse, PhysicsTables.EngineTargetScalar);
            int factor = unchecked(X86Math.One + X86Math.MultiplyQ16(X86Math.Abs(car[NormalisedForceOffset]), 0x8000));
            if (factor >= 0x28000)
            {
                factor = 0x28000;
            }
            secondaryResponse = X86Math.MultiplyQ16(secondaryResponse, factor);
            goto NegativeCounterClamp;

        MismatchResponse:
            if ((context.FeatureFlags & 4) != 0 && runtimeType[0] < 0x0F)
            {
                car[ResponseFlagOffset] = 1;
            }
            car[ResponseStateOffset] = 1;
            secondaryResponse = X86Math.MultiplyQ16(secondaryResponse, unchecked(PhysicsTables.EngineTargetScalar + X86Math.One));
            int decrement = category == 2 ? 5 : category == 3 ? 0x19 : 0x4B;
            if (decrement >= savedDifference)
            {
                decrement = savedDifference;
            }
            car[EngineOffset] = unchecked(car[EngineOffset] - decrement);
            if (target < car[EngineOffset])
            {
                car[EngineOffset] = target;
            }
            goto NegativeCounterClamp;

        ApproachTarget:
            count = car[EngineOffset];
            if (target > count && car.ReadByte(ShiftDelayOffset) == 0)
            {
                count = unchecked(count + 0x1F4);
                if (target < count)
                {
                    count = target;
                }
            }
            else if (car.ReadByte(ShiftDelayOffset) > 0)
            {
                count = unchecked(count - 0x32);
                if (count < 0)
                {
                    count = 0;
                }
            }
            else if (target <= count)
            {
                count = unchecked(count - 0xC8);
                if (target > count)
                {
                    count = target;
                }
            }

            car[EngineOffset] = count;
            car[ResponseStateOffset] = 0;
            car[ResponseFlagOffset] = 0;
            return unchecked(0 - baseResponse);

        NegativeCounterClamp:
            if (car[EngineOffset] >= 0)
            {
                return unchecked(secondaryResponse - baseResponse);
            }

            int boundary = unchecked(-(car[VelocityProjectedOffset] << 5));
            if (secondaryResponse > 0 && boundary > 0 && unchecked(secondaryResponse - boundary) > 0)
            {
                return unchecked(secondaryResponse - baseResponse);
            }

            if (secondaryResponse < 0 && boundary < 0 && unchecked(secondaryResponse - boundary) < 0)
            {
                return unchecked(secondaryResponse - baseResponse);
            }

            secondaryResponse = boundary;
            car[EngineOffset] = 0;
            return unchecked(secondaryResponse - baseResponse);
        }
    }
}