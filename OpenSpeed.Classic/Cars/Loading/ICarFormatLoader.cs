using OpenSpeed.Classic.Assets;

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
    }
}