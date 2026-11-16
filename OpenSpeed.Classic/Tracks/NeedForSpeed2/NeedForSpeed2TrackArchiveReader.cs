using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal static class NeedForSpeed2TrackArchiveReader
    {
        private static int ArchiveHeaderSize => 16;

        private static int ArchiveRecordFixedSize => 8;

        private static int MaximumArchiveRecordCount => 65536;

        private static string MainTrackFileName => "main.trk";

        internal static byte[] Read(byte[] fileData)
        {
            ArgumentNullException.ThrowIfNull(fileData);

            if (HasMarker(fileData, "TRAC"))
            {
                return [.. fileData];
            }

            if (!HasMarker(fileData, "BIGF"))
            {
                throw new InvalidDataException(
                    "The NFS2 track file is neither a TRAC resource nor a BIGF archive.");
            }

            return ReadMainTrackFromArchive(fileData);
        }

        private static byte[] ReadMainTrackFromArchive(byte[] archiveData)
        {
            if (archiveData.Length < ArchiveHeaderSize)
            {
                throw new InvalidDataException("The BIGF track archive header is truncated.");
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
                throw new InvalidDataException("The BIGF track archive header is invalid.");
            }

            int recordPosition = ArchiveHeaderSize;

            for (int recordIndex = 0; recordIndex < recordCount; recordIndex += 1)
            {
                if (recordPosition > recordTableEnd - ArchiveRecordFixedSize)
                {
                    throw new InvalidDataException("The BIGF track archive table is truncated.");
                }

                int fileOffset = ReadBigEndianInt32(archiveData, recordPosition);
                int fileSize = ReadBigEndianInt32(
                    archiveData,
                    recordPosition + sizeof(int));
                recordPosition += ArchiveRecordFixedSize;
                int nameTerminator = Array.IndexOf(
                    archiveData,
                    (byte)0,
                    recordPosition,
                    recordTableEnd - recordPosition);

                if (nameTerminator < 0)
                {
                    throw new InvalidDataException(
                        "A BIGF track archive file name is not terminated.");
                }

                string fileName = Encoding.ASCII.GetString(
                    archiveData,
                    recordPosition,
                    nameTerminator - recordPosition);
                recordPosition = nameTerminator + 1;

                if (!string.Equals(
                        fileName,
                        MainTrackFileName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (fileOffset < recordTableEnd ||
                    fileSize < 0 ||
                    fileOffset > archiveData.Length - fileSize)
                {
                    throw new InvalidDataException(
                        "The main.trk BIGF archive record references invalid data bounds.");
                }

                return archiveData.AsSpan(fileOffset, fileSize).ToArray();
            }

            throw new InvalidDataException(
                "The BIGF track archive does not contain a main.trk resource.");
        }

        private static bool HasMarker(byte[] data, string marker)
            => data.Length >= marker.Length &&
                data.AsSpan(0, marker.Length).SequenceEqual(Encoding.ASCII.GetBytes(marker));

        private static int ReadBigEndianInt32(byte[] data, int offset)
            => BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset, sizeof(int)));
    }
}