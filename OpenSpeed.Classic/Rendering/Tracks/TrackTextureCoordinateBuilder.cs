using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    public static class TrackTextureCoordinateBuilder
    {
        private static byte HalfRotationFlag => 0x04;

        private static byte NextCornerRotationFlag => 0x08;

        private static byte PreviousCornerRotationFlag => 0x02;

        private static byte VerticalAxisReflectionFlag => 0x10;

        public static IEnumerable<Vector2> Build(TrackMaterial material)
        {
            ArgumentNullException.ThrowIfNull(material);

            int[] cornerIndices = [0, 1, 2, 3];
            byte orientationFlags = (byte)(material.Alignment >> 8);

            if ((orientationFlags & VerticalAxisReflectionFlag) != 0)
            {
                cornerIndices = ApplySourcePermutation(cornerIndices, 0);
            }

            if ((orientationFlags & PreviousCornerRotationFlag) != 0)
            {
                cornerIndices = ApplySourcePermutation(cornerIndices, 2);
            }
            else if ((orientationFlags & HalfRotationFlag) != 0)
            {
                cornerIndices = ApplySourcePermutation(cornerIndices, 3);
            }
            else if ((orientationFlags & NextCornerRotationFlag) != 0)
            {
                cornerIndices = ApplySourcePermutation(cornerIndices, 4);
            }

            Vector2[] baseCoordinates =
            [
                Vector2.UnitX,
                Vector2.Zero,
                Vector2.UnitY,
                Vector2.One
            ];

            return
            [
                baseCoordinates[cornerIndices[0]],
                baseCoordinates[cornerIndices[1]],
                baseCoordinates[cornerIndices[2]],
                baseCoordinates[cornerIndices[3]]
            ];
        }

        private static int[] ApplySourcePermutation(
            int[] cornerIndices,
            int permutationCase)
        {
            if (permutationCase == 0)
            {
                return
                [
                    cornerIndices[2],
                    cornerIndices[1],
                    cornerIndices[3],
                    cornerIndices[0]
                ];
            }

            if (permutationCase == 3)
            {
                return
                [
                    cornerIndices[1],
                    cornerIndices[3],
                    cornerIndices[0],
                    cornerIndices[2]
                ];
            }

            if (cornerIndices[1] >= cornerIndices[0])
            {
                return
                [
                    cornerIndices[3],
                    cornerIndices[0],
                    cornerIndices[1],
                    cornerIndices[2]
                ];
            }

            return
            [
                cornerIndices[2],
                cornerIndices[3],
                cornerIndices[0],
                cornerIndices[1]
            ];
        }
    }
}
