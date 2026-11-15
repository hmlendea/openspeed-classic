using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// The external inputs the alternate contact pipeline reads from race-generated globals and
    /// the unresolved route-geometry path. Each member is a required input rather than a
    /// substitute default; a caller that cannot supply one leaves the corresponding pass out of
    /// the frame instead of fabricating a value.
    /// </summary>
    public sealed class AlternateContactDependencies
    {
        /// <summary>
        /// The <c>sub_43D830</c> track-mode value. Its first branch returns the race-selected
        /// value from <c>dword_50AD24[raceType]</c>; the second returns one for a special track.
        /// The value one selects the first <c>sub_466200</c> branch.
        /// </summary>
        public int TrackMode { get; set; }

        /// <summary>
        /// The bit normally held in <c>dword_512228</c>, used by the route-bound lateral target
        /// to mirror the steering and the route walk.
        /// </summary>
        public int RaceDirection { get; set; }

        /// <summary>
        /// The runtime-state selector value read from the car's state record at <c>+0x00</c>.
        /// The demand/capacity guard only runs its body for the contact-alive value <c>0x0E</c>.
        /// </summary>
        public int RuntimeState { get; set; }

        /// <summary>
        /// Replaces the unresolved <c>sub_466280</c> route-geometry path reached from
        /// <c>sub_465F60</c>. When null, the route-bound pass is skipped rather than entering an
        /// unfinished path.
        /// </summary>
        public Action<CarMemory>? WideDeviationQuery { get; set; }
    }
}
