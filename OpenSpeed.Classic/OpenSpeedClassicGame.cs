using System;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using OpenSpeed.Classic.Cars;
using OpenSpeed.Classic.Configuration;
using OpenSpeed.Classic.Input;
using OpenSpeed.Classic.Rendering.Cars;
using OpenSpeed.Classic.Rendering.Tracks;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic
{
    public sealed class OpenSpeedClassicGame : Game
    {
        private readonly GraphicsDeviceManager graphicsDeviceManager;
        private readonly bool areShadowsEnabled = true;
        private readonly string? captureFramePath;
        private readonly CarPhysicsState carPhysicsState = new();
        private readonly DrivingControlsSettings drivingControls = new();
        private ICarRenderer? carRenderer;
        private Matrix? carWorld;
        private bool hasCapturedDiagnosticFrame;
        private TrackCamera? trackCamera;
        private ITrackRenderer? trackRenderer;

        public LoadedCar? CurrentCar { get; }

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
            : this(loadedTrack, new RenderingSettings(), captureFramePath)
        {
        }

        public OpenSpeedClassicGame(
            LoadedTrack loadedTrack,
            RenderingSettings renderingSettings,
            string? captureFramePath)
            : this(loadedTrack, null, renderingSettings, captureFramePath)
        {
        }

        public OpenSpeedClassicGame(
            LoadedTrack loadedTrack,
            LoadedCar? loadedCar,
            RenderingSettings renderingSettings,
            string? captureFramePath)
            : this(
                loadedTrack,
                loadedCar,
                renderingSettings,
                new DrivingControlsSettings(),
                captureFramePath)
        {
        }

        public OpenSpeedClassicGame(
            LoadedTrack loadedTrack,
            LoadedCar? loadedCar,
            RenderingSettings renderingSettings,
            DrivingControlsSettings drivingControls,
            string? captureFramePath)
            : this()
        {
            ArgumentNullException.ThrowIfNull(loadedTrack);
            ArgumentNullException.ThrowIfNull(renderingSettings);
            ArgumentNullException.ThrowIfNull(drivingControls);

            if (!string.IsNullOrWhiteSpace(captureFramePath))
            {
                this.captureFramePath = captureFramePath;
            }

            areShadowsEnabled = renderingSettings.AreShadowsEnabled;
            this.drivingControls = drivingControls;
            CurrentCar = loadedCar;
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
            trackRenderer = new TrackRenderer(GraphicsDevice, areShadowsEnabled);
            trackRenderer.Load(CurrentTrack);
            LoadedCar? currentCar = CurrentCar;

            if (currentCar is not null && CurrentTrack.RoutePoints.Any())
            {
                carWorld = CarWorldTransformBuilder.Build(CurrentTrack.RoutePoints);
                carRenderer = new CarRenderer(GraphicsDevice);
                carRenderer.Load(currentCar);
            }
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
                TrackCameraInput drivingInput = TrackCameraInputReader.Read(
                    keyboardState,
                    drivingControls);
                UpdateCarAndCamera(
                    (float)gameTime.ElapsedGameTime.TotalSeconds,
                    drivingInput);
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
                DrawCar(
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
                DrawCar(width, height);
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

        private void DrawCar(int viewportWidth, int viewportHeight)
        {
            if (carRenderer is null || carWorld is null || trackCamera is null)
            {
                return;
            }

            carRenderer.Draw(
                trackCamera,
                carWorld.Value,
                viewportWidth,
                viewportHeight);
        }

        private void UpdateCarAndCamera(
            float elapsedSeconds,
            TrackCameraInput drivingInput)
        {
            if (trackCamera is null)
            {
                return;
            }

            if (carWorld is null)
            {
                trackCamera.Update(
                    elapsedSeconds,
                    drivingInput.MovementInput,
                    drivingInput.TurningInput);

                return;
            }

            if (CurrentTrack is null)
            {
                return;
            }

            carWorld = CarWorldTransformUpdater.Update(
                carWorld.Value,
                CurrentTrack.RoutePoints,
                carPhysicsState,
                elapsedSeconds,
                drivingInput.MovementInput,
                drivingInput.TurningInput,
                drivingInput.IsHandbrakeApplied);
            trackCamera.Follow(carWorld.Value);
        }

        protected override void UnloadContent()
        {
            carRenderer?.Dispose();
            carRenderer = null;
            trackRenderer?.Dispose();
            trackRenderer = null;

            base.UnloadContent();
        }
    }
}
