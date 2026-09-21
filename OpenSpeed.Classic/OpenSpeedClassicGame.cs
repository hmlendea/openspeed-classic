using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSpeed.Classic
{
    public sealed class OpenSpeedClassicGame : Game
    {
        private static string WindowTitle => "OpenSpeed Classic";
        private static int InitialBackBufferWidth => 1280;
        private static int InitialBackBufferHeight => 720;

        private readonly GraphicsDeviceManager graphicsDeviceManager;

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