using System;
using System.Buffers.Binary;

namespace OpenSpeed.Classic.Physics
{
    public sealed class CarMemory
    {
        private readonly byte[] storage = new byte[SizeInBytes];

        public static int SizeInBytes => 0x600;

        public int this[int offset]
        {
            get => BinaryPrimitives.ReadInt32LittleEndian(GetSpan(offset, sizeof(int)));
            set => BinaryPrimitives.WriteInt32LittleEndian(GetSpan(offset, sizeof(int)), value);
        }

        public byte ReadByte(int offset) => GetSpan(offset, sizeof(byte))[0];

        public sbyte ReadSignedByte(int offset) => unchecked((sbyte)ReadByte(offset));

        public ushort ReadWord(int offset)
            => BinaryPrimitives.ReadUInt16LittleEndian(GetSpan(offset, sizeof(ushort)));

        public short ReadSignedWord(int offset) => unchecked((short)ReadWord(offset));

        public void WriteByte(int offset, byte value) => GetSpan(offset, sizeof(byte))[0] = value;

        public void WriteWord(int offset, ushort value)
            => BinaryPrimitives.WriteUInt16LittleEndian(GetSpan(offset, sizeof(ushort)), value);

        public byte[] Snapshot() => [.. storage];

        private Span<byte> GetSpan(int offset, int width)
        {
            if (offset < 0 || offset > storage.Length - width)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offset),
                    offset,
                    $"The {width}-byte field must fit within the {storage.Length}-byte car state.");
            }

            return storage.AsSpan(offset, width);
        }
    }
}