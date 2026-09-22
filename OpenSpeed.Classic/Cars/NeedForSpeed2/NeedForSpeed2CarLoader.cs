using System;
using System.IO;
using System.Linq;

using OpenSpeed.Classic.Assets;
using OpenSpeed.Classic.Cars.Loading;
using OpenSpeed.Classic.Tracks;
using OpenSpeed.Classic.Tracks.NeedForSpeed2;

namespace OpenSpeed.Classic.Cars.NeedForSpeed2
{
    public sealed class NeedForSpeed2CarLoader(IFilePathResolver filePathResolver)
        : ICarFormatLoader
    {
        public GameVersion Game => GameVersion.NeedForSpeed2SpecialEdition;

        public LoadedCar Load(string rootDirectory, string carIdentifier)
            => Load(rootDirectory, rootDirectory, carIdentifier);

        public LoadedCar Load(
            string rootDirectory,
            string overridesDirectory,
            string carIdentifier)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(overridesDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(carIdentifier);

            if (!Directory.Exists(rootDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"The configured Need for Speed II asset root '{rootDirectory}' does not exist.");
            }

            CarIdentifier parsedIdentifier = NeedForSpeed2CarCatalogue.ParseIdentifier(
                carIdentifier);
            string archivePath = ResolveRequiredFile(
                overridesDirectory,
                rootDirectory,
                NeedForSpeed2CarCatalogue.GetArchiveRelativePath());
            string texturePath = ResolveRequiredFile(
                overridesDirectory,
                rootDirectory,
                NeedForSpeed2CarCatalogue.GetTextureRelativePath(parsedIdentifier));
            byte[] geometryData = NeedForSpeed2CarArchiveReader.Read(
                File.ReadAllBytes(archivePath),
                NeedForSpeed2CarCatalogue.GetGeometryMemberName(parsedIdentifier));
            TrackTexture[] decodedTextures = NeedForSpeed2TextureArchiveDecoder
                .Decode(File.ReadAllBytes(texturePath))
                .ToArray();
            CarTexture[] textures = decodedTextures
                .Select(ToCarTexture)
                .ToArray();
            CarTextureColourRemapper.Apply(textures, parsedIdentifier);

            return new LoadedCar
            {
                Identifier = parsedIdentifier.ToString(),
                DisplayName = NeedForSpeed2CarCatalogue.GetDisplayName(parsedIdentifier),
                Game = Game,
                Geometry = NeedForSpeed2CarGeometryDecoder.Decode(geometryData).ToArray(),
                Textures = textures
            };
        }

        private static CarTexture ToCarTexture(TrackTexture trackTexture)
            => new()
            {
                Identifier = trackTexture.Identifier,
                Name = trackTexture.Name,
                Width = trackTexture.Width,
                Height = trackTexture.Height,
                Pixels = trackTexture.Pixels
            };

        private string ResolveRequiredFile(
            string overridesDirectory,
            string rootDirectory,
            string relativePath)
        {
            string? filePath = filePathResolver.ResolveFile(
                overridesDirectory,
                rootDirectory,
                relativePath);

            if (filePath is null)
            {
                throw new FileNotFoundException(
                    $"The required Need for Speed II car asset '{relativePath}' was not located " +
                    $"under '{overridesDirectory}' or '{rootDirectory}'.",
                    Path.Combine(rootDirectory, relativePath));
            }

            return filePath;
        }
    }
}