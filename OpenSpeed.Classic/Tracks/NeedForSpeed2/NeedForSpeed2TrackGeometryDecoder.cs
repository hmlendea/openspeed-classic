using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal static class NeedForSpeed2TrackGeometryDecoder
    {
        private static int HeaderSize => 0x20;

        private static int ExpectedVersion => 0x16;

        private static int StreamChunkHeaderSize => 12;

        private static int TrackBlockHeaderSize => 0x58;

        private static int VertexSize => 6;

        private static int PolygonSize => 8;

        private static double FixedPointScale => 65536.0;

        private static double LocalVertexScale => 256.0;

        internal static IEnumerable<TrackBlock> Decode(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            ValidateHeader(data);

            int streamChunkCount = ReadInt32(data, 0x18);
            int centreCount = ReadInt32(data, 0x1C);

            if (streamChunkCount < 0 || centreCount < 0)
            {
                throw new InvalidDataException("The TRAC resource contains a negative table count.");
            }

            long tableLength =
                (long)streamChunkCount * sizeof(int) +
                (long)centreCount * 12 +
                (long)centreCount * sizeof(short);

            if (tableLength > data.Length - HeaderSize)
            {
                throw new InvalidDataException("The TRAC resource tables are truncated.");
            }

            int tablePosition = HeaderSize;
            int[] streamChunkOffsets = ReadStreamChunkOffsets(
                data,
                tablePosition,
                streamChunkCount);
            tablePosition += streamChunkCount * sizeof(int);
            TrackPoint[] centrePoints = ReadCentrePoints(data, tablePosition, centreCount);
            tablePosition += centreCount * 12;
            short[] blockSuperBlockIndices = ReadBlockSuperBlockIndices(
                data,
                tablePosition,
                centreCount);
            int streamDataOffset = tablePosition + centreCount * sizeof(short);
            Dictionary<int, TrackBlock>[] superBlocks = DecodeSuperBlocks(
                data,
                streamDataOffset,
                streamChunkOffsets,
                centrePoints);

            return OrderBlocks(blockSuperBlockIndices, superBlocks);
        }

        private static Dictionary<int, TrackBlock>[] DecodeSuperBlocks(
            byte[] data,
            int streamDataOffset,
            int[] streamChunkOffsets,
            TrackPoint[] centrePoints)
        {
            Dictionary<int, TrackBlock>[] superBlocks =
                new Dictionary<int, TrackBlock>[streamChunkOffsets.Length];

            for (int streamChunkIndex = 0;
                streamChunkIndex < streamChunkOffsets.Length;
                streamChunkIndex += 1)
            {
                int streamChunkOffset = streamChunkOffsets[streamChunkIndex];

                if (streamChunkOffset < streamDataOffset ||
                    streamChunkOffset > data.Length - StreamChunkHeaderSize)
                {
                    throw new InvalidDataException(
                        $"TRAC stream chunk {streamChunkIndex} has an invalid offset.");
                }

                int streamChunkSize = ReadInt32(data, streamChunkOffset);
                int childCount = ReadInt32(data, streamChunkOffset + sizeof(int));
                long streamChunkEnd = (long)streamChunkOffset + streamChunkSize;
                long childTableEnd =
                    (long)streamChunkOffset +
                    StreamChunkHeaderSize +
                    (long)childCount * sizeof(int);

                if (streamChunkSize < StreamChunkHeaderSize ||
                    childCount < 0 ||
                    childTableEnd > streamChunkEnd ||
                    streamChunkEnd > data.Length)
                {
                    throw new InvalidDataException(
                        $"TRAC stream chunk {streamChunkIndex} has invalid bounds.");
                }

                superBlocks[streamChunkIndex] = DecodeStreamChunkBlocks(
                    data,
                    streamChunkIndex,
                    streamChunkOffset,
                    childCount,
                    childTableEnd,
                    streamChunkEnd,
                    centrePoints);
            }

            return superBlocks;
        }

        private static Dictionary<int, TrackBlock> DecodeStreamChunkBlocks(
            byte[] data,
            int streamChunkIndex,
            int streamChunkOffset,
            int childCount,
            long childTableEnd,
            long streamChunkEnd,
            TrackPoint[] centrePoints)
        {
            Dictionary<int, TrackBlock> blocks = [];

            for (int childIndex = 0; childIndex < childCount; childIndex += 1)
            {
                int relativeOffset = ReadInt32(
                    data,
                    streamChunkOffset + StreamChunkHeaderSize + childIndex * sizeof(int));
                long blockOffsetValue = (long)streamChunkOffset + relativeOffset;

                if (blockOffsetValue < childTableEnd ||
                    blockOffsetValue > streamChunkEnd - TrackBlockHeaderSize)
                {
                    throw new InvalidDataException(
                        $"TRAC block {childIndex} in stream chunk {streamChunkIndex} " +
                        "has an invalid offset.");
                }

                TrackBlock block = DecodeTrackBlock(
                    data,
                    (int)blockOffsetValue,
                    streamChunkEnd,
                    centrePoints);

                if (!blocks.TryAdd(block.Identifier, block))
                {
                    throw new InvalidDataException(
                        $"TRAC stream chunk {streamChunkIndex} contains duplicate block " +
                        $"identifier {block.Identifier}.");
                }
            }

            return blocks;
        }

        private static TrackBlock DecodeTrackBlock(
            byte[] data,
            int blockOffset,
            long streamChunkEnd,
            TrackPoint[] centrePoints)
        {
            int blockSize = ReadInt32(data, blockOffset);
            int duplicateBlockSize = ReadInt32(data, blockOffset + sizeof(int));
            long blockEnd = (long)blockOffset + blockSize;

            if (blockSize < TrackBlockHeaderSize ||
                blockSize != duplicateBlockSize ||
                blockEnd > streamChunkEnd)
            {
                throw new InvalidDataException(
                    $"The TRAC block at offset {blockOffset} has invalid bounds.");
            }

            int blockIdentifier = ReadInt32(data, blockOffset + 0x0C);

            if (blockIdentifier < 0 || blockIdentifier >= centrePoints.Length)
            {
                throw new InvalidDataException(
                    $"TRAC block identifier {blockIdentifier} has no centre record.");
            }

            int connectedVertexCount = ReadUInt16(data, blockOffset + 0x44);
            int lowDetailVertexCount = ReadUInt16(data, blockOffset + 0x46);
            int mediumDetailVertexCount = ReadUInt16(data, blockOffset + 0x48);
            int highDetailVertexCount = ReadUInt16(data, blockOffset + 0x4A);

            if (lowDetailVertexCount > mediumDetailVertexCount ||
                mediumDetailVertexCount > highDetailVertexCount)
            {
                throw new InvalidDataException(
                    $"TRAC block {blockIdentifier} has an invalid vertex LOD hierarchy.");
            }

            int[] polygonGroupCounts =
            [
                ReadUInt16(data, blockOffset + 0x4C),
                ReadUInt16(data, blockOffset + 0x4E),
                ReadUInt16(data, blockOffset + 0x50),
                ReadUInt16(data, blockOffset + 0x52),
                ReadUInt16(data, blockOffset + 0x54),
                ReadUInt16(data, blockOffset + 0x56)
            ];
            int storedVertexCount = connectedVertexCount + highDetailVertexCount;
            int storedPolygonCount = 0;

            foreach (int polygonGroupCount in polygonGroupCounts)
            {
                storedPolygonCount += polygonGroupCount;
            }

            long vertexTableOffset = (long)blockOffset + TrackBlockHeaderSize;
            long polygonTableOffset = vertexTableOffset + (long)storedVertexCount * VertexSize;
            long geometryEnd = polygonTableOffset + (long)storedPolygonCount * PolygonSize;

            if (geometryEnd > blockEnd)
            {
                throw new InvalidDataException(
                    $"TRAC block {blockIdentifier} contains geometry beyond its bounds.");
            }

            TrackPoint[] vertices = DecodeVertices(
                data,
                (int)vertexTableOffset,
                storedVertexCount,
                connectedVertexCount,
                blockIdentifier,
                centrePoints);
            int extraBlockCount = ReadUInt16(data, blockOffset + 0x08);
            int extraBlockTableRelativeOffset = ReadInt32(data, blockOffset + 0x40);
            long extraBlockTableFileOffset =
                (long)blockOffset +
                0x40 +
                extraBlockTableRelativeOffset;
            NeedForSpeed2TrackExtraBlock[] extraBlocks =
            [
                .. NeedForSpeed2TrackExtraBlockReader.ReadTrackBlock(
                    data,
                    blockOffset,
                    extraBlockCount,
                    extraBlockTableFileOffset,
                    geometryEnd,
                    blockEnd)
            ];

            return new TrackBlock
            {
                Identifier = blockIdentifier,
                Centre = centrePoints[blockIdentifier],
                ClippingPoints = DecodeClippingPoints(data, blockOffset),
                ConnectedVertexCount = connectedVertexCount,
                ScenerySurfaces = NeedForSpeed2TrackSceneryDecoder.Decode(extraBlocks),
                Surfaces = DecodeSurfaces(
                    data,
                    (int)polygonTableOffset,
                    polygonGroupCounts,
                    vertices,
                    blockIdentifier),
                VisibleBlockIdentifiers = NeedForSpeed2TrackVisibilityDecoder.Decode(
                    extraBlocks)
            };
        }

        private static IEnumerable<TrackSurface> DecodeSurfaces(
            byte[] data,
            int polygonTableOffset,
            int[] polygonGroupCounts,
            TrackPoint[] vertices,
            int blockIdentifier)
        {
            List<TrackSurface> surfaces = [];
            int polygonIndex = 0;

            for (int groupIndex = 0; groupIndex < polygonGroupCounts.Length; groupIndex += 1)
            {
                for (int groupPolygonIndex = 0;
                    groupPolygonIndex < polygonGroupCounts[groupIndex];
                    groupPolygonIndex += 1)
                {
                    int polygonOffset = polygonTableOffset + polygonIndex * PolygonSize;
                    byte[] vertexIndices =
                    [
                        data[polygonOffset + 4],
                        data[polygonOffset + 5],
                        data[polygonOffset + 6],
                        data[polygonOffset + 7]
                    ];
                    TrackPoint[] points = new TrackPoint[vertexIndices.Length];

                    for (int vertexIndex = 0;
                        vertexIndex < vertexIndices.Length;
                        vertexIndex += 1)
                    {
                        int sourceVertexIndex = vertexIndices[vertexIndex];

                        if (sourceVertexIndex >= vertices.Length)
                        {
                            throw new InvalidDataException(
                                $"TRAC block {blockIdentifier}, polygon {polygonIndex} " +
                                $"references absent vertex {sourceVertexIndex}.");
                        }

                        points[vertexIndex] = vertices[sourceVertexIndex];
                    }

                    surfaces.Add(new TrackSurface
                    {
                        Identifier = polygonIndex,
                        DetailLevel = GetDetailLevel(groupIndex),
                        Group = GetSurfaceGroup(groupIndex),
                        LightingLevels = ReadUInt16(data, polygonOffset + sizeof(ushort)),
                        MaterialIdentifier = ReadUInt16(data, polygonOffset),
                        Points = points
                    });
                    polygonIndex += 1;
                }
            }

            return surfaces;
        }

        private static TrackPoint[] DecodeVertices(
            byte[] data,
            int vertexTableOffset,
            int vertexCount,
            int connectedVertexCount,
            int blockIdentifier,
            TrackPoint[] centrePoints)
        {
            TrackPoint[] vertices = new TrackPoint[vertexCount];

            for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex += 1)
            {
                int centreIndex = blockIdentifier;

                if (vertexIndex < connectedVertexCount)
                {
                    centreIndex = (blockIdentifier + 1) % centrePoints.Length;
                }

                int vertexOffset = vertexTableOffset + vertexIndex * VertexSize;
                double sourceX = ReadInt16(data, vertexOffset) / LocalVertexScale;
                double sourceZ = ReadInt16(data, vertexOffset + sizeof(short)) /
                    LocalVertexScale;
                double sourceY = ReadInt16(data, vertexOffset + sizeof(short) * 2) /
                    LocalVertexScale;
                TrackPoint centrePoint = centrePoints[centreIndex];
                vertices[vertexIndex] = new TrackPoint
                {
                    X = centrePoint.X + sourceX,
                    Y = centrePoint.Y + sourceZ,
                    Z = centrePoint.Z - sourceY
                };
            }

            return vertices;
        }

        private static IEnumerable<TrackPoint> DecodeClippingPoints(
            byte[] data,
            int blockOffset)
        {
            TrackPoint[] clippingPoints = new TrackPoint[4];

            for (int pointIndex = 0; pointIndex < clippingPoints.Length; pointIndex += 1)
            {
                int pointOffset = blockOffset + 0x10 + pointIndex * 12;
                clippingPoints[pointIndex] = CreateWorldPoint(
                    ReadInt32(data, pointOffset),
                    ReadInt32(data, pointOffset + sizeof(int)),
                    ReadInt32(data, pointOffset + sizeof(int) * 2));
            }

            return clippingPoints;
        }

        private static TrackPoint[] ReadCentrePoints(byte[] data, int offset, int count)
        {
            TrackPoint[] centrePoints = new TrackPoint[count];

            for (int centreIndex = 0; centreIndex < count; centreIndex += 1)
            {
                int centreOffset = offset + centreIndex * 12;
                centrePoints[centreIndex] = CreateWorldPoint(
                    ReadInt32(data, centreOffset),
                    ReadInt32(data, centreOffset + sizeof(int)),
                    ReadInt32(data, centreOffset + sizeof(int) * 2));
            }

            return centrePoints;
        }

        private static int[] ReadStreamChunkOffsets(byte[] data, int offset, int count)
        {
            int[] offsets = new int[count];

            for (int index = 0; index < count; index += 1)
            {
                offsets[index] = ReadInt32(data, offset + index * sizeof(int));
            }

            return offsets;
        }

        private static short[] ReadBlockSuperBlockIndices(
            byte[] data,
            int offset,
            int count)
        {
            short[] indices = new short[count];

            for (int index = 0; index < count; index += 1)
            {
                indices[index] = ReadInt16(data, offset + index * sizeof(short));
            }

            return indices;
        }

        private static IEnumerable<TrackBlock> OrderBlocks(
            short[] blockSuperBlockIndices,
            Dictionary<int, TrackBlock>[] superBlocks)
        {
            TrackBlock[] blocks = new TrackBlock[blockSuperBlockIndices.Length];

            for (int blockIndex = 0; blockIndex < blocks.Length; blockIndex += 1)
            {
                int superBlockIndex = blockSuperBlockIndices[blockIndex];

                if (superBlockIndex < 0 ||
                    superBlockIndex >= superBlocks.Length ||
                    !superBlocks[superBlockIndex].TryGetValue(
                        blockIndex,
                        out TrackBlock? block))
                {
                    throw new InvalidDataException(
                        $"TRAC block {blockIndex} is absent from its declared stream chunk.");
                }

                blocks[blockIndex] = block;
            }

            return blocks;
        }

        private static TrackPoint CreateWorldPoint(int sourceX, int sourceZ, int sourceY)
            => new()
            {
                X = sourceX / FixedPointScale,
                Y = sourceZ / FixedPointScale,
                Z = -sourceY / FixedPointScale
            };

        private static TrackGeometryDetailLevel GetDetailLevel(int groupIndex)
        {
            if (groupIndex < 2)
            {
                return TrackGeometryDetailLevel.Low;
            }

            if (groupIndex < 4)
            {
                return TrackGeometryDetailLevel.Medium;
            }

            return TrackGeometryDetailLevel.High;
        }

        private static TrackSurfaceGroup GetSurfaceGroup(int groupIndex)
        {
            if (groupIndex % 2 == 0)
            {
                return TrackSurfaceGroup.Primary;
            }

            return TrackSurfaceGroup.Secondary;
        }

        private static void ValidateHeader(byte[] data)
        {
            if (data.Length < HeaderSize ||
                !data.AsSpan(0, 4).SequenceEqual(Encoding.ASCII.GetBytes("TRAC")) ||
                ReadInt32(data, sizeof(int)) != ExpectedVersion)
            {
                throw new InvalidDataException(
                    "The track geometry is not a supported NFS2 TRAC version 22 resource.");
            }
        }

        private static int ReadInt32(byte[] data, int offset)
            => BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, sizeof(int)));

        private static short ReadInt16(byte[] data, int offset)
            => BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(offset, sizeof(short)));

        private static ushort ReadUInt16(byte[] data, int offset)
            => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, sizeof(ushort)));
    }
}