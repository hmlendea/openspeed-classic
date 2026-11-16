using System;

namespace OpenSpeed.Classic.Rendering.Cars
{
    internal static class FixedPointArithmetic
    {
        public static int Add(int left, int right)
            => unchecked(left + right);

        public static int Divide(int numerator, int denominator)
        {
            if (denominator == 0)
            {
                throw new DivideByZeroException();
            }

            long quotient = (long)numerator / denominator;
            if (quotient < int.MinValue || quotient > int.MaxValue)
            {
                throw new OverflowException("The signed division quotient overflowed.");
            }

            return (int)quotient;
        }

        public static int MultiplyQ16(int left, int right)
        {
            long product = (long)left * right;
            int low = unchecked((int)product);
            int high = unchecked((int)(product >> 32));
            int shiftedHigh = unchecked(high << 16);
            int shiftedLow = (int)((uint)low >> 16);
            int carry = (low >> 15) & 1;

            return unchecked(shiftedLow + shiftedHigh + carry);
        }
    }
}