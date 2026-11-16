using System;
using System.IO;
using System.Text;

namespace OpenSpeed.Classic.Assets.Compression
{
    internal static class ElectronicArtsBlockDecoder
    {
        private static byte MarkerMask => 0xFE;

        private static byte RefPackMarker => 0x10;

        internal static byte[] Decode(byte[] data, string uncompressedMarker)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentException.ThrowIfNullOrWhiteSpace(uncompressedMarker);

            if (data.Length >= uncompressedMarker.Length &&
                data.AsSpan(0, uncompressedMarker.Length).SequenceEqual(
                    Encoding.ASCII.GetBytes(uncompressedMarker)))
            {
                return [.. data];
            }

            if (data.Length >= 5 &&
                (data[0] & MarkerMask) == RefPackMarker &&
                (data[1] == 0xFB || data[1] == 0x32))
            {
                return RefPackDecoder.Decode(data);
            }

            throw new InvalidDataException(
                "The Electronic Arts resource uses an unsupported compression marker.");
        }
    }
}