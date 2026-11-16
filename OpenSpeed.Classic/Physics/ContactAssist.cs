using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Outcome of one <c>sub_464980</c> invocation, mirroring the routine's terminating paths.
    /// </summary>
    public enum ContactAssistResult
    {
        /// <summary>Every gate rejected the assist; no car state changed.</summary>
        None,

        /// <summary>The upward angular impulse was written to <c>+0xE8..+0xF0</c>.</summary>
        UpwardImpulseApplied,

        /// <summary>The mode-bit-<c>0x04</c> sampled threshold assigned <c>+0x2F4</c>.</summary>
        ResponseStateAssigned,

        /// <summary>The sampling table was absent; the original asserted and continued.</summary>
        MissingTable
    }

    /// <summary>
    /// Translates <c>sub_464980</c>, the optional contact assist invoked by the main solver when
    /// <c>dword_512230</c> is non-null. Two blocks run in source order:
    /// <list type="bullet">
    /// <item><c>loc_4649D6</c>, the upward angular impulse, entered only when
    /// <c>+0x15C &lt; 0x10000</c>, word <c>+0x14C != 0</c>, <c>+0xAC &lt; 0</c>,
    /// <c>+0x198 &gt; 0x8000</c>, and the primary-row-1/normal dot product is positive. It
    /// projects the row and the normal into the secondary basis, crosses them, and rotates the
    /// result back into <c>+0xE8..+0xF0</c>. Control then continues to the mode gate, it does not
    /// return.</item>
    /// <item><c>loc_464D72</c>, the mode-bit-<c>0x04</c> block, entered from the mode gate when
    /// role bit <c>0x04</c> is set, the sampling table is present, and word <c>+0x14C == 0</c>.
    /// It derives a sampled threshold and assigns the response state <c>+0x2F4</c>.</item>
    /// </list>
    /// The original raises an assertion and continues when the sampling table is missing; the
    /// managed caller supplies the table and the diagnostic hook.
    /// </summary>
    public static class ContactAssist
    {
        private static int CorrectionOffset => 0x15C;

        private static int CollisionCountOffset => 0x14C;

        private static int VerticalVelocityOffset => 0xAC;

        private static int SecondaryMiddleOffset => 0x198;

        private static int RoleFlagsOffset => 0x1F4;

        private static int BasisRow1Offset => 0xD0;

        private static int NormalOffset => 0x124;

        private static int SecondaryBasisOffset => 0x188;

        private static int AngularOffset => 0xE8;

        private static int VelocityProjectedOffset => 0x2B8;

        private static int ResponseStateOffset => 0x2F4;

        private static int ContactForceOffset => 0x30C;

        private static int RouteIndexOffset => 0x14;

        private static int ProjectionThreshold => 0xD999;

        private static int NegativeForceThreshold => unchecked((int)0xFFFFFAE2);

        private static int PositiveForceThreshold => 0x51E;

        /// <summary>
        /// Injected data. <see cref="SampleTable"/> is <c>dword_4D5794</c> as a byte span indexed
        /// by twice the halved route index. <see cref="ReportMissingTable"/> receives the original
        /// assertion's diagnostic when the table is empty.
        /// </summary>
        public readonly record struct Dependencies(
            ReadOnlyMemory<byte> SampleTable,
            Action? ReportMissingTable = null);

        public static ContactAssistResult Update(
            CarMemory car,
            CarSpecifications descriptor,
            Dependencies dependencies)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(descriptor);

            if (car[CorrectionOffset] < X86Math.One &&
                car.ReadWord(CollisionCountOffset) != 0 &&
                car[VerticalVelocityOffset] < 0 &&
                car[SecondaryMiddleOffset] > 0x8000)
            {
                ApplyUpwardImpulse(car);
            }

            if ((car.ReadByte(RoleFlagsOffset) & 0x04) == 0)
            {
                return ContactAssistResult.None;
            }

            if (car.ReadWord(CollisionCountOffset) != 0)
            {
                return ContactAssistResult.None;
            }

            if (dependencies.SampleTable.IsEmpty)
            {
                dependencies.ReportMissingTable?.Invoke();

                return ContactAssistResult.MissingTable;
            }

            return EvaluateModeBlock(car, descriptor, dependencies.SampleTable.Span);
        }

        private static void ApplyUpwardImpulse(CarMemory car)
        {
            int dot = FixedVectors.Dot(
                FixedVectors.Read(car, BasisRow1Offset), FixedVectors.Read(car, NormalOffset));

            if (dot <= 0)
            {
                return;
            }

            FixedMatrix secondary = FixedMatrices.Read(car, SecondaryBasisOffset);
            FixedVector localRow = FixedMatrices.Transform(secondary, FixedVectors.Read(car, BasisRow1Offset));
            FixedVector localNormal = FixedMatrices.Transform(secondary, FixedVectors.Read(car, NormalOffset));
            FixedVector cross = FixedVectors.Cross(localRow, localNormal);
            FixedVectors.Write(car, AngularOffset, FixedMatrices.Transform(FixedMatrices.Transpose(secondary), cross));
        }

        /// <summary>
        /// <c>loc_464D72..loc_464F2C</c>. The threshold is
        /// <c>((table[index * 2] &lt;&lt; 16) * 0xE666) &gt;&gt; 16</c> multiplied by
        /// <c>descriptor[0xF4] &gt;&gt; 16</c> and divided by <c>0x68</c> with x86 signed
        /// truncation. The projection and force gates then assign <c>+0x2F4</c>.
        /// </summary>
        private static ContactAssistResult EvaluateModeBlock(
            CarMemory car,
            CarSpecifications descriptor,
            ReadOnlySpan<byte> sampleTable)
        {
            int sampleIndex = X86Math.TruncatePowerOfTwo(car[RouteIndexOffset], 1);
            int sample = sampleTable[unchecked(sampleIndex * 2)] & 0xFF;
            int scaledSample = X86Math.MultiplyQ16(unchecked(sample << 16), 0xE666);
            int reference = X86Math.TruncatePowerOfTwo(descriptor[0xF4], 16);
            int threshold = X86Math.Divide(unchecked(scaledSample * reference), 0x68);
            int projection = FixedVectors.Dot(
                FixedVectors.Read(car, BasisRow1Offset), FixedVectors.Read(car, NormalOffset));

            if (projection > ProjectionThreshold && X86Math.Abs(car[ResponseStateOffset]) == 3)
            {
                car[ResponseStateOffset] = 0;

                return ContactAssistResult.ResponseStateAssigned;
            }

            if (threshold > car[VelocityProjectedOffset])
            {
                return ContactAssistResult.None;
            }

            int force = car[ContactForceOffset];

            if (force <= NegativeForceThreshold)
            {
                car[ResponseStateOffset] = -3;

                return ContactAssistResult.ResponseStateAssigned;
            }

            if (force >= PositiveForceThreshold)
            {
                car[ResponseStateOffset] = 3;

                return ContactAssistResult.ResponseStateAssigned;
            }

            return ContactAssistResult.None;
        }
    }
}