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

        private static int TrackPolygonSize => 8;

        private static int TrackVertexSize => 6;

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
                CompressWithLiteralCommands(BuildTextureArchive("TEST")));
            File.WriteAllText(
                Path.Combine(trackDirectory, "3TR02.HRZ"),
                BuildHorizon());
            File.WriteAllBytes(
                Path.Combine(trackDirectory, "SKY.FSH"),
                BuildTextureArchive("CLD2"));
        }

        private static byte[] BuildTrackGeometry()
        {
            int vertexCount = 4;
            int polygonCount = 1;
            int trackGeometrySize =
                TrackBlockHeaderSize +
                vertexCount * TrackVertexSize +
                polygonCount * TrackPolygonSize;
            int extraBlockTableOffset = trackGeometrySize;
            int objectGeometryBlockOffset = extraBlockTableOffset + sizeof(int) * 3;
            int objectGeometryBlockSize = 48;
            int placementBlockOffset = objectGeometryBlockOffset + objectGeometryBlockSize;
            int placementBlockSize = 52;
            int visibilityBlockOffset = placementBlockOffset + placementBlockSize;
            int visibilityBlockSize = 12;
            int blockSize = visibilityBlockOffset + visibilityBlockSize;
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
            WriteInt16LittleEndian(data, TrackBlockOffset + 0x08, 3);
            WriteInt32LittleEndian(data, TrackBlockOffset + 0x0C, 0);
            WriteInt32LittleEndian(
                data,
                TrackBlockOffset + 0x40,
                extraBlockTableOffset - 0x40);
            WriteInt16LittleEndian(data, TrackBlockOffset + 0x4A, vertexCount);
            WriteInt16LittleEndian(data, TrackBlockOffset + 0x54, polygonCount);
            int vertexOffset = TrackBlockOffset + TrackBlockHeaderSize;
            WriteVertex(data, vertexOffset, 0, 0, 0);
            WriteVertex(data, vertexOffset + TrackVertexSize, 256, 0, 0);
            WriteVertex(data, vertexOffset + TrackVertexSize * 2, 256, 0, 256);
            WriteVertex(data, vertexOffset + TrackVertexSize * 3, 0, 0, 256);
            int polygonOffset = vertexOffset + vertexCount * TrackVertexSize;
            WriteInt16LittleEndian(data, polygonOffset, 0);
            WriteInt16LittleEndian(data, polygonOffset + 2, -1);
            data[polygonOffset + 4] = 0;
            data[polygonOffset + 5] = 1;
            data[polygonOffset + 6] = 2;
            data[polygonOffset + 7] = 3;
            WriteInt32LittleEndian(
                data,
                TrackBlockOffset + extraBlockTableOffset,
                objectGeometryBlockOffset);
            WriteInt32LittleEndian(
                data,
                TrackBlockOffset + extraBlockTableOffset + sizeof(int),
                placementBlockOffset);
            WriteInt32LittleEndian(
                data,
                TrackBlockOffset + extraBlockTableOffset + sizeof(int) * 2,
                visibilityBlockOffset);
            WriteObjectGeometryBlock(
                data,
                TrackBlockOffset + objectGeometryBlockOffset);
            WritePlacementBlock(
                data,
                TrackBlockOffset + placementBlockOffset);
            WriteVisibilityBlock(
                data,
                TrackBlockOffset + visibilityBlockOffset);

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
            int recordCount = 3;
            int recordOffsetTableSize = recordCount * sizeof(int);
            int materialRecordOffset = recordOffsetTableSize;
            int materialRecordSize = 18;
            int objectGeometryRecordOffset = materialRecordOffset + materialRecordSize;
            int objectGeometryRecordSize = 48;
            int placementRecordOffset = objectGeometryRecordOffset + objectGeometryRecordSize;
            int placementRecordSize = 24;
            byte[] data = new byte[16 + placementRecordOffset + placementRecordSize];
            Encoding.ASCII.GetBytes("COLL").CopyTo(data, 0);
            WriteInt32LittleEndian(data, 4, 0x0B);
            WriteInt32LittleEndian(data, 8, data.Length);
            WriteInt32LittleEndian(data, 12, recordCount);
            WriteInt32LittleEndian(data, 16, materialRecordOffset);
            WriteInt32LittleEndian(data, 20, objectGeometryRecordOffset);
            WriteInt32LittleEndian(data, 24, placementRecordOffset);
            int materialFileOffset = 16 + materialRecordOffset;
            WriteInt32LittleEndian(data, materialFileOffset, materialRecordSize);
            WriteInt16LittleEndian(data, materialFileOffset + 4, 2);
            WriteInt16LittleEndian(data, materialFileOffset + 6, 1);
            WriteInt16LittleEndian(data, materialFileOffset + 8, 0);
            WriteInt16LittleEndian(data, materialFileOffset + 10, 0x0401);
            data[materialFileOffset + 12] = 4;
            data[materialFileOffset + 13] = 8;
            data[materialFileOffset + 14] = 16;
            data[materialFileOffset + 15] = 32;
            data[materialFileOffset + 16] = 48;
            data[materialFileOffset + 17] = 64;
            WriteObjectGeometryBlock(data, 16 + objectGeometryRecordOffset);
            int placementFileOffset = 16 + placementRecordOffset;
            WriteInt32LittleEndian(data, placementFileOffset, placementRecordSize);
            WriteInt16LittleEndian(data, placementFileOffset + 4, 7);
            WriteInt16LittleEndian(data, placementFileOffset + 6, 1);
            WriteBasicPlacement(
                data,
                placementFileOffset + 8,
                10 * 65536,
                8 * 65536,
                18 * 65536);

            return data;
        }

        private static void WriteObjectGeometryBlock(byte[] data, int blockOffset)
        {
            int objectGeometrySize = 40;
            int extraBlockSize = 8 + objectGeometrySize;
            WriteInt32LittleEndian(data, blockOffset, extraBlockSize);
            WriteInt16LittleEndian(data, blockOffset + 4, 8);
            WriteInt16LittleEndian(data, blockOffset + 6, 1);
            int geometryOffset = blockOffset + 8;
            WriteInt32LittleEndian(data, geometryOffset, objectGeometrySize);
            WriteInt16LittleEndian(data, geometryOffset + 4, 4);
            WriteInt16LittleEndian(data, geometryOffset + 6, 1);
            int vertexOffset = geometryOffset + 8;
            WriteVertex(data, vertexOffset, 0, 0, 0);
            WriteVertex(data, vertexOffset + TrackVertexSize, 256, 0, 0);
            WriteVertex(data, vertexOffset + TrackVertexSize * 2, 256, 512, 0);
            WriteVertex(data, vertexOffset + TrackVertexSize * 3, 0, 512, 0);
            int polygonOffset = vertexOffset + TrackVertexSize * 4;
            WriteInt16LittleEndian(data, polygonOffset, 0);
            WriteInt16LittleEndian(data, polygonOffset + 2, -1);
            data[polygonOffset + 4] = 0;
            data[polygonOffset + 5] = 1;
            data[polygonOffset + 6] = 2;
            data[polygonOffset + 7] = 3;
        }

        private static void WritePlacementBlock(byte[] data, int blockOffset)
        {
            int basicPlacementSize = 16;
            int animatedPlacementSize = 28;
            int payloadSize = basicPlacementSize + animatedPlacementSize;
            WriteInt32LittleEndian(data, blockOffset, 8 + payloadSize);
            WriteInt16LittleEndian(data, blockOffset + 4, 7);
            WriteInt16LittleEndian(data, blockOffset + 6, 2);
            int payloadOffset = blockOffset + 8;
            WriteBasicPlacement(
                data,
                payloadOffset,
                6 * 65536,
                8 * 65536,
                18 * 65536);
            int animatedPlacementOffset = payloadOffset + basicPlacementSize;
            WriteInt16LittleEndian(
                data,
                animatedPlacementOffset,
                animatedPlacementSize);
            data[animatedPlacementOffset + 2] = 3;
            data[animatedPlacementOffset + 3] = 0;
            WriteInt16LittleEndian(data, animatedPlacementOffset + 4, 1);
            WriteInt16LittleEndian(data, animatedPlacementOffset + 6, 8);
            int frameOffset = animatedPlacementOffset + 8;
            WriteInt32LittleEndian(data, frameOffset, 8 * 65536);
            WriteInt32LittleEndian(data, frameOffset + 4, 8 * 65536);
            WriteInt32LittleEndian(data, frameOffset + 8, 18 * 65536);
            WriteInt16LittleEndian(data, frameOffset + 18, 0x4000);
        }

        private static void WriteVisibilityBlock(byte[] data, int blockOffset)
        {
            WriteInt32LittleEndian(data, blockOffset, 12);
            WriteInt16LittleEndian(data, blockOffset + 4, 4);
            WriteInt16LittleEndian(data, blockOffset + 6, 2);
            WriteInt16LittleEndian(data, blockOffset + 8, 0);
            WriteInt16LittleEndian(data, blockOffset + 10, 42);
        }

        private static void WriteBasicPlacement(
            byte[] data,
            int offset,
            int sourceX,
            int sourceZ,
            int sourceY)
        {
            WriteInt16LittleEndian(data, offset, 16);
            data[offset + 2] = 1;
            data[offset + 3] = 0;
            WriteInt32LittleEndian(data, offset + 4, sourceX);
            WriteInt32LittleEndian(data, offset + 8, sourceZ);
            WriteInt32LittleEndian(data, offset + 12, sourceY);
        }

        private static string BuildHorizon()
            => string.Join(
                " ",
                new[]
                {
                    -100, 250, 0, 1, 1500, 300, 1000, -750, 1250,
                    735, 960, 600, 132, 117, 115, 132, 117, 115,
                    255, 122, 66, 255, 222, 91, 0, 0, 130
                });

        private static byte[] BuildTextureArchive(string textureName)
        {
            byte[] data = new byte[43];
            Encoding.ASCII.GetBytes("SHPI").CopyTo(data, 0);
            WriteInt32LittleEndian(data, 4, data.Length);
            WriteInt32LittleEndian(data, 8, 1);
            Encoding.ASCII.GetBytes(textureName).CopyTo(data, 16);
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