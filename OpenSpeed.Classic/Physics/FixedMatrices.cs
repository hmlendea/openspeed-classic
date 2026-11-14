namespace OpenSpeed.Classic.Physics
{
    public static class FixedMatrices
    {
        public static FixedMatrix Identity()
        {
            FixedMatrix matrix = new();

            for (int index = 0; index < FixedMatrix.Dimension; index += 1)
            {
                matrix[index, index] = X86Math.One;
            }

            return matrix;
        }

        public static FixedMatrix Read(CarMemory memory, int offset)
        {
            FixedMatrix matrix = new();

            for (int row = 0; row < FixedMatrix.Dimension; row += 1)
            {
                for (int column = 0; column < FixedMatrix.Dimension; column += 1)
                {
                    matrix[row, column] = memory[offset + (row * FixedMatrix.Dimension + column) * sizeof(int)];
                }
            }

            return matrix;
        }

        public static void Write(CarMemory memory, int offset, FixedMatrix matrix)
        {
            for (int row = 0; row < FixedMatrix.Dimension; row += 1)
            {
                for (int column = 0; column < FixedMatrix.Dimension; column += 1)
                {
                    memory[offset + (row * FixedMatrix.Dimension + column) * sizeof(int)] = matrix[row, column];
                }
            }
        }

        public static FixedVector Transform(FixedMatrix matrix, FixedVector vector) => new()
        {
            First = DotRow(matrix, 0, vector),
            Second = DotRow(matrix, 1, vector),
            Third = DotRow(matrix, 2, vector)
        };

        public static FixedMatrix Transpose(FixedMatrix source)
        {
            FixedMatrix destination = new();

            for (int row = 0; row < FixedMatrix.Dimension; row += 1)
            {
                for (int column = 0; column < FixedMatrix.Dimension; column += 1)
                {
                    destination[row, column] = source[column, row];
                }
            }

            return destination;
        }

        public static FixedMatrix Multiply(FixedMatrix left, FixedMatrix right)
        {
            FixedMatrix result = new();

            for (int row = 0; row < FixedMatrix.Dimension; row += 1)
            {
                for (int column = 0; column < FixedMatrix.Dimension; column += 1)
                {
                    for (int component = 0; component < FixedMatrix.Dimension; component += 1)
                    {
                        result[row, column] = unchecked(result[row, column] +
                            X86Math.MultiplyQ16(left[row, component], right[component, column]));
                    }
                }
            }

            return result;
        }

        public static FixedMatrix RotateFirst(int angle)
        {
            FixedAnglePair pair = PhysicsAngles.Rotation(angle);
            FixedMatrix matrix = Identity();
            matrix[1, 1] = pair.Cosine;
            matrix[1, 2] = pair.Sine;
            matrix[2, 1] = unchecked(-pair.Sine);
            matrix[2, 2] = pair.Cosine;

            return matrix;
        }

        public static FixedMatrix RotateSecond(int angle)
        {
            FixedAnglePair pair = PhysicsAngles.Rotation(angle);
            FixedMatrix matrix = Identity();
            matrix[0, 0] = pair.Cosine;
            matrix[0, 2] = unchecked(-pair.Sine);
            matrix[2, 0] = pair.Sine;
            matrix[2, 2] = pair.Cosine;

            return matrix;
        }

        public static FixedMatrix RotateThird(int angle)
        {
            FixedAnglePair pair = PhysicsAngles.Rotation(angle);
            FixedMatrix matrix = Identity();
            matrix[0, 0] = pair.Cosine;
            matrix[0, 1] = pair.Sine;
            matrix[1, 0] = unchecked(-pair.Sine);
            matrix[1, 1] = pair.Cosine;

            return matrix;
        }

        public static FixedMatrix Orthonormalise(FixedMatrix matrix)
        {
            for (int iteration = 0; iteration < 4; iteration += 1)
            {
                FixedMatrix error = Multiply(Transpose(matrix), matrix);

                for (int axis = 0; axis < FixedMatrix.Dimension; axis += 1)
                {
                    error[axis, axis] = unchecked(error[axis, axis] - X86Math.One);
                }

                FixedMatrix power = Identity();
                FixedMatrix correction = Identity();

                foreach (int coefficient in PhysicsTables.OrthonormalisationCoefficients[1..])
                {
                    power = Multiply(power, error);
                    AddScaled(correction, power, coefficient);
                }

                matrix = Multiply(matrix, correction);
            }

            return matrix;
        }

        private static int DotRow(FixedMatrix matrix, int row, FixedVector vector) => unchecked(
            X86Math.MultiplyQ16(vector.First, matrix[row, 0]) +
            X86Math.MultiplyQ16(vector.Second, matrix[row, 1]) +
            X86Math.MultiplyQ16(vector.Third, matrix[row, 2]));

        private static void AddScaled(FixedMatrix destination, FixedMatrix source, int coefficient)
        {
            for (int row = 0; row < FixedMatrix.Dimension; row += 1)
            {
                for (int column = 0; column < FixedMatrix.Dimension; column += 1)
                {
                    destination[row, column] = unchecked(destination[row, column] +
                        X86Math.MultiplyQ16(coefficient, source[row, column]));
                }
            }
        }
    }
}