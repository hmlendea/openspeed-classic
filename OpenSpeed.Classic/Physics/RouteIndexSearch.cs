using System;

namespace OpenSpeed.Classic.Physics
{
    public static class RouteIndexSearch
    {
        private static int CoarseStride => 8;

        private static int FallbackThreshold => 0x10000;

        public static int CalculateCandidateMetric(FixedVector point, FixedVector candidate)
        {
            int third = unchecked(point.Third - candidate.Third) >> 12;
            int first = unchecked(point.First - candidate.First) >> 12;

            return unchecked((unchecked(third * third) >> 6) + (unchecked(first * first) >> 6));
        }

        public static int CalculateWalkMetric(FixedVector point, FixedVector candidate)
        {
            int third = unchecked(point.Third - candidate.Third) >> 9;
            int first = unchecked(point.First - candidate.First) >> 9;

            return unchecked(third * third + first * first);
        }

        public static int Select(PhysicsRoute route, FixedVector point, int currentIndex)
        {
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(point);

            if (currentIndex < 0 || currentIndex >= route.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(currentIndex), currentIndex,
                    "The current route index must identify an existing native record.");
            }

            int selected = Walk(route, point, currentIndex);

            if (CalculateCandidateMetric(point, route.Position(selected)) > FallbackThreshold)
            {
                selected = Walk(route, point, CoarseSearch(route, point));
            }

            return selected;
        }

        private static int Walk(PhysicsRoute route, FixedVector point, int index)
        {
            while (true)
            {
                int metric = CalculateWalkMetric(point, route.Position(index));
                int next = (index + 1) % route.Count;

                if (CalculateWalkMetric(point, route.Position(next)) < metric)
                {
                    index = next;

                    continue;
                }

                int previous = (index + route.Count - 1) % route.Count;

                if (CalculateWalkMetric(point, route.Position(previous)) < metric)
                {
                    index = previous;

                    continue;
                }

                return index;
            }
        }

        private static int CoarseSearch(PhysicsRoute route, FixedVector point)
        {
            int bestMetric = int.MaxValue;
            int bestIndex = -1;

            for (int index = 0; index < route.Count; index += CoarseStride)
            {
                int metric = CalculateCandidateMetric(point, route.Position(index));

                if (metric < bestMetric)
                {
                    bestMetric = metric;
                    bestIndex = index;
                }
            }

            return bestIndex;
        }
    }
}