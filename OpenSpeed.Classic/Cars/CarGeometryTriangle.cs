namespace OpenSpeed.Classic.Cars
{
    public sealed class CarGeometryTriangle(
        int sectionPositionX,
        int sectionPositionY,
        int sectionPositionZ,
        string textureName,
        CarGeometryVertex first,
        CarGeometryVertex second,
        CarGeometryVertex third,
        int firstTextureCorner,
        int secondTextureCorner,
        int thirdTextureCorner,
        uint textureMappingFlags = 0,
        int textureRegistrationMode = 3)
    {
        public CarGeometryVertex First { get; } = first;

        public int FirstTextureCorner { get; } = firstTextureCorner;

        public CarGeometryVertex Second { get; } = second;

        public int SecondTextureCorner { get; } = secondTextureCorner;

        public int SectionPositionX { get; } = sectionPositionX;

        public int SectionPositionY { get; } = sectionPositionY;

        public int SectionPositionZ { get; } = sectionPositionZ;

        public CarGeometryVertex Third { get; } = third;

        public int ThirdTextureCorner { get; } = thirdTextureCorner;

        public int TextureRegistrationMode { get; } = textureRegistrationMode;

        public uint TextureMappingFlags { get; } = textureMappingFlags;

        public string TextureName { get; } = textureName;
    }
}