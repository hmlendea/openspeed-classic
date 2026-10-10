using System;

namespace OpenSpeed.Classic.Rendering
{
    public interface IMotionBlurRenderer : IDisposable
    {
        public void Draw(float longitudinalVelocity, Action drawScene);
    }
}