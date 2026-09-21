using System;
using System.IO;

namespace OpenSpeed.Classic.Assets.Compression
{
    internal static class RefPackDecoder
    {
        private static int ShortHeaderSize => 5;

        private static int LongHeaderSize => 8;

        private static byte LongHeaderFlag => 0x01;

        private static byte EndOfStreamThreshold => 0xFC;

        private static byte TwoByteCommandThreshold => 0x80;

        private static byte ThreeByteCommandThreshold => 0xC0;

        private static byte FourByteCommandThreshold => 0xE0;

        internal static byte[] Decode(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (source.Length < ShortHeaderSize)
            {
                throw new InvalidDataException("The RefPack header is truncated.");
            }

            int decompressedSize = ReadBigEndian24(source, 2);

            if (decompressedSize <= 0)
            {
                throw new InvalidDataException(
                    $"The RefPack decompressed size {decompressedSize} is invalid.");
            }

            int sourcePosition = ShortHeaderSize;

            if ((source[0] & LongHeaderFlag) != 0)
            {
                if (source.Length < LongHeaderSize)
                {
                    throw new InvalidDataException("The extended RefPack header is truncated.");
                }

                sourcePosition = LongHeaderSize;
            }

            byte[] output = new byte[decompressedSize];
            int outputPosition = 0;

            while (outputPosition < output.Length)
            {
                EnsureSourceAvailable(source, sourcePosition, 1);
                byte controlByte = source[sourcePosition];
                sourcePosition += 1;

                if (controlByte >= EndOfStreamThreshold)
                {
                    int literalCount = controlByte & 0x03;
                    CopyLiterals(
                        source,
                        sourcePosition,
                        output,
                        outputPosition,
                        literalCount);
                    outputPosition += literalCount;

                    if (outputPosition != output.Length)
                    {
                        throw new InvalidDataException(
                            "The RefPack stream concluded before producing its declared size.");
                    }

                    return output;
                }

                if (controlByte < TwoByteCommandThreshold)
                {
                    EnsureSourceAvailable(source, sourcePosition, 1);
                    byte secondByte = source[sourcePosition];
                    sourcePosition += 1;
                    int literalCount = controlByte & 0x03;
                    int copyCount = ((controlByte & 0x1C) >> 2) + 3;
                    int copyOffset = ((controlByte & 0x60) << 3) + secondByte + 1;
                    CopyLiterals(
                        source,
                        sourcePosition,
                        output,
                        outputPosition,
                        literalCount);
                    sourcePosition += literalCount;
                    outputPosition += literalCount;
                    CopyBackReference(output, outputPosition, copyOffset, copyCount);
                    outputPosition += copyCount;
                }
                else if (controlByte < ThreeByteCommandThreshold)
                {
                    EnsureSourceAvailable(source, sourcePosition, 2);
                    byte secondByte = source[sourcePosition];
                    byte thirdByte = source[sourcePosition + 1];
                    sourcePosition += 2;
                    int literalCount = (secondByte >> 6) & 0x03;
                    int copyCount = (controlByte & 0x3F) + 4;
                    int copyOffset = ((secondByte & 0x3F) << 8) + thirdByte + 1;
                    CopyLiterals(
                        source,
                        sourcePosition,
                        output,
                        outputPosition,
                        literalCount);
                    sourcePosition += literalCount;
                    outputPosition += literalCount;
                    CopyBackReference(output, outputPosition, copyOffset, copyCount);
                    outputPosition += copyCount;
                }
                else if (controlByte < FourByteCommandThreshold)
                {
                    EnsureSourceAvailable(source, sourcePosition, 3);
                    byte secondByte = source[sourcePosition];
                    byte thirdByte = source[sourcePosition + 1];
                    byte fourthByte = source[sourcePosition + 2];
                    sourcePosition += 3;
                    int literalCount = controlByte & 0x03;
                    int copyCount = ((controlByte >> 2) & 0x03) * 256 + fourthByte + 5;
                    int copyOffset =
                        ((controlByte & 0x10) << 12) +
                        (secondByte << 8) +
                        thirdByte +
                        1;
                    CopyLiterals(
                        source,
                        sourcePosition,
                        output,
                        outputPosition,
                        literalCount);
                    sourcePosition += literalCount;
                    outputPosition += literalCount;
                    CopyBackReference(output, outputPosition, copyOffset, copyCount);
                    outputPosition += copyCount;
                }
                else
                {
                    int literalCount = (controlByte & 0x1F) * 4 + 4;
                    CopyLiterals(
                        source,
                        sourcePosition,
                        output,
                        outputPosition,
                        literalCount);
                    sourcePosition += literalCount;
                    outputPosition += literalCount;
                }
            }

            return output;
        }

        private static void CopyLiterals(
            byte[] source,
            int sourcePosition,
            byte[] output,
            int outputPosition,
            int count)
        {
            EnsureSourceAvailable(source, sourcePosition, count);

            if (outputPosition < 0 || count > output.Length - outputPosition)
            {
                throw new InvalidDataException(
                    "A RefPack literal command exceeds the declared output size.");
            }

            Array.Copy(source, sourcePosition, output, outputPosition, count);
        }

        private static void CopyBackReference(
            byte[] output,
            int outputPosition,
            int offset,
            int count)
        {
            if (offset <= 0 ||
                offset > outputPosition ||
                outputPosition < 0 ||
                count > output.Length - outputPosition)
            {
                throw new InvalidDataException(
                    "A RefPack back-reference exceeds the decoded output bounds.");
            }

            int sourcePosition = outputPosition - offset;

            for (int copyIndex = 0; copyIndex < count; copyIndex += 1)
            {
                output[outputPosition + copyIndex] = output[sourcePosition + copyIndex];
            }
        }

        private static void EnsureSourceAvailable(byte[] source, int position, int count)
        {
            if (position < 0 || count < 0 || count > source.Length - position)
            {
                throw new InvalidDataException("The RefPack command stream is truncated.");
            }
        }

        private static int ReadBigEndian24(byte[] data, int offset) =>
            (data[offset] << 16) |
            (data[offset + 1] << 8) |
            data[offset + 2];
    }
}