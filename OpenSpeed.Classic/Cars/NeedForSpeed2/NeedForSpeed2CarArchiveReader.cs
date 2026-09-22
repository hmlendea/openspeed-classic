using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace OpenSpeed.Classic.Cars.NeedForSpeed2
{
    public static class NeedForSpeed2CarArchiveReader
    {
        private static int ArchiveHeaderSize => 16;

        private static int ArchiveRecordFixedSize => 8;

        private static int MaximumArchiveRecordCount => 65536;

        public static byte[] Read(byte[] archiveData, string memberName)
        {
            ArgumentNullException.ThrowIfNull(archiveData);
            ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

            if (archiveData.Length < ArchiveHeaderSize || !HasMarker(archiveData, "BIGF"))
            {
                throw new InvalidDataException("The NFS II car archive header is invalid.");
            }

            int declaredArchiveSize = ReadBigEndianInt32(archiveData, 4);
            int recordCount = ReadBigEndianInt32(archiveData, 8);
            int recordTableEnd = ReadBigEndianInt32(archiveData, 12);

            if (declaredArchiveSize != archiveData.Length ||
                recordCount < 0 ||
                recordCount > MaximumArchiveRecordCount ||
                recordTableEnd < ArchiveHeaderSize ||
                recordTableEnd > archiveData.Length)
            {
                throw new InvalidDataException("The NFS II car archive header is invalid.");
            }

            int recordPosition = ArchiveHeaderSize;

            for (int recordIndex = 0; recordIndex < recordCount; recordIndex += 1)
            {
                ArchiveRecord record = ReadRecord(
                    archiveData,
                    recordPosition,
                    recordTableEnd);
                recordPosition = record.NextRecordPosition;

                if (!string.Equals(
                        record.Name,
                        memberName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (record.Offset < recordTableEnd ||
                    record.Size < 0 ||
                    record.Offset > archiveData.Length - record.Size)
                {
                    throw new InvalidDataException(
                        $"The NFS II car archive member '{memberName}' has invalid bounds.");
                }

                return archiveData.AsSpan(record.Offset, record.Size).ToArray();
            }

            throw new InvalidDataException(
                $"The NFS II car archive does not contain '{memberName}'.");
        }

        private static ArchiveRecord ReadRecord(
            byte[] archiveData,
            int recordPosition,
            int recordTableEnd)
        {
            if (recordPosition > recordTableEnd - ArchiveRecordFixedSize)
            {
                throw new InvalidDataException("The NFS II car archive table is truncated.");
            }

            int fileOffset = ReadBigEndianInt32(archiveData, recordPosition);
            int fileSize = ReadBigEndianInt32(
                archiveData,
                recordPosition + sizeof(int));
            int namePosition = recordPosition + ArchiveRecordFixedSize;
            int nameTerminator = Array.IndexOf(
                archiveData,
                (byte)0,
                namePosition,
                recordTableEnd - namePosition);

            if (nameTerminator < 0)
            {
                throw new InvalidDataException(
                    "An NFS II car archive member name is not terminated.");
            }

            string name = Encoding.ASCII.GetString(
                archiveData,
                namePosition,
                nameTerminator - namePosition);

            return new ArchiveRecord(
                name,
                fileOffset,
                fileSize,
                nameTerminator + 1);
        }

        private static bool HasMarker(byte[] data, string marker)
            => data.AsSpan(0, marker.Length).SequenceEqual(Encoding.ASCII.GetBytes(marker));

        private static int ReadBigEndianInt32(byte[] data, int offset)
            => BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset, sizeof(int)));

        private sealed class ArchiveRecord(
            string name,
            int offset,
            int size,
            int nextRecordPosition)
        {
            public string Name { get; } = name;

            public int NextRecordPosition { get; } = nextRecordPosition;

            public int Offset { get; } = offset;

            public int Size { get; } = size;
        }
    }
}