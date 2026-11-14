using System;

namespace OpenSpeed.Classic.Physics
{
    public static class CarSpecificationsFinaliser
    {
        private static int CurveOffset => 0x4C;

        private static int CurveCount => 41;

        public static void Finalise(CarSpecifications descriptor, CarRuntimeType runtimeType, int mode, byte flags)
        {
            descriptor[0x164] = X86Math.Divide(X86Math.One, descriptor[0xF0]);
            descriptor[0x1D8] = descriptor[CurveOffset];

            for (int index = 1; index < CurveCount; index += 1)
            {
                descriptor[0x1D8] = Math.Max(descriptor[0x1D8], descriptor[CurveOffset + index * sizeof(int)]);
            }

            bool hasPerformanceAdjustment = (flags & 4) != 0 && runtimeType[0] < 0x0F;

            for (int gear = 0; gear < descriptor[0x04]; gear += 1)
            {
                FinaliseGear(descriptor, runtimeType, mode, hasPerformanceAdjustment, gear);
            }

            if (hasPerformanceAdjustment)
            {
                descriptor[0xF8] = 0x7333;
                descriptor[0x138] = X86Math.MultiplyQ16(descriptor[0x138], 0x14000);
            }

            FinaliseResistance(descriptor);

            if (mode == 0)
            {
                descriptor[0x12C] = X86Math.MultiplyQ16(descriptor[0x12C], runtimeType[0x24]);
            }
        }

        private static void FinaliseGear(
            CarSpecifications descriptor,
            CarRuntimeType runtimeType,
            int mode,
            bool hasPerformanceAdjustment,
            int gear)
        {
            int displacement = gear * sizeof(int);
            int ratio = descriptor[0x0C + displacement];

            if (mode == 0 && runtimeType[0x2C] != 0)
            {
                ratio = X86Math.DivideQ16(ratio, runtimeType[0x2C]);
                descriptor[0x0C + displacement] = ratio;
            }

            int reciprocal = 0x28F;

            if (ratio != 0)
            {
                reciprocal = X86Math.DivideQ16(X86Math.One, ratio);
            }

            descriptor[0x188 + displacement] = reciprocal;
            int derived = X86Math.DivideQ16(ratio, descriptor[0]);
            derived = X86Math.DivideQ16(derived, 0xA0000);
            derived = X86Math.MultiplyQ16(derived, descriptor[0x2C + displacement]);

            if (hasPerformanceAdjustment)
            {
                derived = unchecked(derived + derived);
            }

            descriptor[0x1A8 + displacement] = derived;
            int divisor = X86Math.MultiplyQ16(unchecked(descriptor[0xF0] << 16), reciprocal);
            descriptor[0x168 + displacement] = X86Math.DivideQ16(X86Math.One, divisor);
        }

        private static void FinaliseResistance(CarSpecifications descriptor)
        {
            int last = descriptor[0x04];
            int scaled = X86Math.MultiplyQ16(descriptor[0xF4], descriptor[0x08 + last * sizeof(int)]);
            int index = X86Math.TruncatePowerOfTwo(X86Math.TruncatePowerOfTwo(scaled, 16), 8);
            int numerator = X86Math.MultiplyQ16(descriptor[CurveOffset + index * sizeof(int)], descriptor[0x1A4 + last * sizeof(int)]);
            int cubicBase = X86Math.TruncatePowerOfTwo(descriptor[0xF4], 16);
            int divisor = unchecked(cubicBase * cubicBase * cubicBase);
            descriptor[0x1C8] = X86Math.DivideQ16(numerator, divisor);
            descriptor[0x1CC] = unchecked(X86Math.MultiplyQ16(
                X86Math.TruncatePowerOfTwo(X86Math.Abs(descriptor[0x124]), 1), 0x648) << 8);
            descriptor[0x1D0] = X86Math.DivideQ16(X86Math.One, descriptor[0x1CC]);
            descriptor[0x1D4] = X86Math.DivideQ16(X86Math.One, descriptor[0x138]);
        }
    }
}