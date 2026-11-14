using System;

namespace OpenSpeed.Classic.Rendering.Cars
{
    public sealed class FixedPointCarState
    {
        public const int SizeInBytes = 0x380;

        private readonly int[] fields = new int[SizeInBytes / sizeof(int)];

        public int this[int offset]
        {
            get => fields[ValidateOffset(offset)];
            set => fields[ValidateOffset(offset)] = value;
        }

        public int Height
        {
            get => this[0x100];
            set => this[0x100] = value;
        }

        public int HandbrakeGate
        {
            get => this[0x15C];
            set => this[0x15C] = value;
        }

        private static int ValidateOffset(int offset)
        {
            if ((offset & 3) != 0 || offset < 0 || offset >= SizeInBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            return offset / sizeof(int);
        }
    }
}