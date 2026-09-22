using System;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using OpenSpeed.Classic.Input;
using OpenSpeed.Classic.Rendering.Tracks;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic
{
    public sealed class OpenSpeedClassicGame : Game
    {
        private readonly GraphicsDeviceManager graphicsDeviceManager;
        private readonly string? captureFramePath;
        private bool hasCapturedDiagnosticFrame;
        private TrackCamera? trackCamera;
        private ITrackRenderer? trackRenderer;

        public LoadedTrack? CurrentTrack { get; }

        private static Color BackgroundColour => new(92, 142, 170);

        private static string WindowTitle => "OpenSpeed Classic";

        private static int InitialBackBufferWidth => 1280;

        private static int InitialBackBufferHeight => 720;

        public OpenSpeedClassicGame()
        {
            graphicsDeviceManager = new(this)
            {
                PreferredBackBufferWidth = InitialBackBufferWidth,
                PreferredBackBufferHeight = InitialBackBufferHeight
            };

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.Title = WindowTitle;
        }

        public OpenSpeedClassicGame(LoadedTrack loadedTrack)
            : this(loadedTrack, null)
        {
        }

        public OpenSpeedClassicGame(
            LoadedTrack loadedTrack,
            string? captureFramePath)
            : this()
        {
            ArgumentNullException.ThrowIfNull(loadedTrack);

            if (!string.IsNullOrWhiteSpace(captureFramePath))
            {
                this.captureFramePath = captureFramePath;
            }

            CurrentTrack = loadedTrack;
            Window.Title = $"{WindowTitle} - {loadedTrack.DisplayName}";
        }

        protected override void Initialize()
        {
            graphicsDeviceManager.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            if (CurrentTrack is null)
            {
                return;
            }

            trackCamera = new TrackCamera(
                CurrentTrack.Blocks,
                CurrentTrack.RoutePoints);
            trackRenderer = new TrackRenderer(GraphicsDevice);
            trackRenderer.Load(CurrentTrack);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            if (trackCamera is not null)
            {
                TrackCameraInput cameraInput = TrackCameraInputReader.Read(keyboardState);
                trackCamera.Update(
                    (float)gameTime.ElapsedGameTime.TotalSeconds,
                    cameraInput.MovementInput,
                    cameraInput.TurningInput);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(BackgroundColour);

            if (trackCamera is not null && trackRenderer is not null)
            {
                trackRenderer.Draw(
                    trackCamera,
                    GraphicsDevice.Viewport.Width,
                    GraphicsDevice.Viewport.Height);
                CaptureDiagnosticFrame();
            }

            base.Draw(gameTime);
        }

        private void CaptureDiagnosticFrame()
        {
            if (captureFramePath is null ||
                hasCapturedDiagnosticFrame ||
                trackCamera is null ||
                trackRenderer is null)
            {
                return;
            }

            int width = GraphicsDevice.Viewport.Width;
            int height = GraphicsDevice.Viewport.Height;
            using RenderTarget2D renderTarget = new(
                GraphicsDevice,
                width,
                height,
                false,
                SurfaceFormat.Color,
                DepthFormat.Depth24);
            GraphicsDevice.SetRenderTarget(renderTarget);

            try
            {
                GraphicsDevice.Clear(BackgroundColour);
                trackRenderer.Draw(trackCamera, width, height);
            }
            finally
            {
                GraphicsDevice.SetRenderTarget(null);
            }

            string? outputDirectory = Path.GetDirectoryName(captureFramePath);

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            using FileStream stream = File.Create(captureFramePath);
            renderTarget.SaveAsPng(stream, width, height);
            hasCapturedDiagnosticFrame = true;
            Exit();
        }

        protected override void UnloadContent()
        {
            trackRenderer?.Dispose();
            trackRenderer = null;

            base.UnloadContent();
        }
    }
}
