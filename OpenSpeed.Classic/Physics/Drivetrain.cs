using System;

namespace OpenSpeed.Classic.Physics
{
    public static class Drivetrain
    {
        private static int EngineOffset => 0x2F0;

        private static int GearOffset => 0x2DA;

        public static int CalculateForce(CarMemory car, CarSpecifications descriptor, CarRuntimeType runtimeType, PhysicsContext context)
        {
            int limit = descriptor[0xF0];
            bool isAboveLimit = car[EngineOffset] > limit;

            if (isAboveLimit && car[EngineOffset] > unchecked(limit + X86Math.TruncatePowerOfTwo(limit, 1)))
            {
                car.WriteByte(0x2DC, 1);
            }

            int resistance = CalculateResistance(car, descriptor, runtimeType, context);
            int target = X86Math.MultiplyQ16(limit, context.Accelerator);

            if (car.ReadByte(GearOffset) == 1 || car.ReadByte(0x2DB) > 0 || car.ReadWord(0x14C) != 0)
            {
                ApproachUncoupledTarget(car, target);

                return unchecked(-resistance);
            }

            if (runtimeType[0x08] == 1)
            {
                SelectAutomaticGear(car, descriptor, context);
            }

            int gear = car.ReadByte(GearOffset);
            int wheelTarget = X86Math.TruncatePowerOfTwo(X86Math.MultiplyQ16(car[0x2B8], descriptor[0x0C + gear * sizeof(int)]), 16);
            int torque = CalculateTorque(car, descriptor, runtimeType, context, isAboveLimit);
            int targetDifference = unchecked(target - wheelTarget);

            if (X86Math.Abs(targetDifference) < 0x7D && target < unchecked(limit - 0x12C))
            {
                targetDifference = 0;
            }

            int savedDifference = unchecked(car[EngineOffset] - wheelTarget);
            car[0x2F4] = 0;
            car[0x2F8] = 0;

            if (savedDifference > 0x4E2 && car.ReadByte(0x2DB) == 0 && gear <= 4 ||
                gear > 1 && car[0x304] < -0x1999 && car.ReadByte(0x2D7) > 0x40 ||
                gear == 0 && car[0x304] > 0x1999 && car.ReadByte(0x2D7) > 0x40)
            {
                torque = ApplyEngineMismatch(car, runtimeType, context, torque, savedDifference, target);
            }
            else if (targetDifference < 0)
            {
                torque = ApplyEngineBraking(car, descriptor, context, torque, savedDifference, target);
            }
            else if (targetDifference == 0)
            {
                car[EngineOffset] = wheelTarget;
                torque = resistance;
            }
            else
            {
                ApproachWheelTarget(car, wheelTarget, savedDifference, target);
                torque = X86Math.MultiplyQ16(torque, context.Accelerator);
                int factor = Math.Min(unchecked(X86Math.One + X86Math.MultiplyQ16(X86Math.Abs(car[0x310]), 0x8000)), 0x28000);
                torque = X86Math.MultiplyQ16(torque, factor);
            }

            return ClampNegativeEngine(car, torque, resistance);
        }

        public static AxleForceDistribution SplitForce(CarMemory car, CarSpecifications descriptor, CarRuntimeType runtimeType, PhysicsContext context, int netDrive)
        {
            int frontDrive = X86Math.MultiplyQ16(netDrive, descriptor[0xF8]);
            int brakeLimit = X86Math.MultiplyQ16(context.Brake, descriptor[0xFC]);
            int brake = Math.Min(brakeLimit, unchecked(X86Math.Abs(car[0x2B8]) << 5));

            if (car[0x2B8] > 0)
            {
                brake = unchecked(-brake);
            }

            int frontBrake = X86Math.MultiplyQ16(brake, unchecked(runtimeType[0x28] + descriptor[0x100]));

            return new AxleForceDistribution
            {
                FrontDrive = frontDrive,
                RearDrive = unchecked(netDrive - frontDrive),
                FrontBrake = frontBrake,
                RearBrake = unchecked(brake - frontBrake)
            };
        }

        public static void SelectAutomaticGear(CarMemory car, CarSpecifications descriptor, PhysicsContext context)
        {
            int current = car.ReadByte(GearOffset);

            if (current < 2)
            {
                return;
            }

            int next = Math.Min(current + 1, descriptor[0x04] - 1);
            int previous = current;
            int earlier = current;

            if (current > 2)
            {
                previous = current - 1;
            }

            if (current > 3)
            {
                earlier = current - 2;
            }

            int speed = EngineSpeedForGear(car, descriptor, current);
            int previousSpeed = EngineSpeedForGear(car, descriptor, previous);
            int earlierSpeed = EngineSpeedForGear(car, descriptor, earlier);
            int selected = current;
            int limit = descriptor[0xF0];

            if (context.Accelerator > 0xE666 || X86Math.Abs(speed) > limit)
            {
                if (speed > unchecked(limit - 0x1F4) && current < descriptor[0x04] - 1)
                {
                    selected = next;
                }
                else if (earlierSpeed < unchecked(limit - 0x5DC) && earlier != current)
                {
                    selected = earlier;
                }
                else if (previousSpeed < unchecked(limit - 0x5DC))
                {
                    selected = previous;
                }
            }
            else if (previousSpeed < unchecked(limit - 0x5DC))
            {
                selected = previous;
            }

            if (selected != current)
            {
                ControlPipeline.ChangeGear(car, descriptor, context, (byte)selected);
            }
        }

        private static int EngineSpeedForGear(CarMemory car, CarSpecifications descriptor, int gear)
        {
            int projection = X86Math.MultiplyQ16(car[0x304], descriptor[0x168 + gear * sizeof(int)]);

            return X86Math.TruncatePowerOfTwo(unchecked(projection * descriptor[0xF0]), 16);
        }

        private static int CalculateResistance(CarMemory car, CarSpecifications descriptor, CarRuntimeType runtimeType, PhysicsContext context)
        {
            int scale = unchecked(X86Math.TruncatePowerOfTwo(unchecked(runtimeType[0x1C] + runtimeType[0x20] - 0x20000), 3) + X86Math.One);
            int velocity = X86Math.TruncatePowerOfTwo(X86Math.MultiplyQ16(car[0x2B8], scale), 16);
            int resistance = X86Math.MultiplyQ16(descriptor[0x1C8], unchecked(velocity * velocity * velocity));

            if ((context.FeatureFlags & 4) != 0 && runtimeType[0] < 0x0F)
            {
                resistance = X86Math.TruncatePowerOfTwo(resistance, 1);
            }

            return resistance;
        }

        private static int CalculateTorque(CarMemory car, CarSpecifications descriptor, CarRuntimeType runtimeType, PhysicsContext context, bool isAboveLimit)
        {
            int index = X86Math.TruncatePowerOfTwo(car[EngineOffset], 8);

            if (isAboveLimit)
            {
                index = X86Math.TruncatePowerOfTwo(descriptor[0xF0], 8);
            }
            else
            {
                index = Math.Clamp(index, 0, 40);
            }

            int gear = car.ReadByte(GearOffset);
            int torque = X86Math.MultiplyQ16(descriptor[0x4C + index * sizeof(int)], descriptor[0x1A8 + gear * sizeof(int)]);

            if ((context.FeatureFlags & 4) == 0 || runtimeType[0] >= 0x0F)
            {
                int cap = 0x80000;

                if (gear == 2)
                {
                    cap = 0xA0000;
                }

                torque = Math.Min(torque, cap);
            }

            return torque;
        }

        private static void ApproachUncoupledTarget(CarMemory car, int target)
        {
            int count = car[EngineOffset];

            if (target > count && car.ReadByte(0x2DB) == 0)
            {
                count = Math.Min(unchecked(count + 0x1F4), target);
            }
            else if (car.ReadByte(0x2DB) > 0)
            {
                count = Math.Max(unchecked(count - 0x32), 0);
            }
            else if (target <= count)
            {
                count = Math.Max(unchecked(count - 0xC8), target);
            }

            car[EngineOffset] = count;
            car[0x2F4] = 0;
            car[0x2F8] = 0;
        }

        private static int ApplyEngineMismatch(CarMemory car, CarRuntimeType runtimeType, PhysicsContext context, int torque, int difference, int target)
        {
            if ((context.FeatureFlags & 4) != 0 && runtimeType[0] < 0x0F)
            {
                car[0x2F8] = 1;
            }

            car[0x2F4] = 1;
            torque = X86Math.MultiplyQ16(torque, unchecked(context.Accelerator + X86Math.One));
            int decrement = car.ReadByte(GearOffset) switch
            {
                2 => 5,
                3 => 0x19,
                _ => 0x4B
            };
            car[EngineOffset] = Math.Min(unchecked(car[EngineOffset] - Math.Min(decrement, difference)), target);

            return torque;
        }

        private static int ApplyEngineBraking(CarMemory car, CarSpecifications descriptor, PhysicsContext context, int torque, int difference, int target)
        {
            torque = unchecked(-X86Math.MultiplyQ16(torque, descriptor[0x144]));
            int gear = car.ReadByte(GearOffset);

            if (context.GravityThird > 0 && gear > 1 && gear < 3 && torque < 0 ||
                context.GravityThird < 0 && gear == 0 && torque > 0)
            {
                torque = X86Math.TruncatePowerOfTwo(torque, 1);
            }

            int rawStep = unchecked(descriptor[0x188 + gear * sizeof(int)] << 3);
            int maximumStep = X86Math.TruncatePowerOfTwo(X86Math.MultiplyQ16(rawStep, 0x28000000), 16);
            int step = Math.Min(unchecked(-difference), maximumStep);
            car[EngineOffset] = Math.Max(unchecked(car[EngineOffset] + step), target);

            return torque;
        }

        private static void ApproachWheelTarget(CarMemory car, int wheelTarget, int difference, int target)
        {
            if (difference > 0xC8)
            {
                car[EngineOffset] = unchecked(car[EngineOffset] - 0xC8);
            }
            else if (difference < -0x12C)
            {
                car[EngineOffset] = unchecked(car[EngineOffset] + 0xC8);
            }
            else
            {
                car[EngineOffset] = wheelTarget;
            }

            car[EngineOffset] = Math.Min(car[EngineOffset], target);
        }

        private static int ClampNegativeEngine(CarMemory car, int torque, int resistance)
        {
            if (car[EngineOffset] < 0)
            {
                int boundary = unchecked(-(car[0x2B8] << 5));

                if (!(torque > 0 && boundary > 0 && unchecked(torque - boundary) > 0) &&
                    !(torque < 0 && boundary < 0 && unchecked(torque - boundary) < 0))
                {
                    torque = boundary;
                    car[EngineOffset] = 0;
                }
            }

            return unchecked(torque - resistance);
        }
    }
}