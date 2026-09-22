using System;
using System.IO;

using OpenSpeed.Classic.Assets;

namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal static class NeedForSpeed2TrackCatalogue
    {
        private static int TextureNumberMultiplier => 10;

        private static string TrackDirectory => Path.Combine("GameData", "Tracks");

        private static string SpecialEditionTrackDirectory
            => Path.Combine(TrackDirectory, TrackTextureVariant.SE.Name);

        internal static string GetDisplayName(NeedForSpeed2TrackIdentifier trackIdentifier)
            => trackIdentifier switch
            {
                NeedForSpeed2TrackIdentifier.ProvingGrounds => "Proving Grounds",
                NeedForSpeed2TrackIdentifier.Outback => "Outback",
                NeedForSpeed2TrackIdentifier.LastResort => "Last Resort",
                NeedForSpeed2TrackIdentifier.NorthCountry => "North Country",
                NeedForSpeed2TrackIdentifier.PacificSpirit => "Pacific Spirit",
                NeedForSpeed2TrackIdentifier.Mediterraneo => "Mediterraneo",
                NeedForSpeed2TrackIdentifier.MysticPeaks => "Mystic Peaks",
                NeedForSpeed2TrackIdentifier.MonolithicStudios => "Monolithic Studios",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(trackIdentifier),
                    trackIdentifier,
                    "The Need for Speed II track identifier is not defined.")
            };

        internal static string GetGeometryRelativePath(
            NeedForSpeed2TrackIdentifier trackIdentifier)
            => Path.Combine(
                SpecialEditionTrackDirectory,
                $"Tr{GetOriginalTrackNumber(trackIdentifier):D2}.trk");

        internal static string GetHorizonRelativePath(
            NeedForSpeed2TrackIdentifier trackIdentifier)
            => Path.Combine(
                SpecialEditionTrackDirectory,
                $"3Tr{GetOriginalTrackNumber(trackIdentifier):D2}.hrz");

        internal static string GetMaterialRelativePath(
            NeedForSpeed2TrackIdentifier trackIdentifier)
            => Path.Combine(
                SpecialEditionTrackDirectory,
                $"Tr{GetOriginalTrackNumber(trackIdentifier):D2}.col");

        internal static string GetTextureRelativePath(
            NeedForSpeed2TrackIdentifier trackIdentifier,
            TrackTextureVariant textureVariant)
            => Path.Combine(
                TrackDirectory,
                textureVariant.Name,
                $"Tr{GetOriginalTrackNumber(trackIdentifier) * TextureNumberMultiplier:D3}.qfs");

        internal static string GetSkyTextureName(
            NeedForSpeed2TrackIdentifier trackIdentifier)
            => $"CLD{GetOriginalTrackNumber(trackIdentifier)}";

        internal static string GetSkyTextureRelativePath()
            => Path.Combine(SpecialEditionTrackDirectory, "sky.fsh");

        internal static NeedForSpeed2TrackIdentifier ParseIdentifier(string trackIdentifier)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(trackIdentifier);

            bool identifierWasParsed = Enum.TryParse(
                trackIdentifier,
                true,
                out NeedForSpeed2TrackIdentifier parsedIdentifier);

            if (!identifierWasParsed || !Enum.IsDefined(parsedIdentifier))
            {
                throw new ArgumentException(
                    $"'{trackIdentifier}' is not a supported Need for Speed II track identifier.",
                    nameof(trackIdentifier));
            }

            return parsedIdentifier;
        }

        private static int GetOriginalTrackNumber(
            NeedForSpeed2TrackIdentifier trackIdentifier)
            => trackIdentifier switch
            {
                NeedForSpeed2TrackIdentifier.ProvingGrounds => 0,
                NeedForSpeed2TrackIdentifier.Outback => 2,
                NeedForSpeed2TrackIdentifier.LastResort => 4,
                NeedForSpeed2TrackIdentifier.NorthCountry => 5,
                NeedForSpeed2TrackIdentifier.PacificSpirit => 3,
                NeedForSpeed2TrackIdentifier.Mediterraneo => 6,
                NeedForSpeed2TrackIdentifier.MysticPeaks => 7,
                NeedForSpeed2TrackIdentifier.MonolithicStudios => 8,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(trackIdentifier),
                    trackIdentifier,
                    "The Need for Speed II track identifier is not defined.")
            };
    }
}
