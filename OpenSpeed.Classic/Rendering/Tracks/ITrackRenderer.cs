using System;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    public interface ITrackRenderer : IDisposable
    {
        public bool HasGeometry { get; }

        public void Draw(
            TrackCamera camera,
            int viewportWidth,
            int viewportHeight);

        public void Load(LoadedTrack track);
    }
}