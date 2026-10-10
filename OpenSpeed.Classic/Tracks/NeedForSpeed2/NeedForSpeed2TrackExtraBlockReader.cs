using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal static class NeedForSpeed2TrackExtraBlockReader
    {
        private static int CollectionHeaderSize => 16;

        private static int CollectionRecordOffsetSize => 4;

        private static int CollectionVersion => 0x0B;

        private static int ExtraBlockHeaderSize => 8;

        private static int TrackBlockExtraBlockOffsetSize => 4;

        internal static IEnumerable<NeedForSpeed2TrackExtraBlock> ReadCollection(
            byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            ValidateCollectionHeader(data);

            int recordCount = ReadInt32(data, 12);
            int bodyLength = data.Length - CollectionHeaderSize;

            if (recordCount < 0 ||
                recordCount > bodyLength / CollectionRecordOffsetSize)
            {
                throw new InvalidDataException("The COLL record count is invalid.");
            }

            int[] recordOffsets = ReadCollectionRecordOffsets(
                data,
                recordCount,
                bodyLength);
            NeedForSpeed2TrackExtraBlock[] extraBlocks =
                new NeedForSpeed2TrackExtraBlock[recordCount];

            for (int recordIndex = 0; recordIndex < recordOffsets.Length; recordIndex += 1)
            {
                int recordStart = CollectionHeaderSize + recordOffsets[recordIndex];
                int recordEnd = data.Length;

                if (recordIndex + 1 < recordOffsets.Length)
                {
                    recordEnd = CollectionHeaderSize + recordOffsets[recordIndex + 1];
                }

                extraBlocks[recordIndex] = ReadExtraBlock(
                    data,
                    recordStart,
                    recordEnd,
                    $"COLL record {recordIndex}");
            }

            return extraBlocks;
        }

        internal static IEnumerable<NeedForSpeed2TrackExtraBlock> ReadTrackBlock(
            byte[] data,
            int blockOffset,
            int extraBlockCount,
            long extraBlockTableFileOffset,
            long geometryEnd,
            long blockEnd)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (extraBlockCount == 0)
            {
                return [];
            }

            long extraBlockTableEnd =
                extraBlockTableFileOffset +
                (long)extraBlockCount * TrackBlockExtraBlockOffsetSize;

            if (extraBlockTableFileOffset < geometryEnd || extraBlockTableEnd > blockEnd)
            {
                throw new InvalidDataException(
                    $"The TRAC block at offset {blockOffset} has an invalid extra-block table.");
            }

            NeedForSpeed2TrackExtraBlock[] extraBlocks =
                new NeedForSpeed2TrackExtraBlock[extraBlockCount];

            for (int extraBlockIndex = 0;
                extraBlockIndex < extraBlockCount;
                extraBlockIndex += 1)
            {
                int relativeOffset = ReadInt32(
                    data,
                    (int)extraBlockTableFileOffset +
                    extraBlockIndex * TrackBlockExtraBlockOffsetSize);
                long extraBlockFileOffset = (long)blockOffset + relativeOffset;

                if (extraBlockFileOffset < extraBlockTableEnd ||
                    extraBlockFileOffset > blockEnd - ExtraBlockHeaderSize)
                {
                    throw new InvalidDataException(
                        $"TRAC extra block {extraBlockIndex} at block offset " +
                        $"{blockOffset} has invalid bounds.");
                }

                extraBlocks[extraBlockIndex] = ReadExtraBlock(
                    data,
                    (int)extraBlockFileOffset,
                    (int)blockEnd,
                    $"TRAC extra block {extraBlockIndex}");
            }

            return extraBlocks;
        }

        private static NeedForSpeed2TrackExtraBlock ReadExtraBlock(
            byte[] data,
            int recordStart,
            int recordEnd,
            string description)
        {
            if (recordStart < 0 || recordStart > recordEnd - ExtraBlockHeaderSize)
            {
                throw new InvalidDataException($"{description} has a truncated header.");
            }

            int declaredSize = ReadInt32(data, recordStart);

            if (declaredSize < ExtraBlockHeaderSize || declaredSize > recordEnd - recordStart)
            {
                throw new InvalidDataException(
                    $"{description} has invalid size {declaredSize}.");
            }

            ushort typeIdentifier = ReadUInt16(data, recordStart + 4);
            ushort recordCount = ReadUInt16(data, recordStart + 6);
            byte[] payload = data.AsSpan(
                recordStart + ExtraBlockHeaderSize,
                declaredSize - ExtraBlockHeaderSize).ToArray();

            return new NeedForSpeed2TrackExtraBlock(
                typeIdentifier,
                recordCount,
                payload);
        }

        private static int[] ReadCollectionRecordOffsets(
            byte[] data,
            int recordCount,
            int bodyLength)
        {
            int[] recordOffsets = new int[recordCount];
            int minimumRecordOffset = recordCount * CollectionRecordOffsetSize;
            int previousOffset = minimumRecordOffset - 1;

            for (int recordIndex = 0; recordIndex < recordCount; recordIndex += 1)
            {
                int recordOffset = ReadInt32(
                    data,
                    CollectionHeaderSize + recordIndex * CollectionRecordOffsetSize);

                if (recordOffset < minimumRecordOffset ||
                    recordOffset >= bodyLength ||
                    recordOffset <= previousOffset)
                {
                    throw new InvalidDataException(
                        $"COLL record {recordIndex} has an invalid relative offset.");
                }

                recordOffsets[recordIndex] = recordOffset;
                previousOffset = recordOffset;
            }

            return recordOffsets;
        }

        private static void ValidateCollectionHeader(byte[] data)
        {
            if (data.Length < CollectionHeaderSize ||
                !data.AsSpan(0, 4).SequenceEqual(Encoding.ASCII.GetBytes("COLL")) ||
                ReadInt32(data, 4) != CollectionVersion ||
                ReadInt32(data, 8) != data.Length)
            {
                throw new InvalidDataException(
                    "The resource is not a supported NFS2 COLL version 11 file.");
            }
        }

        private static int ReadInt32(byte[] data, int offset)
            => BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, sizeof(int)));

        private static ushort ReadUInt16(byte[] data, int offset)
            => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, sizeof(ushort)));
    }
}