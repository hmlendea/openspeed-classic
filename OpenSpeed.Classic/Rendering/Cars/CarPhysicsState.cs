namespace OpenSpeed.Classic.Rendering.Cars
{
    public sealed class CarPhysicsState
    {
        public bool IsGrounded { get; set; }

        public float LateralVelocity { get; set; }

        public float LongitudinalVelocity { get; set; }

        public float VerticalVelocity { get; set; }
    }
}