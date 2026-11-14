using System;

namespace OpenSpeed.Classic.Rendering
{
    public interface IVignetteRenderer : IDisposable
    {
        public void Draw();
    }
}