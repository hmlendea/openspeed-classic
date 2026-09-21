using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal static class NeedForSpeed2TrackMaterialDecoder
    {
        private static int HeaderSize => 16;

        private static int ExpectedVersion => 0x0B;

        private static int RecordOffsetSize => 4;

        private static int RecordHeaderSize => 8;

        private static int TextureMappingRecordSize => 10;

        private static ushort TextureMappingType => 2;

        private static byte OpaqueAlpha => byte.MaxValue;

        internal static IEnumerable<TrackMaterial> Decode(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            ValidateHeader(data);

            int recordCount = ReadInt32(data, 12);
            int bodyLength = data.Length - HeaderSize;

            if (recordCount < 0 || recordCount > bodyLength / RecordOffsetSize)
            {
                throw new InvalidDataException("The COLL record count is invalid.");
            }

            int[] recordOffsets = ReadRecordOffsets(data, recordCount, bodyLength);
            TrackMaterial[] materials = [];

            for (int recordIndex = 0; recordIndex < recordOffsets.Length; recordIndex += 1)
            {
                int recordStart = HeaderSize + recordOffsets[recordIndex];
                int recordEnd = data.Length;

                if (recordIndex + 1 < recordOffsets.Length)
                {
                    recordEnd = HeaderSize + recordOffsets[recordIndex + 1];
                }

                if (recordEnd - recordStart < RecordHeaderSize)
                {
                    throw new InvalidDataException(
                        $"COLL record {recordIndex} is shorter than its header.");
                }

                int declaredSize = ReadInt32(data, recordStart);
                ushort typeIdentifier = ReadUInt16(data, recordStart + 4);
                int itemCount = ReadUInt16(data, recordStart + 6);

                if (declaredSize < RecordHeaderSize || declaredSize > recordEnd - recordStart)
                {
                    throw new InvalidDataException(
                        $"COLL record {recordIndex} has invalid size {declaredSize}.");
                }

                if (typeIdentifier == TextureMappingType)
                {
                    materials = DecodeTextureMappings(
                        data,
                        recordStart + RecordHeaderSize,
                        declaredSize - RecordHeaderSize,
                        itemCount);
                }
            }

            return materials;
        }

        private static TrackMaterial[] DecodeTextureMappings(
            byte[] data,
            int payloadOffset,
            int payloadSize,
            int materialCount)
        {
            long requiredSize = (long)materialCount * TextureMappingRecordSize;

            if (requiredSize > payloadSize)
            {
                throw new InvalidDataException(
                    "The COLL texture mapping table is truncated.");
            }

            TrackMaterial[] materials = new TrackMaterial[materialCount];

            for (int materialIndex = 0; materialIndex < materialCount; materialIndex += 1)
            {
                int materialOffset = payloadOffset + materialIndex * TextureMappingRecordSize;
                byte secondaryRed = data[materialOffset + 7];
                byte secondaryGreen = data[materialOffset + 8];
                materials[materialIndex] = new TrackMaterial
                {
                    Identifier = materialIndex,
                    TextureIdentifier = ReadUInt16(data, materialOffset),
                    Alignment = ReadUInt16(data, materialOffset + sizeof(ushort)),
                    PrimaryColour = ReadColour(data, materialOffset + 4),
                    SecondaryColour = ReadColour(data, materialOffset + 7),
                    AnimationFrameCount = secondaryRed,
                    AnimationFrameInterval = secondaryGreen
                };
            }

            return materials;
        }

        private static int[] ReadRecordOffsets(byte[] data, int recordCount, int bodyLength)
        {
            int[] recordOffsets = new int[recordCount];
            int minimumRecordOffset = recordCount * RecordOffsetSize;
            int previousOffset = minimumRecordOffset - 1;

            for (int recordIndex = 0; recordIndex < recordCount; recordIndex += 1)
            {
                int recordOffset = ReadInt32(
                    data,
                    HeaderSize + recordIndex * RecordOffsetSize);

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

        private static TrackColour ReadColour(byte[] data, int offset)
            => new()
            {
                Alpha = OpaqueAlpha,
                Red = data[offset],
                Green = data[offset + 1],
                Blue = data[offset + 2]
            };

        private static void ValidateHeader(byte[] data)
        {
            if (data.Length < HeaderSize ||
                !data.AsSpan(0, 4).SequenceEqual(Encoding.ASCII.GetBytes("COLL")) ||
                ReadInt32(data, 4) != ExpectedVersion ||
                ReadInt32(data, 8) != data.Length)
            {
                throw new InvalidDataException(
                    "The material resource is not a supported NFS2 COLL version 11 file.");
            }
        }

        private static int ReadInt32(byte[] data, int offset)
            => BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, sizeof(int)));

        private static ushort ReadUInt16(byte[] data, int offset)
            => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, sizeof(ushort)));
    }
}