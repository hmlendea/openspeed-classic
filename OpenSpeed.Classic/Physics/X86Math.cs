using System;

namespace OpenSpeed.Classic.Physics
{
    public static class X86Math
    {
        public static int FractionalBits => 16;

        public static int One => 0x10000;

        public static int Add(int left, int right) => unchecked(left + right);

        public static int Subtract(int left, int right) => unchecked(left - right);

        public static int Negate(int value) => unchecked(-value);

        public static int Abs(int value)
        {
            if (value < 0)
            {
                return Negate(value);
            }

            return value;
        }

        public static int Multiply(int left, int right) => unchecked(left * right);

        public static int MultiplyQ16(int left, int right)
        {
            long product = (long)left * right;
            uint low = unchecked((uint)product);
            uint high = unchecked((uint)((ulong)product >> 32));
            uint carry = low >> (FractionalBits - 1) & 1;

            return unchecked((int)((high << FractionalBits) + (low >> FractionalBits) + carry));
        }

        public static int TruncatePowerOfTwo(int value, int shift)
        {
            int bias = 0;

            if (value < 0)
            {
                bias = unchecked((1 << shift) - 1);
            }

            return unchecked(value + bias) >> shift;
        }

        public static int Divide(int numerator, int denominator)
            => DivideSigned(numerator, denominator);

        public static int DivideQ16(int numerator, int denominator)
        {
            int integerPart = Divide(numerator, denominator);
            int remainder = unchecked(numerator - integerPart * denominator);
            int fractionalPart = DivideSigned((long)remainder << FractionalBits, denominator);

            return unchecked((integerPart << FractionalBits) + fractionalPart);
        }

        public static int DivideSigned(long dividend, int divisor)
        {
            if (divisor == 0)
            {
                throw new DivideByZeroException("The x86 signed divide divisor is zero.");
            }

            long quotient = dividend / divisor;

            if (quotient < int.MinValue || quotient > int.MaxValue)
            {
                throw new OverflowException("The x86 signed divide quotient exceeds a signed dword.");
            }

            return (int)quotient;
        }

        public static uint DivideUnsigned(ulong dividend, uint divisor)
        {
            if (divisor == 0)
            {
                throw new DivideByZeroException("The x86 unsigned divide divisor is zero.");
            }

            ulong quotient = dividend / divisor;

            if (quotient > uint.MaxValue)
            {
                throw new OverflowException("The x86 unsigned divide quotient exceeds an unsigned dword.");
            }

            return (uint)quotient;
        }
    }
}