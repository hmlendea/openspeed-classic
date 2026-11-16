using System;
using System.IO;
using System.Linq;

using OpenSpeed.Classic.Assets;
using OpenSpeed.Classic.Cars.Loading;
using OpenSpeed.Classic.Physics;
using OpenSpeed.Classic.Tracks;
using OpenSpeed.Classic.Tracks.NeedForSpeed2;

namespace OpenSpeed.Classic.Cars.NeedForSpeed2
{
    public sealed class NeedForSpeed2CarLoader(IFilePathResolver filePathResolver)
        : ICarFormatLoader
    {
        private static string SimulationTuningMemberName => "SimTune.dat";

        public GameVersion Game => GameVersion.NeedForSpeed2SpecialEdition;

        public LoadedCar Load(string rootDirectory, string carIdentifier)
            => Load(rootDirectory, rootDirectory, carIdentifier);

        public LoadedCar Load(
            string rootDirectory,
            string overridesDirectory,
            string carIdentifier)
            => Load(rootDirectory, overridesDirectory, carIdentifier, null);

        public LoadedCar Load(
            string rootDirectory,
            string overridesDirectory,
            string carIdentifier,
            TrackColour? colour)
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
            byte[] archiveData = File.ReadAllBytes(archivePath);
            byte[] geometryData = NeedForSpeed2CarArchiveReader.Read(
                archiveData,
                NeedForSpeed2CarCatalogue.GetGeometryMemberName(parsedIdentifier));
            CarSpecifications physicsSpecifications = CarSpecifications.Decode(
                NeedForSpeed2CarArchiveReader.Read(archiveData, NeedForSpeed2CarCatalogue.GetPhysicsMemberName(parsedIdentifier)));
            SimulationTuning simulationTuning = SimulationTuning.Decode(
                NeedForSpeed2CarArchiveReader.Read(archiveData, SimulationTuningMemberName));
            TrackTexture[] decodedTextures = [.. NeedForSpeed2TextureArchiveDecoder.Decode(File.ReadAllBytes(texturePath))];
            CarTexture[] textures = [.. decodedTextures.Select(ToCarTexture)];
            TrackColour paintColour;

            if (colour is null)
            {
                paintColour = CarTextureColourRemapper.GetPaintColour(textures, parsedIdentifier);
                CarTextureColourRemapper.Apply(textures, parsedIdentifier);
            }
            else
            {
                paintColour = colour;
                CarTextureColourRemapper.Apply(textures, colour);
            }

            return new LoadedCar
            {
                Identifier = parsedIdentifier.ToString(),
                DisplayName = NeedForSpeed2CarCatalogue.GetDisplayName(parsedIdentifier),
                Game = Game,
                PaintColour = paintColour,
                PhysicsSpecifications = physicsSpecifications,
                SimulationTuning = simulationTuning,
                Geometry = NeedForSpeed2CarGeometryDecoder
                    .Decode(geometryData, parsedIdentifier)
                    .ToArray(),
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