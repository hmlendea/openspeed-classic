using System.Collections.Generic;

using OpenSpeed.Classic.Assets;
using OpenSpeed.Classic.Physics;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Cars
{
    public sealed class LoadedCar
    {
        public string Identifier { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public GameVersion Game { get; set; }

        public IEnumerable<CarGeometryTriangle> Geometry { get; set; } = [];

        public TrackColour PaintColour { get; set; } = new()
        {
            Red = byte.MaxValue,
            Green = byte.MaxValue,
            Blue = byte.MaxValue,
            Alpha = byte.MaxValue
        };

        public IEnumerable<CarTexture> Textures { get; set; } = [];

        public CarSpecifications? PhysicsSpecifications { get; set; }

        public SimulationTuning? SimulationTuning { get; set; }
    }
}