namespace OpenSpeed.Classic.Physics
{
    public static class ContactResponse
    {
        public static int Calculate(int first, int second)
        {
            int lower = first;
            int upper = second;

            if (first > second)
            {
                lower = second;
                upper = first;
            }

            int quarter = upper >> 2;

            if (lower < quarter)
            {
                return unchecked(upper + (lower >> 4) + (lower >> 6) + (lower >> 7) +
                    (lower >> 9) + (lower >> 14) + (lower >> 15) + (lower >> 16));
            }

            int half = upper >> 1;

            if (lower < half)
            {
                return unchecked(upper + (lower >> 3) + (lower >> 5) + (lower >> 6) +
                    (lower >> 7) + (lower >> 8) + (lower >> 9) + (lower >> 12) +
                    (lower >> 13) + (lower >> 14) + (lower >> 16));
            }

            if (lower < unchecked(half + quarter))
            {
                return unchecked(upper + (lower >> 2) + (lower >> 5) + (lower >> 8) +
                    (lower >> 10) + (lower >> 11) + (lower >> 12) + (lower >> 13));
            }

            return unchecked(upper + (lower >> 4) + (lower >> 5) + (lower >> 6) +
                (lower >> 7) + (lower >> 8) + (lower >> 9) + (lower >> 11) +
                (lower >> 12) + (lower >> 13) + (lower >> 14) + (lower >> 15));
        }
    }
}