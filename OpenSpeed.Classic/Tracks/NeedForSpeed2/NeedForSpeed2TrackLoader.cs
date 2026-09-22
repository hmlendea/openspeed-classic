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
        private static int MinimumPanoramaDimension => 64;

        private static int PanoramaTextureCount => 8;

        public GameVersion Game => GameVersion.NeedForSpeed2SpecialEdition;

        public LoadedTrack Load(string rootDirectory, string trackIdentifier)
            => Load(rootDirectory, trackIdentifier, TrackTextureVariant.SE);

        public LoadedTrack Load(
            string rootDirectory,
            string trackIdentifier,
            TrackTextureVariant textureVariant)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(trackIdentifier);
            ArgumentNullException.ThrowIfNull(textureVariant);

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
            string horizonPath = ResolveRequiredFile(
                rootDirectory,
                NeedForSpeed2TrackCatalogue.GetHorizonRelativePath(parsedIdentifier));
            string legacyHorizonPath = ResolveRequiredFile(
                rootDirectory,
                NeedForSpeed2TrackCatalogue.GetLegacyHorizonRelativePath(
                    parsedIdentifier));
            string materialPath = ResolveRequiredFile(
                rootDirectory,
                NeedForSpeed2TrackCatalogue.GetMaterialRelativePath(parsedIdentifier));
            string texturePath = ResolveRequiredFile(
                rootDirectory,
                NeedForSpeed2TrackCatalogue.GetTextureRelativePath(
                    parsedIdentifier,
                    textureVariant));
            byte[] geometryData = NeedForSpeed2TrackArchiveReader.Read(
                File.ReadAllBytes(geometryPath));
            TrackBlock[] blocks = NeedForSpeed2TrackGeometryDecoder
                .Decode(geometryData)
                .ToArray();
            byte[] collectionData = File.ReadAllBytes(materialPath);
            NeedForSpeed2TrackExtraBlock[] collectionExtraBlocks =
            [
                .. NeedForSpeed2TrackExtraBlockReader.ReadCollection(collectionData)
            ];
            TrackMaterial[] materials = NeedForSpeed2TrackMaterialDecoder
                .Decode(collectionExtraBlocks)
                .ToArray();
            TrackRoutePoint[] routePoints = NeedForSpeed2TrackRouteDecoder
                .Decode(collectionExtraBlocks)
                .ToArray();
            ClassifyRoadShoulders(blocks, routePoints);
            TrackSurface[] globalScenerySurfaces = NeedForSpeed2TrackSceneryDecoder
                .Decode(collectionExtraBlocks)
                .ToArray();
            TrackTexture[] textures = NeedForSpeed2TextureArchiveDecoder
                .Decode(File.ReadAllBytes(texturePath))
                .ToArray();
            TrackTexture[] panoramaSourceTextures = textures
                .Where(texture => texture.Identifier < PanoramaTextureCount)
                .OrderBy(texture => texture.Identifier)
                .ToArray();
            TrackTexture? panoramaTexture = null;

            if (panoramaSourceTextures.Length == PanoramaTextureCount &&
                panoramaSourceTextures.All(texture =>
                    texture.Width >= MinimumPanoramaDimension &&
                    texture.Height >= MinimumPanoramaDimension))
            {
                panoramaTexture = TrackTextureStripBuilder.BuildMirrored(
                    int.MaxValue,
                    "HorizonPanorama",
                    panoramaSourceTextures);
            }

            TrackHorizon horizon = NeedForSpeed2HorizonDecoder.Decode(
                File.ReadAllText(horizonPath),
                File.ReadAllText(legacyHorizonPath),
                panoramaTexture);

            return new LoadedTrack
            {
                Identifier = parsedIdentifier.ToString(),
                DisplayName = NeedForSpeed2TrackCatalogue.GetDisplayName(parsedIdentifier),
                Game = Game,
                Blocks = blocks,
                Horizon = horizon,
                Materials = materials,
                RoutePoints = routePoints,
                ScenerySurfaces = globalScenerySurfaces,
                Textures = textures,
                SourceFiles = BuildSourceFiles(
                    geometryPath,
                    horizonPath,
                    legacyHorizonPath,
                    materialPath,
                    texturePath)
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

        private static void ClassifyRoadShoulders(
            IEnumerable<TrackBlock> blocks,
            IEnumerable<TrackRoutePoint> routePoints)
        {
            TrackRoutePoint[] routePointArray = routePoints.ToArray();

            foreach (TrackBlock block in blocks)
            {
                TrackRoutePoint[] blockRoutePoints = routePointArray
                    .Where(routePoint => routePoint.BlockIdentifier == block.Identifier)
                    .ToArray();

                foreach (TrackSurface surface in block.Surfaces)
                {
                    surface.Side = TrackRoadShoulderSelector.Select(
                        surface,
                        blockRoutePoints);
                }
            }
        }

        private static IEnumerable<TrackAssetFile> BuildSourceFiles(
            string geometryPath,
            string horizonPath,
            string legacyHorizonPath,
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
                    Role = TrackAssetRole.Horizon,
                    Path = horizonPath
                },
                new TrackAssetFile
                {
                    Role = TrackAssetRole.Horizon,
                    Path = legacyHorizonPath
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
