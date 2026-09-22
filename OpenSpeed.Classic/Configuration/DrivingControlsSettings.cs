using Microsoft.Xna.Framework.Input;

namespace OpenSpeed.Classic.Configuration
{
    public sealed class DrivingControlsSettings
    {
        public ControlBindingSettings Accelerate { get; set; } = new()
        {
            Primary = Keys.Up,
            Secondary = Keys.W
        };

        public ControlBindingSettings Reverse { get; set; } = new()
        {
            Primary = Keys.Down,
            Secondary = Keys.S
        };

        public ControlBindingSettings SteerLeft { get; set; } = new()
        {
            Primary = Keys.Left,
            Secondary = Keys.A
        };

        public ControlBindingSettings SteerRight { get; set; } = new()
        {
            Primary = Keys.Right,
            Secondary = Keys.D
        };
    }
}