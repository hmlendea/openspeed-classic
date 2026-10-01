using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    public static class TrackBlockVisibilitySelector
    {
        private static int GlobalBlockIdentifier => -1;

        public static bool IsVisible(
            int blockIdentifier,
            IEnumerable<int>? visibleBlockIdentifiers)
        {
            if (blockIdentifier == GlobalBlockIdentifier ||
                visibleBlockIdentifiers is null)
            {
                return true;
            }

            if (visibleBlockIdentifiers is HashSet<int> visibleBlockIdentifierSet)
            {
                return visibleBlockIdentifierSet.Contains(blockIdentifier);
            }

            foreach (int visibleBlockIdentifier in visibleBlockIdentifiers)
            {
                if (visibleBlockIdentifier == blockIdentifier)
                {
                    return true;
                }
            }

            return false;
        }

        public static int SelectNearestBlock(
            IEnumerable<KeyValuePair<int, Vector3>> blockCentres,
            Vector3 cameraPosition)
        {
            ArgumentNullException.ThrowIfNull(blockCentres);

            int nearestBlockIdentifier = GlobalBlockIdentifier;
            float nearestDistanceSquared = float.PositiveInfinity;

            foreach (KeyValuePair<int, Vector3> blockCentre in blockCentres)
            {
                float distanceX = cameraPosition.X - blockCentre.Value.X;
                float distanceZ = cameraPosition.Z - blockCentre.Value.Z;
                float distanceSquared =
                    distanceX * distanceX +
                    distanceZ * distanceZ;

                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestBlockIdentifier = blockCentre.Key;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            return nearestBlockIdentifier;
        }
    }
}