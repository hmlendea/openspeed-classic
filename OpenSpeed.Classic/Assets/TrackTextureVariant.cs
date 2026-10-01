using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenSpeed.Classic.Assets
{
    public sealed class TrackTextureVariant : IEquatable<TrackTextureVariant>
    {
        private static readonly Dictionary<string, TrackTextureVariant> values =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { nameof(PC), new(nameof(PC)) },
                { nameof(SE), new(nameof(SE)) }
            };

        public string Name { get; }

        public static TrackTextureVariant PC => values[nameof(PC)];

        public static TrackTextureVariant SE => values[nameof(SE)];

        private TrackTextureVariant(string name)
        {
            Name = name;
        }

        public static Array GetValues() => values.Values.ToArray();

        public static TrackTextureVariant FromString(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (!values.ContainsKey(name))
            {
                return SE;
            }

            return values[name];
        }

        public bool Equals(TrackTextureVariant? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object? value)
        {
            if (value is null)
            {
                return false;
            }

            if (ReferenceEquals(this, value))
            {
                return true;
            }

            if (value.GetType() != GetType())
            {
                return false;
            }

            return Equals((TrackTextureVariant)value);
        }

        public override int GetHashCode()
            => $"{nameof(TrackTextureVariant)}:{Name}".GetHashCode(StringComparison.Ordinal);

        public override string ToString() => Name;

        public static bool operator ==(
            TrackTextureVariant? current,
            TrackTextureVariant? other)
            => Equals(current, other);

        public static bool operator !=(
            TrackTextureVariant? current,
            TrackTextureVariant? other)
            => !Equals(current, other);

        public static implicit operator string(TrackTextureVariant variant) => variant.Name;
    }
}
