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

            if (X86Math.Abs(first) > 0x0D)
            {
                delta = FixedMatrices.Multiply(FixedMatrices.RotateFirst(first), delta);
                isSignificant = true;
            }

            if (X86Math.Abs(third) > 0x0D)
            {
                delta = FixedMatrices.Multiply(FixedMatrices.RotateThird(third), delta);
                isSignificant = true;
            }

            if (!isSignificant)
            {
                return;
            }

            FixedMatrix basis = FixedMatrices.Multiply(FixedMatrices.Read(car, PrimaryBasisOffset), delta);
            byte countdown = unchecked((byte)(car.ReadByte(NormalisationCountdownOffset) - 1));
            car.WriteByte(NormalisationCountdownOffset, countdown);

            if (countdown == 0 || car[AccumulatedAngleOffset] > 0x1000)
            {
                basis = FixedMatrices.Orthonormalise(basis);
                car.WriteByte(NormalisationCountdownOffset, countdownReset);
                car[AccumulatedAngleOffset] = 0;
            }

            FixedMatrices.Write(car, PrimaryBasisOffset, basis);
        }
    }
}