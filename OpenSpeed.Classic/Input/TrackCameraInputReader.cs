using Microsoft.Xna.Framework.Input;

namespace OpenSpeed.Classic.Input
{
    public static class TrackCameraInputReader
    {
        public static TrackCameraInput Read(KeyboardState keyboardState)
            => new()
            {
                MovementInput = GetAxisValue(
                    keyboardState.IsKeyDown(Keys.Up),
                    keyboardState.IsKeyDown(Keys.Down)),
                TurningInput = GetAxisValue(
                    keyboardState.IsKeyDown(Keys.Right),
                    keyboardState.IsKeyDown(Keys.Left))
            };

        private static float GetAxisValue(bool positiveIsPressed, bool negativeIsPressed)
        {
            if (positiveIsPressed == negativeIsPressed)
            {
                return 0.0f;
            }

            if (positiveIsPressed)
            {
                return 1.0f;
            }

            return -1.0f;
        }
    }
}