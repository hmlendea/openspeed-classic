using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
            int viewportHeight,
            Vector3 sunDirection,
            Texture2D? shadowMap,
            Matrix shadowViewProjection);

        public void Load(LoadedCar car);
    }
}