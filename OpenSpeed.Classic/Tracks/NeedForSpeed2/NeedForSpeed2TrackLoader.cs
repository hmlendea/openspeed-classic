using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using OpenSpeed.Classic.Assets;
using OpenSpeed.Classic.Tracks.Loading;

namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    public sealed class NeedForSpeed2TrackLoader(IFilePathResolver filePathResolver)
        : ITrackFormatLoader
    {
        public GameVersion Game => GameVersion.NeedForSpeed2SpecialEdition;

        public LoadedTrack Load(string rootDirectory, string trackIdentifier)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(trackIdentifier);

            if (!Directory.Exists(rootDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"The configured Need for Speed II asset root '{rootDirectory}' does not exist.");
            }

            NeedForSpeed2TrackIdentifier parsedIdentifier =
                NeedForSpeed2TrackCatalogue.ParseIdentifier(trackIdentifier);
            string geometryPath = ResolveRequiredFile(
                rootDirectory,
                NeedForSpeed2TrackCatalogue.GetGeometryRelativePath(parsedIdentifier));
            string materialPath = ResolveRequiredFile(
                rootDirectory,
                NeedForSpeed2TrackCatalogue.GetMaterialRelativePath(parsedIdentifier));
            string texturePath = ResolveRequiredFile(
                rootDirectory,
                NeedForSpeed2TrackCatalogue.GetTextureRelativePath(parsedIdentifier));
            byte[] geometryData = NeedForSpeed2TrackArchiveReader.Read(
                File.ReadAllBytes(geometryPath));
            TrackBlock[] blocks = NeedForSpeed2TrackGeometryDecoder
                .Decode(geometryData)
                .ToArray();
            TrackMaterial[] materials = NeedForSpeed2TrackMaterialDecoder
                .Decode(File.ReadAllBytes(materialPath))
                .ToArray();
            TrackTexture[] textures = NeedForSpeed2TextureArchiveDecoder
                .Decode(File.ReadAllBytes(texturePath))
                .ToArray();

            return new LoadedTrack
            {
                Identifier = parsedIdentifier.ToString(),
                DisplayName = NeedForSpeed2TrackCatalogue.GetDisplayName(parsedIdentifier),
                Game = Game,
                Blocks = blocks,
                Materials = materials,
                Textures = textures,
                SourceFiles = BuildSourceFiles(geometryPath, materialPath, texturePath)
            };
        }

        private string ResolveRequiredFile(string rootDirectory, string relativePath)
        {
            string? filePath = filePathResolver.ResolveFile(rootDirectory, relativePath);

            if (filePath is null)
            {
                throw new FileNotFoundException(
                    $"The required Need for Speed II asset '{relativePath}' was not located " +
                    $"under '{rootDirectory}'.",
                    Path.Combine(rootDirectory, relativePath));
            }

            return filePath;
        }

        private static IEnumerable<TrackAssetFile> BuildSourceFiles(
            string geometryPath,
            string materialPath,
            string texturePath)
            =>
            [
                new TrackAssetFile
                {
                    Role = TrackAssetRole.Geometry,
                    Path = geometryPath
                },
                new TrackAssetFile
                {
                    Role = TrackAssetRole.Materials,
                    Path = materialPath
                },
                new TrackAssetFile
                {
                    Role = TrackAssetRole.Textures,
                    Path = texturePath
                }
            ];
    }
}