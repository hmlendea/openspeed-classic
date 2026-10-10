using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

using NUnit.Framework;

using OpenSpeed.Classic.Cars.NeedForSpeed2;

namespace OpenSpeed.Classic.UnitTests.Cars.NeedForSpeed2
{
    [TestFixture]
    public sealed class NeedForSpeed2CarArchiveReaderTests
    {
        private static int HeaderSize => 16;

        [Test]
        public void GivenAnArchiveMember_WhenReadingWithDifferentCase_ThenItsPayloadIsReturned()
        {
            byte[] expectedPayload = [8, 16, 32, 64];
            byte[] archive = BuildArchive("MCF1.geo", expectedPayload);

            byte[] payload = NeedForSpeed2CarArchiveReader.Read(archive, "mcf1.GEO");

            Assert.That(payload, Is.EqualTo(expectedPayload));
        }

        [Test]
        public void GivenAnAbsentArchiveMember_WhenReading_ThenTheArchiveIsRejected()
            => Assert.That(
                () => NeedForSpeed2CarArchiveReader.Read(
                    BuildArchive("MCF1.geo", [8]),
                    "FF50.geo"),
                Throws.TypeOf<InvalidDataException>());

        [Test]
        public void GivenAnInvalidArchiveHeader_WhenReading_ThenTheArchiveIsRejected()
            => Assert.That(
                () => NeedForSpeed2CarArchiveReader.Read(new byte[HeaderSize], "MCF1.geo"),
                Throws.TypeOf<InvalidDataException>());

        private static byte[] BuildArchive(string memberName, byte[] payload)
        {
            byte[] nameData = Encoding.ASCII.GetBytes(memberName);
            int tableEnd = HeaderSize + sizeof(int) * 2 + nameData.Length + 1;
            byte[] archive = new byte[tableEnd + payload.Length];
            Encoding.ASCII.GetBytes("BIGF").CopyTo(archive, 0);
            BinaryPrimitives.WriteInt32BigEndian(archive.AsSpan(4), archive.Length);
            BinaryPrimitives.WriteInt32BigEndian(archive.AsSpan(8), 1);
            BinaryPrimitives.WriteInt32BigEndian(archive.AsSpan(12), tableEnd);
            BinaryPrimitives.WriteInt32BigEndian(archive.AsSpan(HeaderSize), tableEnd);
            BinaryPrimitives.WriteInt32BigEndian(
                archive.AsSpan(HeaderSize + sizeof(int)),
                payload.Length);
            nameData.CopyTo(archive, HeaderSize + sizeof(int) * 2);
            payload.CopyTo(archive, tableEnd);

            return archive;
        }
    }
}