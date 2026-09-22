using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal static class NeedForSpeed2TrackMaterialDecoder
    {
        private static int TextureMappingRecordSize => 10;

        private static ushort TextureMappingType => 2;

        private static byte OpaqueAlpha => byte.MaxValue;

        internal static IEnumerable<TrackMaterial> Decode(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            IEnumerable<NeedForSpeed2TrackExtraBlock> extraBlocks =
                NeedForSpeed2TrackExtraBlockReader.ReadCollection(data);

            return Decode(extraBlocks);
        }

        internal static IEnumerable<TrackMaterial> Decode(
            IEnumerable<NeedForSpeed2TrackExtraBlock> extraBlocks)
        {
            ArgumentNullException.ThrowIfNull(extraBlocks);

            TrackMaterial[] materials = [];

            foreach (NeedForSpeed2TrackExtraBlock extraBlock in extraBlocks)
            {
                if (extraBlock.TypeIdentifier == TextureMappingType)
                {
                    materials = DecodeTextureMappings(extraBlock);
                }
            }

            return materials;
        }

        private static TrackMaterial[] DecodeTextureMappings(
            NeedForSpeed2TrackExtraBlock extraBlock)
        {
            ReadOnlySpan<byte> payload = extraBlock.Payload.Span;
            int materialCount = extraBlock.RecordCount;
            long requiredSize = (long)materialCount * TextureMappingRecordSize;

            if (requiredSize > payload.Length)
            {
                throw new InvalidDataException(
                    "The COLL texture mapping table is truncated.");
            }

            TrackMaterial[] materials = new TrackMaterial[materialCount];

            for (int materialIndex = 0; materialIndex < materialCount; materialIndex += 1)
            {
                int materialOffset = materialIndex * TextureMappingRecordSize;
                byte secondaryRed = payload[materialOffset + 7];
                byte secondaryGreen = payload[materialOffset + 8];
                materials[materialIndex] = new TrackMaterial
                {
                    Identifier = materialIndex,
                    TextureIdentifier = ReadUInt16(payload, materialOffset),
                    Alignment = ReadUInt16(payload, materialOffset + sizeof(ushort)),
                    PrimaryColour = ReadColour(payload, materialOffset + 4),
                    SecondaryColour = ReadColour(payload, materialOffset + 7),
                    AnimationFrameCount = secondaryRed,
                    AnimationFrameInterval = secondaryGreen
                };
            }

            return materials;
        }

        private static TrackColour ReadColour(ReadOnlySpan<byte> data, int offset)
            => new()
            {
                Alpha = OpaqueAlpha,
                Red = data[offset],
                Green = data[offset + 1],
                Blue = data[offset + 2]
            };

        private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset)
            => BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, sizeof(ushort)));
    }
}