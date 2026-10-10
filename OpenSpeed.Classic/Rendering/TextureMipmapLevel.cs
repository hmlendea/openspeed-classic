using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace OpenSpeed.Classic.Rendering
{
    public sealed class TextureMipmapLevel
    {
        public int Height { get; set; }

        public IEnumerable<Color> Pixels { get; set; } = [];

        public int Width { get; set; }
    }
}