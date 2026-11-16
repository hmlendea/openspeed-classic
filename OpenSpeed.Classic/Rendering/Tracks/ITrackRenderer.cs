using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    public interface ITrackRenderer : IDisposable
    {
        public bool HasGeometry { get; }

        public Vector3 SunDirection { get; }

        public Texture2D? ShadowMap { get; }

        public Matrix ShadowViewProjection { get; }

        public void Draw(
            TrackCamera camera,
            int viewportWidth,
            int viewportHeight);

        public void Load(LoadedTrack track);
    }
}