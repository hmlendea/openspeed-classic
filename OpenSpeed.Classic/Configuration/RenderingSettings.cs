namespace OpenSpeed.Classic.Configuration
{
    public sealed class RenderingSettings
    {
        public bool AreShadowsEnabled { get; set; } = true;

        public bool? Is3DfxEnabled { get; set; }

        public bool IsVignetteEnabled { get; set; } = true;

        public float MotionBlurMinimumSpeedKilometresPerHour { get; set; } = 80.0f;

        public int ScreenHeight { get; set; } = 968;

        public int ScreenWidth { get; set; } = 1720;
    }
}
