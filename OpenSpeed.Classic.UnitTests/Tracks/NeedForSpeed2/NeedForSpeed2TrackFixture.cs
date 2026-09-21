using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace OpenSpeed.Classic.UnitTests.Tracks.NeedForSpeed2
{
    internal static class NeedForSpeed2TrackFixture
    {
        private static int TrackBlockOffset => 0x60;

        private static int TrackBlockHeaderSize => 0x58;

        internal static string MaterialRelativePath
            => Path.Combine("gAmEdAtA", "tRaCkS", "sE", "TR02.COL");

        internal static void Write(string rootDirectory)
        {
            string trackDirectory = Path.Combine(rootDirectory, "gAmEdAtA", "tRaCkS", "sE");
            Directory.CreateDirectory(trackDirectory);
            File.WriteAllBytes(
                Path.Combine(trackDirectory, "TR02.TRK"),
                BuildTrackArchive(BuildTrackGeometry()));
            File.WriteAllBytes(Path.Combine(trackDirectory, "TR02.COL"), BuildMaterials());
            File.WriteAllBytes(
                Path.Combine(trackDirectory, "TR020.QFS"),
                CompressWithLiteralCommands(BuildTextureArchive()));
        }

        private static byte[] BuildTrackGeometry()
        {
            int vertexCount = 4;
            int polygonCount = 1;
            int blockSize =
                TrackBlockHeaderSize +
                vertexCount * 6 +
                polygonCount * 8;
            byte[] data = new byte[TrackBlockOffset + blockSize];
            Encoding.ASCII.GetBytes("TRAC").CopyTo(data, 0);
            WriteInt32LittleEndian(data, 4, 0x16);
            WriteInt32LittleEndian(data, 0x18, 1);
            WriteInt32LittleEndian(data, 0x1C, 1);
            WriteInt32LittleEndian(data, 0x20, 0x40);
            WriteInt32LittleEndian(data, 0x24, 4 * 65536);
            WriteInt32LittleEndian(data, 0x28, 8 * 65536);
            WriteInt32LittleEndian(data, 0x2C, 16 * 65536);
            WriteInt16LittleEndian(data, 0x30, 0);
            WriteInt32LittleEndian(data, 0x40, data.Length - 0x40);
            WriteInt32LittleEndian(data, 0x44, 1);
            WriteInt32LittleEndian(data, 0x4C, TrackBlockOffset - 0x40);
            WriteInt32LittleEndian(data, TrackBlockOffset, blockSize);
            WriteInt32LittleEndian(data, TrackBlockOffset + 4, blockSize);
            WriteInt32LittleEndian(data, TrackBlockOffset + 0x0C, 0);
            WriteInt16LittleEndian(data, TrackBlockOffset + 0x4A, vertexCount);
            WriteInt16LittleEndian(data, TrackBlockOffset + 0x54, polygonCount);
            int vertexOffset = TrackBlockOffset + TrackBlockHeaderSize;
            WriteVertex(data, vertexOffset, 0, 0, 0);
            WriteVertex(data, vertexOffset + 6, 256, 0, 0);
            WriteVertex(data, vertexOffset + 12, 256, 0, 256);
            WriteVertex(data, vertexOffset + 18, 0, 0, 256);
            int polygonOffset = vertexOffset + vertexCount * 6;
            WriteInt16LittleEndian(data, polygonOffset, 0);
            WriteInt16LittleEndian(data, polygonOffset + 2, -1);
            data[polygonOffset + 4] = 0;
            data[polygonOffset + 5] = 1;
            data[polygonOffset + 6] = 2;
            data[polygonOffset + 7] = 3;

            return data;
        }

        private static byte[] BuildTrackArchive(byte[] trackGeometry)
        {
            string entryName = "MAIN.TRK";
            int entryTableSize = sizeof(int) * 2 + Encoding.ASCII.GetByteCount(entryName) + 1;
            int entryOffset = 16 + entryTableSize;
            byte[] archiveData = new byte[entryOffset + trackGeometry.Length];
            Encoding.ASCII.GetBytes("BIGF").CopyTo(archiveData, 0);
            WriteInt32BigEndian(archiveData, 4, archiveData.Length);
            WriteInt32BigEndian(archiveData, 8, 1);
            WriteInt32BigEndian(archiveData, 12, entryOffset);
            WriteInt32BigEndian(archiveData, 16, entryOffset);
            WriteInt32BigEndian(archiveData, 20, trackGeometry.Length);
            Encoding.ASCII.GetBytes(entryName).CopyTo(archiveData, 24);
            trackGeometry.CopyTo(archiveData, entryOffset);

            return archiveData;
        }

        private static byte[] BuildMaterials()
        {
            int recordSize = 18;
            byte[] data = new byte[16 + sizeof(int) + recordSize];
            Encoding.ASCII.GetBytes("COLL").CopyTo(data, 0);
            WriteInt32LittleEndian(data, 4, 0x0B);
            WriteInt32LittleEndian(data, 8, data.Length);
            WriteInt32LittleEndian(data, 12, 1);
            WriteInt32LittleEndian(data, 16, sizeof(int));
            int recordOffset = 20;
            WriteInt32LittleEndian(data, recordOffset, recordSize);
            WriteInt16LittleEndian(data, recordOffset + 4, 2);
            WriteInt16LittleEndian(data, recordOffset + 6, 1);
            WriteInt16LittleEndian(data, recordOffset + 8, 0);
            WriteInt16LittleEndian(data, recordOffset + 10, 0x0401);
            data[recordOffset + 12] = 4;
            data[recordOffset + 13] = 8;
            data[recordOffset + 14] = 16;
            data[recordOffset + 15] = 32;
            data[recordOffset + 16] = 48;
            data[recordOffset + 17] = 64;

            return data;
        }

        private static byte[] BuildTextureArchive()
        {
            byte[] data = new byte[43];
            Encoding.ASCII.GetBytes("SHPI").CopyTo(data, 0);
            WriteInt32LittleEndian(data, 4, data.Length);
            WriteInt32LittleEndian(data, 8, 1);
            Encoding.ASCII.GetBytes("TEST").CopyTo(data, 16);
            WriteInt32LittleEndian(data, 20, 24);
            WriteInt32LittleEndian(data, 24, 0x7F);
            WriteInt16LittleEndian(data, 28, 1);
            WriteInt16LittleEndian(data, 30, 1);
            data[40] = 16;
            data[41] = 32;
            data[42] = 48;

            return data;
        }

        private static byte[] CompressWithLiteralCommands(byte[] data)
        {
            int leadingLiteralCount = data.Length - 3;
            byte literalCommand = (byte)(0xE0 + (leadingLiteralCount - 4) / 4);
            byte[] compressedData = new byte[5 + 1 + leadingLiteralCount + 1 + 3];
            compressedData[0] = 0x10;
            compressedData[1] = 0xFB;
            compressedData[2] = (byte)(data.Length >> 16);
            compressedData[3] = (byte)(data.Length >> 8);
            compressedData[4] = (byte)data.Length;
            compressedData[5] = literalCommand;
            Array.Copy(data, 0, compressedData, 6, leadingLiteralCount);
            compressedData[6 + leadingLiteralCount] = 0xFF;
            Array.Copy(
                data,
                leadingLiteralCount,
                compressedData,
                7 + leadingLiteralCount,
                3);

            return compressedData;
        }

        private static void WriteVertex(
            byte[] data,
            int offset,
            short sourceX,
            short sourceZ,
            short sourceY)
        {
            WriteInt16LittleEndian(data, offset, sourceX);
            WriteInt16LittleEndian(data, offset + sizeof(short), sourceZ);
            WriteInt16LittleEndian(data, offset + sizeof(short) * 2, sourceY);
        }

        private static void WriteInt16LittleEndian(byte[] data, int offset, int value)
            => BinaryPrimitives.WriteInt16LittleEndian(
                data.AsSpan(offset, sizeof(short)),
                (short)value);

        private static void WriteInt32LittleEndian(byte[] data, int offset, int value)
            => BinaryPrimitives.WriteInt32LittleEndian(
                data.AsSpan(offset, sizeof(int)),
                value);

        private static void WriteInt32BigEndian(byte[] data, int offset, int value)
            => BinaryPrimitives.WriteInt32BigEndian(
                data.AsSpan(offset, sizeof(int)),
                value);
    }
}