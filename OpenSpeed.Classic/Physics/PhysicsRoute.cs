using System;
using System.Buffers.Binary;
using System.IO;

namespace OpenSpeed.Classic.Physics
{
    public sealed class PhysicsRoute
    {
        private readonly byte[] records;

        public static int RecordSize => 36;

        public int Count => records.Length / RecordSize;

        public PhysicsRoute(ReadOnlySpan<byte> records)
        {
            if (records.Length == 0 || records.Length % RecordSize != 0)
            {
                throw new InvalidDataException($"The physics route contains {records.Length} bytes; complete 36-byte XBID 15 records are required.");
            }

            this.records = records.ToArray();
        }

        public int ReadDword(int record, int offset)
            => BinaryPrimitives.ReadInt32LittleEndian(records.AsSpan(record * RecordSize + offset, sizeof(int)));

        public short ReadWord(int record, int offset)
            => BinaryPrimitives.ReadInt16LittleEndian(records.AsSpan(record * RecordSize + offset, sizeof(short)));

        public int ReadVectorComponent(int record, int offset)
            => unchecked((sbyte)records[record * RecordSize + offset]) << 9;

        public FixedVector Position(int record) => new()
        {
            First = ReadDword(record, 0),
            Second = ReadDword(record, sizeof(int)),
            Third = ReadDword(record, sizeof(int) * 2)
        };

        public FixedVector Direction(int record, int offset) => new()
        {
            First = ReadVectorComponent(record, offset),
            Second = ReadVectorComponent(record, offset + 1),
            Third = ReadVectorComponent(record, offset + 2)
        };

        public int Heading(int record)
        {
            int next = record + 1;

            if (next >= Count)
            {
                next -= Count;
            }

            return PhysicsAngles.RatioAngle(
                unchecked(ReadDword(next, 0) - ReadDword(record, 0)),
                unchecked(ReadDword(next, 8) - ReadDword(record, 8)));
        }
    }
}