using Microsoft.Xna.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.Rendering.Cars
{
    public static class PhysicsRenderAdapter
    {
        public static Matrix CreateWorld(CarMemory state)
            => Matrix.CreateWorld(
                ToVector(FixedVectors.Read(state, 0x9C)),
                ToVector(FixedVectors.Read(state, 0xDC)),
                ToVector(FixedVectors.Read(state, 0xD0)));

        public static float GetLongitudinalVelocity(CarMemory state)
            => FixedVectors.Dot(FixedVectors.Read(state, 0xDC), FixedVectors.Read(state, 0xA8)) / (float)X86Math.One;

        private static Vector3 ToVector(FixedVector vector)
            => new(vector.First / (float)X86Math.One, vector.Second / (float)X86Math.One, -vector.Third / (float)X86Math.One);
    }
}