using Microsoft.Xna.Framework;

namespace OpenSpeed.Classic.Rendering.Cars
{
    internal sealed class TrackRouteProjection
    {
        internal Vector3 Forward { get; set; }

        internal float LeftBorderDistance { get; set; }

        internal Vector3 Normal { get; set; }

        internal Vector3 Position { get; set; }

        internal Vector3 Right { get; set; }

        internal float RightBorderDistance { get; set; }
    }
}