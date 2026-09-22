using System;

using Microsoft.Xna.Framework.Input;

using OpenSpeed.Classic.Configuration;

namespace OpenSpeed.Classic.Input
{
    public static class TrackCameraInputReader
    {
        public static TrackCameraInput Read(KeyboardState keyboardState)
            => Read(keyboardState, new DrivingControlsSettings());

        public static TrackCameraInput Read(
            KeyboardState keyboardState,
            DrivingControlsSettings controls)
        {
            ArgumentNullException.ThrowIfNull(controls);

            return new TrackCameraInput
            {
                CameraView = ReadCameraView(keyboardState),
                IsHandbrakeApplied = IsControlPressed(
                    keyboardState,
                    controls.Handbrake),
                MovementInput = GetAxisValue(
                    IsControlPressed(keyboardState, controls.Accelerate),
                    IsControlPressed(keyboardState, controls.Reverse)),
                TurningInput = GetAxisValue(
                    IsControlPressed(keyboardState, controls.SteerRight),
                    IsControlPressed(keyboardState, controls.SteerLeft))
            };
        }

        private static TrackCameraView ReadCameraView(KeyboardState keyboardState)
        {
            if (keyboardState.IsKeyDown(Keys.F1) ||
                keyboardState.IsKeyDown(Keys.D1))
            {
                return TrackCameraView.Right;
            }

            if (keyboardState.IsKeyDown(Keys.F2) ||
                keyboardState.IsKeyDown(Keys.D2))
            {
                return TrackCameraView.Left;
            }

            if (keyboardState.IsKeyDown(Keys.F3) ||
                keyboardState.IsKeyDown(Keys.D3))
            {
                return TrackCameraView.Centre;
            }

            if (keyboardState.IsKeyDown(Keys.F4) ||
                keyboardState.IsKeyDown(Keys.D4))
            {
                return TrackCameraView.Rear;
            }

            return TrackCameraView.Centre;
        }

        private static bool IsControlPressed(
            KeyboardState keyboardState,
            ControlBindingSettings binding)
            => keyboardState.IsKeyDown(binding.Primary) ||
                keyboardState.IsKeyDown(binding.Secondary);

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