using Microsoft.Xna.Framework;

namespace OpenSpeed.Classic.Rendering
{
    public static class MinimapProjection
    {
        public static Matrix CreateTransform(Matrix playerWorld)
        {
            Vector2 forward = new(playerWorld.Forward.X, playerWorld.Forward.Z);

            if (forward.LengthSquared() == 0.0f)
            {
                forward = -Vector2.UnitY;
            }

            forward.Normalize();
            Matrix rotation = new()
            {
                M11 = -forward.Y,
                M31 = forward.X,
                M12 = -forward.X,
                M32 = -forward.Y,
                M44 = 1.0f
            };

            return Matrix.CreateTranslation(-playerWorld.Translation) * rotation;
        }
    }
}