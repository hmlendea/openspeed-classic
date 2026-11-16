namespace OpenSpeed.Classic.Physics
{
    public static class BodyMotion
    {
        private static int ActiveOffset => 0x8D;

        private static int PrimaryBasisOffset => 0xC4;

        private static int AngularOffset => 0xE8;

        private static int AccumulatedAngleOffset => 0x98;

        private static int NormalisationCountdownOffset => 0x8F;

        public static void IntegratePosition(CarMemory car)
        {
            if (car.ReadByte(ActiveOffset) == 0)
            {
                return;
            }

            car[0x9C] = unchecked(car[0x9C] + X86Math.TruncatePowerOfTwo(car[0xA8], 6));
            car[0xA0] = unchecked(car[0xA0] + X86Math.TruncatePowerOfTwo(car[0xAC], 6));
            car[0xA4] = unchecked(car[0xA4] + X86Math.TruncatePowerOfTwo(car[0xB0], 6));
        }

        public static void IntegrateScaledPlayerPosition(CarMemory car)
        {
            if (car.ReadByte(ActiveOffset) == 0)
            {
                return;
            }

            int scale = car[0x218];
            car[0x9C] = unchecked(car[0x9C] + X86Math.TruncatePowerOfTwo(X86Math.MultiplyQ16(car[0xA8], scale), 6));
            car[0xA0] = unchecked(car[0xA0] + X86Math.TruncatePowerOfTwo(X86Math.MultiplyQ16(car[0xAC], scale), 6));
            car[0xA4] = unchecked(car[0xA4] + X86Math.TruncatePowerOfTwo(X86Math.MultiplyQ16(car[0xB0], scale), 6));
            car[0x94] = scale;
        }

        public static void IntegratePlayerBasis(CarMemory car) => IntegrateBasis(car, 6, 0x10);

        public static void IntegrateAiBasis(CarMemory car) => IntegrateBasis(car, 5, 0x20);

        private static void IntegrateBasis(CarMemory car, int shift, byte countdownReset)
        {
            if (car.ReadByte(ActiveOffset) == 0)
            {
                return;
            }

            int first = X86Math.TruncatePowerOfTwo(car[AngularOffset], shift);
            int second = X86Math.TruncatePowerOfTwo(car[AngularOffset + sizeof(int)], shift);
            int third = X86Math.TruncatePowerOfTwo(car[AngularOffset + sizeof(int) * 2], shift);
            car[AccumulatedAngleOffset] = unchecked(car[AccumulatedAngleOffset] +
                X86Math.Abs(first) + X86Math.Abs(second) + X86Math.Abs(third));
            bool isSignificant = X86Math.Abs(second) > 0x13;
            FixedMatrix delta = FixedMatrices.RotateSecond(second);

            // The source composes Rx * Ry * Rz. It starts from Ry, applies Rx on the left, then
            // applies Rz on the right of the accumulated product:
            //   lea eax,[esp+48h] (Rx) / mov edx,[esp+6Ch] (Ry) / call sub_49E840  -> Rx * Ry
            //   mov edx,esp (Rz) / eax,[esp+24h] (Rx*Ry)       / call sub_49E840  -> (Rx*Ry) * Rz
            // sub_49E840 computes `eax * edx`, so the second call multiplies the accumulator by
            // Rz rather than pre-multiplying it.
            if (X86Math.Abs(first) > 0x0D)
            {
                delta = FixedMatrices.Multiply(FixedMatrices.RotateFirst(first), delta);
                isSignificant = true;
            }

            if (X86Math.Abs(third) > 0x0D)
            {
                delta = FixedMatrices.Multiply(delta, FixedMatrices.RotateThird(third));
                isSignificant = true;
            }

            if (!isSignificant)
            {
                return;
            }

            FixedMatrix basis = FixedMatrices.Multiply(FixedMatrices.Read(car, PrimaryBasisOffset), delta);
            byte previousCountdown = car.ReadByte(NormalisationCountdownOffset);
            byte countdown = unchecked((byte)(previousCountdown - 1));
            car.WriteByte(NormalisationCountdownOffset, countdown);

            // The source decrements the byte and follows it with a `jbe` (opcode 0x76, confirmed
            // from the original executable). `dec` sets CF only when the operand was zero and ZF
            // when the result is zero, so the branch fires for the previous value 0x00 (wrap) and
            // for 0x01 (result 0) - that is, for any previous value at most one. A signed
            // `<= 0` test would instead fire for every value from 0x80 upward and normalise the
            // basis while the countdown is still high.
            if (previousCountdown <= 1 || car[AccumulatedAngleOffset] > 0x1000)
            {
                basis = FixedMatrices.Orthonormalise(basis);
                car.WriteByte(NormalisationCountdownOffset, countdownReset);
                car[AccumulatedAngleOffset] = 0;
            }

            FixedMatrices.Write(car, PrimaryBasisOffset, basis);
        }
    }
}