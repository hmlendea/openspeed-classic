using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic
{
    public sealed class OpenSpeedClassicGame : Game
    {
        private readonly GraphicsDeviceManager graphicsDeviceManager;

        public LoadedTrack? CurrentTrack { get; }

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
            : this()
        {
            ArgumentNullException.ThrowIfNull(loadedTrack);

            CurrentTrack = loadedTrack;
            Window.Title = $"{WindowTitle} - {loadedTrack.DisplayName}";
        }

        protected override void Initialize()
        {
            graphicsDeviceManager.ApplyChanges();

            base.Initialize();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            base.Draw(gameTime);
        }
    }
}