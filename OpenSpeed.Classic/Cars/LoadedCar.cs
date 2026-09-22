using System.Collections.Generic;

using OpenSpeed.Classic.Assets;

namespace OpenSpeed.Classic.Cars
{
    public sealed class LoadedCar
    {
        public string Identifier { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public GameVersion Game { get; set; }

        public IEnumerable<CarGeometryTriangle> Geometry { get; set; } = [];

        public IEnumerable<CarTexture> Textures { get; set; } = [];
    }
}