namespace OpenSpeed.Classic.Physics
{
    public sealed class RawVehicleInput
    {
        public int SteeringWord { get; set; }

        public byte Accelerator { get; set; }

        public byte Brake { get; set; }

        public byte Actions { get; set; }
    }
}