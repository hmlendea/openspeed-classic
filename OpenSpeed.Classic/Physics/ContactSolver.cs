using System;

namespace OpenSpeed.Classic.Physics
{
    public static class ContactSolver
    {
        public static void UpdateRecord(CarMemory car, CarSpecifications descriptor, CarRuntimeType runtimeType, PhysicsContext context, ContactRecord record)
        {
            record[0x2C] = 0;

            if (record[0x0C] != 0)
            {
                RotateRecord(record, 0x10, 0x18, record[0x0C]);
            }

            bool isSelected = SelectDriveLimit(car, record);
            record[0] = X86Math.MultiplyQ16(record[0], descriptor[0x138]);
            bool isSpecial = isSelected && X86Math.Abs(record[0]) > record[0x04];

            if (!isSpecial && car.ReadByte(0x2DC) != 0 && record[0x28] == 0 && X86Math.Abs(car[0x2B8]) > 0x8000)
            {
                isSpecial = true;
            }

            if (isSpecial && RequiresSlipDelegate(car, runtimeType, record, context))
            {
                int increment = 2;

                if (record[0x28] != 0)
                {
                    increment = 1;
                }

                car[0x2FC] = unchecked(car[0x2FC] + increment);
                CalculateSlip(record, context);
                StoreResponse(car, record);

                return;
            }

            int angular = CalculateAngularResponse(record);
            int force = CalculateLateralForce(car, record, context, angular);
            record[0x1C] = unchecked(force + X86Math.TruncatePowerOfTwo(X86Math.Abs(context.GravityFirst), 1));

            if (unchecked(X86Math.Abs(record[0x10]) + X86Math.Abs(record[0x18])) < 0x200000)
            {
                record[0x1C] = SignedMagnitudeClamp(record[0x1C], record[0x10]);
            }

            record[0x24] = record[0];
            record[0x20] = 0;
            LimitCapacity(car, runtimeType, record, context);

            if (record[0x0C] != 0)
            {
                RotateRecord(record, 0x1C, 0x24, unchecked(-record[0x0C]));
            }

            StoreResponse(car, record);
        }

        public static void LimitCapacity(CarMemory car, CarRuntimeType runtimeType, ContactRecord record, PhysicsContext context)
        {
            if (runtimeType[0] == 0x0E)
            {
                return;
            }

            int lateral = X86Math.Abs(record[0x1C]);
            int longitudinal = X86Math.Abs(record[0x24]);
            int candidate = unchecked(Math.Max(lateral, longitudinal) + (Math.Min(lateral, longitudinal) >> 2));
            int threshold = unchecked((lateral >> 2) + 0x70000);

            if (lateral > 0x70000)
            {
                threshold = unchecked(lateral + 0x1C000);
            }

            int scaled = X86Math.MultiplyQ16(record[0x04], SelectCoefficient(record, context));

            if (record[0x28] != 0)
            {
                car[0x368] = unchecked(-candidate);

                if (record[0x1C] > 0)
                {
                    car[0x368] = candidate;
                }
            }

            car[0x364] = scaled;

            if (candidate <= scaled)
            {
                return;
            }

            int normal = X86Math.Abs(car[0x2B8]);
            record[0x2C] = normal;

            if (normal > 0x50000 || car[0x2F4] != 0)
            {
                record[0x2C] = unchecked(candidate - scaled);
            }

            bool scalesLongitudinal = car.ReadByte(0x2DA) == 2 && car.ReadByte(0x2D7) > 0 &&
                car[0x2F4] == 0 && record[0x28] == 0 && threshold < scaled;

            if (scalesLongitudinal)
            {
                record[0x2C] = X86Math.TruncatePowerOfTwo(record[0x2C], 3);
            }

            int shift = 2;

            if (car[0x200] < 3)
            {
                shift = 3;
            }

            int difference = unchecked(candidate - scaled);
            int subtractAmount = Math.Min(X86Math.TruncatePowerOfTwo(difference, shift), X86Math.TruncatePowerOfTwo(scaled, shift));
            int ratio = X86Math.DivideQ16(unchecked(scaled - subtractAmount), candidate);
            record[0x1C] = X86Math.MultiplyQ16(record[0x1C], ratio);

            if (scalesLongitudinal && car[0x200] <= 2)
            {
                record[0x24] = X86Math.MultiplyQ16(record[0x24], ratio);
            }
        }

        public static void CalculateSlip(ContactRecord record, PhysicsContext context)
        {
            int left = X86Math.Abs(record[0x10]);
            int right = X86Math.Abs(record[0x18]);
            int extent = unchecked(Math.Max(left, right) + (Math.Min(left, right) >> 2));
            int temporary = unchecked(record[0x04] - X86Math.TruncatePowerOfTwo(record[0x04], 3));
            record[0x2C] = 0;

            if (extent > record[0x04])
            {
                record[0x2C] = unchecked(extent - record[0x04]);
            }

            int factor = left;

            if (X86Math.Abs(extent) > 0x100)
            {
                factor = X86Math.TruncatePowerOfTwo(X86Math.DivideQ16(temporary, extent), 8);
            }

            record[0x1C] = X86Math.TruncatePowerOfTwo(unchecked(record[0x10] * factor), 8);
            record[0x24] = X86Math.TruncatePowerOfTwo(unchecked(record[0x18] * factor), 8);
        }

        private static bool SelectDriveLimit(CarMemory car, ContactRecord record)
        {
            if (record[0] < 0 && record[0x18] < 0 && (car.ReadByte(0x2D7) <= 0x40 || car.ReadByte(0x2DA) == 0))
            {
                record[0] = Math.Max(record[0], record[0x18]);

                return true;
            }

            if (record[0] > 0 && record[0x18] > 0 && (car.ReadByte(0x2D7) <= 0x40 || car.ReadByte(0x2DA) <= 1))
            {
                record[0] = Math.Min(record[0], record[0x18]);

                return true;
            }

            return false;
        }

        private static bool RequiresSlipDelegate(CarMemory car, CarRuntimeType runtimeType, ContactRecord record, PhysicsContext context)
        {
            if (car.ReadByte(0x2DC) != 0)
            {
                return true;
            }

            int state = runtimeType[0];
            bool supportsConstant = state is 5 or 7 or 8 or 9 or 0x0A or 0x0D or 0x0E;
            bool shouldClamp = supportsConstant || car[0x2B8] > 0x280000 || car.ReadByte(0x2D8) < 0xEC ||
                X86Math.Abs(car[0x2B8]) < 0x50000 && car[0x2F4] == 0;

            if (!shouldClamp)
            {
                return true;
            }

            if (record[0] > record[0x04])
            {
                record[0] = record[0x04];
            }

            if (record[0] < unchecked(-record[0x04]))
            {
                record[0] = unchecked(-record[0x04]);
            }

            record[0x2C] = 0;

            if (car[0x2B8] < 0x280000 && (state == 7 || supportsConstant && context.BaseTick % 4 == 0))
            {
                record[0x2C] = 0x30000;
            }

            return false;
        }

        private static int CalculateAngularResponse(ContactRecord record)
        {
            int magnitude = X86Math.Abs(record[0x18]);

            if (magnitude == 0)
            {
                return 0;
            }

            int angle = PhysicsAngles.InterpolatedAngle(record[0x10], X86Math.TruncatePowerOfTwo(magnitude, 1));

            if (record[0x18] > 0)
            {
                if (record[0x10] > 0)
                {
                    angle = unchecked(0x8000 - angle);
                }
                else if (record[0x10] < 0)
                {
                    angle = unchecked(-0x8000 - angle);
                }
            }

            return unchecked(angle << 8);
        }

        private static int CalculateLateralForce(CarMemory car, ContactRecord record, PhysicsContext context, int angular)
        {
            int scaled = X86Math.MultiplyQ16(record[0x04], SelectCoefficient(record, context));
            int force;

            if (record[0x28] != 0)
            {
                int magnitude = Math.Min(X86Math.Abs(angular), 0x100000);
                force = X86Math.MultiplyQ16(X86Math.MultiplyQ16(magnitude, scaled), 0x1555);
            }
            else
            {
                int minimum = Math.Max(X86Math.MultiplyQ16(X86Math.Abs(car[0x2B8]), 0xA3D), 0x8000);
                int magnitude = Math.Min(Math.Max(X86Math.Abs(angular), minimum), 0x20000);
                force = X86Math.TruncatePowerOfTwo(X86Math.MultiplyQ16(magnitude, scaled), 1);
            }

            if (angular < 0)
            {
                force = unchecked(-force);
            }

            return force;
        }

        private static int SelectCoefficient(ContactRecord record, PhysicsContext context)
        {
            if (record[0x28] != 0)
            {
                return context.FrontCoefficient;
            }

            return context.RearCoefficient;
        }

        private static int SignedMagnitudeClamp(int value, int reference)
        {
            int magnitude = Math.Min(X86Math.Abs(value), X86Math.Abs(reference));

            if (reference > 0)
            {
                return magnitude;
            }

            return unchecked(-magnitude);
        }

        private static void RotateRecord(ContactRecord record, int firstOffset, int secondOffset, int angle)
        {
            FixedVector result = PhysicsAngles.RotateComponents(record[firstOffset], record[secondOffset], angle);
            record[firstOffset] = result.First;
            record[secondOffset] = result.Third;
        }

        private static void StoreResponse(CarMemory car, ContactRecord record)
        {
            int offset = 0x328;

            if (record[0x28] != 0)
            {
                offset = 0x324;
            }

            car[offset] = record[0x2C];
        }
    }
}