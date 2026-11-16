using System;

namespace OpenSpeed.Classic.Physics
{
    public static class ControlPipeline
    {
        private static int AcceleratorTargetOffset => 0x2D4;

        private static int BrakeTargetOffset => 0x2D5;

        private static int GearTargetOffset => 0x2D6;

        private static int AcceleratorOffset => 0x2D7;

        private static int BrakeOffset => 0x2D8;

        private static int GearOffset => 0x2DA;

        private static int ShiftDelayOffset => 0x2DB;

        private static int SteeringTargetOffset => 0x2E0;

        private static int SteeringOffset => 0x2E4;

        private static int CarIndexOffset => 0x1E8;

        private static int RaceModeOffset => 0x280;

        private static int ContactEventOffset => 0x284;

        private static int RuntimeTypeOffset => 0x21C;

        private static int DescriptorOffset => 0x2EC;

        private static int EngineSpeedOffset => 0x2F0;

        private static int CurrentGearOffset => 0x2DA;

        private static int TargetGearOffset => 0x2D6;

        private static int PreviousGearOffset => 0x2D9;

        private static int ShiftDelayOffset2 => 0x2DB;

        private static int SteeringTargetOffset2 => 0x2E0;

        private static int SteeringOffset2 => 0x2E4;

        private static int AcceleratorTargetOffset2 => 0x2D4;

        private static int BrakeTargetOffset2 => 0x2D5;

        private static int AcceleratorOffset2 => 0x2D7;

        private static int BrakeOffset2 => 0x2D8;

        private static int ProjectedNormalOffset => 0x2B8;

        private static int GlobalMode => PhysicsTables.GlobalMode;

        public static void SmoothAlternate(
            CarMemory car,
            CarSpecifications descriptor,
            CarRuntimeType runtimeType,
            PhysicsContext context)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentNullException.ThrowIfNull(runtimeType);
            ArgumentNullException.ThrowIfNull(context);

            int mode = GlobalMode;

            if (mode == 1)
            {
                if (car[CarIndexOffset] != 0)
                {
                    context.Accelerator = 0x10000;
                }
                else
                {
                    context.Accelerator = unchecked((int)0xFFFF0000);
                }
            }
            else
            {
                context.Accelerator = 0;
            }

            if (car[RaceModeOffset] == 2)
            {
                car.WriteByte(AcceleratorOffset2, 0);
                car.WriteByte(BrakeOffset2, byte.MaxValue);
                car[SteeringOffset2] = 0;
            }
            else
            {
                int runtimeTypePtr = car[RuntimeTypeOffset];
                if (runtimeTypePtr != 0 && runtimeType[0x14] != 0)
                {
                    int step = 0x10;
                    car.WriteByte(AcceleratorOffset2, (byte)Approach(car.ReadByte(AcceleratorOffset2), car.ReadByte(AcceleratorTargetOffset2), step));
                }
                else
                {
                    int step = 0x18;
                    car.WriteByte(AcceleratorOffset2, (byte)Approach(car.ReadByte(AcceleratorOffset2), car.ReadByte(AcceleratorTargetOffset2), step));
                }

                int brakeTarget = car.ReadByte(BrakeTargetOffset2);
                if (runtimeTypePtr != 0 && runtimeType[0x18] == 0)
                {
                    car.WriteByte(BrakeOffset2, (byte)brakeTarget);
                }
                else
                {
                    int currentBrake = car.ReadByte(BrakeOffset2);
                    int tableOffset = 0x114;
                    if (brakeTarget < currentBrake)
                    {
                        tableOffset = 0x11C;
                    }
                    int step = descriptor.ReadByte(tableOffset + (currentBrake >> 5));
                    car.WriteByte(BrakeOffset2, (byte)Approach(currentBrake, brakeTarget, step));
                }
            }

            if (PhysicsTables.RaceState == 0)
            {
                car.WriteByte(BrakeOffset2, byte.MaxValue);
            }

            if (car[ContactEventOffset] == 2)
            {
                car.WriteByte(AcceleratorOffset2, 0);
            }

            int accel = car.ReadByte(AcceleratorOffset2);
            int accelPlus1 = accel + 1;
            int accelDiv = X86Math.Divide(accelPlus1 << 16, 0xF8);
            if (accelDiv >= 0x10000)
            {
                accelDiv = 0x10000;
            }
            context.Accelerator = accelDiv;

            int brake = car.ReadByte(BrakeOffset2);
            int brakePlus1 = brake + 1;
            int brakeDiv = X86Math.Divide(brakePlus1 << 16, 0xF8);
            if (brakeDiv >= 0x10000)
            {
                brakeDiv = 0x10000;
            }
            context.Brake = brakeDiv;
            PhysicsTables.GlobalForce = brakeDiv;

            int steering = car[SteeringOffset2];
            int steeringPlus1 = steering + 1;
            int absSteering = X86Math.Abs(steeringPlus1);
            int steeringShifted = unchecked(absSteering << 16);
            int steeringDiv = X86Math.TruncatePowerOfTwo(steeringShifted, 7);
            context.Steering = steeringDiv;
            PhysicsTables.GlobalSteering = steeringDiv;

            SmoothGearAlternate(car, descriptor, runtimeType, context);
            SmoothSteeringAlternate(car, descriptor, runtimeType);
        }

        private static void SmoothGearAlternate(
            CarMemory car,
            CarSpecifications descriptor,
            CarRuntimeType runtimeType,
            PhysicsContext context)
        {
            byte delay = car.ReadByte(ShiftDelayOffset2);
            if (delay != 0)
            {
                car.WriteByte(ShiftDelayOffset2, (byte)(delay - 1));
            }

            if (context.InputEnabled == 0)
            {
                return;
            }

            int carIndex = car[CarIndexOffset];
            int raceMode = PhysicsTables.RaceMode;
            int baseTick = PhysicsTables.BaseTick;

            if (runtimeType[0x08] == 1 && raceMode == 1 && baseTick < 0x23C && context.IsStartingDriveRangeForced)
            {
                car.WriteByte(TargetGearOffset, 2);
            }

            byte targetGear = car.ReadByte(TargetGearOffset);
            byte currentGear = car.ReadByte(CurrentGearOffset);

            if (targetGear == currentGear)
            {
                return;
            }

            if (runtimeType[0x08] == 1)
            {
                int dword512278Index = carIndex * 64;
                if (PhysicsTables.OpponentFlags[dword512278Index] == 1)
                {
                    if (PhysicsTables.RaceMode == 1 && PhysicsTables.BaseTick < 0x23C)
                    {
                        car.WriteByte(TargetGearOffset, 2);
                    }

                    if (targetGear >= 2)
                    {
                        if (targetGear != 2 || targetGear <= currentGear)
                        {
                            return;
                        }

                        byte selected = 2;
                        int numGears = descriptor[0x04];

                        for (int gear = 2; gear < numGears; gear++)
                        {
                            int threshold = X86Math.MultiplyQ16(unchecked(descriptor[0xF0] << 16), descriptor[0x188 + gear * sizeof(int)]);
                            if (threshold < car[EngineSpeedOffset])
                            {
                                selected = (byte)gear;
                            }
                        }

                        ChangeGear(car, descriptor, context, selected);
                        return;
                    }
                }
            }

            if (PhysicsTables.GlobalMode == 2)
            {
                int dword51220C = PhysicsTables.GlobalCarIndex;
                if (dword51220C != carIndex)
                {
                    int eventResult = CallEventRoutine(car, descriptor, context, targetGear, currentGear);
                    if (eventResult != 0)
                    {
                        return;
                    }
                }
            }

            ChangeGearSimple(car, descriptor, context, targetGear);
        }

        private static int CallEventRoutine(
            CarMemory car,
            CarSpecifications descriptor,
            PhysicsContext context,
            byte targetGear,
            byte currentGear)
        {
            return 0;
        }

        private static void ChangeGearSimple(
            CarMemory car,
            CarSpecifications descriptor,
            PhysicsContext context,
            byte gear)
        {
            car.WriteByte(PreviousGearOffset, car.ReadByte(CurrentGearOffset));
            car.WriteByte(CurrentGearOffset, gear);
            car.WriteByte(ShiftDelayOffset2, descriptor.ReadByte(0x08));
        }

        private static void SmoothSteeringAlternate(
            CarMemory car,
            CarSpecifications descriptor,
            CarRuntimeType runtimeType)
        {
            int target = car[SteeringTargetOffset2];
            int current = car[SteeringOffset2];

            if (runtimeType[0x10] == 0)
            {
                car[SteeringOffset2] = target;
                return;
            }

            int step = descriptor[0x130];

            if (target >= 0 && current < 0 || target <= 0 && current > 0)
            {
                target = 0;
                step = descriptor[0x134];
            }

            car[SteeringOffset2] = Approach(current, target, step);
        }

        public static void Sample(
            CarMemory car,
            CarSpecifications descriptor,
            CarRuntimeType runtimeType,
            PhysicsContext context,
            RawVehicleInput input)
        {
            car.WriteByte(AcceleratorTargetOffset, (byte)(input.Accelerator >> 3 << 3));
            car.WriteByte(BrakeTargetOffset, (byte)(input.Brake >> 3 << 3));
            car[SteeringTargetOffset] = X86Math.TruncatePowerOfTwo(input.SteeringWord >> 24, 2) << 2;
            car.WriteByte(0x2DC, (byte)(input.Actions >> 3 & 1));
            car.WriteByte(0x2DD, (byte)(input.Actions >> 2 & 1));
            SelectTargetGear(car, descriptor, runtimeType, input.Actions);

            if ((input.Actions & 0x40) != 0)
            {
                context.RecoveryRequested?.Invoke(car);
            }
        }

        public static void Smooth(
            CarMemory car,
            CarSpecifications descriptor,
            CarRuntimeType runtimeType,
            PhysicsContext context)
        {
            if (car[0x280] == 2)
            {
                car.WriteByte(AcceleratorOffset, 0);
                car.WriteByte(BrakeOffset, byte.MaxValue);
                car[SteeringOffset] = 0;
            }
            else
            {
                SmoothAccelerator(car, runtimeType);
                SmoothBrake(car, descriptor, runtimeType);
                SmoothGear(car, descriptor, runtimeType, context);
                SmoothSteering(car, descriptor, runtimeType);
            }

            if (context.InputEnabled == 0)
            {
                car.WriteByte(BrakeOffset, byte.MaxValue);
            }

            if (car[0x284] == 2)
            {
                car.WriteByte(AcceleratorOffset, 0);
            }

            context.Accelerator = Math.Min(X86Math.Divide((car.ReadByte(AcceleratorOffset) + 1) << 16, 248), X86Math.One);
            context.Brake = Math.Min(X86Math.Divide((car.ReadByte(BrakeOffset) + 1) << 16, 248), X86Math.One);
            context.Steering = X86Math.TruncatePowerOfTwo(unchecked(X86Math.Abs(unchecked(car[SteeringOffset] + 1)) << 16), 7);
        }

        public static void ChangeGear(CarMemory car, CarSpecifications descriptor, PhysicsContext context, byte gear)
        {
            if (context.RaceMode < 2 || context.PlayerIndex == car[0x1E8])
            {
                int eventValue = 0;

                if (context.RaceMode == 1)
                {
                    eventValue = X86Math.One;

                    if (car[0x1E8] == 0)
                    {
                        eventValue = -X86Math.One;
                    }
                }

                context.GearChanged?.Invoke(car, eventValue);
            }

            car.WriteByte(0x2D9, car.ReadByte(GearOffset));
            car.WriteByte(GearOffset, gear);
            car.WriteByte(ShiftDelayOffset, descriptor.ReadByte(0x08));
        }

        private static void SelectTargetGear(CarMemory car, CarSpecifications descriptor, CarRuntimeType runtimeType, byte actions)
        {
            int gear = car.ReadByte(GearTargetOffset);

            if ((actions & 2) != 0)
            {
                if (runtimeType[0x08] == 1)
                {
                    if (gear < 2)
                    {
                        gear += 1;
                    }
                }
                else if (gear < descriptor[0x04] - 1)
                {
                    gear += 1;
                }
            }

            if ((actions & 1) != 0)
            {
                if (runtimeType[0x08] == 1 && gear > 1)
                {
                    gear = 1;
                }
                else if (gear > 0)
                {
                    gear -= 1;
                }
            }

            car.WriteByte(GearTargetOffset, (byte)gear);
        }

        private static void SmoothAccelerator(CarMemory car, CarRuntimeType runtimeType)
        {
            int step = 0x18;

            if (runtimeType[0x14] != 0)
            {
                step = 0x10;
            }

            car.WriteByte(AcceleratorOffset, (byte)Approach(car.ReadByte(AcceleratorOffset), car.ReadByte(AcceleratorTargetOffset), step));
        }

        private static void SmoothBrake(CarMemory car, CarSpecifications descriptor, CarRuntimeType runtimeType)
        {
            int target = car.ReadByte(BrakeTargetOffset);

            if (runtimeType[0x18] == 0)
            {
                car.WriteByte(BrakeOffset, (byte)target);

                return;
            }

            int current = car.ReadByte(BrakeOffset);
            int tableOffset = 0x114;

            if (target < current)
            {
                tableOffset = 0x11C;
            }

            int step = descriptor.ReadByte(tableOffset + (current >> 5));
            car.WriteByte(BrakeOffset, (byte)Approach(current, target, step));
        }

        private static void SmoothGear(CarMemory car, CarSpecifications descriptor, CarRuntimeType runtimeType, PhysicsContext context)
        {
            byte delay = car.ReadByte(ShiftDelayOffset);

            if (delay != 0)
            {
                car.WriteByte(ShiftDelayOffset, (byte)(delay - 1));
            }

            if (context.InputEnabled == 0)
            {
                return;
            }

            if (runtimeType[0x08] == 1 && context.BaseTick < 0x23C && context.IsStartingDriveRangeForced)
            {
                car.WriteByte(GearTargetOffset, 2);
            }

            byte target = car.ReadByte(GearTargetOffset);
            byte current = car.ReadByte(GearOffset);

            if (target == current)
            {
                return;
            }

            byte selected = target;

            if (runtimeType[0x08] == 1 && target >= 2)
            {
                if (target != 2 || target <= current)
                {
                    return;
                }

                selected = 2;

                for (int gear = 2; gear < descriptor[0x04]; gear += 1)
                {
                    int threshold = X86Math.MultiplyQ16(unchecked(descriptor[0xF0] << 16), descriptor[0x188 + gear * sizeof(int)]);

                    if (threshold < car[0x2B8])
                    {
                        selected = (byte)gear;
                    }
                }
            }

            ChangeGear(car, descriptor, context, selected);
        }

        private static void SmoothSteering(CarMemory car, CarSpecifications descriptor, CarRuntimeType runtimeType)
        {
            int target = car[SteeringTargetOffset];

            if (runtimeType[0x10] == 0)
            {
                car[SteeringOffset] = target;

                return;
            }

            int current = car[SteeringOffset];
            int step = descriptor[0x130];

            if (target >= 0 && current < 0 || target <= 0 && current > 0)
            {
                target = 0;
                step = descriptor[0x134];
            }

            car[SteeringOffset] = Approach(current, target, step);
        }

        private static int Approach(int current, int target, int step)
        {
            int difference = unchecked(target - current);

            if (difference < 0)
            {
                return unchecked(current - Math.Min(step, unchecked(-difference)));
            }

            return unchecked(current + Math.Min(step, difference));
        }
    }
}