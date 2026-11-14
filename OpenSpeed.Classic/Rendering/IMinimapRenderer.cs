using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering
{
    public interface IMinimapRenderer : IDisposable
    {
        void Load(IEnumerable<TrackRoutePoint> routePoints);

        void Prepare(Matrix playerWorld, GameTime gameTime);

        void Draw();
    }
}