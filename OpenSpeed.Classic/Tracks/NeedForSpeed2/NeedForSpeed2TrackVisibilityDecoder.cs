using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal static class NeedForSpeed2TrackVisibilityDecoder
    {
        private static int BlockIdentifierSize => sizeof(ushort);

        private static int OptionalPaddingSize => 2;

        private static ushort VisibilityType => 4;

        internal static IEnumerable<int>? Decode(
            IEnumerable<NeedForSpeed2TrackExtraBlock> extraBlocks)
        {
            ArgumentNullException.ThrowIfNull(extraBlocks);

            NeedForSpeed2TrackExtraBlock? visibilityBlock = extraBlocks.FirstOrDefault(
                extraBlock => extraBlock.TypeIdentifier == VisibilityType);

            if (visibilityBlock is null)
            {
                return null;
            }

            ReadOnlySpan<byte> payload = visibilityBlock.Payload.Span;
            int requiredSize = visibilityBlock.RecordCount * BlockIdentifierSize;
            int paddingSize = payload.Length - requiredSize;

            if (paddingSize != 0 && paddingSize != OptionalPaddingSize)
            {
                throw new InvalidDataException(
                    $"The visibility block declares {visibilityBlock.RecordCount} records, " +
                    $"but contains {payload.Length} payload bytes.");
            }

            int[] blockIdentifiers = new int[visibilityBlock.RecordCount];

            for (int recordIndex = 0;
                recordIndex < blockIdentifiers.Length;
                recordIndex += 1)
            {
                blockIdentifiers[recordIndex] = BinaryPrimitives.ReadUInt16LittleEndian(
                    payload.Slice(recordIndex * BlockIdentifierSize, BlockIdentifierSize));
            }

            return blockIdentifiers;
        }
    }
}