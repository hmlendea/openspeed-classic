namespace OpenSpeed.Classic.Physics
{
    public static class PhysicsAngles
    {
        private static int FineFractionMask => 0x3F;

        private static int FineCorrectionFactor => 0x6487E;

        public static int RatioAngle(int first, int second)
            => CalculateAngle(first, second, false);

        public static int InterpolatedAngle(int first, int second)
            => CalculateAngle(first, second, true);

        public static FixedAnglePair Rotation(int angle)
        {
            FixedAnglePair coarse = QuarterWavePair(angle >> 6);
            int correction = unchecked((angle & FineFractionMask) * FineCorrectionFactor) >> 9;

            return new FixedAnglePair
            {
                Cosine = unchecked(coarse.Cosine - (unchecked((coarse.Sine >> 2) * correction) >> 21)),
                Sine = unchecked(coarse.Sine + (unchecked((coarse.Cosine >> 2) * correction) >> 21))
            };
        }

        public static FixedVector RotateComponents(int first, int second, int rawAngle)
        {
            int angle = X86Math.TruncatePowerOfTwo(rawAngle, 8);
            FixedAnglePair coarse = QuarterWavePair(angle >> 6);
            int correction = unchecked((angle & FineFractionMask) * FineCorrectionFactor) >> 9;
            int sine = unchecked(coarse.Sine + (unchecked((coarse.Cosine >> 2) * correction) >> 21));
            int cosine = unchecked(coarse.Cosine + (unchecked((unchecked(-coarse.Sine) >> 2) * correction) >> 21));

            return new FixedVector
            {
                First = unchecked(X86Math.MultiplyQ16(cosine, first) - X86Math.MultiplyQ16(sine, second)),
                Third = unchecked(X86Math.MultiplyQ16(sine, first) + X86Math.MultiplyQ16(cosine, second))
            };
        }

        private static FixedAnglePair QuarterWavePair(int coarse)
        {
            int index = coarse & 0xFF;
            int sine = PhysicsTables.QuarterWave[index];
            int cosine = PhysicsTables.QuarterWave[0x100 - index];

            return (coarse >> 8 & 3) switch
            {
                0 => new() { Cosine = cosine, Sine = sine },
                1 => new() { Cosine = -sine, Sine = cosine },
                2 => new() { Cosine = -cosine, Sine = -sine },
                _ => new() { Cosine = sine, Sine = -cosine }
            };
        }

        private static int CalculateAngle(int first, int second, bool isInterpolated)
        {
            int flags = 0;

            if (second < 0)
            {
                flags = 2;
                second = unchecked(-second);
            }

            if (first < 0)
            {
                flags |= 4;
                first = unchecked(-first);
            }

            int quarterTurn = 0x100;

            if (isInterpolated)
            {
                quarterTurn = 0x4000;
            }

            int result = quarterTurn / 2;

            if (first != second)
            {
                if (first > second)
                {
                    int previousFirst = first;
                    first = second;
                    second = previousFirst;
                    flags |= 1;
                }

                uint quotient = X86Math.DivideUnsigned((ulong)(uint)first << 32, (uint)second);
                result = LookupAngle(quotient, isInterpolated);
            }

            return flags switch
            {
                0 => result,
                1 => unchecked(-result + quarterTurn),
                2 => unchecked(-result + quarterTurn * 2),
                3 => unchecked(result + quarterTurn),
                4 => unchecked(-result),
                5 => unchecked(result - quarterTurn),
                6 => unchecked(result - quarterTurn * 2),
                _ => unchecked(-result - quarterTurn)
            };
        }

        private static int LookupAngle(uint quotient, bool isInterpolated)
        {
            int index = (int)(quotient >> 24);

            if (!isInterpolated)
            {
                return PhysicsTables.RatioAngle[index + (int)(quotient >> 23 & 1)];
            }

            int fraction = (int)(quotient >> 8 & 0xFFFF);
            int lower = PhysicsTables.InterpolatedAngle[index];
            int upper = PhysicsTables.InterpolatedAngle[index + 1];
            int product = unchecked((upper - lower) * fraction);

            return unchecked(lower + (int)((uint)product >> 16));
        }
    }
}