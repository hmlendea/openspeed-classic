using System;
using System.Buffers.Binary;
using System.IO;

namespace OpenSpeed.Classic.Physics
{
    public sealed class CarSpecifications
    {
        private readonly byte[] storage = new byte[RuntimeSize];

        public static int SourceSize => 0x164;

        public static int RuntimeSize => 0x1DC;

        public int this[int offset]
        {
            get => BinaryPrimitives.ReadInt32LittleEndian(storage.AsSpan(offset, sizeof(int)));
            set => BinaryPrimitives.WriteInt32LittleEndian(storage.AsSpan(offset, sizeof(int)), value);
        }

        public byte ReadByte(int offset) => storage[offset];

        public byte[] Snapshot() => [.. storage];

        public static CarSpecifications Decode(ReadOnlySpan<byte> source)
        {
            if (source.Length < SourceSize)
            {
                throw new InvalidDataException($"The car specification contains {source.Length} bytes; at least {SourceSize} are required.");
            }

            CarSpecifications specifications = new();
            source[..SourceSize].CopyTo(specifications.storage);
            specifications[0x140] = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(0x144, sizeof(int)));
            specifications[0x144] = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(0x140, sizeof(int)));

            return specifications;
        }
    }
}