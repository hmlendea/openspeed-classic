namespace OpenSpeed.Classic.Physics
{
    public sealed class FixedMatrix
    {
        private readonly int[] cells = new int[CellCount];

        public static int Dimension => 3;

        public static int CellCount => Dimension * Dimension;

        public int this[int row, int column]
        {
            get => cells[row * Dimension + column];
            set => cells[row * Dimension + column] = value;
        }
    }
}