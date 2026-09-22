using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
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

            CarGeometryTriangle[] triangles = NeedForSpeed2CarGeometryDecoder
                .Decode(data)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(triangles, Has.Length.EqualTo(expectedTriangleCount));
                Assert.That(triangles[0].TextureName, Is.EqualTo("CAR1"));
                Assert.That(triangles[0].SectionPositionX, Is.EqualTo(65536));
                Assert.That(triangles[0].First, Is.EqualTo(new CarGeometryVertex(8, 32, 16)));
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

        private static byte[] BuildGeometry(bool isQuad, bool hasInvalidVertexIndex)
        {
            int vertexCount = 4;
            int firstSectionSize = DescriptorSize + vertexCount * VertexSize + PolygonSize;
            byte[] data = new byte[
                HeaderSize + firstSectionSize + (SectionCount - 1) * DescriptorSize];
            int descriptorOffset = HeaderSize;
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