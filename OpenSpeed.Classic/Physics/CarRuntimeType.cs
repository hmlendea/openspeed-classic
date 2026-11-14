using System;

namespace OpenSpeed.Classic.Physics
{
    public sealed class CarRuntimeType
    {
        private readonly int[] fields = new int[SizeInBytes / sizeof(int)];

        public static int SizeInBytes => 0x40;

        public int this[int offset]
        {
            get => fields[ValidateOffset(offset)];
            set => fields[ValidateOffset(offset)] = value;
        }

        private static int ValidateOffset(int offset)
        {
            if (offset < 0 || offset >= SizeInBytes || offset % sizeof(int) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "The runtime car-type offset must address an aligned dword within its 64-byte record.");
            }

            return offset / sizeof(int);
        }
    }
}