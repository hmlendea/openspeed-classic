using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

using NUnit.Framework;

using OpenSpeed.Classic.Cars;
using OpenSpeed.Classic.Cars.NeedForSpeed2;

namespace OpenSpeed.Classic.UnitTests.Cars.NeedForSpeed2
{
    [TestFixture]
    public sealed class NeedForSpeed2CarGeometryDecoderTests
    {
        private static int DescriptorSize => 0x34;

        private static int HeaderSize => 0x8C;

        private static int PolygonSize => 12;

        private static int SectionCount => 0x20;

        private static int VertexSize => 6;

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void GivenAValidPolygon_WhenDecoding_ThenItsTrianglesAreReturned(
            bool isQuad,
            int expectedTriangleCount)
        {
            byte[] data = BuildGeometry(isQuad, false);

            CarGeometryTriangle[] triangles = [.. NeedForSpeed2CarGeometryDecoder.Decode(data)];

            Assert.Multiple(() =>
            {
                Assert.That(triangles, Has.Length.EqualTo(expectedTriangleCount));
                Assert.That(triangles[0].TextureName, Is.EqualTo("CAR1"));
                Assert.That(triangles[0].SectionPositionX, Is.EqualTo(65536));
                Assert.That(triangles[0].SectionPositionY, Is.EqualTo(0));
                Assert.That(triangles[0].SectionPositionZ, Is.EqualTo(0));
                Assert.That(
                    triangles[0].TextureMappingFlags,
                    Is.EqualTo(isQuad ? 2u : 0u));
                Assert.That(triangles[0].TextureRegistrationMode, Is.EqualTo(3));
                Assert.That(triangles[0].First, Is.EqualTo(new CarGeometryVertex(8, 32, 16)));
                Assert.That(triangles[0].FirstTextureCorner, Is.EqualTo(3));
                Assert.That(triangles[0].SecondTextureCorner, Is.EqualTo(2));
                Assert.That(triangles[0].ThirdTextureCorner, Is.EqualTo(1));

                if (isQuad)
                {
                    Assert.That(triangles[1].FirstTextureCorner, Is.EqualTo(3));
                    Assert.That(triangles[1].SecondTextureCorner, Is.EqualTo(1));
                    Assert.That(triangles[1].ThirdTextureCorner, Is.EqualTo(0));
                }
            });
        }

        [Test]
        public void GivenAnInvalidVertexIndex_WhenDecoding_ThenThePayloadIsRejected()
            => Assert.That(
                () => NeedForSpeed2CarGeometryDecoder.Decode(BuildGeometry(false, true)),
                Throws.TypeOf<InvalidDataException>());

        [Test]
        public void GivenATruncatedPayload_WhenDecoding_ThenThePayloadIsRejected()
            => Assert.That(
                () => NeedForSpeed2CarGeometryDecoder.Decode(new byte[HeaderSize - 1]),
                Throws.TypeOf<InvalidDataException>());

        [Test]
        public void GivenAMirroredQuad_WhenDecoding_ThenItsWindingAndTextureCornersAreReversed()
        {
            byte[] data = BuildGeometry(true, false);
            int polygonOffset = HeaderSize + DescriptorSize + 4 * VertexSize;
            data[polygonOffset] = 0x06;

            CarGeometryTriangle[] triangles =
                [.. NeedForSpeed2CarGeometryDecoder.Decode(data)];

            Assert.Multiple(() =>
            {
                Assert.That(triangles[0].First.X, Is.EqualTo(8));
                Assert.That(triangles[0].Second.X, Is.EqualTo(48));
                Assert.That(triangles[0].Third.X, Is.EqualTo(32));
                Assert.That(triangles[0].FirstTextureCorner, Is.EqualTo(3));
                Assert.That(triangles[0].SecondTextureCorner, Is.EqualTo(0));
                Assert.That(triangles[0].ThirdTextureCorner, Is.EqualTo(1));
                Assert.That(triangles[1].First.X, Is.EqualTo(8));
                Assert.That(triangles[1].Second.X, Is.EqualTo(32));
                Assert.That(triangles[1].Third.X, Is.EqualTo(16));
                Assert.That(triangles[1].FirstTextureCorner, Is.EqualTo(3));
                Assert.That(triangles[1].SecondTextureCorner, Is.EqualTo(1));
                Assert.That(triangles[1].ThirdTextureCorner, Is.EqualTo(2));
            });
        }

        [TestCase(19, 1)]
        [TestCase(20, 0)]
        public void GivenASection_WhenDecoding_ThenOnlyHighDetailGeometryIsReturned(
            int sectionIndex,
            int expectedTriangleCount)
        {
            CarGeometryTriangle[] triangles =
                [.. NeedForSpeed2CarGeometryDecoder.Decode(
                    BuildGeometry(false, false, sectionIndex))];

            Assert.That(triangles, Has.Length.EqualTo(expectedTriangleCount));
        }

        [Test]
        public void GivenSectionTranslations_WhenDecoding_ThenTheirCoordinateOrderIsPreserved()
        {
            byte[] data = BuildGeometry(false, false);
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(HeaderSize + 12), 131072);
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(HeaderSize + 16), 196608);

            CarGeometryTriangle[] triangles =
                [.. NeedForSpeed2CarGeometryDecoder.Decode(data)];
            CarGeometryTriangle triangle = triangles[0];

            Assert.Multiple(() =>
            {
                Assert.That(triangle.SectionPositionY, Is.EqualTo(196608));
                Assert.That(triangle.SectionPositionZ, Is.EqualTo(131072));
            });
        }

        [TestCase(12, CarIdentifier.FordGT90, 5)]
        [TestCase(14, CarIdentifier.FordGT90, -5)]
        [TestCase(16, CarIdentifier.McLarenF1, 5)]
        [TestCase(18, CarIdentifier.McLarenF1, -5)]
        public void GivenAnAdjustedSection_WhenDecoding_ThenItsSelectedVerticesAreCorrected(
            int sectionIndex,
            CarIdentifier carIdentifier,
            short expectedPositionXAdjustment)
        {
            CarGeometryTriangle[] triangles = [.. NeedForSpeed2CarGeometryDecoder.Decode(BuildGeometry(true, false, sectionIndex), carIdentifier)];

            Assert.Multiple(() =>
            {
                Assert.That(triangles, Has.Length.EqualTo(2));
                Assert.That(triangles[0].First.X, Is.EqualTo(8 + expectedPositionXAdjustment));
                Assert.That(triangles[0].Second.X, Is.EqualTo(16 + expectedPositionXAdjustment));
                Assert.That(triangles[0].Third.X, Is.EqualTo(32 + expectedPositionXAdjustment));
                Assert.That(triangles[1].Third.X, Is.EqualTo(48 + expectedPositionXAdjustment));
            });
        }

        private static byte[] BuildGeometry(
            bool isQuad,
            bool hasInvalidVertexIndex,
            int sectionIndex = 0)
        {
            int vertexCount = 4;
            int selectedSectionSize = DescriptorSize + vertexCount * VertexSize + PolygonSize;
            byte[] data = new byte[
                HeaderSize + selectedSectionSize + (SectionCount - 1) * DescriptorSize];
            int descriptorOffset = HeaderSize + sectionIndex * DescriptorSize;
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(descriptorOffset), vertexCount);
            BinaryPrimitives.WriteInt32LittleEndian(
                data.AsSpan(descriptorOffset + sizeof(int)),
                1);
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(descriptorOffset + 8), 65536);
            int vertexOffset = descriptorOffset + DescriptorSize;
            WriteVertex(data, vertexOffset, 8, 16, 32);
            WriteVertex(data, vertexOffset + VertexSize, 16, 32, 48);
            WriteVertex(data, vertexOffset + VertexSize * 2, 32, 48, 64);
            WriteVertex(data, vertexOffset + VertexSize * 3, 48, 64, 96);
            int polygonOffset = vertexOffset + vertexCount * VertexSize;

            if (isQuad)
            {
                data[polygonOffset] = 0x02;
            }

            data[polygonOffset + 4] = 0;
            data[polygonOffset + 5] = 1;
            data[polygonOffset + 6] = hasInvalidVertexIndex ? byte.MaxValue : (byte)2;
            data[polygonOffset + 7] = 3;
            Encoding.ASCII.GetBytes("CAR1").CopyTo(data, polygonOffset + 8);

            return data;
        }

        private static void WriteVertex(
            byte[] data,
            int offset,
            short positionX,
            short positionZ,
            short positionY)
        {
            BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset), positionX);
            BinaryPrimitives.WriteInt16LittleEndian(
                data.AsSpan(offset + sizeof(short)),
                positionZ);
            BinaryPrimitives.WriteInt16LittleEndian(
                data.AsSpan(offset + sizeof(short) * 2),
                positionY);
        }
    }
}