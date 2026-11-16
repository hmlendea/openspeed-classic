using System;

namespace OpenSpeed.Classic.Physics
{
    public sealed class ContactRecord
    {
        private readonly int[] fields = new int[SizeInBytes / sizeof(int)];

        public static int SizeInBytes => 0x30;

        public int this[int offset]
        {
            get => fields[ValidateOffset(offset)];
            set => fields[ValidateOffset(offset)] = value;
        }

        public int[] Snapshot() => [.. fields];

        private static int ValidateOffset(int offset)
        {
            if (offset < 0 || offset >= SizeInBytes || offset % sizeof(int) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "The contact record field must be an aligned dword within its 48-byte record.");
            }

            return offset / sizeof(int);
        }
    }
}