using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenSpeed.Classic.Cars.NeedForSpeed2
{
    public static class NeedForSpeed2CarGeometryDecoder
    {
        private static readonly int[] MirroredQuadVertexIndices = [0, 3, 2, 0, 2, 1];

        private static readonly int[] MirroredTriangleVertexIndices = [2, 1, 0];

        private static readonly int[] StandardQuadVertexIndices = [0, 1, 2, 0, 2, 3];

        private static readonly int[] StandardTriangleVertexIndices = [0, 1, 2];

        private static readonly int[] TextureCornerBySourceVertex = [3, 2, 1, 0];

        private static int DescriptorSize => 0x34;

        private static CarIdentifier FordGt90Identifier => CarIdentifier.FordGT90;

        private static int HeaderSize => 0x8C;

        private static int HighDetailSectionCount => 20;

        private static uint MirroredFlag => 0x04;

        private static int PolygonSize => 12;

        private static int PositiveVertexPositionXAdjustment => 5;

        private static int NegativeVertexPositionXAdjustment => -PositiveVertexPositionXAdjustment;

        private static uint QuadFlag => 0x02;

        private static int SectionCount => 0x20;

        private static int TextureNameLength => 4;

        private static int TextureNameOffset => 8;

        private static int VertexSize => 6;

        public static IEnumerable<CarGeometryTriangle> Decode(byte[] data)
            => Decode(data, null);

        public static IEnumerable<CarGeometryTriangle> Decode(
            byte[] data,
            CarIdentifier carIdentifier)
            => Decode(data, carIdentifier as CarIdentifier?);

        private static IEnumerable<CarGeometryTriangle> Decode(
            byte[] data,
            CarIdentifier? carIdentifier)
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
                position = DecodeSection(
                    data,
                    position,
                    sectionIndex,
                    carIdentifier,
                    triangles);
            }

            return triangles;
        }

        private static int DecodeSection(
            byte[] data,
            int descriptorOffset,
            int sectionIndex,
            CarIdentifier? carIdentifier,
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
                    sectionIndex,
                    carIdentifier,
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
            int sectionIndex,
            CarIdentifier? carIdentifier,
            List<CarGeometryTriangle> triangles)
        {
            int sectionPositionX = ReadInt32(data, descriptorOffset + 8);
            int sectionPositionY = ReadInt32(data, descriptorOffset + 16);
            int sectionPositionZ = ReadInt32(data, descriptorOffset + 12);
            int vertexPositionXAdjustment = GetVertexPositionXAdjustment(
                sectionIndex,
                carIdentifier);
            HashSet<byte> adjustedVertexIndices = GetAdjustedVertexIndices(
                data,
                polygonTableOffset,
                polygonCount,
                vertexPositionXAdjustment);

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
                    firstVertexIndex,
                    vertexPositionXAdjustment,
                    adjustedVertexIndices);
                CarGeometryVertex second = DecodeVertex(
                    data,
                    vertexTableOffset,
                    secondVertexIndex,
                    vertexPositionXAdjustment,
                    adjustedVertexIndices);
                CarGeometryVertex third = DecodeVertex(
                    data,
                    vertexTableOffset,
                    thirdVertexIndex,
                    vertexPositionXAdjustment,
                    adjustedVertexIndices);
                int textureRegistrationMode = GetTextureRegistrationMode(mappingFlags);
                CarGeometryVertex[] polygonVertices = [first, second, third];
                int[] triangleVertexIndices = StandardTriangleVertexIndices;

                if ((mappingFlags & QuadFlag) != 0)
                {
                    ValidateVertexIndex(fourthVertexIndex, vertexCount);
                    polygonVertices =
                    [
                        first,
                        second,
                        third,
                        DecodeVertex(
                            data,
                            vertexTableOffset,
                            fourthVertexIndex,
                            vertexPositionXAdjustment,
                            adjustedVertexIndices)
                    ];
                    triangleVertexIndices = StandardQuadVertexIndices;
                }

                if ((mappingFlags & MirroredFlag) != 0)
                {
                    triangleVertexIndices = MirroredTriangleVertexIndices;

                    if ((mappingFlags & QuadFlag) != 0)
                    {
                        triangleVertexIndices = MirroredQuadVertexIndices;
                    }
                }

                for (
                    int triangleVertexIndexOffset = 0;
                    triangleVertexIndexOffset < triangleVertexIndices.Length;
                    triangleVertexIndexOffset += 3)
                {
                    int firstSourceVertexIndex =
                        triangleVertexIndices[triangleVertexIndexOffset];
                    int secondSourceVertexIndex =
                        triangleVertexIndices[triangleVertexIndexOffset + 1];
                    int thirdSourceVertexIndex =
                        triangleVertexIndices[triangleVertexIndexOffset + 2];
                    triangles.Add(new CarGeometryTriangle(
                        sectionPositionX,
                        sectionPositionY,
                        sectionPositionZ,
                        textureName,
                        polygonVertices[firstSourceVertexIndex],
                        polygonVertices[secondSourceVertexIndex],
                        polygonVertices[thirdSourceVertexIndex],
                        TextureCornerBySourceVertex[firstSourceVertexIndex],
                        TextureCornerBySourceVertex[secondSourceVertexIndex],
                        TextureCornerBySourceVertex[thirdSourceVertexIndex],
                        mappingFlags,
                        textureRegistrationMode));
                }
            }
        }

        private static HashSet<byte> GetAdjustedVertexIndices(
            byte[] data,
            int polygonTableOffset,
            int polygonCount,
            int vertexPositionXAdjustment)
        {
            if (polygonCount == 0 || vertexPositionXAdjustment == 0)
            {
                return [];
            }

            return
            [
                data[polygonTableOffset + 4],
                data[polygonTableOffset + 5],
                data[polygonTableOffset + 6],
                data[polygonTableOffset + 7]
            ];
        }

        private static int GetVertexPositionXAdjustment(
            int sectionIndex,
            CarIdentifier? carIdentifier)
        {
            if (carIdentifier == FordGt90Identifier)
            {
                if (sectionIndex == 12)
                {
                    return PositiveVertexPositionXAdjustment;
                }

                if (sectionIndex == 14)
                {
                    return NegativeVertexPositionXAdjustment;
                }
            }

            if (carIdentifier is not null && carIdentifier < CarIdentifier.BonusCarFuture)
            {
                if (sectionIndex == 16)
                {
                    return PositiveVertexPositionXAdjustment;
                }

                if (sectionIndex == 18)
                {
                    return NegativeVertexPositionXAdjustment;
                }
            }

            return 0;
        }

        private static int GetTextureRegistrationMode(uint mappingFlags)
        {
            if ((mappingFlags & MirroredFlag) == 0)
            {
                return 3;
            }

            if ((mappingFlags & 0x01) == 0)
            {
                return 2;
            }

            return 4;
        }

        private static CarGeometryVertex DecodeVertex(
            byte[] data,
            int vertexTableOffset,
            int vertexIndex,
            int vertexPositionXAdjustment,
            HashSet<byte> adjustedVertexIndices)
        {
            int vertexOffset = vertexTableOffset + vertexIndex * VertexSize;
            short positionX = BinaryPrimitives.ReadInt16LittleEndian(
                data.AsSpan(vertexOffset, sizeof(short)));

            if (adjustedVertexIndices.Contains((byte)vertexIndex))
            {
                positionX = unchecked((short)(positionX + vertexPositionXAdjustment));
            }

            return new CarGeometryVertex(
                positionX,
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