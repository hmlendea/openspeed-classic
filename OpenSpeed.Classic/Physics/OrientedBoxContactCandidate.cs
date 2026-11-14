namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Describes an accepted <c>sub_455100</c> contact candidate.
    /// </summary>
    public sealed class OrientedBoxContactCandidate
    {
        public OrientedBoxContactCandidateRoutine Routine { get; set; }

        public FixedVector Point { get; set; }

        public FixedVector? Normal { get; set; }
    }
}