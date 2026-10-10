using System;

using OpenSpeed.Classic.Assets;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Cars.Loading
{
    public interface ICarFormatLoader
    {
        public GameVersion Game { get; }

        public LoadedCar Load(string rootDirectory, string carIdentifier);

        public LoadedCar Load(
            string rootDirectory,
            string overridesDirectory,
            string carIdentifier)
            => Load(rootDirectory, carIdentifier);

        public LoadedCar Load(
            string rootDirectory,
            string overridesDirectory,
            string carIdentifier,
            TrackColour? colour)
        {
            if (colour is not null)
            {
                throw new NotSupportedException($"The car loader for '{Game}' does not support custom paint colours.");
            }

            return Load(rootDirectory, overridesDirectory, carIdentifier);
        }
    }
}