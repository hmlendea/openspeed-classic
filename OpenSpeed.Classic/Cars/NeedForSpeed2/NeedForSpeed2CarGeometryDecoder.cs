using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenSpeed.Classic.Cars.NeedForSpeed2
{
    public static class NeedForSpeed2CarGeometryDecoder
    {
        private static int DescriptorSize => 0x34;

        private static int HeaderSize => 0x8C;

        private static int HighDetailSectionCount => 19;

        private static int PolygonSize => 12;

        private static uint QuadFlag => 0x02;

        private static int SectionCount => 0x20;

        private static int TextureNameLength => 4;

        private static int TextureNameOffset => 8;

        private static int VertexSize => 6;

        public static IEnumerable<CarGeometryTriangle> Decode(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (data.Length < HeaderSize)
            {
                throw new InvalidDataException("The NFS II car geometry header is truncated.");
            }

            List<CarGeometryTriangle> triangles = [];
            int position = HeaderSize;

            for (int sectionIndex = 0; sectionIndex < SectionCount; sectionIndex += 1)
            {
                position = DecodeSection(data, position, sectionIndex, triangles);
            }

            return triangles;
        }

        private static int DecodeSection(
            byte[] data,
            int descriptorOffset,
            int sectionIndex,
            List<CarGeometryTriangle> triangles)
        {
            if (descriptorOffset > data.Length - DescriptorSize)
            {
                throw new InvalidDataException("An NFS II car geometry section descriptor is truncated.");
            }

            int vertexCount = ReadInt32(data, descriptorOffset);
            int polygonCount = ReadInt32(data, descriptorOffset + sizeof(int));

            if (vertexCount < 0 || polygonCount < 0)
            {
                throw new InvalidDataException("An NFS II car geometry section has a negative element count.");
            }

            long storedVertexCount = (long)vertexCount + (vertexCount & 1);
            long vertexTableOffset = (long)descriptorOffset + DescriptorSize;
            long polygonTableOffset = vertexTableOffset + storedVertexCount * VertexSize;
            long sectionEnd = polygonTableOffset + (long)polygonCount * PolygonSize;

            if (sectionEnd > data.Length || sectionEnd > int.MaxValue)
            {
                throw new InvalidDataException("An NFS II car geometry section payload is truncated.");
            }

            if (sectionIndex < HighDetailSectionCount)
            {
                DecodeHighDetailPolygons(
                    data,
                    descriptorOffset,
                    (int)vertexTableOffset,
                    vertexCount,
                    (int)polygonTableOffset,
                    polygonCount,
                    triangles);
            }

            return (int)sectionEnd;
        }

        private static void DecodeHighDetailPolygons(
            byte[] data,
            int descriptorOffset,
            int vertexTableOffset,
            int vertexCount,
            int polygonTableOffset,
            int polygonCount,
            List<CarGeometryTriangle> triangles)
        {
            int sectionPositionX = ReadInt32(data, descriptorOffset + 8);
            int sectionPositionY = ReadInt32(data, descriptorOffset + 16);
            int sectionPositionZ = ReadInt32(data, descriptorOffset + 12);

            for (int polygonIndex = 0; polygonIndex < polygonCount; polygonIndex += 1)
            {
                int polygonOffset = polygonTableOffset + polygonIndex * PolygonSize;
                uint mappingFlags = BinaryPrimitives.ReadUInt32LittleEndian(
                    data.AsSpan(polygonOffset, sizeof(uint)));
                byte firstVertexIndex = data[polygonOffset + 4];
                byte secondVertexIndex = data[polygonOffset + 5];
                byte thirdVertexIndex = data[polygonOffset + 6];
                byte fourthVertexIndex = data[polygonOffset + 7];
                string textureName = Encoding.ASCII.GetString(
                    data,
                    polygonOffset + TextureNameOffset,
                    TextureNameLength);
                ValidateVertexIndex(firstVertexIndex, vertexCount);
                ValidateVertexIndex(secondVertexIndex, vertexCount);
                ValidateVertexIndex(thirdVertexIndex, vertexCount);
                CarGeometryVertex first = DecodeVertex(
                    data,
                    vertexTableOffset,
                    firstVertexIndex);
                CarGeometryVertex second = DecodeVertex(
                    data,
                    vertexTableOffset,
                    secondVertexIndex);
                CarGeometryVertex third = DecodeVertex(
                    data,
                    vertexTableOffset,
                    thirdVertexIndex);
                triangles.Add(new CarGeometryTriangle(
                    sectionPositionX,
                    sectionPositionY,
                    sectionPositionZ,
                    textureName,
                    first,
                    second,
                    third,
                    0,
                    1,
                    2));

                if ((mappingFlags & QuadFlag) == 0)
                {
                    continue;
                }

                ValidateVertexIndex(fourthVertexIndex, vertexCount);
                triangles.Add(new CarGeometryTriangle(
                    sectionPositionX,
                    sectionPositionY,
                    sectionPositionZ,
                    textureName,
                    first,
                    third,
                    DecodeVertex(data, vertexTableOffset, fourthVertexIndex),
                    0,
                    2,
                    3));
            }
        }

        private static CarGeometryVertex DecodeVertex(
            byte[] data,
            int vertexTableOffset,
            int vertexIndex)
        {
            int vertexOffset = vertexTableOffset + vertexIndex * VertexSize;

            return new CarGeometryVertex(
                BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(vertexOffset, sizeof(short))),
                BinaryPrimitives.ReadInt16LittleEndian(
                    data.AsSpan(vertexOffset + sizeof(short) * 2, sizeof(short))),
                BinaryPrimitives.ReadInt16LittleEndian(
                    data.AsSpan(vertexOffset + sizeof(short), sizeof(short))));
        }

        private static void ValidateVertexIndex(int vertexIndex, int vertexCount)
        {
            if (vertexIndex >= vertexCount)
            {
                throw new InvalidDataException(
                    $"An NFS II car polygon references vertex {vertexIndex}, " +
                    $"but its section contains {vertexCount} vertices.");
            }
        }

        private static int ReadInt32(byte[] data, int offset)
            => BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, sizeof(int)));
    }
}