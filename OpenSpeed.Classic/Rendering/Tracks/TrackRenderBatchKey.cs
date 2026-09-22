using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    internal readonly record struct TrackRenderBatchKey(
        int BlockIdentifier,
        TrackGeometryDetailLevel DetailLevel,
        int TextureIdentifier);
}