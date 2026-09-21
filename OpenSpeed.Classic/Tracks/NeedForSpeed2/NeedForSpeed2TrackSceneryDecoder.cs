using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal static class NeedForSpeed2TrackSceneryDecoder
    {
        private static int AnimatedPlacementHeaderSize => 8;

        private static int AnimationFrameSize => 20;

        private static int BasicPlacementSize => 16;

        private static int GeometryHeaderSize => 8;

        private static int LocalCoordinateScale => 256;

        private static int MatrixDimension => 3;

        private static int OptionalPaddingSize => 2;

        private static int PlacementHeaderSize => 4;

        private static int PolygonSize => 8;

        private static int QuaternionMatrixShift => 12;

        private static int QuaternionUnitSquared => 0x10000000;

        private static ushort AlternateObjectPlacementType => 18;

        private static byte AnimatedPlacementType => 3;

        private static byte BasicPlacementType => 1;

        private static ushort ObjectGeometryType => 8;

        private static ushort ObjectPlacementType => 7;

        private static double FixedPointScale => 65536.0;

        internal static IEnumerable<TrackSurface> Decode(
            IEnumerable<NeedForSpeed2TrackExtraBlock> extraBlocks)
        {
            ArgumentNullException.ThrowIfNull(extraBlocks);

            NeedForSpeed2TrackExtraBlock[] blocks = extraBlocks.ToArray();
            NeedForSpeed2TrackExtraBlock? geometryBlock = blocks.FirstOrDefault(
                block => block.TypeIdentifier == ObjectGeometryType);

            if (geometryBlock is null)
            {
                return [];
            }

            NeedForSpeed2ObjectGeometry[] geometries = DecodeGeometries(geometryBlock);
            List<TrackSurface> surfaces = [];

            foreach (NeedForSpeed2TrackExtraBlock placementBlock in blocks)
            {
                if (placementBlock.TypeIdentifier != ObjectPlacementType &&
                    placementBlock.TypeIdentifier != AlternateObjectPlacementType)
                {
                    continue;
                }

                DecodePlacements(placementBlock, geometries, surfaces);
            }

            return surfaces;
        }

        private static NeedForSpeed2ObjectGeometry[] DecodeGeometries(
            NeedForSpeed2TrackExtraBlock extraBlock)
        {
            NeedForSpeed2ObjectGeometry[] geometries =
                new NeedForSpeed2ObjectGeometry[extraBlock.RecordCount];
            ReadOnlySpan<byte> payload = extraBlock.Payload.Span;
            int offset = 0;

            for (int geometryIndex = 0;
                geometryIndex < geometries.Length;
                geometryIndex += 1)
            {
                if (offset < 0 || offset > payload.Length - GeometryHeaderSize)
                {
                    throw new InvalidDataException(
                        $"Object geometry {geometryIndex} has a truncated header.");
                }

                int size = ReadInt32(payload, offset);
                int vertexCount = ReadUInt16(payload, offset + sizeof(int));
                int polygonCount = ReadUInt16(
                    payload,
                    offset + sizeof(int) + sizeof(ushort));
                long vertexTableSize = (long)vertexCount * 6;
                long polygonTableSize = (long)polygonCount * PolygonSize;
                long geometrySize = GeometryHeaderSize + vertexTableSize + polygonTableSize;
                long internalPaddingSize = size - geometrySize;

                if (size < GeometryHeaderSize ||
                    (long)offset + size > payload.Length ||
                    internalPaddingSize != 0 &&
                    internalPaddingSize != OptionalPaddingSize)
                {
                    throw new InvalidDataException(
                        $"Object geometry {geometryIndex} has invalid size {size}.");
                }

                int vertexTableOffset = offset + GeometryHeaderSize;
                int polygonTableOffset = vertexTableOffset + (int)vertexTableSize;
                geometries[geometryIndex] = new NeedForSpeed2ObjectGeometry(
                    DecodeVertices(payload, vertexTableOffset, vertexCount),
                    DecodePolygons(payload, polygonTableOffset, polygonCount));
                offset += size;
            }

            ValidateTrailingPadding(payload.Length - offset, "object geometry");

            return geometries;
        }

        private static NeedForSpeed2ObjectVertex[] DecodeVertices(
            ReadOnlySpan<byte> payload,
            int offset,
            int vertexCount)
        {
            NeedForSpeed2ObjectVertex[] vertices =
                new NeedForSpeed2ObjectVertex[vertexCount];

            for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex += 1)
            {
                int vertexOffset = offset + vertexIndex * 6;
                vertices[vertexIndex] = new NeedForSpeed2ObjectVertex(
                    ReadInt16(payload, vertexOffset),
                    ReadInt16(payload, vertexOffset + sizeof(short)),
                    ReadInt16(payload, vertexOffset + sizeof(short) * 2));
            }

            return vertices;
        }

        private static NeedForSpeed2ObjectPolygon[] DecodePolygons(
            ReadOnlySpan<byte> payload,
            int offset,
            int polygonCount)
        {
            NeedForSpeed2ObjectPolygon[] polygons =
                new NeedForSpeed2ObjectPolygon[polygonCount];

            for (int polygonIndex = 0; polygonIndex < polygonCount; polygonIndex += 1)
            {
                int polygonOffset = offset + polygonIndex * PolygonSize;
                polygons[polygonIndex] = new NeedForSpeed2ObjectPolygon(
                    ReadUInt16(payload, polygonOffset),
                    ReadUInt16(payload, polygonOffset + sizeof(ushort)),
                    payload[polygonOffset + 4],
                    payload[polygonOffset + 5],
                    payload[polygonOffset + 6],
                    payload[polygonOffset + 7]);
            }

            return polygons;
        }

        private static void DecodePlacements(
            NeedForSpeed2TrackExtraBlock extraBlock,
            NeedForSpeed2ObjectGeometry[] geometries,
            List<TrackSurface> surfaces)
        {
            ReadOnlySpan<byte> payload = extraBlock.Payload.Span;
            int offset = 0;

            for (int placementIndex = 0;
                placementIndex < extraBlock.RecordCount;
                placementIndex += 1)
            {
                if (offset < 0 || offset > payload.Length - PlacementHeaderSize)
                {
                    throw new InvalidDataException(
                        $"Object placement {placementIndex} has a truncated header.");
                }

                int size = ReadUInt16(payload, offset);

                if (size < PlacementHeaderSize || (long)offset + size > payload.Length)
                {
                    throw new InvalidDataException(
                        $"Object placement {placementIndex} has invalid size {size}.");
                }

                byte placementType = payload[offset + 2];
                byte geometryIndex = payload[offset + 3];

                if (placementType == BasicPlacementType)
                {
                    DecodeBasicPlacement(
                        payload,
                        offset,
                        size,
                        geometryIndex,
                        geometries,
                        surfaces);
                }
                else if (placementType == AnimatedPlacementType)
                {
                    DecodeAnimatedPlacement(
                        payload,
                        offset,
                        size,
                        geometryIndex,
                        geometries,
                        surfaces);
                }

                offset += size;
            }

            ValidateTrailingPadding(payload.Length - offset, "object placement");
        }

        private static void DecodeBasicPlacement(
            ReadOnlySpan<byte> payload,
            int offset,
            int size,
            byte geometryIndex,
            NeedForSpeed2ObjectGeometry[] geometries,
            List<TrackSurface> surfaces)
        {
            if (size != BasicPlacementSize)
            {
                throw new InvalidDataException(
                    $"Basic object placement has size {size}; expected {BasicPlacementSize}.");
            }

            AddPlacementSurfaces(
                geometries,
                geometryIndex,
                ReadInt32(payload, offset + 4),
                ReadInt32(payload, offset + 8),
                ReadInt32(payload, offset + 12),
                null,
                surfaces);
        }

        private static void DecodeAnimatedPlacement(
            ReadOnlySpan<byte> payload,
            int offset,
            int size,
            byte geometryIndex,
            NeedForSpeed2ObjectGeometry[] geometries,
            List<TrackSurface> surfaces)
        {
            if (size < AnimatedPlacementHeaderSize)
            {
                throw new InvalidDataException(
                    $"Animated object placement has invalid size {size}.");
            }

            int animationLength = ReadUInt16(payload, offset + 4);
            long expectedSize =
                AnimatedPlacementHeaderSize +
                (long)animationLength * AnimationFrameSize;

            if (size != expectedSize)
            {
                throw new InvalidDataException(
                    $"Animated object placement declares {animationLength} frames " +
                    $"in {size} bytes; expected {expectedSize} bytes.");
            }

            if (animationLength == 0)
            {
                return;
            }

            int frameOffset = offset + AnimatedPlacementHeaderSize;
            int[] rotation = CreateQuaternionRotation(
                ReadInt16(payload, frameOffset + 12),
                ReadInt16(payload, frameOffset + 14),
                ReadInt16(payload, frameOffset + 16),
                ReadInt16(payload, frameOffset + 18));
            AddPlacementSurfaces(
                geometries,
                geometryIndex,
                ReadInt32(payload, frameOffset),
                ReadInt32(payload, frameOffset + 4),
                ReadInt32(payload, frameOffset + 8),
                rotation,
                surfaces);
        }

        private static void AddPlacementSurfaces(
            NeedForSpeed2ObjectGeometry[] geometries,
            int geometryIndex,
            int positionX,
            int positionZ,
            int positionY,
            int[]? rotation,
            List<TrackSurface> surfaces)
        {
            if (geometryIndex >= geometries.Length)
            {
                return;
            }

            NeedForSpeed2ObjectGeometry geometry = geometries[geometryIndex];

            for (int polygonIndex = 0;
                polygonIndex < geometry.PolygonCount;
                polygonIndex += 1)
            {
                NeedForSpeed2ObjectPolygon polygon = geometry.GetPolygon(polygonIndex);
                int[] vertexIndices =
                [
                    polygon.FirstVertexIndex,
                    polygon.SecondVertexIndex,
                    polygon.ThirdVertexIndex,
                    polygon.FourthVertexIndex
                ];
                TrackPoint[] points = new TrackPoint[vertexIndices.Length];
                bool verticesAreValid = true;

                for (int pointIndex = 0; pointIndex < vertexIndices.Length; pointIndex += 1)
                {
                    int vertexIndex = vertexIndices[pointIndex];

                    if (vertexIndex >= geometry.VertexCount)
                    {
                        verticesAreValid = false;

                        break;
                    }

                    points[pointIndex] = ResolveVertex(
                        geometry.GetVertex(vertexIndex),
                        positionX,
                        positionZ,
                        positionY,
                        rotation);
                }

                if (!verticesAreValid)
                {
                    continue;
                }

                surfaces.Add(new TrackSurface
                {
                    Identifier = surfaces.Count,
                    DetailLevel = TrackGeometryDetailLevel.Unrestricted,
                    Group = TrackSurfaceGroup.Unrestricted,
                    LightingLevels = polygon.LightingLevels,
                    MaterialIdentifier = polygon.MaterialIdentifier,
                    Points = points
                });
            }
        }

        private static TrackPoint ResolveVertex(
            NeedForSpeed2ObjectVertex vertex,
            int positionX,
            int positionZ,
            int positionY,
            int[]? rotation)
        {
            int[] localPosition =
            [
                vertex.SourceX * LocalCoordinateScale,
                vertex.SourceZ * LocalCoordinateScale,
                vertex.SourceY * LocalCoordinateScale
            ];

            if (rotation is not null)
            {
                localPosition = Transform(localPosition, rotation);
            }

            return CreateWorldPoint(
                unchecked(positionX + localPosition[0]),
                unchecked(positionZ + localPosition[1]),
                unchecked(positionY + localPosition[2]));
        }

        private static int[] CreateQuaternionRotation(
            short quaternionX,
            short quaternionY,
            short quaternionZ,
            short quaternionW)
        {
            int doubledX = quaternionX * 2;
            int doubledY = quaternionY * 2;
            int doubledZ = quaternionZ * 2;
            int xx = unchecked(doubledX * quaternionX);
            int xy = unchecked(doubledX * quaternionY);
            int xz = unchecked(doubledX * quaternionZ);
            int xw = unchecked(doubledX * quaternionW);
            int yy = unchecked(doubledY * quaternionY);
            int yz = unchecked(doubledY * quaternionZ);
            int yw = unchecked(doubledY * quaternionW);
            int zz = unchecked(doubledZ * quaternionZ);
            int zw = unchecked(doubledZ * quaternionW);

            return
            [
                unchecked(QuaternionUnitSquared - yy - zz) >> QuaternionMatrixShift,
                unchecked(xy + zw) >> QuaternionMatrixShift,
                unchecked(xz - yw) >> QuaternionMatrixShift,
                unchecked(xy - zw) >> QuaternionMatrixShift,
                unchecked(QuaternionUnitSquared - xx - zz) >> QuaternionMatrixShift,
                unchecked(xw + yz) >> QuaternionMatrixShift,
                unchecked(xz + yw) >> QuaternionMatrixShift,
                unchecked(yz - xw) >> QuaternionMatrixShift,
                unchecked(QuaternionUnitSquared - xx - yy) >> QuaternionMatrixShift
            ];
        }

        private static int[] Transform(int[] vector, int[] matrix)
        {
            int[] result = new int[MatrixDimension];

            for (int column = 0; column < MatrixDimension; column += 1)
            {
                long value = unchecked((long)vector[0] * matrix[column]);
                value = unchecked(value + (long)vector[1] * matrix[MatrixDimension + column]);
                value = unchecked(
                    value + (long)vector[2] * matrix[MatrixDimension * 2 + column]);
                result[column] = unchecked((int)(value >> 16));
            }

            return result;
        }

        private static TrackPoint CreateWorldPoint(int sourceX, int sourceZ, int sourceY)
            => new()
            {
                X = sourceX / FixedPointScale,
                Y = sourceZ / FixedPointScale,
                Z = -sourceY / FixedPointScale
            };

        private static void ValidateTrailingPadding(int paddingSize, string recordType)
        {
            if (paddingSize != 0 && paddingSize != OptionalPaddingSize)
            {
                throw new InvalidDataException(
                    $"The {recordType} payload contains {paddingSize} trailing bytes.");
            }
        }

        private static int ReadInt32(ReadOnlySpan<byte> data, int offset)
            => BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset, sizeof(int)));

        private static short ReadInt16(ReadOnlySpan<byte> data, int offset)
            => BinaryPrimitives.ReadInt16LittleEndian(data.Slice(offset, sizeof(short)));

        private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset)
            => BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, sizeof(ushort)));
    }
}