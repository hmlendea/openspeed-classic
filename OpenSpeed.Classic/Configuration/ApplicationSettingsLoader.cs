using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Xna.Framework.Input;

using OpenSpeed.Classic.Assets;
using OpenSpeed.Classic.Cars;

namespace OpenSpeed.Classic.Configuration
{
    public sealed class ApplicationSettingsLoader : IApplicationSettingsLoader
    {
        private static readonly JsonSerializerOptions serializerOptions = new()
        {
            Converters =
            {
                new TrackTextureVariantJsonConverter(),
                new JsonStringEnumConverter(null, false)
            },
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public ApplicationSettings Load(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    $"The application settings file '{filePath}' does not exist.",
                    filePath);
            }

            using FileStream settingsStream = File.OpenRead(filePath);
            ApplicationSettings? settings = JsonSerializer.Deserialize<ApplicationSettings>(
                settingsStream,
                serializerOptions);

            if (settings is null)
            {
                throw new InvalidDataException(
                    $"The application settings file '{filePath}' contains no settings.");
            }

            if (settings.Assets is null || settings.Assets.Sources is null)
            {
                throw new InvalidDataException(
                    $"The application settings file '{filePath}' contains no asset sources.");
            }

            if (settings.Rendering is null)
            {
                throw new InvalidDataException(
                    $"The application settings file '{filePath}' contains no rendering settings.");
            }

            ValidateAssetSources(settings.Assets.Sources, filePath);
            ValidateControls(settings.Controls, filePath);
            ValidateStartupCar(settings.StartupCar, filePath);
            ValidateStartupTrack(settings.StartupTrack, filePath);

            return settings;
        }

        private static void ValidateControls(
            DrivingControlsSettings controls,
            string filePath)
        {
            if (controls is null)
            {
                throw new InvalidDataException(
                    $"The application settings file '{filePath}' contains no controls.");
            }

            ValidateControlBinding(controls.Accelerate, nameof(controls.Accelerate), filePath);
            ValidateControlBinding(controls.Reverse, nameof(controls.Reverse), filePath);
            ValidateControlBinding(controls.SteerLeft, nameof(controls.SteerLeft), filePath);
            ValidateControlBinding(controls.SteerRight, nameof(controls.SteerRight), filePath);
        }

        private static void ValidateControlBinding(
            ControlBindingSettings binding,
            string controlName,
            string filePath)
        {
            if (binding is null)
            {
                throw new InvalidDataException(
                    $"The '{controlName}' control in '{filePath}' contains no bindings.");
            }

            if (!Enum.IsDefined(binding.Primary) || binding.Primary == Keys.None)
            {
                throw new InvalidDataException(
                    $"The primary binding for '{controlName}' in '{filePath}' is invalid.");
            }

            if (!Enum.IsDefined(binding.Secondary) || binding.Secondary == Keys.None)
            {
                throw new InvalidDataException(
                    $"The secondary binding for '{controlName}' in '{filePath}' is invalid.");
            }

            if (binding.Primary == binding.Secondary)
            {
                throw new InvalidDataException(
                    $"The bindings for '{controlName}' in '{filePath}' must be distinct.");
            }
        }

        private static void ValidateStartupCar(
            StartupCarSettings startupCar,
            string filePath)
        {
            if (startupCar is null)
            {
                throw new InvalidDataException(
                    $"The application settings file '{filePath}' contains no startup car.");
            }

            if (!Enum.TryParse(
                    startupCar.Game,
                    true,
                    out GameVersion gameVersion) ||
                !Enum.IsDefined(gameVersion))
            {
                throw new InvalidDataException(
                    $"The application settings file '{filePath}' contains the unsupported " +
                    $"startup car game value '{startupCar.Game}'.");
            }

            if (string.IsNullOrWhiteSpace(startupCar.Identifier))
            {
                throw new InvalidDataException(
                    $"The startup car identifier in '{filePath}' is empty.");
            }

            if (!Enum.TryParse(
                    startupCar.Identifier,
                    true,
                    out CarIdentifier carIdentifier) ||
                !Enum.IsDefined(carIdentifier))
            {
                throw new InvalidDataException(
                    $"The application settings file '{filePath}' contains the unsupported " +
                    $"startup car identifier '{startupCar.Identifier}'.");
            }
        }

        private static void ValidateAssetSources(
            IEnumerable<AssetSourceSettings> assetSources,
            string filePath)
        {
            HashSet<GameVersion> configuredGames = [];

            foreach (AssetSourceSettings assetSource in assetSources)
            {
                if (!Enum.TryParse(
                        assetSource.Game,
                        true,
                        out GameVersion gameVersion) ||
                    !Enum.IsDefined(gameVersion))
                {
                    throw new InvalidDataException(
                        $"The application settings file '{filePath}' contains the unsupported " +
                        $"game value '{assetSource.Game}'.");
                }

                if (string.IsNullOrWhiteSpace(assetSource.RootDirectory))
                {
                    throw new InvalidDataException(
                        $"The asset root for '{assetSource.Game}' in '{filePath}' is empty.");
                }

                if (string.IsNullOrWhiteSpace(assetSource.OverridesDirectory))
                {
                    assetSource.OverridesDirectory = assetSource.RootDirectory;
                }

                if (!configuredGames.Add(gameVersion))
                {
                    throw new InvalidDataException(
                        $"The application settings file '{filePath}' configures " +
                        $"'{assetSource.Game}' more than once.");
                }
            }
        }

        private static void ValidateStartupTrack(
            StartupTrackSettings startupTrack,
            string filePath)
        {
            if (startupTrack is null)
            {
                throw new InvalidDataException(
                    $"The application settings file '{filePath}' contains no startup track.");
            }

            if (!Enum.TryParse(
                    startupTrack.Game,
                    true,
                    out GameVersion gameVersion) ||
                !Enum.IsDefined(gameVersion))
            {
                throw new InvalidDataException(
                    $"The application settings file '{filePath}' contains the unsupported " +
                    $"startup game value '{startupTrack.Game}'.");
            }

            if (string.IsNullOrWhiteSpace(startupTrack.Identifier))
            {
                throw new InvalidDataException(
                    $"The startup track identifier in '{filePath}' is empty.");
            }
        }
    }
}
