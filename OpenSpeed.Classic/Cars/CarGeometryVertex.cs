using System;

namespace OpenSpeed.Classic.Cars
{
    public sealed class CarGeometryVertex(short x, short y, short z)
        : IEquatable<CarGeometryVertex>
    {
        public short X { get; } = x;

        public short Y { get; } = y;

        public short Z { get; } = z;

        public bool Equals(CarGeometryVertex? other)
        {
            if (other is null)
            {
                return false;
            }

            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object? other)
            => other is CarGeometryVertex vertex && Equals(vertex);

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }
}