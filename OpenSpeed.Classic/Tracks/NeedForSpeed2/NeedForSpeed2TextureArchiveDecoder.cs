using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

using OpenSpeed.Classic.Assets.Compression;

namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal static class NeedForSpeed2TextureArchiveDecoder
    {
        private static int HeaderSize => 16;

        private static int DirectoryEntrySize => 8;

        private static int DirectoryNameLength => 4;

        private static int EntryHeaderSize => 16;

        private static int MaximumPaletteSize => 256;

        private static int Argb1555AlphaBit => 0x8000;

        private static int Rgb565TransparentValue => 0x07C0;

        private static byte FullAlpha => byte.MaxValue;

        internal static IEnumerable<TrackTexture> Decode(byte[] resourceData)
        {
            byte[] data = ElectronicArtsBlockDecoder.Decode(resourceData, "SHPI");
            int declaredSize = ValidateContainer(data);
            int entryCount = ReadInt32(data, 8);
            TrackColour[]? globalPalette = ReadGlobalPalette(
                data,
                entryCount,
                declaredSize);
            List<TrackTexture> textures = [];

            for (int entryIndex = 0; entryIndex < entryCount; entryIndex += 1)
            {
                TrackTexture? texture = DecodeTexture(
                    data,
                    entryIndex,
                    entryCount,
                    declaredSize,
                    globalPalette);

                if (texture is not null)
                {
                    textures.Add(texture);
                }
            }

            return textures;
        }

        private static TrackTexture? DecodeTexture(
            byte[] data,
            int entryIndex,
            int entryCount,
            int declaredSize,
            TrackColour[]? globalPalette)
        {
            int entryOffset = ReadEntryOffset(data, entryIndex);
            int entryLimit = FindEntryLimit(
                data,
                entryOffset,
                entryCount,
                declaredSize);

            if (entryOffset < GetDirectoryEnd(entryCount) ||
                entryOffset > entryLimit - EntryHeaderSize)
            {
                throw new InvalidDataException(
                    $"SHPI entry {entryIndex} has invalid bounds.");
            }

            int code = ReadInt32(data, entryOffset);
            NeedForSpeed2PixelFormat? pixelFormat = ReadPixelFormat(code & 0x7F);

            if (pixelFormat is null)
            {
                return null;
            }

            int width = ReadUInt16(data, entryOffset + 4);
            int height = ReadUInt16(data, entryOffset + 6);

            if (width <= 0 || height <= 0)
            {
                throw new InvalidDataException(
                    $"SHPI bitmap entry {entryIndex} has invalid dimensions {width}x{height}.");
            }

            TrackColour[]? palette = globalPalette;

            if (Equals(pixelFormat.Value, NeedForSpeed2PixelFormat.Indexed8))
            {
                TrackColour[]? attachedPalette = ReadAttachedPalette(
                    data,
                    entryOffset,
                    entryLimit,
                    code);

                if (attachedPalette is not null)
                {
                    palette = attachedPalette;
                }

                if (palette is null)
                {
                    palette = CreateEmptyPalette();
                }
            }

            int pixelDataOffset = entryOffset + EntryHeaderSize;
            byte[] pixelSource = data;
            int pixelOffset = pixelDataOffset;
            int pixelLimit = entryLimit;

            if ((code & 0x80) != 0)
            {
                int compressedEnd = FindFirstAttachmentOffset(
                    entryOffset,
                    entryLimit,
                    code);

                if (pixelDataOffset >= compressedEnd)
                {
                    throw new InvalidDataException(
                        $"SHPI bitmap entry {entryIndex} has no compressed pixel payload.");
                }

                pixelSource = RefPackDecoder.Decode(data[pixelDataOffset..compressedEnd]);
                pixelOffset = 0;
                pixelLimit = pixelSource.Length;
            }

            return new TrackTexture
            {
                Identifier = entryIndex,
                Name = ReadEntryName(data, entryIndex),
                Width = width,
                Height = height,
                Pixels = DecodePixels(
                    pixelSource,
                    pixelOffset,
                    pixelLimit,
                    width,
                    height,
                    pixelFormat.Value,
                    palette)
            };
        }

        private static IEnumerable<TrackColour> DecodePixels(
            byte[] source,
            int offset,
            int sourceLimit,
            int width,
            int height,
            NeedForSpeed2PixelFormat pixelFormat,
            TrackColour[]? palette)
        {
            long pixelCountValue = (long)width * height;

            if (pixelCountValue > int.MaxValue)
            {
                throw new InvalidDataException("The SHPI bitmap dimensions are excessive.");
            }

            int pixelCount = (int)pixelCountValue;
            int bytesPerPixel = GetBytesPerPixel(pixelFormat);
            long requiredByteCount = (long)pixelCount * bytesPerPixel;

            if (offset < 0 ||
                sourceLimit > source.Length ||
                requiredByteCount > sourceLimit - offset)
            {
                throw new InvalidDataException("The SHPI bitmap pixel payload is truncated.");
            }

            TrackColour[] colours = new TrackColour[pixelCount];

            for (int pixelIndex = 0; pixelIndex < pixelCount; pixelIndex += 1)
            {
                colours[pixelIndex] = DecodePixel(
                    source,
                    offset + pixelIndex * bytesPerPixel,
                    pixelFormat,
                    palette);
            }

            return colours;
        }

        private static TrackColour DecodePixel(
            byte[] source,
            int offset,
            NeedForSpeed2PixelFormat pixelFormat,
            TrackColour[]? palette)
        {
            if (Equals(pixelFormat, NeedForSpeed2PixelFormat.Indexed8))
            {
                if (palette is null)
                {
                    throw new InvalidDataException("An indexed SHPI bitmap has no palette.");
                }

                return palette[source[offset]];
            }

            if (Equals(pixelFormat, NeedForSpeed2PixelFormat.Bgra32))
            {
                return CreateColour(
                    source[offset + 2],
                    source[offset + 1],
                    source[offset],
                    source[offset + 3]);
            }

            if (Equals(pixelFormat, NeedForSpeed2PixelFormat.Bgr24))
            {
                return CreateColour(
                    source[offset + 2],
                    source[offset + 1],
                    source[offset],
                    FullAlpha);
            }

            int value = ReadUInt16(source, offset);

            if (Equals(pixelFormat, NeedForSpeed2PixelFormat.Argb1555))
            {
                byte alpha = 0;

                if ((value & Argb1555AlphaBit) != 0)
                {
                    alpha = FullAlpha;
                }

                return CreateColour(
                    (byte)(((value >> 10) & 0x1F) << 3),
                    (byte)(((value >> 5) & 0x1F) << 3),
                    (byte)((value & 0x1F) << 3),
                    alpha);
            }

            if (value == Rgb565TransparentValue)
            {
                return CreateColour(0, 0, 0, 0);
            }

            return CreateColour(
                (byte)(((value >> 11) & 0x1F) << 3),
                (byte)(((value >> 5) & 0x3F) << 2),
                (byte)((value & 0x1F) << 3),
                FullAlpha);
        }

        private static TrackColour[]? ReadAttachedPalette(
            byte[] data,
            int entryOffset,
            int entryLimit,
            int code)
        {
            int currentOffset = entryOffset;
            int blockSize = ReadBlockSize(code);

            while (blockSize > 0)
            {
                int attachmentOffset = currentOffset + blockSize;

                if (attachmentOffset >= entryLimit ||
                    attachmentOffset > entryLimit - EntryHeaderSize)
                {
                    return null;
                }

                int attachmentCode = ReadInt32(data, attachmentOffset);
                NeedForSpeed2PaletteFormat? paletteFormat = ReadPaletteFormat(
                    attachmentCode & 0xFF);
                int attachmentLimit = FindFirstAttachmentOffset(
                    attachmentOffset,
                    entryLimit,
                    attachmentCode);

                if (paletteFormat is not null)
                {
                    return DecodePalette(
                        data,
                        attachmentOffset,
                        attachmentLimit,
                        paletteFormat.Value);
                }

                currentOffset = attachmentOffset;
                blockSize = ReadBlockSize(attachmentCode);
            }

            return null;
        }

        private static TrackColour[]? ReadGlobalPalette(
            byte[] data,
            int entryCount,
            int declaredSize)
        {
            TrackColour[]? candidatePalette = null;

            for (int entryIndex = 0; entryIndex < entryCount; entryIndex += 1)
            {
                int entryOffset = ReadEntryOffset(data, entryIndex);
                int entryLimit = FindEntryLimit(
                    data,
                    entryOffset,
                    entryCount,
                    declaredSize);

                if (entryOffset < GetDirectoryEnd(entryCount) ||
                    entryOffset > entryLimit - EntryHeaderSize)
                {
                    continue;
                }

                int code = ReadInt32(data, entryOffset);
                NeedForSpeed2PaletteFormat? paletteFormat = ReadPaletteFormat(code & 0xFF);

                if (paletteFormat is null)
                {
                    continue;
                }

                candidatePalette = DecodePalette(
                    data,
                    entryOffset,
                    entryLimit,
                    paletteFormat.Value);

                if (string.Equals(
                        ReadEntryName(data, entryIndex),
                        "!pal",
                        StringComparison.Ordinal))
                {
                    return candidatePalette;
                }
            }

            return candidatePalette;
        }

        private static TrackColour[] DecodePalette(
            byte[] data,
            int paletteOffset,
            int paletteLimit,
            NeedForSpeed2PaletteFormat paletteFormat)
        {
            int paletteEntryCount = ReadUInt16(data, paletteOffset + 4);
            int colourCount = Math.Min(paletteEntryCount, MaximumPaletteSize);
            int bytesPerColour = GetPaletteBytesPerColour(paletteFormat);
            int dataOffset = paletteOffset + EntryHeaderSize;
            long requiredByteCount = (long)colourCount * bytesPerColour;

            if (requiredByteCount > paletteLimit - dataOffset)
            {
                throw new InvalidDataException("The SHPI palette payload is truncated.");
            }

            TrackColour[] palette = CreateEmptyPalette();

            for (int colourIndex = 0; colourIndex < colourCount; colourIndex += 1)
            {
                palette[colourIndex] = DecodePaletteColour(
                    data,
                    dataOffset + colourIndex * bytesPerColour,
                    paletteFormat);
            }

            return palette;
        }

        private static TrackColour DecodePaletteColour(
            byte[] data,
            int offset,
            NeedForSpeed2PaletteFormat paletteFormat)
        {
            if (Equals(paletteFormat, NeedForSpeed2PaletteFormat.Rgb8))
            {
                return CreateColour(
                    data[offset],
                    data[offset + 1],
                    data[offset + 2],
                    FullAlpha);
            }

            if (Equals(paletteFormat, NeedForSpeed2PaletteFormat.Rgb6))
            {
                return CreateColour(
                    (byte)(data[offset] << 2),
                    (byte)(data[offset + 1] << 2),
                    (byte)(data[offset + 2] << 2),
                    FullAlpha);
            }

            if (Equals(paletteFormat, NeedForSpeed2PaletteFormat.Bgra32))
            {
                return CreateColour(
                    data[offset + 2],
                    data[offset + 1],
                    data[offset],
                    data[offset + 3]);
            }

            int value = ReadUInt16(data, offset);

            if (Equals(paletteFormat, NeedForSpeed2PaletteFormat.Argb1555))
            {
                byte alpha = 0;

                if ((value & Argb1555AlphaBit) != 0)
                {
                    alpha = FullAlpha;
                }

                return CreateColour(
                    (byte)(((value >> 10) & 0x1F) << 3),
                    (byte)(((value >> 5) & 0x1F) << 3),
                    (byte)((value & 0x1F) << 3),
                    alpha);
            }

            if (value == Rgb565TransparentValue)
            {
                return CreateColour(0, 0, 0, 0);
            }

            return CreateColour(
                (byte)(((value >> 11) & 0x1F) << 3),
                (byte)(((value >> 5) & 0x3F) << 2),
                (byte)((value & 0x1F) << 3),
                FullAlpha);
        }

        private static int ValidateContainer(byte[] data)
        {
            if (data.Length < HeaderSize ||
                !data.AsSpan(0, 4).SequenceEqual(Encoding.ASCII.GetBytes("SHPI")))
            {
                throw new InvalidDataException("The texture resource is not an SHPI container.");
            }

            int declaredSize = ReadInt32(data, 4);
            int entryCount = ReadInt32(data, 8);

            if (declaredSize < HeaderSize ||
                declaredSize > data.Length ||
                entryCount < 0 ||
                entryCount > (declaredSize - HeaderSize) / DirectoryEntrySize)
            {
                throw new InvalidDataException("The SHPI container header is invalid.");
            }

            return declaredSize;
        }

        private static int FindEntryLimit(
            byte[] data,
            int entryOffset,
            int entryCount,
            int declaredSize)
        {
            int entryLimit = declaredSize;

            for (int entryIndex = 0; entryIndex < entryCount; entryIndex += 1)
            {
                int candidateOffset = ReadEntryOffset(data, entryIndex);

                if (candidateOffset > entryOffset && candidateOffset < entryLimit)
                {
                    entryLimit = candidateOffset;
                }
            }

            return entryLimit;
        }

        private static int FindFirstAttachmentOffset(
            int entryOffset,
            int entryLimit,
            int code)
        {
            int blockSize = ReadBlockSize(code);

            if (blockSize <= 0 || blockSize > entryLimit - entryOffset)
            {
                return entryLimit;
            }

            return entryOffset + blockSize;
        }

        private static TrackColour CreateColour(byte red, byte green, byte blue, byte alpha)
            => new()
            {
                Alpha = alpha,
                Blue = blue,
                Green = green,
                Red = red
            };

        private static TrackColour[] CreateEmptyPalette()
        {
            TrackColour[] palette = new TrackColour[MaximumPaletteSize];

            for (int colourIndex = 0; colourIndex < palette.Length; colourIndex += 1)
            {
                palette[colourIndex] = new TrackColour();
            }

            return palette;
        }

        private static NeedForSpeed2PixelFormat? ReadPixelFormat(int typeCode)
        {
            if (!Enum.IsDefined(typeof(NeedForSpeed2PixelFormat), typeCode))
            {
                return null;
            }

            return (NeedForSpeed2PixelFormat)typeCode;
        }

        private static NeedForSpeed2PaletteFormat? ReadPaletteFormat(int typeCode)
        {
            if (!Enum.IsDefined(typeof(NeedForSpeed2PaletteFormat), typeCode))
            {
                return null;
            }

            return (NeedForSpeed2PaletteFormat)typeCode;
        }

        private static int GetBytesPerPixel(NeedForSpeed2PixelFormat pixelFormat)
        {
            if (Equals(pixelFormat, NeedForSpeed2PixelFormat.Indexed8))
            {
                return 1;
            }

            if (Equals(pixelFormat, NeedForSpeed2PixelFormat.Bgr24))
            {
                return 3;
            }

            if (Equals(pixelFormat, NeedForSpeed2PixelFormat.Bgra32))
            {
                return 4;
            }

            return 2;
        }

        private static int GetPaletteBytesPerColour(
            NeedForSpeed2PaletteFormat paletteFormat)
        {
            if (Equals(paletteFormat, NeedForSpeed2PaletteFormat.Rgb6) ||
                Equals(paletteFormat, NeedForSpeed2PaletteFormat.Rgb8))
            {
                return 3;
            }

            if (Equals(paletteFormat, NeedForSpeed2PaletteFormat.Bgra32))
            {
                return 4;
            }

            return 2;
        }

        private static string ReadEntryName(byte[] data, int entryIndex)
        {
            int directoryPosition = HeaderSize + entryIndex * DirectoryEntrySize;
            int nameLength = 0;

            while (nameLength < DirectoryNameLength &&
                   data[directoryPosition + nameLength] != 0)
            {
                nameLength += 1;
            }

            return Encoding.ASCII.GetString(data, directoryPosition, nameLength);
        }

        private static int ReadEntryOffset(byte[] data, int entryIndex)
            => ReadInt32(
                data,
                HeaderSize + entryIndex * DirectoryEntrySize + DirectoryNameLength);

        private static int GetDirectoryEnd(int entryCount)
            => HeaderSize + entryCount * DirectoryEntrySize;

        private static int ReadBlockSize(int code) => (int)((uint)code >> 8);

        private static int ReadInt32(byte[] data, int offset)
            => BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, sizeof(int)));

        private static ushort ReadUInt16(byte[] data, int offset)
            => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, sizeof(ushort)));
    }
}