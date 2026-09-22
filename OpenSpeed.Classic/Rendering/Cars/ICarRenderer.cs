using System;

using Microsoft.Xna.Framework;

using OpenSpeed.Classic.Cars;
using OpenSpeed.Classic.Rendering.Tracks;

namespace OpenSpeed.Classic.Rendering.Cars
{
    public interface ICarRenderer : IDisposable
    {
        public bool HasGeometry { get; }

        public void Draw(
            TrackCamera camera,
            Matrix world,
            int viewportWidth,
            int viewportHeight);

        public void Load(LoadedCar car);
    }
}