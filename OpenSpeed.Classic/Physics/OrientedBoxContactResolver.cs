using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Translates the iterative contact-geometry loop of <c>sub_4543F0</c>. The routine
    /// precomputes the process-wide scratch matrices once, then repeatedly refreshes the centre
    /// deltas, runs the ordered candidate search of <c>sub_455100</c>, applies the position
    /// corrections selected by the gates at <c>loc_454BFA..loc_454FB5</c>, and stops at the
    /// first failed search or after 32 corrections. The corrections are calculated at the first
    /// acceptance and reused afterwards. Impulse application (<c>sub_455B60</c>) and the motion
    /// refresh (<c>sub_46A130</c>) belong to the caller <c>sub_453F30</c>.
    /// </summary>
    public static class OrientedBoxContactResolver
    {
        /// <summary>Correction iteration cap, the source's <c>cmp eax, 20h</c>.</summary>
        public const int MaximumIterations = 0x20;

        /// <summary>Relative-velocity component-maximum gate, <c>dword_4D62B0</c>.</summary>
        public const int RelativeVelocityThreshold = 0x140000;

        /// <summary>Centre-delta component-maximum gate, <c>dword_4D62B4</c>.</summary>
        public const int PlaneMagnitudeThreshold = 0x3333;

        /// <summary>Primary normalised-dot gate, <c>dword_4D62BC</c>.</summary>
        public const int NormalisedDotThreshold = 0xD999;

        /// <summary>Secondary normalised-dot gate, <c>dword_4D62C0</c>.</summary>
        public const int SecondaryDotThreshold = 0xC000;

        /// <summary>Secondary planar gate, <c>dword_4D62B8</c>.</summary>
        public const int SecondaryPlaneThreshold = 0x9999;

        /// <summary>
        /// The four process-global switches tested by the constructor selection. Each is a
        /// nonzero test rather than a magnitude, and all four executable defaults are zero.
        /// <c>dword_4D6280</c> suppresses the small-delta reciprocal branch, <c>dword_4D6284</c>
        /// suppresses the normalised-dot reciprocal branch, <c>dword_4D6288</c> enables the
        /// secondary dot/planar branch, and <c>dword_4D628C</c> forces the reciprocal constructor
        /// before the mass-split form.
        /// </summary>
        public readonly record struct ConstructorFlags(
            int SuppressSmallDeltaReciprocal = 0,
            int SuppressDotReciprocal = 0,
            int EnableSecondaryGate = 0,
            int ForceReciprocal = 0);

        /// <summary>
        /// One <c>sub_4543F0</c> invocation. <see cref="Iterations"/> is the source's
        /// <c>eax</c>: zero when no candidate was ever accepted. <see cref="Point"/> and
        /// <see cref="Normal"/> mirror the caller-owned buffers also handed to
        /// <c>sub_455B60</c>. <see cref="HasNormal"/> is false when the accepted candidate came
        /// from a corner-only helper, in which case <see cref="Normal"/> retains the value
        /// supplied by the caller.
        /// </summary>
        public readonly record struct Result(
            int Iterations,
            FixedVector Point,
            FixedVector Normal,
            bool HasNormal);

        /// <summary>
        /// Executes the correction loop. The two cars are supplied in the source's <c>eax</c> and
        /// <c>edx</c> roles: the first is the tested box in every probe, the second supplies the
        /// emitted corner.
        /// </summary>
        public static Result Resolve(
            CarMemory firstCar,
            CarMemory secondCar,
            OrientedBoxScratch scratch,
            ConstructorFlags flags = default,
            FixedVector? initialNormal = null)
        {
            ArgumentNullException.ThrowIfNull(firstCar);
            ArgumentNullException.ThrowIfNull(secondCar);
            ArgumentNullException.ThrowIfNull(scratch);
            OrientedBoxScratchCalculator.Calculate(firstCar, secondCar, scratch);
            int iteration = 0;
            bool hasCorrections = false;
            bool hasNormal = false;
            FixedVector firstCorrection = new();
            FixedVector secondCorrection = new();
            FixedVector point = new();
            FixedVector normal = initialNormal ?? new FixedVector();

            while (true)
            {
                OrientedBoxScratchCalculator.UpdateDeltas(firstCar, secondCar, scratch);

                if (!OrientedBoxContactSearch.TrySearch(firstCar, secondCar, scratch, out OrientedBoxContactCandidate? candidate))
                {
                    return new Result(iteration, point, normal, hasNormal);
                }

                point = candidate.Point;

                if (candidate.Normal is not null)
                {
                    normal = candidate.Normal;
                    hasNormal = true;
                }

                if (!hasCorrections)
                {
                    CalculateCorrections(firstCar, secondCar, flags, out firstCorrection, out secondCorrection);
                    hasCorrections = true;
                }

                AddPosition(firstCar, firstCorrection);
                AddPosition(secondCar, secondCorrection);
                iteration = unchecked(iteration + 1);

                if (iteration >= MaximumIterations)
                {
                    return new Result(iteration, point, normal, hasNormal);
                }
            }
        }

        private static void AddPosition(CarMemory car, FixedVector correction)
        {
            for (int component = 0; component < FixedMatrix.Dimension; component += 1)
            {
                int offset = 0x9C + component * sizeof(int);
                car[offset] = unchecked(car[offset] + GetComponent(correction, component));
            }
        }

        private static int GetComponent(FixedVector vector, int index) => index switch
        {
            0 => vector.First,
            1 => vector.Second,
            _ => vector.Third
        };

        private static void SetComponent(FixedVector vector, int index, int value)
        {
            switch (index)
            {
                case 0:
                    vector.First = value;

                    break;
                case 1:
                    vector.Second = value;

                    break;
                default:
                    vector.Third = value;

                    break;
            }
        }

        /// <summary>
        /// Reproduces the constructor selection at <c>loc_454BFA..loc_454FB5</c>. Every
        /// comparison uses the wrapped <c>neg</c>-derived absolute value, so <c>INT_MIN</c>
        /// remains negative. A relative-velocity maximum at or below <c>0x140000</c> selects the
        /// normalised-delta mass-split constructor unconditionally.
        /// </summary>
        private static void CalculateCorrections(
            CarMemory firstCar,
            CarMemory secondCar,
            ConstructorFlags flags,
            out FixedVector firstCorrection,
            out FixedVector secondCorrection)
        {
            FixedVector firstVelocity = FixedVectors.Read(firstCar, 0xA8);
            FixedVector secondVelocity = FixedVectors.Read(secondCar, 0xA8);
            FixedVector relativeVelocity = FixedVectors.Subtract(firstVelocity, secondVelocity);
            FixedVector delta = FixedVectors.Subtract(
                FixedVectors.Read(secondCar, 0x9C), FixedVectors.Read(firstCar, 0x9C));

            if (MaximumAbsoluteComponent(relativeVelocity) <= RelativeVelocityThreshold)
            {
                BuildMassSplitCorrections(firstCar, secondCar, delta, out firstCorrection, out secondCorrection);

                return;
            }

            int deltaMaximum = MaximumAbsoluteComponent(delta);

            if (deltaMaximum < PlaneMagnitudeThreshold && flags.SuppressSmallDeltaReciprocal == 0)
            {
                BuildReciprocalCorrections(firstVelocity, secondVelocity, relativeVelocity, out firstCorrection, out secondCorrection);

                return;
            }

            FixedVector normalisedVelocity = relativeVelocity;
            FixedVectors.Normalise(normalisedVelocity);
            FixedVector normalisedDelta = delta;
            FixedVectors.NormaliseBasisVector(normalisedDelta);
            int dot = FixedVectors.Dot(normalisedVelocity, normalisedDelta);

            if (dot > NormalisedDotThreshold && flags.SuppressDotReciprocal == 0)
            {
                BuildReciprocalCorrections(firstVelocity, secondVelocity, relativeVelocity, out firstCorrection, out secondCorrection);

                return;
            }

            if (flags.EnableSecondaryGate != 0 &&
                dot > SecondaryDotThreshold &&
                deltaMaximum < SecondaryPlaneThreshold)
            {
                BuildReciprocalCorrections(firstVelocity, secondVelocity, relativeVelocity, out firstCorrection, out secondCorrection);

                return;
            }

            if (flags.ForceReciprocal != 0)
            {
                BuildReciprocalCorrections(firstVelocity, secondVelocity, relativeVelocity, out firstCorrection, out secondCorrection);

                return;
            }

            BuildMassSplitCorrections(firstCar, secondCar, delta, out firstCorrection, out secondCorrection);
        }

        private static int MaximumAbsoluteComponent(FixedVector vector)
            => Math.Max(
                Math.Max(X86Math.Abs(vector.First), X86Math.Abs(vector.Second)),
                X86Math.Abs(vector.Third));

        /// <summary>
        /// Relative-velocity constructor (<c>loc_454D13..loc_454D2F</c>). One reciprocal-derived
        /// factor, truncated toward zero after negation, scales both cars' velocity components:
        /// the source reuses the same register for the first and second correction.
        /// </summary>
        private static void BuildReciprocalCorrections(
            FixedVector firstVelocity,
            FixedVector secondVelocity,
            FixedVector relativeVelocity,
            out FixedVector firstCorrection,
            out FixedVector secondCorrection)
        {
            int halfMagnitude = X86Math.TruncatePowerOfTwo(FixedVectors.Length(relativeVelocity), 1);
            int reciprocal = halfMagnitude == 0 ? X86Math.One : X86Math.DivideQ16(0x8000, halfMagnitude);
            int factor = X86Math.TruncatePowerOfTwo(unchecked(-reciprocal), 8);
            firstCorrection = ScaleTruncated(firstVelocity, factor);
            secondCorrection = ScaleTruncated(secondVelocity, factor);
        }

        private static FixedVector ScaleTruncated(FixedVector vector, int factor)
        {
            FixedVector result = new();

            for (int component = 0; component < FixedMatrix.Dimension; component += 1)
            {
                int product = unchecked(GetComponent(vector, component) * factor);
                SetComponent(result, component, X86Math.TruncatePowerOfTwo(product, 8) >> 5);
            }

            return result;
        }

        /// <summary>
        /// Normalised-delta constructor (<c>loc_454FB5</c>): normalise the centre delta with
        /// <c>sub_45E760</c>, split the correction by the source-order Q16 mass fractions, then
        /// truncate by 14 and arithmetic-shift by 5. The first car's correction is negated before
        /// its shift and uses the second mass fraction.
        /// </summary>
        private static void BuildMassSplitCorrections(
            CarMemory firstCar,
            CarMemory secondCar,
            FixedVector delta,
            out FixedVector firstCorrection,
            out FixedVector secondCorrection)
        {
            FixedVector direction = new()
            {
                First = delta.First,
                Second = delta.Second,
                Third = delta.Third
            };
            FixedVectors.NormaliseBasisVector(direction);
            int total = unchecked(firstCar[0xB4] + secondCar[0xB4]);
            int firstFraction = X86Math.DivideQ16(firstCar[0xB4], total);
            int secondFraction = X86Math.DivideQ16(secondCar[0xB4], total);
            int firstFactor = X86Math.TruncatePowerOfTwo(secondFraction, 2);
            int secondFactor = X86Math.TruncatePowerOfTwo(firstFraction, 2);
            firstCorrection = new FixedVector();
            secondCorrection = new FixedVector();

            for (int component = 0; component < FixedMatrix.Dimension; component += 1)
            {
                int value = GetComponent(direction, component);
                int firstProduct = unchecked(value * firstFactor);
                int secondProduct = unchecked(value * secondFactor);
                SetComponent(firstCorrection, component,
                    unchecked(-X86Math.TruncatePowerOfTwo(firstProduct, 14)) >> 5);
                SetComponent(secondCorrection, component,
                    X86Math.TruncatePowerOfTwo(secondProduct, 14) >> 5);
            }
        }
    }
}